using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.Data;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using SharedContracts.Events;

namespace Core.API.Services;

// ── CategoriaService ──────────────────────────────────────────────────────────
public class CategoriaService(CoreDbContext db) : ICategoriaService
{
    public async Task<IEnumerable<CategoriaResponse>> GetAllAsync() =>
        await db.Categorias
            .Where(c => c.EsActiva)
            .Select(c => new CategoriaResponse(c.Id, c.Nombre, c.Descripcion,
                c.Productos.Count(p => p.EsActivo), c.EsActiva))
            .ToListAsync();

    public async Task<CategoriaResponse?> GetByIdAsync(int id)
    {
        var c = await db.Categorias.Include(x => x.Productos).FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? null : new CategoriaResponse(c.Id, c.Nombre, c.Descripcion,
            c.Productos.Count(p => p.EsActivo), c.EsActiva);
    }

    public async Task<CategoriaResponse> CrearAsync(CrearCategoriaRequest req)
    {
        var cat = new Categoria { Nombre = req.Nombre, Descripcion = req.Descripcion };
        db.Categorias.Add(cat);
        await db.SaveChangesAsync();
        return new CategoriaResponse(cat.Id, cat.Nombre, cat.Descripcion, 0, cat.EsActiva);
    }

    public async Task<bool> ActualizarAsync(int id, CrearCategoriaRequest req)
    {
        var cat = await db.Categorias.FindAsync(id);
        if (cat is null) return false;
        cat.Nombre = req.Nombre;
        cat.Descripcion = req.Descripcion;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var cat = await db.Categorias.FindAsync(id);
        if (cat is null) return false;
        cat.EsActiva = false;
        await db.SaveChangesAsync();
        return true;
    }
}

// ── ClienteService ────────────────────────────────────────────────────────────
public class ClienteService(CoreDbContext db) : IClienteService
{
    public async Task<PagedResult<ClienteResponse>> GetPagedAsync(int pagina, int tamano, string? busqueda)
    {
        var query = db.Clientes.Where(c => c.EsActivo);
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => c.Nombre.Contains(busqueda)
                || (c.Apellido != null && c.Apellido.Contains(busqueda))
                || (c.RFC != null && c.RFC.Contains(busqueda)));

        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Nombre)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(c => Map(c)).ToListAsync();
        return new PagedResult<ClienteResponse>(items, total, pagina, tamano);
    }

    public async Task<ClienteResponse?> GetByIdAsync(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        return c is null ? null : Map(c);
    }

    public async Task<ClienteResponse> CrearAsync(CrearClienteRequest req)
    {
        var tipo = Enum.TryParse<TipoCliente>(req.Tipo, true, out var t) ? t : TipoCliente.Regular;
        var cliente = new Cliente
        {
            Nombre = req.Nombre, Apellido = req.Apellido, Email = req.Email,
            Telefono = req.Telefono, Direccion = req.Direccion, RFC = req.RFC,
            Tipo = tipo, LimiteCredito = req.LimiteCredito
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return Map(cliente);
    }

    public async Task<ClienteResponse?> ActualizarAsync(int id, ActualizarClienteRequest req)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return null;
        var tipo = Enum.TryParse<TipoCliente>(req.Tipo, true, out var t) ? t : TipoCliente.Regular;
        c.Nombre = req.Nombre; c.Apellido = req.Apellido; c.Email = req.Email;
        c.Telefono = req.Telefono; c.Direccion = req.Direccion; c.RFC = req.RFC;
        c.Tipo = tipo; c.LimiteCredito = req.LimiteCredito; c.EsActivo = req.EsActivo;
        await db.SaveChangesAsync();
        return Map(c);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return false;
        c.EsActivo = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static ClienteResponse Map(Cliente c) => new(
        c.Id, c.Nombre, c.Apellido, $"{c.Nombre} {c.Apellido}".Trim(),
        c.Email, c.Telefono, c.Direccion, c.RFC,
        c.Tipo.ToString(), c.LimiteCredito, c.SaldoPendiente,
        c.CreditoDisponible(), c.EsActivo, c.FechaCreacion);
}

