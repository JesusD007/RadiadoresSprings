using Core.API.Authorization;
using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.API.Services;
using Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Core.API.Controllers;

// ── CategoriasController ──────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/categorias")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class CategoriasController(ICategoriaService categoriaService) : ControllerBase
{
    /// <summary>Listar todas las categorías. Todos los roles (Cliente las necesita para sincronizar productos).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoriaResponse>>>> GetAll()
        => Ok(new ApiResponse<IEnumerable<CategoriaResponse>>(true, null, await categoriaService.GetAllAsync()));

    /// <summary>Obtener categoría por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoriaResponse>>> GetById(int id)
    {
        var c = await categoriaService.GetByIdAsync(id);
        return c is null
            ? NotFound(new ApiResponse<CategoriaResponse>(false, "Categoría no encontrada.", null))
            : Ok(new ApiResponse<CategoriaResponse>(true, null, c));
    }

    /// <summary>Crear categoría. Solo Administrador.</summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<CategoriaResponse>>> Crear([FromBody] CrearCategoriaRequest req)
    {
        var c = await categoriaService.CrearAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = c.Id },
            new ApiResponse<CategoriaResponse>(true, "Categoría creada.", c));
    }

    /// <summary>Actualizar categoría. Solo Administrador.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<bool>>> Actualizar(int id, [FromBody] CrearCategoriaRequest req)
    {
        var ok = await categoriaService.ActualizarAsync(id, req);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Categoría actualizada.", true))
            : NotFound(new ApiResponse<bool>(false, "Categoría no encontrada.", false));
    }
}

// ── ClientesController ────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/clientes")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class ClientesController(IClienteService clienteService) : ControllerBase
{
    /// <summary>Listado paginado de clientes. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ClienteResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50, [FromQuery] string? busqueda = null)
        => Ok(new ApiResponse<PagedResult<ClienteResponse>>(true, null,
            await clienteService.GetPagedAsync(pagina, tamano, busqueda)));

    /// <summary>Obtener cliente por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ClienteResponse>>> GetById(int id)
    {
        var c = await clienteService.GetByIdAsync(id);
        return c is null
            ? NotFound(new ApiResponse<ClienteResponse>(false, "Cliente no encontrado.", null))
            : Ok(new ApiResponse<ClienteResponse>(true, null, c));
    }

    /// <summary>
    /// Crear cliente. Administrador, Vendedor y Cliente.
    /// Cliente lo usa para registrar clientes nuevos desde el e-commerce.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionClientes)]
    public async Task<ActionResult<ApiResponse<ClienteResponse>>> Crear([FromBody] CrearClienteRequest req)
    {
        var c = await clienteService.CrearAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = c.Id },
            new ApiResponse<ClienteResponse>(true, "Cliente creado.", c));
    }

    /// <summary>
    /// Actualizar cliente. Administrador, Vendedor y Cliente.
    /// Cliente lo usa para sincronizar datos del cliente desde el portal web.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.GestionClientes)]
    public async Task<ActionResult<ApiResponse<ClienteResponse>>> Actualizar(
        int id, [FromBody] ActualizarClienteRequest req)
    {
        var c = await clienteService.ActualizarAsync(id, req);
        return c is null
            ? NotFound(new ApiResponse<ClienteResponse>(false, "Cliente no encontrado.", null))
            : Ok(new ApiResponse<ClienteResponse>(true, "Cliente actualizado.", c));
    }

    /// <summary>Eliminar (desactivar) cliente. Solo Administrador.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<bool>>> Eliminar(int id)
    {
        var ok = await clienteService.EliminarAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Cliente eliminado.", true))
            : NotFound(new ApiResponse<bool>(false, "Cliente no encontrado.", false));
    }
}

