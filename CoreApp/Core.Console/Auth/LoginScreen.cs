using Core.API.Services;
using Spectre.Console;

namespace Core.ConsoleUI.Auth;

/// <summary>
/// Pantalla de autenticación de la consola.
/// Muestra un formulario visual, valida credenciales contra AuthService
/// y retorna un <see cref="ConsoleSession"/> si el login es exitoso.
/// Permite hasta 3 intentos; al agotarlos retorna null.
/// </summary>
public static class LoginScreen
{
    private const int MaxIntentos = 3;

    public static async Task<ConsoleSession?> ShowAsync(IAuthService authSvc)
    {
        string? mensajeError = null;

        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            AnsiConsole.Clear();
            RenderBanner();

            if (mensajeError is not null)
            {
                AnsiConsole.MarkupLine(
                    $"[red bold]⚠️  {Markup.Escape(mensajeError)}[/]  " +
                    $"[grey]({intento - 1}/{MaxIntentos} intento(s) fallido(s))[/]\n");
            }

            // ── Campos de entrada ─────────────────────────────────────────────
            var username = AnsiConsole.Prompt(
                new TextPrompt<string>("[deepskyblue1]  👤  Usuario :[/] ")
                    .PromptStyle("white bold"));

            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("[deepskyblue1]  🔑  Contraseña:[/] ")
                    .PromptStyle("white bold")
                    .Secret());

            // ── Autenticar ────────────────────────────────────────────────────
            Core.API.DTOs.Responses.AuthResponse? auth = null;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[grey]Verificando credenciales...[/]", async _ =>
                {
                    await Task.Delay(350); // feedback visual mínimo
                    auth = await authSvc.LoginAsync(username, password);
                });

            if (auth is not null)
            {
                RenderBienvenida(auth.Usuario);
                await Task.Delay(1400);

                return new ConsoleSession(
                    UsuarioId:      auth.Usuario.Id,
                    Username:       auth.Usuario.Username,
                    NombreCompleto: $"{auth.Usuario.Nombre} {auth.Usuario.Apellido}".Trim(),
                    Rol:            auth.Usuario.Rol,
                    NombreSucursal: auth.Usuario.NombreSucursal
                );
            }

            mensajeError = intento < MaxIntentos
                ? $"Credenciales incorrectas. Quedan {MaxIntentos - intento} intento(s)."
                : "Credenciales incorrectas.";
        }

        // ── Intentos agotados ─────────────────────────────────────────────────
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new Panel(
                "[red bold]Se agotaron los 3 intentos de acceso.\n" +
                "El acceso ha sido denegado.[/]\n\n" +
                "[grey]Si olvidaste tu contraseña, contacta al Administrador del sistema.[/]")
            .Header("[red]🔒 Acceso Denegado[/]")
            .Border(BoxBorder.Heavy)
            .BorderColor(Color.Red)
            .Padding(2, 1));

        await Task.Delay(2500);
        return null;
    }

    // ── Helpers visuales ──────────────────────────────────────────────────────

    private static void RenderBanner()
    {
        AnsiConsole.Write(
            new FigletText("RadiadoresSprings")
                .Centered()
                .Color(Color.DeepSkyBlue1));

        AnsiConsole.Write(
            new Rule("[bold grey]Core Management Console — P2[/]")
                .RuleStyle(Style.Parse("grey")));

        AnsiConsole.Write(
            new Panel("[grey]Sistema de gestión de Radiadores Springs.\nIdentifícate para continuar.[/]")
                .Header("[deepskyblue1]🔐 Iniciar Sesión[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.DeepSkyBlue1)
                .Padding(2, 1));

        AnsiConsole.WriteLine();
    }

    private static void RenderBienvenida(Core.API.DTOs.Responses.UsuarioResponse u)
    {
        AnsiConsole.Clear();

        var (rolColor, rolIcon) = u.Rol switch
        {
            "Administrador" => ("red",     "🛡️"),
            "Cajero"        => ("blue",    "🏦"),
            "Vendedor"      => ("green",   "🧾"),
            "Almacenista"   => ("yellow3", "📦"),
            _               => ("grey",    "👤")
        };

        var nombreCompleto = Markup.Escape($"{u.Nombre} {u.Apellido}".Trim());
        var acceso = Markup.Escape(PermisoConsola.DescripcionAcceso(u.Rol));

        AnsiConsole.Write(
            new Panel(
                $"[bold]¡Bienvenido, {nombreCompleto}![/]\n\n" +
                $"{rolIcon}  Rol:       [{rolColor} bold]{Markup.Escape(u.Rol)}[/]\n" +
                $"🏢  Sucursal:  [grey]{Markup.Escape(u.NombreSucursal)}[/]\n" +
                $"🔓  Acceso:    [italic]{acceso}[/]")
            .Header("[green bold]✅ Acceso Concedido[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(3, 1));
    }
}