// ── CajaService ───────────────────────────────────────────────────────────────
public class CajaService(CoreDbContext db, ILogger<CajaService> logger) : ICajaService
{
    public async Task<IEnumerable<CajaResponse>> GetCajasAsync(int? sucursalId)
    {
        var q = db.Cajas.Include(c => c.Sucursal).Where(c => c.EsActiva);
        if (sucursalId.HasValue) q = q.Where(c => c.SucursalId == sucursalId.Value);
        return await q.Select(c => new CajaResponse(c.Id, c.SucursalId,
            c.Sucursal.Nombre, c.Numero, c.Nombre, c.EsActiva)).ToListAsync();
    }

    public async Task<SesionCajaResponse?> GetSesionActivaAsync(int cajaId)
    {
        var s = await db.SesionesCaja
            .Include(s => s.Caja).Include(s => s.Usuario).Include(s => s.Ventas)
            .FirstOrDefaultAsync(s => s.CajaId == cajaId && s.Estado == EstadoSesionCaja.Abierta);
        return s is null ? null : MapSesion(s);
    }

    public async Task<SesionCajaResponse> AbrirSesionAsync(AbrirSesionCajaRequest req, int usuarioId)
    {
        var activa = await db.SesionesCaja.AnyAsync(s =>
            s.CajaId == req.CajaId && s.Estado == EstadoSesionCaja.Abierta);
        if (activa)
            throw new InvalidOperationException("La caja ya tiene una sesión abierta.");

        var sesion = new SesionCaja
        {
            CajaId = req.CajaId, UsuarioId = usuarioId, MontoApertura = req.MontoApertura
        };
        db.SesionesCaja.Add(sesion);
        await db.SaveChangesAsync();
        await db.Entry(sesion).Reference(s => s.Caja).LoadAsync();
        await db.Entry(sesion).Reference(s => s.Usuario).LoadAsync();
        logger.LogInformation("🏦 Sesión de caja abierta: Caja {CajaId}", req.CajaId);
        return MapSesion(sesion);
    }

    public async Task<SesionCajaResponse?> CerrarSesionAsync(CerrarSesionCajaRequest req)
    {
        var sesion = await db.SesionesCaja
            .Include(s => s.Caja).Include(s => s.Usuario).Include(s => s.Ventas)
            .FirstOrDefaultAsync(s => s.Id == req.SesionId);
        if (sesion is null) return null;

        var totalSistema = sesion.Ventas
            .Where(v => v.MetodoPago == MetodoPago.Efectivo && v.Estado == EstadoVenta.Completada)
            .Sum(v => v.Total) + sesion.MontoApertura;

        sesion.Cerrar(req.MontoCierre, totalSistema);
        sesion.Observaciones = req.Observaciones;
        await db.SaveChangesAsync();
        logger.LogInformation("🏦 Sesión cerrada: {Id} | Diferencia: {Dif:C}", sesion.Id, sesion.Diferencia);
        return MapSesion(sesion);
    }

    public async Task<IEnumerable<SesionCajaResponse>> GetHistorialAsync(int cajaId, int dias)
    {
        var desde = DateTime.UtcNow.AddDays(-dias);
        return await db.SesionesCaja
            .Include(s => s.Caja).Include(s => s.Usuario).Include(s => s.Ventas)
            .Where(s => s.CajaId == cajaId && s.FechaApertura >= desde)
            .OrderByDescending(s => s.FechaApertura)
            .Select(s => MapSesion(s)).ToListAsync();
    }

    private static SesionCajaResponse MapSesion(SesionCaja s) => new(
        s.Id, s.CajaId, s.Caja?.Nombre ?? "-", s.UsuarioId,
        s.Usuario is null ? "-" : $"{s.Usuario.Nombre} {s.Usuario.Apellido}".Trim(),
        s.FechaApertura, s.FechaCierre, s.MontoApertura,
        s.MontoCierre, s.MontoSistema, s.Diferencia,
        s.Estado.ToString(),
        s.Ventas?.Count ?? 0,
        s.Ventas?.Where(v => v.Estado == EstadoVenta.Completada).Sum(v => v.Total) ?? 0);
}

