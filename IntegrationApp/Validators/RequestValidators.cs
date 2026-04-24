using FluentValidation;
using IntegrationApp.Contracts.Requests.Auth;
using IntegrationApp.Contracts.Requests.Caja;
using IntegrationApp.Contracts.Requests.Ordenes;
using IntegrationApp.Contracts.Requests.Ventas;

namespace IntegrationApp.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rol).Must(r => r is null or "Cajero" or "Admin" or "ServicioWeb" or "Cliente")
            .WithMessage("Rol debe ser Cajero, Admin, ServicioWeb o Cliente");
    }
}

public class CrearVentaRequestValidator : AbstractValidator<CrearVentaRequest>
{
    public CrearVentaRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.CajeroId).NotEmpty();
        RuleFor(x => x.SucursalId).NotEmpty();
        RuleFor(x => x.MetodoPago).NotEmpty()
            .Must(m => m is "Efectivo" or "Tarjeta" or "Transferencia")
            .WithMessage("MetodoPago debe ser: Efectivo, Tarjeta o Transferencia");
        RuleFor(x => x.MontoRecibido).GreaterThan(0);
        RuleFor(x => x.Lineas).NotEmpty().WithMessage("La venta debe tener al menos una línea");
        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.ProductoId).GreaterThan(0);
            linea.RuleFor(l => l.Cantidad).GreaterThan(0);
            linea.RuleFor(l => l.PrecioUnitario).GreaterThan(0);
        });
    }
}

public class CrearOrdenRequestValidator : AbstractValidator<CrearOrdenRequest>
{
    public CrearOrdenRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Lineas).NotEmpty().WithMessage("La orden debe tener al menos una línea");
        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.ProductoId).GreaterThan(0);
            linea.RuleFor(l => l.Cantidad).GreaterThan(0);
        });
    }
}

public class MovimientoCajaRequestValidator : AbstractValidator<MovimientoCajaRequest>
{
    public MovimientoCajaRequestValidator()
    {
        RuleFor(x => x.SesionCajaId).NotEmpty();
        RuleFor(x => x.Tipo).Must(t => t is "IN" or "OUT").WithMessage("Tipo debe ser IN o OUT");
        RuleFor(x => x.Monto).GreaterThan(0);
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FirmaDigital).NotEmpty();
    }
}
