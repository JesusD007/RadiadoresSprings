using Core.API.DTOs.Requests;
using Core.API.Services;
using Core.ConsoleUI.Auth;
using Spectre.Console;

namespace Core.ConsoleUI.Menus;

public static class ClientesMenu
{
    public static async Task ShowAsync(
        IClienteService clienteSvc,
        ICuentaCobrarService ccSvc,
        IPagoService pagoSvc,
        ConsoleSession session)
    {
        var puedeEscribir = PermisoConsola.PuedeEscribirClientes(session.Rol);

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Clientes").Color(Color.Green));

            var opciones = new List<string>
            {
                "📋 Listar clientes",
                "🔍 Buscar cliente",
                "💳 Cuentas por cobrar",
                "⚠️  Cuentas vencidas"
            };

            if (puedeEscribir)
            {
                opciones.Insert(2, "➕ Nuevo cliente");
                opciones.Add("💰 Registrar pago");
            }

            opciones.Add("← Volver");

            var opcion = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]■ Módulo de Clientes[/]")
                    .AddChoices(opciones));

            switch (opcion)
            {
                case "📋 Listar clientes":
                    await ListarClientesAsync(clienteSvc);
                    break;
                case "🔍 Buscar cliente":
                    await BuscarClienteAsync(clienteSvc);
                    break;
                case "➕ Nuevo cliente":
                    await CrearClienteAsync(clienteSvc);
                    break;
                case "💳 Cuentas por cobrar":
                    await ListarCuentasCobrarAsync(ccSvc);
                    break;
                case "💰 Registrar pago":
                    await RegistrarPagoAsync(pagoSvc, session.UsuarioId);
                    break;
                case "⚠️  Cuentas vencidas":
                    await CuentasVencidasAsync(ccSvc);
                    break;
                case "← Volver":
                    return;
            }
        }
    }

    private static async Task ListarClientesAsync(IClienteService svc)
    {
        var result = await svc.GetPagedAsync(1, 100, null);
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]")
            .AddColumn("[bold]Nombre[/]")
            .AddColumn("[bold]RFC[/]")
            .AddColumn("[bold]Tipo[/]")
            .AddColumn(new TableColumn("[bold]Crédito Disp.[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Saldo Pend.[/]").RightAligned())
            .AddColumn("[bold]Estado[/]");

        foreach (var c in result.Items)
        {
            var saldoColor = c.SaldoPendiente > 0 ? "[red]" : "[green]";
            table.AddRow(
                c.Id.ToString(),
                Markup.Escape(c.NombreCompleto),
                Markup.Escape(c.RFC ?? "-"),
                Markup.Escape(c.Tipo),
                $"[green]${c.CreditoDisponible:F2}[/]",
                $"{saldoColor}${c.SaldoPendiente:F2}[/]",
                c.EsActivo ? "[green]●[/]" : "[red]●[/]"
            );
        }

        AnsiConsole.MarkupLine($"\n[bold]Total clientes:[/] {result.Total}\n");
        AnsiConsole.Write(table);
        Console.ReadLine();
    }

    private static async Task BuscarClienteAsync(IClienteService svc)
    {
        var busq   = AnsiConsole.Ask<string>("🔍 Nombre, apellido o RFC:");
        var result = await svc.GetPagedAsync(1, 20, busq);

        if (!result.Items.Any())
        {
            AnsiConsole.MarkupLine("[red]No se encontraron resultados.[/]");
        }
        else
        {
            foreach (var c in result.Items)
                AnsiConsole.MarkupLine(
                    $"[bold]#{c.Id}[/] {Markup.Escape(c.NombreCompleto)} | " +
                    $"RFC: {Markup.Escape(c.RFC ?? "-")} | " +
                    $"Tipo: {Markup.Escape(c.Tipo)} | " +
                    $"Saldo: [yellow]${c.SaldoPendiente:F2}[/]");
        }
        Console.ReadLine();
    }

    private static async Task CrearClienteAsync(IClienteService svc)
    {
        AnsiConsole.MarkupLine("[green bold]➕ Nuevo cliente[/]\n");

        var nombre   = AnsiConsole.Ask<string>("Nombre:");
        var apellido = AnsiConsole.Prompt(new TextPrompt<string>("Apellido [grey](enter para omitir)[/]:").AllowEmpty());
        var email    = AnsiConsole.Prompt(new TextPrompt<string>("Email [grey](enter para omitir)[/]:").AllowEmpty());
        var telefono = AnsiConsole.Prompt(new TextPrompt<string>("Teléfono [grey](enter para omitir)[/]:").AllowEmpty());
        var rfc      = AnsiConsole.Prompt(new TextPrompt<string>("RFC [grey](enter para omitir)[/]:").AllowEmpty());
        var tipo     = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Tipo de cliente:")
                .AddChoices("Regular", "Mayorista", "VIP", "Anonimo"));
        var credito  = AnsiConsole.Ask<decimal>("Límite de crédito [grey](0 = sin crédito)[/]:", 0m);

        var req = new CrearClienteRequest(nombre,
            string.IsNullOrWhiteSpace(apellido)  ? null : apellido,
            string.IsNullOrWhiteSpace(email)     ? null : email,
            string.IsNullOrWhiteSpace(telefono)  ? null : telefono,
            null,
            string.IsNullOrWhiteSpace(rfc)       ? null : rfc,
            tipo, credito);

        var cliente = await svc.CrearAsync(req);
        AnsiConsole.MarkupLine(
            $"[green]✅ Cliente creado: #{cliente.Id} — {Markup.Escape(cliente.NombreCompleto)}[/]");
        Console.ReadLine();
    }

    private static async Task ListarCuentasCobrarAsync(ICuentaCobrarService svc)
    {
        var result = await svc.GetPagedAsync(1, 50, null, null);
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]").AddColumn("[bold]Cliente[/]")
            .AddColumn("[bold]Factura[/]")
            .AddColumn(new TableColumn("[bold]Original[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Saldo[/]").RightAligned())
            .AddColumn("[bold]Vencimiento[/]").AddColumn("[bold]Estado[/]");

        foreach (var cc in result.Items)
        {
            var saldoColor = cc.EstaVencida ? "[red bold]" : "[yellow]";
            table.AddRow(
                cc.Id.ToString(),
                Markup.Escape(cc.NombreCliente),
                cc.NumeroFactura,
                $"${cc.MontoOriginal:F2}",
                $"{saldoColor}${cc.SaldoPendiente:F2}[/]",
                cc.FechaVencimiento.ToString("dd/MM/yyyy"),
                cc.Estado
            );
        }

        AnsiConsole.MarkupLine($"\n[bold]Cuentas por cobrar:[/] {result.Total}\n");
        AnsiConsole.Write(table);
        Console.ReadLine();
    }

    private static async Task RegistrarPagoAsync(IPagoService svc, int usuarioId)
    {
        AnsiConsole.MarkupLine("[green bold]💰 Registrar pago[/]\n");

        var clienteId = AnsiConsole.Ask<int>("ID del cliente:");
        var ccId      = AnsiConsole.Ask<int>("ID de cuenta por cobrar [grey](0 = pago general)[/]:");
        var monto     = AnsiConsole.Ask<decimal>("Monto del pago:");
        var metodo    = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Método:")
                .AddChoices("Efectivo", "TarjetaCredito", "TarjetaDebito", "Transferencia"));
        var referencia = AnsiConsole.Prompt(new TextPrompt<string>("Referencia [grey](enter para omitir)[/]:").AllowEmpty());

        var req = new RegistrarPagoRequest(
            clienteId,
            ccId > 0 ? ccId : null,
            monto,
            metodo,
            string.IsNullOrWhiteSpace(referencia) ? null : referencia,
            null);

        var pago = await svc.RegistrarAsync(req, usuarioId);
        AnsiConsole.MarkupLine($"[green]✅ Pago registrado: #{pago.Id} — ${pago.Monto:F2}[/]");
        Console.ReadLine();
    }

    private static async Task CuentasVencidasAsync(ICuentaCobrarService svc)
    {
        var vencidas = (await svc.GetVencidasAsync()).ToList();

        if (!vencidas.Any())
        {
            AnsiConsole.MarkupLine("[green]✅ No hay cuentas vencidas.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red bold]⚠️  {vencidas.Count} cuentas vencidas:[/]\n");
            foreach (var cc in vencidas)
                AnsiConsole.MarkupLine(
                    $"  [red]•[/] #{cc.Id} {Markup.Escape(cc.NombreCliente)} — " +
                    $"Factura {cc.NumeroFactura} — " +
                    $"Saldo [red]${cc.SaldoPendiente:F2}[/] " +
                    $"(Vencida: {cc.FechaVencimiento:dd/MM/yyyy})");
        }
        Console.ReadLine();
    }
}
