using Core.API.Authorization;
using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Core.API.Controllers;

[ApiController]
[Route("api/v1/ventas")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class VentasController(IVentaService ventaService) : ControllerBase
{
    // ── Lectura — todos los roles autenticados ────────────────────────────────

    /// <summary>Listado paginado de ventas. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VentaResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        var result = await ventaService.GetPagedAsync(pagina, tamano, desde, hasta);
        return Ok(new ApiResponse<PagedResult<VentaResponse>>(true, null, result));
    }

    /// <summary>Obtener venta por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<VentaResponse>>> GetById(int id)
    {
        var v = await ventaService.GetByIdAsync(id);
        return v is null
            ? NotFound(new ApiResponse<VentaResponse>(false, "Venta no encontrada.", null))
            : Ok(new ApiResponse<VentaResponse>(true, null, v));
    }

    /// <summary>Obtener venta por número de factura. Todos los roles.</summary>
    [HttpGet("factura/{numero}")]
    public async Task<ActionResult<ApiResponse<VentaResponse>>> GetByFactura(string numero)
    {
        var v = await ventaService.GetByFacturaAsync(numero);
        return v is null
            ? NotFound(new ApiResponse<VentaResponse>(false, "Factura no encontrada.", null))
            : Ok(new ApiResponse<VentaResponse>(true, null, v));
    }

    // ── Escritura ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Registrar venta. Administrador, Cajero, Vendedor y ServicioWeb.
    /// ServicioWeb lo usa para aplicar ventas offline acumuladas en IntegrationApp.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionVentas)]
    public async Task<ActionResult<ApiResponse<VentaResponse>>> Crear([FromBody] CrearVentaRequest req)
    {
        try
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var v = await ventaService.CrearAsync(req, usuarioId);
            return CreatedAtAction(nameof(GetById), new { id = v.Id },
                new ApiResponse<VentaResponse>(true, "Venta registrada.", v));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<VentaResponse>(false, ex.Message, null));
        }
    }

    // ── Operaciones destructivas — solo Administrador ─────────────────────────

    /// <summary>
    /// Cancelar venta. Solo Administrador.
    /// El rol ServicioWeb NO puede cancelar ventas para evitar reversiones no supervisadas.
    /// </summary>
    [HttpPost("{id:int}/cancelar")]
    [Authorize(Policy = ApiPolicies.CancelarVentas)]
    public async Task<ActionResult<ApiResponse<bool>>> Cancelar(int id, [FromBody] string motivo)
    {
        var ok = await ventaService.CancelarAsync(id, motivo);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Venta cancelada.", true))
            : NotFound(new ApiResponse<bool>(false, "Venta no encontrada o ya cancelada.", false));
    }
}