// ── CajaController ────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/caja")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class CajaController(ICajaService cajaService) : ControllerBase
{
    /// <summary>Listar cajas. Todos los roles (Cliente puede consultar la caja activa para incluirla en ventas).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CajaResponse>>>> GetCajas(
        [FromQuery] int? sucursalId = null)
        => Ok(new ApiResponse<IEnumerable<CajaResponse>>(true, null,
            await cajaService.GetCajasAsync(sucursalId)));

    /// <summary>Obtener sesión activa de una caja. Todos los roles.</summary>
    [HttpGet("{cajaId:int}/sesion-activa")]
    public async Task<ActionResult<ApiResponse<SesionCajaResponse>>> GetSesionActiva(int cajaId)
    {
        var s = await cajaService.GetSesionActivaAsync(cajaId);
        return s is null
            ? NotFound(new ApiResponse<SesionCajaResponse>(false, "No hay sesión activa.", null))
            : Ok(new ApiResponse<SesionCajaResponse>(true, null, s));
    }

    /// <summary>
    /// Abrir sesión de caja. Administrador y Cajero.
    /// Cliente NO puede abrir cajas — es una operación física que requiere presencia humana.
    /// </summary>
    [HttpPost("abrir")]
    [Authorize(Policy = ApiPolicies.GestionCaja)]
    public async Task<ActionResult<ApiResponse<SesionCajaResponse>>> AbrirSesion(
        [FromBody] AbrirSesionCajaRequest req)
    {
        try
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var s = await cajaService.AbrirSesionAsync(req, usuarioId);
            return Ok(new ApiResponse<SesionCajaResponse>(true, "Sesión de caja abierta.", s));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<SesionCajaResponse>(false, ex.Message, null));
        }
    }

    /// <summary>
    /// Cerrar sesión de caja. Administrador y Cajero.
    /// Cliente NO puede cerrar cajas — requiere conteo físico de efectivo.
    /// </summary>
    [HttpPost("cerrar")]
    [Authorize(Policy = ApiPolicies.GestionCaja)]
    public async Task<ActionResult<ApiResponse<SesionCajaResponse>>> CerrarSesion(
        [FromBody] CerrarSesionCajaRequest req)
    {
        var s = await cajaService.CerrarSesionAsync(req);
        return s is null
            ? NotFound(new ApiResponse<SesionCajaResponse>(false, "Sesión no encontrada.", null))
            : Ok(new ApiResponse<SesionCajaResponse>(true, "Sesión cerrada.", s));
    }

    /// <summary>Historial de sesiones de una caja. Todos los roles.</summary>
    [HttpGet("{cajaId:int}/historial")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SesionCajaResponse>>>> GetHistorial(
        int cajaId, [FromQuery] int dias = 30)
        => Ok(new ApiResponse<IEnumerable<SesionCajaResponse>>(true, null,
            await cajaService.GetHistorialAsync(cajaId, dias)));
}

// ── OrdenesController ─────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/ordenes")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class OrdenesController(IOrdenService ordenService) : ControllerBase
{
    /// <summary>Listado paginado de órdenes. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrdenResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50, [FromQuery] string? estado = null)
        => Ok(new ApiResponse<PagedResult<OrdenResponse>>(true, null,
            await ordenService.GetPagedAsync(pagina, tamano, estado)));

    /// <summary>Obtener orden por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrdenResponse>>> GetById(int id)
    {
        var o = await ordenService.GetByIdAsync(id);
        return o is null
            ? NotFound(new ApiResponse<OrdenResponse>(false, "Orden no encontrada.", null))
            : Ok(new ApiResponse<OrdenResponse>(true, null, o));
    }

    /// <summary>
    /// Crear orden. Administrador, Vendedor y Cliente.
    /// Cliente la usa para registrar órdenes del e-commerce.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionOrdenes)]
    public async Task<ActionResult<ApiResponse<OrdenResponse>>> Crear([FromBody] CrearOrdenRequest req)
    {
        try
        {
            var o = await ordenService.CrearAsync(req);
            return CreatedAtAction(nameof(GetById), new { id = o.Id },
                new ApiResponse<OrdenResponse>(true, "Orden creada.", o));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<OrdenResponse>(false, ex.Message, null));
        }
    }

    /// <summary>
    /// Cambiar estado de una orden. Administrador, Vendedor y Cliente.
    /// Cliente lo usa para notificar cambios de estado desde el sistema de envíos.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = ApiPolicies.GestionOrdenes)]
    public async Task<ActionResult<ApiResponse<OrdenResponse>>> CambiarEstado(
        int id, [FromBody] CambiarEstadoOrdenRequest req)
    {
        try
        {
            var o = await ordenService.CambiarEstadoAsync(id, req);
            return o is null
                ? NotFound(new ApiResponse<OrdenResponse>(false, "Orden no encontrada.", null))
                : Ok(new ApiResponse<OrdenResponse>(true, "Estado actualizado.", o));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<OrdenResponse>(false, ex.Message, null));
        }
    }

    /// <summary>Órdenes de un cliente. Todos los roles.</summary>
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrdenResponse>>>> GetByCliente(int clienteId)
        => Ok(new ApiResponse<IEnumerable<OrdenResponse>>(true, null,
            await ordenService.GetByClienteAsync(clienteId)));
}

