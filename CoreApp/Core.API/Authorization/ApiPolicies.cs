namespace Core.API.Authorization;

/// <summary>
/// Fuente única de verdad para las políticas de autorización del API.
///
/// Jerarquía de roles:
///   Administrador  — acceso total
///   Cajero         — operaciones de punto de venta y caja
///   Vendedor       — ventas, clientes y órdenes
///   Almacenista    — gestión de inventario
///   Cliente    — cuenta machine-to-machine para IntegrationApp (P3)
///
/// Flujo M2M (machine-to-machine):
///   1. IntegrationApp llama POST /api/v1/auth/login con las credenciales del
///      usuario "servicio_web" (rol Cliente).
///   2. Recibe un JWT firmado con claim role=Cliente.
///   3. Incluye el token en Authorization: Bearer <token> en todas sus llamadas.
///   4. Solo puede acceder a los endpoints marcados con las políticas que
///      incluyen el rol Cliente.
/// </summary>
public static class ApiPolicies
{
    // ── Nombres de política (constantes) ──────────────────────────────────────

    /// <summary>Cualquier usuario autenticado (todos los roles).</summary>
    public const string Autenticado = "Autenticado";

    /// <summary>Solo Administrador — operaciones críticas del sistema.</summary>
    public const string AdminSistema = "AdminSistema";

    /// <summary>Creación y edición de productos y categorías.</summary>
    public const string GestionInventario = "GestionInventario";

    /// <summary>
    /// Ajuste de stock — incluye Cliente para sincronización offline
    /// (IntegrationApp aplica transacciones acumuladas offline).
    /// </summary>
    public const string SincronizacionOffline = "SincronizacionOffline";

    /// <summary>Crear y consultar ventas.</summary>
    public const string GestionVentas = "GestionVentas";

    /// <summary>Cancelar ventas (operación destructiva).</summary>
    public const string CancelarVentas = "CancelarVentas";

    /// <summary>Apertura y cierre de sesiones de caja.</summary>
    public const string GestionCaja = "GestionCaja";

    /// <summary>Crear y gestionar órdenes (incluye Cliente para e-commerce).</summary>
    public const string GestionOrdenes = "GestionOrdenes";

    /// <summary>Crear y actualizar clientes (incluye Cliente).</summary>
    public const string GestionClientes = "GestionClientes";

    /// <summary>Registrar pagos (incluye Cliente para cobros online).</summary>
    public const string GestionPagos = "GestionPagos";

    // ── Grupos de roles por política ──────────────────────────────────────────
    // Usados en AddAuthorization para construir cada policy.

    public static readonly string[] Roles_Autenticado =
        ["Administrador", "Cajero", "Vendedor", "Almacenista", "Cliente"];

    public static readonly string[] Roles_AdminSistema =
        ["Administrador"];

    public static readonly string[] Roles_GestionInventario =
        ["Administrador", "Almacenista"];

    public static readonly string[] Roles_SincronizacionOffline =
        ["Administrador", "Almacenista", "Cliente"];

    public static readonly string[] Roles_GestionVentas =
        ["Administrador", "Cajero", "Vendedor", "Cliente"];

    public static readonly string[] Roles_CancelarVentas =
        ["Administrador"];

    public static readonly string[] Roles_GestionCaja =
        ["Administrador", "Cajero"];

    public static readonly string[] Roles_GestionOrdenes =
        ["Administrador", "Vendedor", "Cliente"];

    public static readonly string[] Roles_GestionClientes =
        ["Administrador", "Vendedor", "Cliente"];

    public static readonly string[] Roles_GestionPagos =
        ["Administrador", "Cajero", "Cliente"];
}
