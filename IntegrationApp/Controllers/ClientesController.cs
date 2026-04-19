using IntegrationApp.Contracts.Responses.Clientes;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private static readonly Guid AnonimoCLIENTEId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ClientesController(ICoreApiClient core, ICircuitBreakerStateService cbState)
    {
        _core = core;
        _cbState = cbState;
    }

    /// <summary>Datos del cliente. Si no existe, retorna el registro "Cliente Anónimo".</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> GetCliente(Guid id, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio no disponible temporalmente" });

        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var response = await _core.GetAsync($"/api/v1/clientes/{id}", bearerToken: token, ct: ct);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            return Ok(JsonSerializer.Deserialize<ClienteDto>(content, _json));
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Devolver Cliente Anónimo
            return Ok(new ClienteDto
            {
                Id = AnonimoCLIENTEId,
                EsAnonimo = true,
                Nombre = "Cliente Anónimo",
                LimiteCredito = 0,
                SaldoPendiente = 0
            });
        }

        return StatusCode((int)response.StatusCode);
    }
}
