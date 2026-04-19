using AutoMapper;
using IntegrationApp.Contracts.Responses.Productos;
using IntegrationApp.Data;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1")]
public class ProductosController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductosController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ProductosController(ICoreApiClient core, ICircuitBreakerStateService cbState,
        IntegrationDbContext db, IMapper mapper, ILogger<ProductosController> logger)
    {
        _core = core; _cbState = cbState; _db = db; _mapper = mapper; _logger = logger;
    }

    /// <summary>Lista paginada de productos. Sirve desde mirror en modo offline.</summary>
    [HttpGet("productos")]
    public async Task<ActionResult<ProductoPagedResult>> GetProductos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? nombre = null,
        [FromQuery] string? codigo = null,
        [FromQuery] int? categoriaId = null,
        [FromQuery] bool soloActivos = false,
        [FromQuery] bool conStock = false,
        CancellationToken ct = default)
    {
        if (_cbState.CoreAvailable)
        {
            // Proxy hacia Core
            try
            {
                var query = $"?page={page}&pageSize={pageSize}" +
                    (nombre != null ? $"&nombre={nombre}" : "") +
                    (codigo != null ? $"&codigo={codigo}" : "") +
                    (categoriaId.HasValue ? $"&categoriaId={categoriaId}" : "") +
                    (soloActivos ? "&soloActivos=true" : "") +
                    (conStock ? "&conStock=true" : "");

                var response = await _core.GetAsync($"/api/v1/productos{query}", ct: ct);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    return Ok(JsonSerializer.Deserialize<ProductoPagedResult>(content, _json));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Productos] Fallo proxy, sirviendo desde mirror");
            }
        }

        // Servir desde mirror
        HttpContext.Items["FromMirror"] = true;
        var query2 = _db.ProductosMirror.AsQueryable();
        if (!string.IsNullOrWhiteSpace(nombre)) query2 = query2.Where(p => p.Nombre.Contains(nombre));
        if (!string.IsNullOrWhiteSpace(codigo)) query2 = query2.Where(p => p.Codigo == codigo);
        if (categoriaId.HasValue) query2 = query2.Where(p => p.CategoriaId == categoriaId);
        if (soloActivos) query2 = query2.Where(p => p.EsActivo);
        if (conStock) query2 = query2.Where(p => p.Stock > 0);

        var total = await query2.CountAsync(ct);
        var items = await query2.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new ProductoPagedResult
        {
            Items = _mapper.Map<List<ProductoResumenDto>>(items),
            Total = total,
            Page = page,
            PageSize = pageSize,
            FromMirror = true
        });
    }

    /// <summary>Detalle completo de producto. [CRÍTICO] Sirve desde mirror en modo offline.</summary>
    [HttpGet("productos/{id:int}")]
    public async Task<ActionResult<ProductoDetalleDto>> GetProducto(int id, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            try
            {
                var response = await _core.GetAsync($"/api/v1/productos/{id}", ct: ct);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    return Ok(JsonSerializer.Deserialize<ProductoDetalleDto>(content, _json));
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound(new { error = "Producto no encontrado" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProductoDetalle] Fallo proxy, sirviendo desde mirror");
            }
        }

        // Desde mirror
        HttpContext.Items["FromMirror"] = true;
        var producto = await _db.ProductosMirror.FindAsync([id], ct);
        if (producto is null) return NotFound(new { error = "Producto no encontrado" });

        return Ok(_mapper.Map<ProductoDetalleDto>(producto));
    }

    /// <summary>Lista de servicios ofrecidos. Proxy hacia Core.</summary>
    [HttpGet("servicios")]
    public async Task<IActionResult> GetServicios(CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio no disponible temporalmente", fromMirror = false });

        var response = await _core.GetAsync("/api/v1/servicios", ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
