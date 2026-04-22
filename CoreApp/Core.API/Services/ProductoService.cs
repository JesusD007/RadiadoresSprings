using Core.API.DTOs.Requests;   // TransaccionOfflineItem (movido desde CoreCommands)
using Core.API.DTOs.Responses;
using Core.Data;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using SharedContracts.Events;

namespace Core.API.Services;

public class ProductoService(
    CoreDbContext db,
    IMessageSession bus,
    ILogger<ProductoService> logger) : IProductoService
{
    public async Task<PagedResult<ProductoResponse>> GetPagedAsync(int pagina, int tamano, string? busqueda, int? categoriaId)
    {
        var query = db.Productos
            .Include(p => p.Categoria)
            .Where(p => p.EsActivo);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || p.Codigo.Contains(busqueda));

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(p => MapToResponse(p))
            .ToListAsync();

        return new PagedResult<ProductoResponse>(items, total, pagina, tamano);
    }

    public async Task<ProductoResponse?> GetByIdAsync(int id)
    {
        var p = await db.Productos.Include(x => x.Categoria).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : MapToResponse(p);
    }

    public async Task<ProductoResponse?> GetByCodigoAsync(string codigo)
    {
        var p = await db.Productos.Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Codigo == codigo && x.EsActivo);
        return p is null ? null : MapToResponse(p);
    }

    public async Task<ProductoResponse> CrearAsync(CrearProductoRequest req)
    {
        if (await db.Productos.AnyAsync(p => p.Codigo == req.Codigo))
            throw new InvalidOperationException($"Ya existe un producto con código '{req.Codigo}'.");

        var producto = new Producto
        {
            Codigo = req.Codigo,
            Nombre = req.Nombre,
            Descripcion = req.Descripcion,
            Precio = req.Precio,
            PrecioOferta = req.PrecioOferta,
            Stock = req.Stock,
            StockMinimo = req.StockMinimo,
            CategoriaId = req.CategoriaId
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        logger.LogInformation("✅ Producto creado: {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);

        // Stock anterior = 0 (producto nuevo)
        await PublicarInventarioActualizadoAsync(producto, stockAnterior: 0, motivo: "Compra");

        return MapToResponse(await db.Productos.Include(x => x.Categoria).FirstAsync(x => x.Id == producto.Id));
    }

    public async Task<ProductoResponse?> ActualizarAsync(int id, ActualizarProductoRequest req)
    {
        var producto = await db.Productos.Include(x => x.Categoria).FirstOrDefaultAsync(x => x.Id == id);
        if (producto is null) return null;

        // Capturar stock antes de la actualización (ActualizarAsync no cambia el stock)
        var stockAnterior = producto.Stock;

        producto.Nombre = req.Nombre;
        producto.Descripcion = req.Descripcion;
        producto.Precio = req.Precio;
        producto.PrecioOferta = req.PrecioOferta;
        producto.StockMinimo = req.StockMinimo;
        producto.CategoriaId = req.CategoriaId;
        producto.EsActivo = req.EsActivo;
        producto.FechaModificacion = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // El stock no cambió, pero notificamos de todos modos en caso de cambio de precio/estado
        await PublicarInventarioActualizadoAsync(producto, stockAnterior, motivo: "Ajuste");
        return MapToResponse(producto);
    }

    public async Task<bool> AjustarStockAsync(int id, int cantidad, string motivo)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return false;

        var stockAnterior = producto.Stock;

        if (cantidad < 0 && !producto.ReducirStock(Math.Abs(cantidad)))
            throw new InvalidOperationException($"Stock insuficiente. Disponible: {producto.Stock}");

        if (cantidad > 0)
            producto.AumentarStock(cantidad);

        await db.SaveChangesAsync();

        logger.LogInformation("📦 Stock ajustado: {Codigo} {Delta:+#;-#} | Motivo: {Motivo}", producto.Codigo, cantidad, motivo);
        await PublicarInventarioActualizadoAsync(producto, stockAnterior, motivo);
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return false;
        producto.EsActivo = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProductoResponse>> GetStockBajoAsync()
    {
        return await db.Productos
            .Include(p => p.Categoria)
            .Where(p => p.EsActivo && p.Stock <= p.StockMinimo)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<int> ProcesarTransaccionOfflineAsync(TransaccionOfflineItem tx)
    {
        // Invocado desde VentaService; solo reduce stock (sin publicar eventos aquí
        // para evitar doble publicación — VentaService publica por cada línea)
        foreach (var linea in tx.Lineas)
        {
            var producto = await db.Productos.FindAsync(linea.ProductoId)
                ?? throw new InvalidOperationException($"Producto {linea.ProductoId} no encontrado.");
            if (!producto.ReducirStock(linea.Cantidad))
                throw new InvalidOperationException($"Stock insuficiente para producto {linea.ProductoId}.");
        }
        await db.SaveChangesAsync();
        return 0; // ventaId lo genera VentaService
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Publicar InventarioActualizadoEvent con el contrato de SharedContracts
    // ─────────────────────────────────────────────────────────────────────────
    internal async Task PublicarInventarioActualizadoAsync(Producto p, int stockAnterior, string motivo)
    {
        try
        {
            await bus.Publish(new InventarioActualizadoEvent
            {
                ProductoId  = p.Id,
                StockNuevo  = p.Stock,
                StockAnterior = stockAnterior,
                Motivo      = motivo,
                Timestamp   = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ No se pudo publicar InventarioActualizadoEvent para producto {Id}", p.Id);
        }
    }

    private static ProductoResponse MapToResponse(Producto p) => new(
        p.Id, p.Codigo, p.Nombre, p.Descripcion,
        p.Precio, p.PrecioOferta, p.PrecioVigente(),
        p.Stock, p.StockMinimo, p.TieneStockBajo(),
        p.CategoriaId, p.Categoria?.Nombre ?? "-", p.EsActivo, p.FechaCreacion);
}
