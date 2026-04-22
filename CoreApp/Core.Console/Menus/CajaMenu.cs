using Core.API.DTOs.Requests;
using Core.API.Services;
using Core.ConsoleUI.Auth;
using Spectre.Console;

namespace Core.ConsoleUI.Menus;

public static class CajaMenu
{
    public static async Task ShowAsync(ICajaService cajaSvc, ConsoleSession session)
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Caja").Color(Color.Gold1));

            var opcion = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]■ Módulo de Caja[/]")
                    .AddChoices(
                        "🏦 Ver cajas disponibles",
                        "🔓 Abrir sesión de caja",
                        "🔒 Cerrar sesión de caja",
                        "📊 Historial de sesiones",
                        "← Volver"
                    ));

            switch (opcion)
            {
                case "🏦 Ver cajas disponibles":
                    await VerCajasAsync(cajaSvc);
                    break;
                case "🔓 Abrir sesión de caja":
                    await AbrirSesionAsync(cajaSvc, session.UsuarioId);
                    break;
                case "🔒 Cerrar sesión de caja":
                    await CerrarSesionAsync(cajaSvc);
                    break;
                case "📊 Historial de sesiones":
                    await HistorialAsync(cajaSvc);
                    break;
                case "← Volver":
                    return;
            }
        }
    }

    private static async Task VerCajasAsync(ICajaService svc)
    {
        var cajas = (await svc.GetCajasAsync(null)).ToList();
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]").AddColumn("[bold]Número[/]")
            .AddColumn("[bold]Nombre[/]").AddColumn("[bold]Sucursal[/]")
            .AddColumn("[bold]Sesión Activa[/]");

        foreach (var c in cajas)
        {
            var sesion = await svc.GetSesionActivaAsync(c.Id);
            table.AddRow(
                c.Id.ToString(),
                c.Numero,
                Markup.Escape(c.Nombre),
                Markup.Escape(c.NombreSucursal),
                sesion is null
                    ? "[grey]Sin sesión[/]"
                    : $"[green]Abierta desde {sesion.FechaApertura.ToLocalTime():HH:mm}[/]");
        }

        AnsiConsole.Write(table);
        Console.ReadLine();
    }

    private static async Task AbrirSesionAsync(ICajaService svc, int usuarioId)
    {
        var cajaId = AnsiConsole.Ask<int>("ID de la caja:");
        var monto  = AnsiConsole.Ask<decimal>("Monto de apertura:");

        try
        {
            var sesion = await svc.AbrirSesionAsync(
                new AbrirSesionCajaRequest(cajaId, monto), usuarioId);

            AnsiConsole.MarkupLine(
                $"[green]✅ Sesión abierta: #{sesion.Id} — Caja {Markup.Escape(sesion.NombreCaja)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ {Markup.Escape(ex.Message)}[/]");
        }

        Console.ReadLine();
    }

    private static async Task CerrarSesionAsync(ICajaService svc)
    {
        // Buscar la sesión activa por ID de caja (el usuario conoce su caja, no el ID de sesión)
        var cajaId = AnsiConsole.Ask<int>("ID de la caja a cerrar:");

        var sesionActiva = await svc.GetSesionActivaAsync(cajaId);
        if (sesionActiva is null)
        {
            AnsiConsole.MarkupLine("[red]⚠️  No hay una sesión activa en esa caja.[/]");
            Console.ReadLine();
            return;
        }

        // Mostrar resumen de la sesión antes del cierre
        AnsiConsole.Write(new Panel(
            $"[bold]Sesión:[/]        #{sesionActiva.Id}\n" +
            $"[bold]Caja:[/]          {Markup.Escape(sesionActiva.NombreCaja)}\n" +
            $"[bold]Usuario:[/]       {Markup.Escape(sesionActiva.NombreUsuario)}\n" +
            $"[bold]Apertura:[/]      {sesionActiva.FechaApertura.ToLocalTime():dd/MM/yyyy HH:mm}\n" +
            $"[bold]Monto apertura:[/] ${sesionActiva.MontoApertura:F2}\n" +
            $"[bold]Ventas:[/]        {sesionActiva.TotalVentas} — [yellow]${sesionActiva.TotalVendido:F2}[/]")
        .Header("[gold1]Sesión activa[/]").Border(BoxBorder.Rounded));

        var montoCierre = AnsiConsole.Ask<decimal>("\nMonto físico en caja (conteo real):");
        var obs         = AnsiConsole.Ask<string>("Observaciones [grey](enter para omitir)[/]:");

        if (!AnsiConsole.Confirm($"¿Confirmar cierre de sesión #{sesionActiva.Id}?")) return;

        var sesionCerrada = await svc.CerrarSesionAsync(
            new CerrarSesionCajaRequest(
                sesionActiva.Id, montoCierre,
                string.IsNullOrWhiteSpace(obs) ? null : obs));

        if (sesionCerrada is null)
        {
            AnsiConsole.MarkupLine("[red]❌ No se pudo cerrar la sesión.[/]");
        }
        else
        {
            var difColor = sesionCerrada.Diferencia < 0 ? "[red]"
                         : sesionCerrada.Diferencia > 0 ? "[yellow]"
                         : "[green]";

            AnsiConsole.Write(new Panel(
                $"[bold]Total vendido:[/]  [yellow]${sesionCerrada.TotalVendido:F2}[/]\n" +
                $"[bold]Monto sistema:[/]  ${sesionCerrada.MontoSistema:F2}\n" +
                $"[bold]Monto físico:[/]   ${sesionCerrada.MontoCierre:F2}\n" +
                $"[bold]Diferencia:[/]     {difColor}{sesionCerrada.Diferencia:F2}[/]\n" +
                $"[bold]Estado:[/]         [green]{sesionCerrada.Estado}[/]")
            .Header("[green]✅ Resumen de cierre de caja[/]")
            .Border(BoxBorder.Rounded));
        }

        Console.ReadLine();
    }

    private static async Task HistorialAsync(ICajaService svc)
    {
        var cajaId  = AnsiConsole.Ask<int>("ID de la caja:");
        var dias    = AnsiConsole.Ask<int>("Días de historial:", 30);
        var sesiones = (await svc.GetHistorialAsync(cajaId, dias)).ToList();

        if (!sesiones.Any())
        {
            AnsiConsole.MarkupLine("[grey]Sin historial en el período.[/]");
        }
        else
        {
            var table = new Table().Border(TableBorder.Simple)
                .AddColumn("[bold]ID[/]").AddColumn("[bold]Apertura[/]")
                .AddColumn("[bold]Cierre[/]").AddColumn("[bold]Usuario[/]")
                .AddColumn(new TableColumn("[bold]Vendido[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Diferencia[/]").RightAligned())
                .AddColumn("[bold]Estado[/]");

            foreach (var s in sesiones)
            {
                var difColor = s.Diferencia.HasValue
                    ? (s.Diferencia < 0 ? "[red]" : s.Diferencia > 0 ? "[yellow]" : "[green]")
                    : "[grey]";

                table.AddRow(
                    s.Id.ToString(),
                    s.FechaApertura.ToLocalTime().ToString("dd/MM HH:mm"),
                    s.FechaCierre?.ToLocalTime().ToString("dd/MM HH:mm") ?? "[grey]-[/]",
                    Markup.Escape(s.NombreUsuario),
                    $"[yellow]${s.TotalVendido:F2}[/]",
                    s.Diferencia.HasValue ? $"{difColor}{s.Diferencia:F2}[/]" : "-",
                    s.Estado
                );
            }

            AnsiConsole.Write(table);
        }

        Console.ReadLine();
    }
}