// ── PagosController ───────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/pagos")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class PagosController(IPagoService pagoService) : ControllerBase
{
    /// <summary>Listado paginado de pagos. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PagoResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50, [FromQuery] int? clienteId = null)
        => Ok(new ApiResponse<PagedResult<PagoResponse>>(true, null,
            await pagoService.GetPagedAsync(pagina, tamano, clienteId)));

    /// <summary>Obtener pago por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PagoResponse>>> GetById(int id)
    {
        var p = await pagoService.GetByIdAsync(id);
        return p is null
            ? NotFound(new ApiResponse<PagoResponse>(false, "Pago no encontrado.", null))
            : Ok(new ApiResponse<PagoResponse>(true, null, p));
    }

    /// <summary>
    /// Registrar pago. Administrador, Cajero y Cliente.
    /// Cliente lo usa para registrar cobros procesados en la pasarela de pagos online.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionPagos)]
    public async Task<ActionResult<ApiResponse<PagoResponse>>> Registrar([FromBody] RegistrarPagoRequest req)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var p = await pagoService.RegistrarAsync(req, usuarioId);
        return Ok(new ApiResponse<PagoResponse>(true, "Pago registrado.", p));
    }
}

// ── CuentasCobrarController ───────────────────────────────────────────────────
[ApiController]
[Route("api/v1/cuentas-cobrar")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class CuentasCobrarController(ICuentaCobrarService cuentaCobrarService) : ControllerBase
{
    /// <summary>Listado paginado de cuentas por cobrar. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CuentaCobrarResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50,
        [FromQuery] string? estado = null, [FromQuery] int? clienteId = null)
        => Ok(new ApiResponse<PagedResult<CuentaCobrarResponse>>(true, null,
            await cuentaCobrarService.GetPagedAsync(pagina, tamano, estado, clienteId)));

    /// <summary>Obtener cuenta por cobrar por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CuentaCobrarResponse>>> GetById(int id)
    {
        var cc = await cuentaCobrarService.GetByIdAsync(id);
        return cc is null
            ? NotFound(new ApiResponse<CuentaCobrarResponse>(false, "Cuenta por cobrar no encontrada.", null))
            : Ok(new ApiResponse<CuentaCobrarResponse>(true, null, cc));
    }

    /// <summary>Cuentas por cobrar vencidas. Todos los roles.</summary>
    [HttpGet("vencidas")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CuentaCobrarResponse>>>> GetVencidas()
        => Ok(new ApiResponse<IEnumerable<CuentaCobrarResponse>>(true, null,
            await cuentaCobrarService.GetVencidasAsync()));

    /// <summary>
    /// Crear cuenta por cobrar. Administrador y Cajero.
    /// Cliente NO puede abrir cuentas de crédito — requiere aprobación humana.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionCaja)]
    public async Task<ActionResult<ApiResponse<CuentaCobrarResponse>>> Crear(
        [FromBody] CrearCuentaCobrarRequest req)
    {
        var cc = await cuentaCobrarService.CrearAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = cc.Id },
            new ApiResponse<CuentaCobrarResponse>(true, "Cuenta por cobrar creada.", cc));
    }
}

// ── HealthController ──────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthController(CoreDbContext db) : ControllerBase
{
    /// <summary>Estado del sistema. Público — usado por load balancers y monitoreo.</summary>
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> GetHealth()
    {
        try
        {
            await db.Database.CanConnectAsync();
            var totalProductos = await db.Productos.CountAsync();
            var totalVentas    = await db.Ventas.CountAsync();
            return Ok(new HealthResponse("Healthy", "1.0.0", DateTime.UtcNow,
                "Connected", totalProductos, totalVentas));
        }
        catch (Exception ex)
        {
            return StatusCode(503, new HealthResponse($"Unhealthy: {ex.Message}",
                "1.0.0", DateTime.UtcNow, "Error", 0, 0));
        }
    }
}