// ── OrdenService ──────────────────────────────────────────────────────────────
public class OrdenService(CoreDbContext db, IMessageSession bus, ILogger<OrdenService> logger) : IOrdenService
{
    public async Task<PagedResult<OrdenResponse>> GetPagedAsync(int pagina, int tamano, string? estado)
    {
        var query = db.Ordenes.Include(o => o.Cliente)
            .Include(o => o.Lineas).ThenInclude(l => l.Producto).AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoOrden>(estado, true, out var e))
            query = query.Where(o => o.Estado == e);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.Fecha)
            .Skip((pagina - 1) * tamano).Take(tamano).Select(o => Map(o)).ToListAsync();
        return new PagedResult<OrdenResponse>(items, total, pagina, tamano);
    }

    public async Task<OrdenResponse?> GetByIdAsync(int id)
    {
        var o = await db.Ordenes.Include(x => x.Cliente)
            .Include(x => x.Lineas).ThenInclude(l => l.Producto)
            .FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? null : Map(o);
    }

    public async Task<OrdenResponse> CrearAsync(CrearOrdenRequest req)
    {
        var metodo = Enum.TryParse<MetodoPago>(req.MetodoPago, true, out var mp) ? mp : MetodoPago.Efectivo;
        var orden = new Orden
        {
            NumeroOrden = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            ClienteId = req.ClienteId, MetodoPago = metodo,
            DireccionEnvio = req.DireccionEnvio, Notas = req.Notas
        };

        decimal total = 0;
        foreach (var item in req.Lineas)
        {
            var producto = await db.Productos.FindAsync(item.ProductoId)
                ?? throw new InvalidOperationException($"Producto {item.ProductoId} no encontrado.");
            var linea = new LineaOrden { ProductoId = item.ProductoId, Cantidad = item.Cantidad, PrecioUnitario = producto.PrecioVigente() };
            orden.Lineas.Add(linea);
            total += linea.Subtotal;
        }
        orden.TotalOrden = total;

        db.Ordenes.Add(orden);
        await db.SaveChangesAsync();
        logger.LogInformation("📦 Orden creada: {Numero}", orden.NumeroOrden);
        return (await GetByIdAsync(orden.Id))!;
    }

    public async Task<OrdenResponse?> CambiarEstadoAsync(int id, CambiarEstadoOrdenRequest req)
    {
        var orden = await db.Ordenes.Include(o => o.Cliente)
            .Include(o => o.Lineas).ThenInclude(l => l.Producto)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null) return null;
        if (!Enum.TryParse<EstadoOrden>(req.NuevoEstado, true, out var nuevoEstado))
            throw new InvalidOperationException($"Estado inválido: {req.NuevoEstado}");

        var estadoAnterior = orden.Estado.ToString();
        orden.CambiarEstado(nuevoEstado);
        await db.SaveChangesAsync();

        try
        {
            await bus.Publish(new OrdenCambioEstadoEvent
            {
                OrdenId       = orden.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo   = nuevoEstado.ToString(),
                Nota          = req.Notas,
                Timestamp     = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex) { logger.LogWarning(ex, "⚠️ No se pudo publicar OrdenCambioEstadoEvent"); }

        return Map(orden);
    }

    public async Task<IEnumerable<OrdenResponse>> GetByClienteAsync(int clienteId) =>
        await db.Ordenes.Include(o => o.Cliente)
            .Include(o => o.Lineas).ThenInclude(l => l.Producto)
            .Where(o => o.ClienteId == clienteId)
            .OrderByDescending(o => o.Fecha)
            .Select(o => Map(o)).ToListAsync();

    private static OrdenResponse Map(Orden o) => new(
        o.Id, o.NumeroOrden, o.ClienteId,
        o.Cliente is null ? "-" : $"{o.Cliente.Nombre} {o.Cliente.Apellido}".Trim(),
        o.Estado.ToString(), o.Fecha, o.FechaEntrega, o.TotalOrden,
        o.Lineas.Sum(l => l.Cantidad),
        o.MetodoPago.ToString(), o.DireccionEnvio,
        o.Lineas.Select(l => new LineaOrdenResponse(l.Id, l.ProductoId,
            l.Producto?.Nombre ?? "-", l.Cantidad, l.PrecioUnitario, l.Subtotal)).ToList());
}

// ── PagoService ───────────────────────────────────────────────────────────────
public class PagoService(CoreDbContext db, ILogger<PagoService> logger) : IPagoService
{
    public async Task<PagedResult<PagoResponse>> GetPagedAsync(int pagina, int tamano, int? clienteId)
    {
        var query = db.Pagos.Include(p => p.Cliente).AsQueryable();
        if (clienteId.HasValue) query = query.Where(p => p.ClienteId == clienteId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.Fecha)
            .Skip((pagina - 1) * tamano).Take(tamano).Select(p => Map(p)).ToListAsync();
        return new PagedResult<PagoResponse>(items, total, pagina, tamano);
    }

