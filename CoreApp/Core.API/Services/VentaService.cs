using Core.API.DTOs.Requests;   // TransaccionOfflineItem, CrearVentaRequest
using Core.API.DTOs.Responses;
using Core.Data;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using SharedContracts.Events;

namespace Core.API.Services;

public class VentaService(
    CoreDbContext db,
    IMessageSession bus,
    ILogger<VentaService> logger) : IVentaService
{
    public async Task<PagedResult<VentaResponse>> GetPagedAsync(int pagina, int tamano, DateTime? desde, DateTime? hasta)
    {
        var query = db.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.Lineas).ThenInclude(l => l.Producto)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(v => v.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(v => v.Fecha <= hasta.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.Fecha)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(v => MapToResponse(v))
            .ToListAsync();

        return new PagedResult<VentaResponse>(items, total, pagina, tamano);
    }

    public async Task<VentaResponse?> GetByIdAsync(int id)
    {
        var v = await db.Ventas
            .Include(x => x.Cliente).Include(x => x.Usuario)
            .Include(x => x.Lineas).ThenInclude(l => l.Producto)
            .FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? null : MapToResponse(v);
    }

    public async Task<VentaResponse?> GetByFacturaAsync(string numero)
    {
        var v = await db.Ventas
            .Include(x => x.Cliente).Include(x => x.Usuario)
            .Include(x => x.Lineas).ThenInclude(l => l.Producto)
            .FirstOrDefaultAsync(x => x.NumeroFactura == numero);
        return v is null ? null : MapToResponse(v);
    }

    public async Task<VentaResponse> CrearAsync(CrearVentaRequest req, int usuarioId)
    {
        // Idempotencia: verificar si ya existe la transacción local
        if (!string.IsNullOrEmpty(req.IdTransaccionLocal))
        {
            var existente = await db.Ventas
                .Include(v => v.Cliente).Include(v => v.Usuario)
                .Include(v => v.Lineas).ThenInclude(l => l.Producto)
                .FirstOrDefaultAsync(v => v.IdTransaccionLocal == req.IdTransaccionLocal);

            if (existente is not null)
            {
                logger.LogWarning("🔁 Venta ya procesada (idempotencia): {Id}", req.IdTransaccionLocal);

                // Publicar VentaAplicadaEnCoreEvent de todas formas para que la
                // Saga de Integration pueda confirmar (en caso de retry)
                if (Guid.TryParse(req.IdTransaccionLocal, out var idGuid))
                    await PublicarVentaAplicadaAsync(idGuid, existente);

                return MapToResponse(existente);
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var lineas = new List<LineaVenta>();
            var stocksAnteriores = new Dictionary<int, int>(); // productoId → stock previo
            decimal subtotal = 0;

            foreach (var item in req.Lineas)
            {
                var producto = await db.Productos.FindAsync(item.ProductoId)
                    ?? throw new InvalidOperationException($"Producto {item.ProductoId} no encontrado.");

                stocksAnteriores[producto.Id] = producto.Stock;

                if (!producto.ReducirStock(item.Cantidad))
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}");

                var precio = item.PrecioOverride ?? producto.PrecioVigente();
                var linea = new LineaVenta
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precio
                };
                lineas.Add(linea);
                subtotal += linea.Subtotal;
            }

            var iva = subtotal * 0.16m;
            var total = subtotal + iva - req.Descuento;
            var numero = await GenerarNumeroFacturaAsync();

            var metodo = Enum.TryParse<MetodoPago>(req.MetodoPago, true, out var mp) ? mp : MetodoPago.Efectivo;

            var venta = new Venta
            {
                NumeroFactura = numero,
                SucursalId = req.SucursalId,
                CajaId = req.CajaId,
                SesionCajaId = req.SesionCajaId,
                ClienteId = req.ClienteId,
                UsuarioId = usuarioId,
                Subtotal = subtotal,
                IVA = iva,
                Total = total,
                Descuento = req.Descuento,
                MetodoPago = metodo,
                IdTransaccionLocal = req.IdTransaccionLocal,
                Observaciones = req.Observaciones
            };

            db.Ventas.Add(venta);
            await db.SaveChangesAsync();

            foreach (var l in lineas) l.VentaId = venta.Id;
            db.LineasVenta.AddRange(lineas);
            await db.SaveChangesAsync();

            await tx.CommitAsync();

            logger.LogInformation("🧾 Venta creada: {Factura} | Total: {Total:C}", numero, total);

            // ── Publicar InventarioActualizadoEvent por cada producto vendido ──
            foreach (var linea in lineas)
            {
                var producto = await db.Productos.FindAsync(linea.ProductoId);
                if (producto is null) continue;

                await PublicarInventarioAsync(producto,
                    stockAnterior: stocksAnteriores[linea.ProductoId],
                    motivo: "Venta");
            }

            var ventaCompleta = await GetByIdAsync(venta.Id)
                ?? throw new InvalidOperationException("Error recuperando venta creada.");

            // ── Si fue una venta offline sincronizada, confirmar al saga ──────
            if (Guid.TryParse(req.IdTransaccionLocal, out var txGuid))
                await PublicarVentaAplicadaAsync(txGuid, venta);

            return ventaCompleta;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CancelarAsync(int id, string motivo)
    {
        var venta = await db.Ventas.Include(v => v.Lineas).FirstOrDefaultAsync(v => v.Id == id);
        if (venta is null || venta.Estado == EstadoVenta.Cancelada) return false;

        // Devolver stock y publicar eventos
        foreach (var linea in venta.Lineas)
        {
            var producto = await db.Productos.FindAsync(linea.ProductoId);
            if (producto is null) continue;

            var stockAnterior = producto.Stock;
            producto.AumentarStock(linea.Cantidad);
            await PublicarInventarioAsync(producto, stockAnterior, motivo: "Devolucion");
        }

        venta.Estado = EstadoVenta.Cancelada;
        venta.Observaciones = $"CANCELADA: {motivo}";
        await db.SaveChangesAsync();
        logger.LogInformation("❌ Venta cancelada: {Id} | Motivo: {Motivo}", id, motivo);
        return true;
    }

    public async Task<int> ProcesarTransaccionOfflineAsync(TransaccionOfflineItem tx)
    {
        var metodo = Enum.TryParse<MetodoPago>(tx.MetodoPago, true, out var mp) ? mp : MetodoPago.Efectivo;

        var lineas = new List<LineaVenta>();
        var stocksAnteriores = new Dictionary<int, int>();

        foreach (var item in tx.Lineas)
        {
            var producto = await db.Productos.FindAsync(item.ProductoId)
                ?? throw new InvalidOperationException($"Producto {item.ProductoId} no encontrado.");

            stocksAnteriores[producto.Id] = producto.Stock;
            producto.ReducirStock(item.Cantidad);

            lineas.Add(new LineaVenta
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario
            });
        }

        var subtotal = lineas.Sum(l => l.Subtotal);
        var iva = subtotal * 0.16m;
        var numero = await GenerarNumeroFacturaAsync();

        var venta = new Venta
        {
            NumeroFactura = numero,
            SucursalId = 1,
            CajaId = 1,
            SesionCajaId = 1,
            ClienteId = tx.ClienteId,
            UsuarioId = 1,
            Subtotal = subtotal,
            IVA = iva,
            Total = subtotal + iva,
            MetodoPago = metodo,
            EsOffline = true,
            IdTransaccionLocal = tx.IdTransaccionLocal,
            Fecha = tx.FechaOffline
        };

        db.Ventas.Add(venta);
        await db.SaveChangesAsync();

        foreach (var l in lineas) l.VentaId = venta.Id;
        db.LineasVenta.AddRange(lineas);
        await db.SaveChangesAsync();

        // Publicar eventos de inventario por cada producto del lote offline
        foreach (var linea in lineas)
        {
            var producto = await db.Productos.FindAsync(linea.ProductoId);
            if (producto is null) continue;
            await PublicarInventarioAsync(producto, stocksAnteriores[linea.ProductoId], "Venta");
        }

        return venta.Id;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers de eventos NServiceBus
    // ─────────────────────────────────────────────────────────────────────────

    private async Task PublicarInventarioAsync(Producto p, int stockAnterior, string motivo)
    {
        try
        {
            await bus.Publish(new InventarioActualizadoEvent
            {
                ProductoId    = p.Id,
                StockNuevo    = p.Stock,
                StockAnterior = stockAnterior,
                Motivo        = motivo,
                Timestamp     = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ No se pudo publicar InventarioActualizadoEvent para producto {Id}", p.Id);
        }
    }

    private async Task PublicarVentaAplicadaAsync(Guid idTransaccionLocal, Venta venta)
    {
        try
        {
            await bus.Publish(new VentaAplicadaEnCoreEvent
            {
                IdTransaccionLocal = idTransaccionLocal,
                VentaId            = venta.Id,
                NumeroFactura      = venta.NumeroFactura,
                Total              = venta.Total,
                Exitoso            = true,
                Timestamp          = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ No se pudo publicar VentaAplicadaEnCoreEvent para {Id}", idTransaccionLocal);
        }
    }

    private async Task<string> GenerarNumeroFacturaAsync()
    {
        var ultimo = await db.Ventas.CountAsync();
        return $"F-{DateTime.UtcNow:yyyyMM}-{(ultimo + 1):D5}";
    }

    private static VentaResponse MapToResponse(Venta v) => new(
        v.Id, v.NumeroFactura, v.SucursalId, v.CajaId,
        v.ClienteId,
        v.Cliente is null ? null : $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim(),
        v.UsuarioId,
        v.Usuario is null ? "-" : $"{v.Usuario.Nombre} {v.Usuario.Apellido}".Trim(),
        v.Fecha, v.Subtotal, v.IVA, v.Total, v.Descuento,
        v.MetodoPago.ToString(), v.Estado.ToString(), v.EsOffline,
        v.Lineas.Select(l => new LineaVentaResponse(
            l.Id, l.ProductoId,
            l.Producto?.Codigo ?? "-",
            l.Producto?.Nombre ?? "-",
            l.Cantidad, l.PrecioUnitario, l.Descuento, l.Subtotal
        )).ToList());
}
