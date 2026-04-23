using Core.API.Authorization;
using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiController]
[Route("api/v1/productos")]
[Authorize(Policy = ApiPolicies.Autenticado)]
public class ProductosController(IProductoService productoService) : ControllerBase
{
    // ── Lectura — todos los roles autenticados ────────────────────────────────

    /// <summary>Listado paginado de productos. Todos los roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductoResponse>>>> GetAll(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50,
        [FromQuery] string? busqueda = null, [FromQuery] int? categoriaId = null)
    {
        var result = await productoService.GetPagedAsync(pagina, tamano, busqueda, categoriaId);
        return Ok(new ApiResponse<PagedResult<ProductoResponse>>(true, null, result));
    }

    /// <summary>Obtener producto por ID. Todos los roles.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductoResponse>>> GetById(int id)
    {
        var p = await productoService.GetByIdAsync(id);
        return p is null
            ? NotFound(new ApiResponse<ProductoResponse>(false, "Producto no encontrado.", null))
            : Ok(new ApiResponse<ProductoResponse>(true, null, p));
    }

    /// <summary>Obtener producto por código. Todos los roles (incluye Cliente para consulta de precios).</summary>
    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult<ApiResponse<ProductoResponse>>> GetByCodigo(string codigo)
    {
        var p = await productoService.GetByCodigoAsync(codigo);
        return p is null
            ? NotFound(new ApiResponse<ProductoResponse>(false, "Producto no encontrado.", null))
            : Ok(new ApiResponse<ProductoResponse>(true, null, p));
    }

    /// <summary>Productos con stock bajo. Todos los roles.</summary>
    [HttpGet("stock-bajo")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductoResponse>>>> GetStockBajo()
    {
        var result = await productoService.GetStockBajoAsync();
        return Ok(new ApiResponse<IEnumerable<ProductoResponse>>(true, null, result));
    }

    // ── Escritura — Administrador y Almacenista ───────────────────────────────

    /// <summary>Crear producto. Administrador y Almacenista.</summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.GestionInventario)]
    public async Task<ActionResult<ApiResponse<ProductoResponse>>> Crear([FromBody] CrearProductoRequest req)
    {
        try
        {
            var p = await productoService.CrearAsync(req);
            return CreatedAtAction(nameof(GetById), new { id = p.Id },
                new ApiResponse<ProductoResponse>(true, "Producto creado.", p));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<ProductoResponse>(false, ex.Message, null));
        }
    }

    /// <summary>Actualizar producto. Administrador y Almacenista.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiPolicies.GestionInventario)]
    public async Task<ActionResult<ApiResponse<ProductoResponse>>> Actualizar(
        int id, [FromBody] ActualizarProductoRequest req)
    {
        var p = await productoService.ActualizarAsync(id, req);
        return p is null
            ? NotFound(new ApiResponse<ProductoResponse>(false, "Producto no encontrado.", null))
            : Ok(new ApiResponse<ProductoResponse>(true, "Producto actualizado.", p));
    }

    /// <summary>
    /// Ajustar stock. Administrador, Almacenista y Cliente.
    /// Cliente lo usa para aplicar transacciones offline desde IntegrationApp.
    /// </summary>
    [HttpPatch("{id:int}/stock")]
    [Authorize(Policy = ApiPolicies.SincronizacionOffline)]
    public async Task<ActionResult<ApiResponse<bool>>> AjustarStock(
        int id, [FromBody] AjustarStockRequest req)
    {
        try
        {
            var ok = await productoService.AjustarStockAsync(id, req.Cantidad, req.Motivo);
            return ok
                ? Ok(new ApiResponse<bool>(true, "Stock ajustado.", true))
                : NotFound(new ApiResponse<bool>(false, "Producto no encontrado.", false));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<bool>(false, ex.Message, false));
        }
    }

    // ── Operaciones destructivas — solo Administrador ─────────────────────────

    /// <summary>Desactivar producto. Solo Administrador.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<bool>>> Eliminar(int id)
    {
        var ok = await productoService.EliminarAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Producto desactivado.", true))
            : NotFound(new ApiResponse<bool>(false, "Producto no encontrado.", false));
    }
}