    public async Task<PagoResponse?> GetByIdAsync(int id)
    {
        var p = await db.Pagos.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : Map(p);
    }

    public async Task<PagoResponse> RegistrarAsync(RegistrarPagoRequest req, int usuarioId)
    {
        var metodo = Enum.TryParse<MetodoPago>(req.MetodoPago, true, out var mp) ? mp : MetodoPago.Efectivo;
        var pago = new Pago
        {
            ClienteId = req.ClienteId, CuentaCobrarId = req.CuentaCobrarId,
            Monto = req.Monto, MetodoPago = metodo,
            Referencia = req.Referencia, Notas = req.Notas, UsuarioId = usuarioId
        };
        db.Pagos.Add(pago);

        if (req.CuentaCobrarId.HasValue)
        {
            var cc = await db.CuentasCobrar.Include(x => x.Cliente)
                .FirstOrDefaultAsync(x => x.Id == req.CuentaCobrarId.Value);
            if (cc is not null)
            {
                cc.AplicarPago(req.Monto);
                cc.Cliente.SaldoPendiente = Math.Max(0, cc.Cliente.SaldoPendiente - req.Monto);
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("💰 Pago registrado: ${Monto} - Cliente {ClienteId}", req.Monto, req.ClienteId);
        return (await GetByIdAsync(pago.Id))!;
    }

    private static PagoResponse Map(Pago p) => new(
        p.Id, p.ClienteId,
        p.Cliente is null ? "-" : $"{p.Cliente.Nombre} {p.Cliente.Apellido}".Trim(),
        p.VentaId, p.CuentaCobrarId, p.Monto,
        p.MetodoPago.ToString(), p.Fecha, p.Referencia);
}

// ── CuentaCobrarService ───────────────────────────────────────────────────────
public class CuentaCobrarService(CoreDbContext db) : ICuentaCobrarService
{
    public async Task<PagedResult<CuentaCobrarResponse>> GetPagedAsync(int pagina, int tamano, string? estado, int? clienteId)
    {
        var query = db.CuentasCobrar.Include(cc => cc.Cliente).AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoCuentaCobrar>(estado, true, out var e))
            query = query.Where(cc => cc.Estado == e);
        if (clienteId.HasValue) query = query.Where(cc => cc.ClienteId == clienteId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(cc => cc.FechaEmision)
            .Skip((pagina - 1) * tamano).Take(tamano).Select(cc => Map(cc)).ToListAsync();
        return new PagedResult<CuentaCobrarResponse>(items, total, pagina, tamano);
    }

    public async Task<CuentaCobrarResponse?> GetByIdAsync(int id)
    {
        var cc = await db.CuentasCobrar.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id);
        return cc is null ? null : Map(cc);
    }

    public async Task<CuentaCobrarResponse> CrearAsync(CrearCuentaCobrarRequest req)
    {
        var cc = new CuentaCobrar
        {
            ClienteId = req.ClienteId, VentaId = req.VentaId,
            NumeroFactura = req.NumeroFactura, MontoOriginal = req.MontoOriginal,
            FechaVencimiento = req.FechaVencimiento, Notas = req.Notas
        };
        db.CuentasCobrar.Add(cc);

        // Actualizar saldo del cliente
        var cliente = await db.Clientes.FindAsync(req.ClienteId);
        if (cliente is not null) cliente.SaldoPendiente += req.MontoOriginal;

        await db.SaveChangesAsync();
        return (await GetByIdAsync(cc.Id))!;
    }

    public async Task<IEnumerable<CuentaCobrarResponse>> GetVencidasAsync() =>
        await db.CuentasCobrar.Include(cc => cc.Cliente)
            .Where(cc => cc.FechaVencimiento < DateTime.UtcNow
                && cc.Estado != EstadoCuentaCobrar.Pagada
                && cc.Estado != EstadoCuentaCobrar.Cancelada)
            .Select(cc => Map(cc)).ToListAsync();

    private static CuentaCobrarResponse Map(CuentaCobrar cc) => new(
        cc.Id, cc.ClienteId,
        cc.Cliente is null ? "-" : $"{cc.Cliente.Nombre} {cc.Cliente.Apellido}".Trim(),
        cc.VentaId, cc.NumeroFactura, cc.MontoOriginal, cc.MontoPagado,
        cc.SaldoPendiente, cc.FechaEmision, cc.FechaVencimiento,
        cc.Estado.ToString(), cc.EstaVencida());
}
