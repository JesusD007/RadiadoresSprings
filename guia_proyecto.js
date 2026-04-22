const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
  ShadingType, VerticalAlign, PageNumber, LevelFormat,
} = require('/sessions/admiring-sharp-brown/npm-global/lib/node_modules/docx');
const fs = require('fs');

// ── Colores ──────────────────────────────────────────────────────────────────
const AZUL       = "1F4E79";
const AZUL_MED   = "2E75B6";
const AZUL_CLARO = "DEEAF1";
const GRIS_CLARO = "F2F2F2";
const VERDE      = "375623";
const VERDE_CLR  = "E2EFDA";
const NARANJA    = "843C0C";
const NARANJA_CLR= "FCE4D6";
const MORADO     = "44336A";
const MORADO_CLR = "EAE4F0";
const BLANCO     = "FFFFFF";

// ── Helpers ───────────────────────────────────────────────────────────────────
function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 400, after: 200 },
    shading: { fill: AZUL, type: ShadingType.CLEAR },
    indent: { left: 200, right: 200 },
    children: [new TextRun({ text, bold: true, size: 36, color: BLANCO, font: "Arial" })],
  });
}

function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 300, after: 120 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: AZUL_MED } },
    children: [new TextRun({ text, bold: true, size: 28, color: AZUL, font: "Arial" })],
  });
}

function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 80 },
    children: [new TextRun({ text, bold: true, size: 24, color: AZUL_MED, font: "Arial" })],
  });
}

function p(text) {
  return new Paragraph({
    spacing: { before: 80, after: 80 },
    children: [new TextRun({ text, size: 22, font: "Arial" })],
  });
}

function bullet(text) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { before: 40, after: 40 },
    children: [new TextRun({ text, size: 22, font: "Arial" })],
  });
}

function infoBox(title, text, bgColor, lineColor) {
  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: [9360],
    rows: [new TableRow({ children: [new TableCell({
      width: { size: 9360, type: WidthType.DXA },
      shading: { fill: bgColor || AZUL_CLARO, type: ShadingType.CLEAR },
      margins: { top: 120, bottom: 120, left: 200, right: 200 },
      borders: {
        top:    { style: BorderStyle.SINGLE, size: 6, color: lineColor || AZUL_MED },
        bottom: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
        left:   { style: BorderStyle.SINGLE, size: 14, color: lineColor || AZUL_MED },
        right:  { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
      },
      children: [
        new Paragraph({ spacing:{before:0,after:60}, children:[new TextRun({ text: title, bold:true, size:22, font:"Arial", color: lineColor||AZUL_MED })] }),
        new Paragraph({ spacing:{before:0,after:0},  children:[new TextRun({ text, size:21, font:"Arial" })] }),
      ],
    })] })],
  });
}

function codeBlock(lines) {
  const rows = lines.map(line =>
    new TableRow({ children: [new TableCell({
      width: { size: 9360, type: WidthType.DXA },
      shading: { fill: "1E1E1E", type: ShadingType.CLEAR },
      margins: { top: 0, bottom: 0, left: 160, right: 160 },
      borders: {
        top:   { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
        bottom:{ style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
        left:  { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
        right: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
      },
      children: [new Paragraph({ spacing:{before:20,after:20}, children:[
        new TextRun({ text: line, font:"Courier New", size:18, color:"D4D4D4" })
      ]})],
    })] })
  );
  return new Table({ width:{size:9360,type:WidthType.DXA}, columnWidths:[9360], rows });
}

function sp() {
  return new Paragraph({ spacing:{before:0,after:0}, children:[new TextRun({text:""})] });
}

function twoColTable(leftTitle, rightTitle, rows) {
  const border = { style: BorderStyle.SINGLE, size: 4, color: "CCCCCC" };
  const headerRow = new TableRow({ tableHeader: true, children: [
    new TableCell({
      width:{size:3600,type:WidthType.DXA},
      shading:{fill:AZUL,type:ShadingType.CLEAR},
      margins:{top:100,bottom:100,left:140,right:140},
      borders:{top:border,bottom:border,left:border,right:border},
      children:[new Paragraph({children:[new TextRun({text:leftTitle,bold:true,size:22,font:"Arial",color:BLANCO})]})]
    }),
    new TableCell({
      width:{size:5760,type:WidthType.DXA},
      shading:{fill:AZUL,type:ShadingType.CLEAR},
      margins:{top:100,bottom:100,left:140,right:140},
      borders:{top:border,bottom:border,left:border,right:border},
      children:[new Paragraph({children:[new TextRun({text:rightTitle,bold:true,size:22,font:"Arial",color:BLANCO})]})]
    }),
  ]});
  const dataRows = rows.map(([l,r], i) => new TableRow({ children:[
    new TableCell({
      width:{size:3600,type:WidthType.DXA},
      shading:{fill: i%2===0 ? GRIS_CLARO : BLANCO, type:ShadingType.CLEAR},
      margins:{top:80,bottom:80,left:140,right:140},
      borders:{top:border,bottom:border,left:border,right:border},
      children:[new Paragraph({children:[new TextRun({text:l,size:21,font:"Arial",bold:true,color:AZUL})]})]
    }),
    new TableCell({
      width:{size:5760,type:WidthType.DXA},
      shading:{fill: i%2===0 ? GRIS_CLARO : BLANCO, type:ShadingType.CLEAR},
      margins:{top:80,bottom:80,left:140,right:140},
      borders:{top:border,bottom:border,left:border,right:border},
      children:[new Paragraph({children:[new TextRun({text:r,size:21,font:"Arial"})]})]
    }),
  ]}));
  return new Table({ width:{size:9360,type:WidthType.DXA}, columnWidths:[3600,5760], rows:[headerRow,...dataRows] });
}

// ══════════════════════════════════════════════════════════════════════════════
// DOCUMENTO
// ══════════════════════════════════════════════════════════════════════════════
const doc = new Document({
  numbering: {
    config: [
      { reference:"bullets", levels:[
        { level:0, format:LevelFormat.BULLET, text:"•", alignment:AlignmentType.LEFT,
          style:{paragraph:{indent:{left:720,hanging:360}}} },
        { level:1, format:LevelFormat.BULLET, text:"-", alignment:AlignmentType.LEFT,
          style:{paragraph:{indent:{left:1080,hanging:360}}} },
      ]},
    ]
  },
  styles: {
    default: { document:{ run:{ font:"Arial", size:22 } } },
    paragraphStyles: [
      { id:"Heading1", name:"Heading 1", basedOn:"Normal", next:"Normal", quickFormat:true,
        run:{ size:36, bold:true, font:"Arial", color:BLANCO },
        paragraph:{ spacing:{before:400,after:200}, outlineLevel:0 } },
      { id:"Heading2", name:"Heading 2", basedOn:"Normal", next:"Normal", quickFormat:true,
        run:{ size:28, bold:true, font:"Arial", color:AZUL },
        paragraph:{ spacing:{before:300,after:120}, outlineLevel:1 } },
      { id:"Heading3", name:"Heading 3", basedOn:"Normal", next:"Normal", quickFormat:true,
        run:{ size:24, bold:true, font:"Arial", color:AZUL_MED },
        paragraph:{ spacing:{before:200,after:80}, outlineLevel:2 } },
    ]
  },
  sections: [{
    properties: {
      page: { size:{ width:12240, height:15840 }, margin:{top:1080,right:1080,bottom:1080,left:1080} }
    },
    headers: {
      default: new Header({ children:[
        new Paragraph({
          border:{ bottom:{style:BorderStyle.SINGLE,size:4,color:AZUL_MED} },
          children:[
            new TextRun({ text:"RadiadoresSprings – IntegrationApp  |  Guia de Codigo", bold:true, font:"Arial", size:20, color:AZUL }),
          ],
        })
      ]}),
    },
    footers: {
      default: new Footer({ children:[
        new Paragraph({
          alignment:AlignmentType.CENTER,
          border:{ top:{style:BorderStyle.SINGLE,size:4,color:AZUL_MED} },
          children:[
            new TextRun({ text:"Pagina ", font:"Arial", size:18, color:"666666" }),
            new TextRun({ children:[PageNumber.CURRENT], font:"Arial", size:18, color:AZUL_MED }),
            new TextRun({ text:" de ", font:"Arial", size:18, color:"666666" }),
            new TextRun({ children:[PageNumber.TOTAL_PAGES], font:"Arial", size:18, color:AZUL_MED }),
          ],
        })
      ]}),
    },
    children: [

      // ═════════════════════════════
      // PORTADA
      // ═════════════════════════════
      new Paragraph({ spacing:{before:600,after:0}, alignment:AlignmentType.CENTER,
        children:[new TextRun({text:"RADIADORESSPRINGS", bold:true, size:72, font:"Arial", color:AZUL})] }),
      new Paragraph({ spacing:{before:40,after:0}, alignment:AlignmentType.CENTER,
        children:[new TextRun({text:"IntegrationApp", bold:true, size:52, font:"Arial", color:AZUL_MED})] }),
      sp(),
      new Paragraph({ alignment:AlignmentType.CENTER,
        children:[new TextRun({text:"GUIA COMPLETA DE CODIGO", bold:true, size:36, font:"Arial", color:NARANJA})] }),
      new Paragraph({ alignment:AlignmentType.CENTER,
        children:[new TextRun({text:"Para Desarrolladores Junior", size:28, font:"Arial", color:"666666"})] }),
      sp(), sp(),
      new Table({
        width:{size:9360,type:WidthType.DXA}, columnWidths:[9360],
        rows:[new TableRow({children:[new TableCell({
          width:{size:9360,type:WidthType.DXA},
          shading:{fill:AZUL_CLARO,type:ShadingType.CLEAR},
          margins:{top:200,bottom:200,left:300,right:300},
          borders:{
            top:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            bottom:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            left:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            right:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
          },
          children:[
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Que encontraras en esta guia?", bold:true, size:26, font:"Arial", color:AZUL})]}),
            sp(),
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Explicacion paso a paso de cada archivo, patron y decision de arquitectura.", size:22, font:"Arial"})]}),
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Escrita en lenguaje claro para que cualquier desarrollador pueda entenderla.", size:22, font:"Arial"})]}),
          ],
        })]})],
      }),
      sp(), sp(),
      new Paragraph({ alignment:AlignmentType.CENTER,
        children:[new TextRun({text:".NET 9  |  PostgreSQL  |  NServiceBus  |  Polly  |  SignalR  |  JWT  |  Serilog", size:20, font:"Arial", color:AZUL_MED, italics:true})] }),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 1 – QUE ES ESTE PROYECTO
      // ═════════════════════════════
      h1("1. Que es IntegrationApp y por que existe?"),
      sp(),
      p("Imagina que tienes una tienda de radiadores con varias sucursales. Cada sucursal tiene una caja POS (Point of Sale) que los cajeros usan para registrar ventas. Ademas, hay un sitio web donde los clientes pueden hacer pedidos. Todo esto debe comunicarse con un servidor central (el Core API) que guarda los datos reales."),
      sp(),
      p("El problema es: que pasa si el Core API se cae o hay problemas de red? Las ventas no pueden detenerse. Ahi es donde entra IntegrationApp."),
      sp(),
      infoBox("Rol de IntegrationApp",
        "IntegrationApp actua como un intermediario inteligente (API Gateway) entre las cajas POS, el sitio web y el Core API. Su trabajo es: recibir peticiones, validarlas, reenviarlas al Core, y si el Core no esta disponible, seguir funcionando en modo offline hasta que se restablezca la conexion.",
        AZUL_CLARO, AZUL_MED),
      sp(),
      p("La arquitectura del sistema tiene 4 partes:"),
      sp(),
      twoColTable("Componente", "Descripcion", [
        ["P1 - Caja POS",        "Punto de venta fisico en cada sucursal. Usa IntegrationApp como su API."],
        ["P2 - Core API",        "Servidor central con la logica de negocio y base de datos principal."],
        ["P3 - IntegrationApp",  "ESTE proyecto. Intermediario entre P1/P4 y P2. Es el foco de esta guia."],
        ["P4 - Sitio Web",       "Portal web de clientes para hacer pedidos y rastrear ordenes."],
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 2 – ESTRUCTURA DE CARPETAS
      // ═════════════════════════════
      h1("2. Estructura de Carpetas - El Mapa del Proyecto"),
      sp(),
      p("Una buena estructura de carpetas es como un buen mapa: te dice donde esta todo sin necesidad de buscar. Aqui vemos que hace cada carpeta:"),
      sp(),
      twoColTable("Carpeta", "Que contiene y para que sirve?", [
        ["Controllers/",        "Los endpoints REST de la API. Son la puerta de entrada. Reciben peticiones HTTP."],
        ["Services/",           "La logica de negocio: cliente HTTP para el Core y estado del Circuit Breaker."],
        ["Domain/Entities/",    "Clases que representan las tablas de la base de datos (modelos de datos)."],
        ["Data/",               "El DbContext de Entity Framework. Puente entre C# y PostgreSQL."],
        ["Contracts/",          "DTOs (Data Transfer Objects): clases de Request y Response que viajan por la red."],
        ["Middleware/",          "Codigo que se ejecuta en CADA peticion: IDs de correlacion, logs de auditoria."],
        ["BackgroundServices/",  "Tareas en segundo plano: verificar Core, sincronizar productos mirror."],
        ["Handlers/",            "Manejadores de eventos NServiceBus. Reaccionan a mensajes del bus."],
        ["Sagas/",               "Orquestadores de procesos distribuidos complejos (ej: ventas offline)."],
        ["Hubs/",                "Hubs de SignalR para comunicacion en tiempo real via WebSockets."],
        ["Validators/",          "Reglas de validacion de datos con FluentValidation."],
        ["Messages/",            "Definicion de comandos y eventos del bus de mensajes NServiceBus."],
        ["Migrations/",          "Historial de cambios de estructura de la base de datos."],
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 3 – PROGRAM.CS
      // ═════════════════════════════
      h1("3. Program.cs - El Corazon de la Aplicacion"),
      sp(),
      p("Program.cs es el primer archivo que se ejecuta cuando arranca la aplicacion. Es como el plan de obra de un edificio: aqui se registran todos los servicios y se configura como funciona todo."),
      sp(),
      infoBox("Concepto Clave: Dependency Injection (DI)",
        "En .NET, en lugar de crear objetos manualmente (new MiServicio()), los registramos en un contenedor central. Cuando un controlador necesita un servicio, el framework se lo inyecta automaticamente. Esto facilita las pruebas y desacopla el codigo. Todo se configura en Program.cs.",
        VERDE_CLR, VERDE),
      sp(),
      h2("3.1 Configuracion de la Base de Datos"),
      codeBlock([
        "// Program.cs",
        "builder.Services.AddDbContext<IntegrationDbContext>(options =>",
        "    options.UseNpgsql(",
        "        builder.Configuration.GetConnectionString(\"IntegrationDb\")));",
      ]),
      sp(),
      p("Esta linea registra el DbContext en el contenedor de DI. Cuando un controlador o servicio pida IntegrationDbContext, el framework lo creara y lo conectara a PostgreSQL usando la cadena de conexion del archivo appsettings.json."),
      sp(),
      h2("3.2 HTTP Client con Resilencia (Polly v8)"),
      codeBlock([
        "builder.Services.AddHttpClient<ICoreApiClient, CoreApiClient>(client => {",
        "    client.BaseAddress = new Uri(coreApiBaseUrl);",
        "    client.Timeout = TimeSpan.FromSeconds(30);",
        "})",
        ".AddStandardResilienceHandler();  // Polly v8: retry + circuit breaker + timeout",
      ]),
      sp(),
      p("Se crea un cliente HTTP especial hacia el Core API. AddStandardResilienceHandler() agrega automaticamente: reintentos con backoff exponencial, circuit breaker, y timeout. Polly v8 es la libreria de resiliencia estandar en .NET."),
      sp(),
      h2("3.3 Autenticacion JWT"),
      codeBlock([
        "builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)",
        "    .AddJwtBearer(options => {",
        "        options.TokenValidationParameters = new TokenValidationParameters {",
        "            ValidateIssuer = true,          // Verifica quien emitio el token",
        "            ValidateAudience = true,         // Verifica para quien es el token",
        "            ValidateLifetime = true,         // Verifica que no este expirado",
        "            ValidateIssuerSigningKey = true, // Verifica la firma digital",
        "        };",
        "    });",
      ]),
      sp(),
      infoBox("Que es JWT?",
        "JSON Web Token es un estandar para transmitir informacion de forma segura. Cuando un usuario hace login, el Core API genera un token JWT firmado digitalmente. Este token se incluye en todas las peticiones posteriores como Header: Authorization: Bearer {token}. IntegrationApp lo valida sin consultar la BD en cada peticion.",
        MORADO_CLR, MORADO),
      sp(),
      h2("3.4 Auto-migracion en Startup"),
      codeBlock([
        "// Se ejecuta al arrancar la aplicacion",
        "using (var scope = app.Services.CreateScope()) {",
        "    var db = scope.ServiceProvider",
        "        .GetRequiredService<IntegrationDbContext>();",
        "    db.Database.Migrate(); // Aplica migraciones pendientes automaticamente",
        "}",
      ]),
      sp(),
      p("Garantiza que la base de datos siempre este actualizada al arrancar. Si hay migraciones nuevas (cambios en el esquema), las aplica automaticamente. Es especialmente util en contenedores Docker."),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 4 – BASE DE DATOS
      // ═════════════════════════════
      h1("4. Base de Datos - Entity Framework Core con PostgreSQL"),
      sp(),
      p("Entity Framework Core (EF Core) es un ORM (Object-Relational Mapper). Te permite trabajar con la base de datos usando objetos C# en lugar de escribir SQL directamente. Cada clase C# representa una tabla, y cada propiedad representa una columna."),
      sp(),
      h2("4.1 Las 4 Tablas Principales"),
      twoColTable("Tabla (DbSet)", "Para que sirve?", [
        ["ProductosMirror",         "Copia local del catalogo del Core. Se usa en modo offline para que los cajeros puedan seguir viendo productos aunque el Core este caido."],
        ["IntegrationLogs",         "Registro de auditoria de TODAS las peticiones. Util para debugging y monitoreo de transacciones."],
        ["IdempotencyLogs",         "Previene que una misma venta se procese dos veces. Indice UNIQUE en IdTransaccionLocal garantiza esto."],
        ["VentasOfflinePendientes",  "Ventas guardadas cuando el Core no esta disponible. Se sincronizan cuando vuelve la conexion."],
      ]),
      sp(),
      h2("4.2 La Entidad Mas Importante: VentaOfflinePendiente"),
      codeBlock([
        "public class VentaOfflinePendiente {",
        "    public int Id { get; set; }",
        "    // GUID unico, previene duplicados (indice UNIQUE en BD)",
        "    public string IdTransaccionLocal { get; set; }",
        "    public string CajeroId { get; set; }",
        "    public string SucursalId { get; set; }",
        "    // Solo puede ser: Efectivo | Tarjeta | Transferencia",
        "    public string MetodoPago { get; set; }",
        "    public decimal MontoTotal { get; set; }",
        "    // Los productos vendidos se guardan como JSON serializado",
        "    public string LineasJson { get; set; }",
        "    // Estados: Pendiente -> EnCola -> Sincronizada | Rechazada",
        "    public string Estado { get; set; }",
        "    // Cuantas veces se intento sincronizar con el Core",
        "    public int IntentosSync { get; set; }",
        "}",
      ]),
      sp(),
      infoBox("Que es Idempotencia?",
        "Una operacion es idempotente si hacerla varias veces produce el mismo resultado que hacerla una sola vez. En ventas: si por un error de red se envia la misma venta dos veces, el sistema debe procesarla solo una vez. El IdempotencyLog con su indice UNIQUE garantiza esto: si llega la misma IdTransaccionLocal, ya sabe que se proceso y devuelve la respuesta guardada sin volver a procesar.",
        VERDE_CLR, VERDE),
      sp(),
      h2("4.3 Migraciones de Entity Framework"),
      codeBlock([
        "// Comandos de migraciones (se ejecutan en la terminal del proyecto):",
        "dotnet ef migrations add NombreCambio  // Crear archivo de migracion",
        "dotnet ef database update              // Aplicar migraciones a la BD",
        "dotnet ef migrations list              // Ver historial de migraciones",
      ]),
      sp(),
      p("Las migraciones son como un historial de cambios de la base de datos. Cuando modificas una entidad (ej: agregar una columna), generas una migracion que contiene el SQL necesario. EF Core los aplica en orden, por eso NUNCA debes borrar archivos de migracion."),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 5 – CONTROLADORES
      // ═════════════════════════════
      h1("5. Controladores - Los Endpoints REST"),
      sp(),
      p("Los controladores son las puertas de entrada de la API. Cada metodo publico con un atributo HTTP ([HttpGet], [HttpPost], etc.) se convierte en un endpoint que puedes llamar con una peticion HTTP."),
      sp(),
      infoBox("Patron General de los Controladores",
        "Casi todos siguen el mismo patron: 1) Verificar si el Core esta disponible usando CircuitBreakerStateService. 2) Si esta disponible, reenviar (proxear) la peticion al Core API. 3) Si no esta disponible (modo offline), usar datos locales o guardar la operacion para despues.",
        AZUL_CLARO, AZUL_MED),
      sp(),
      h2("5.1 VentasController.cs - El Mas Importante"),
      codeBlock([
        "[HttpPost]",
        "[Authorize]  // Requiere token JWT valido",
        "public async Task<IActionResult> CrearVenta(",
        "    [FromBody] CrearVentaRequest request,",
        "    // Header obligatorio: ID unico de la transaccion",
        "    [FromHeader(Name = \"Idempotency-Key\")] string idempotencyKey,",
        "    CancellationToken ct) {",
        "",
        "    // 1. Verificar si ya se proceso antes (prevenir duplicados)",
        "    var existente = await _db.IdempotencyLogs",
        "        .FirstOrDefaultAsync(x => x.IdTransaccionLocal == idempotencyKey, ct);",
        "    if (existente != null) return Ok(existente); // Ya procesada",
        "",
        "    if (_circuitBreaker.CoreAvailable) {",
        "        // MODO ONLINE: reenviar al Core API",
        "        var response = await _coreClient.PostAsync(",
        "            \"/api/ventas\", request, bearerToken: token,",
        "            idempotencyKey: idempotencyKey, ct: ct);",
        "        // Guardar en IdempotencyLog para futuras verificaciones",
        "        // ...",
        "    } else {",
        "        // MODO OFFLINE: guardar localmente para sincronizar despues",
        "        var ventaPendiente = new VentaOfflinePendiente { ... };",
        "        _db.VentasOfflinePendientes.Add(ventaPendiente);",
        "        await _db.SaveChangesAsync(ct);",
        "        // Publicar evento en NServiceBus para sincronizacion posterior",
        "        await _bus.Publish(new VentaRealizadaOfflineMessage { ... });",
        "        return Accepted(new { Mensaje = \"Venta guardada en modo offline\" });",
        "    }",
        "}",
      ]),
      sp(),
      h2("5.2 ProductosController.cs - Fallback al Mirror"),
      codeBlock([
        "[HttpGet]",
        "public async Task<IActionResult> GetProductos(...) {",
        "    if (_circuitBreaker.CoreAvailable) {",
        "        // ONLINE: pedir productos al Core en tiempo real",
        "        var response = await _coreClient.GetAsync(\"/api/productos?...\", ct: ct);",
        "        return Ok(response);",
        "    }",
        "    // OFFLINE: servir desde la copia local (ProductosMirror)",
        "    var productos = await _db.ProductosMirror",
        "        .Where(p => p.EsActivo)",
        "        .Skip((page - 1) * pageSize).Take(pageSize)",
        "        .ToListAsync(ct);",
        "    // Marcar en el header que viene del mirror local",
        "    Response.Headers[\"X-From-Mirror\"] = \"true\";",
        "    return Ok(new { Items = productos, FromMirror = true });",
        "}",
      ]),
      sp(),
      h2("5.3 OrdenesController.cs - Patron Asincrono (202 Accepted)"),
      codeBlock([
        "[HttpPost]",
        "public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenRequest req) {",
        "    var ordenId = Guid.NewGuid().ToString();",
        "    // URL donde el cliente puede verificar el estado de su orden",
        "    var pollUrl = $\"/api/v1/ordenes/{ordenId}/estado\";",
        "    // Devolver INMEDIATAMENTE 202 (Aceptado, procesando...)",
        "    return Accepted(pollUrl, new { OrdenId = ordenId, PollUrl = pollUrl });",
        "}",
        "",
        "[HttpGet(\"{id}/estado\")]",
        "[AllowAnonymous]  // Sin autenticacion: cualquiera puede rastrear su orden",
        "public async Task<IActionResult> GetEstadoOrden(string id) { ... }",
      ]),
      sp(),
      infoBox("Por que 202 Accepted?",
        "Crear una orden puede tomar tiempo (verificar inventario, calcular costos, coordinar entrega). En lugar de hacer esperar al cliente con una conexion abierta, devolvemos 202 inmediatamente con una URL de polling. El cliente visita esa URL cada pocos segundos para ver si la orden se proceso. Esto mejora la experiencia y es mas resiliente ante problemas de red.",
        NARANJA_CLR, NARANJA),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 6 – SERVICIOS
      // ═════════════════════════════
      h1("6. Servicios - La Logica Central"),
      sp(),
      h2("6.1 CoreApiClient.cs - Hablar con el Core"),
      p("Este servicio es el UNICO responsable de hacer peticiones HTTP al Core API. Centralizar esto tiene ventajas: si cambia la URL del Core, solo hay que cambiarla en un sitio; si quieres agregar headers a todas las peticiones, lo haces una vez."),
      sp(),
      codeBlock([
        "public interface ICoreApiClient {",
        "    // GET: obtener datos del Core",
        "    Task<HttpResponseMessage> GetAsync(string path, string? bearerToken = null, ...);",
        "",
        "    // POST: crear recursos (soporta Idempotency-Key para prevenir duplicados)",
        "    Task<HttpResponseMessage> PostAsync(string path, object body,",
        "        string? bearerToken = null, string? idempotencyKey = null, ...);",
        "",
        "    // PUT: actualizar recursos completos",
        "    Task<HttpResponseMessage> PutAsync(string path, object body, ...);",
        "",
        "    // DELETE: eliminar recursos",
        "    Task<HttpResponseMessage> DeleteAsync(string path, ...);",
        "}",
      ]),
      sp(),
      h2("6.2 CircuitBreakerStateService.cs - Esta Vivo el Core?"),
      codeBlock([
        "public interface ICircuitBreakerStateService {",
        "    bool CoreAvailable { get; }         // Esta el Core disponible ahora?",
        "    DateTimeOffset? UltimaSync { get; } // Cuando fue la ultima sincronizacion?",
        "    void MarkCoreAvailable();    // CoreHealthCheckService llama esto cuando Core responde",
        "    void MarkCoreUnavailable();  // CoreHealthCheckService llama esto cuando Core falla",
        "    void UpdateLastSync();       // MirrorSyncService llama esto al sincronizar",
        "}",
        "",
        "// Internamente usa 'volatile' para garantizar acceso thread-safe",
        "// volatile = todos los hilos ven el mismo valor actualizado",
        "private volatile bool _coreAvailable = true;",
      ]),
      sp(),
      infoBox("Por que volatile?",
        "La palabra clave 'volatile' garantiza que cuando un hilo (thread) actualiza el valor, todos los demas hilos ven el valor actualizado inmediatamente. Sin esto, por optimizaciones del compilador, un hilo podria ver un valor cacheado y no el actual. Como CoreAvailable es leido por muchos hilos al mismo tiempo (una peticion por hilo), necesitamos que todos vean el mismo valor.",
        MORADO_CLR, MORADO),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 7 – CIRCUIT BREAKER
      // ═════════════════════════════
      h1("7. Circuit Breaker Pattern - Resiliencia ante Fallos"),
      sp(),
      p("El Circuit Breaker (interruptor de circuito) previene que un sistema siga intentando una operacion que casi con certeza fallara, dando tiempo al sistema remoto para recuperarse. Piensalo como el interruptor electrico de tu casa: si hay un cortocircuito, el interruptor se dispara. No tiene sentido seguir enviando electricidad."),
      sp(),
      twoColTable("Estado", "Que significa?", [
        ["CLOSED (Verde - Normal)",   "Todo funciona. Las peticiones pasan al Core normalmente. Estado saludable."],
        ["OPEN (Rojo - Fallo)",       "El Core fallo 3+ veces seguidas. Las peticiones NO van al Core. IntegrationApp opera en modo offline."],
        ["HALF-OPEN (Amarillo)",      "Despues del tiempo de espera, se deja pasar UNA peticion de prueba. Exito = vuelve a CLOSED. Fallo = vuelve a OPEN."],
      ]),
      sp(),
      h2("7.1 Dos Niveles Complementarios"),
      h3("Nivel 1: Polly v8 (automatico, a nivel HTTP)"),
      p("Opera de forma transparente en el HttpClient. Maneja reintentos con backoff exponencial y circuit breaker a nivel de peticion individual. Lanza BrokenCircuitException cuando el circuito esta abierto."),
      sp(),
      h3("Nivel 2: CircuitBreakerStateService (personalizado, a nivel negocio)"),
      p("Este es el estado 'manual' que los controladores consultan ANTES de intentar la peticion. Es actualizado por CoreHealthCheckService que hace pings periodicos. Permite tomar decisiones de negocio (sirvo del mirror? rechazo?) sin ni siquiera intentar una peticion HTTP."),
      sp(),
      codeBlock([
        "// Flujo en un controlador:",
        "",
        "// Paso 1: Verificar estado ANTES de hacer la peticion",
        "if (_circuitBreaker.CoreAvailable) {",
        "    try {",
        "        // Paso 2: Polly maneja reintentos automaticamente",
        "        var resultado = await _coreClient.GetAsync(\"/api/...\");",
        "        return Ok(resultado);",
        "    }",
        "    catch (BrokenCircuitException) {",
        "        // Paso 3: Polly abrio el circuito, marcar como no disponible",
        "        _circuitBreaker.MarkCoreUnavailable();",
        "        goto modoOffline;",
        "    }",
        "} else {",
        "    modoOffline:",
        "    // Usar datos locales del mirror",
        "}",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 8 – BACKGROUND SERVICES
      // ═════════════════════════════
      h1("8. Background Services - Tareas en Segundo Plano"),
      sp(),
      p("Los Background Services corren en paralelo a la aplicacion principal, sin bloquear las peticiones HTTP. Se inician cuando arranca la app y terminan cuando se apaga. Se registran en Program.cs con AddHostedService<T>()."),
      sp(),
      h2("8.1 CoreHealthCheckService.cs - El Vigilante"),
      codeBlock([
        "public class CoreHealthCheckService : BackgroundService {",
        "    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {",
        "        int fallosConsecutivos = 0;",
        "        while (!stoppingToken.IsCancellationRequested) {",
        "            try {",
        "                var response = await _coreClient.GetAsync(\"/health\", ct: stoppingToken);",
        "                if (response.IsSuccessStatusCode) {",
        "                    fallosConsecutivos = 0;",
        "                    _circuitBreaker.MarkCoreAvailable(); // Core vivo",
        "                } else {",
        "                    fallosConsecutivos++;",
        "                }",
        "            } catch {",
        "                fallosConsecutivos++;",
        "            }",
        "            // 3 fallos seguidos -> abrir el circuit breaker",
        "            if (fallosConsecutivos >= 3) {",
        "                _circuitBreaker.MarkCoreUnavailable();",
        "            }",
        "            // Esperar 15 segundos antes del proximo ping",
        "            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);",
        "        }",
        "    }",
        "}",
      ]),
      sp(),
      h2("8.2 MirrorSyncService.cs - El Sincronizador de Productos"),
      codeBlock([
        "protected override async Task ExecuteAsync(CancellationToken stoppingToken) {",
        "    while (!stoppingToken.IsCancellationRequested) {",
        "        // Solo sincronizar si el Core esta disponible",
        "        if (_circuitBreaker.CoreAvailable) {",
        "            int pagina = 1;",
        "            while (true) {",
        "                // Obtener 100 productos por pagina (no saturar la red)",
        "                var prods = await _coreClient.GetAsync(",
        "                    $\"/api/productos?page={pagina}&size=100\", ct: stoppingToken);",
        "                if (prods == null || !prods.Any()) break;",
        "",
        "                // Upsert: insertar nuevos, actualizar existentes",
        "                foreach (var prod in prods) {",
        "                    var existente = await _db.ProductosMirror.FindAsync(prod.Id);",
        "                    if (existente == null) _db.ProductosMirror.Add(prod.ToMirror());",
        "                    else prod.ActualizarMirror(existente);",
        "                }",
        "                await _db.SaveChangesAsync(stoppingToken);",
        "                pagina++;",
        "            }",
        "            _circuitBreaker.UpdateLastSync();",
        "        }",
        "        // Esperar 5 minutos antes de la proxima sincronizacion",
        "        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);",
        "    }",
        "}",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 9 – NSERVICEBUS
      // ═════════════════════════════
      h1("9. NServiceBus - Mensajeria Distribuida"),
      sp(),
      p("NServiceBus es una plataforma de mensajeria que permite que diferentes partes del sistema se comuniquen de forma asincrona y resiliente. En lugar de llamadas HTTP directas, se publican mensajes en un bus que los entrega aunque el receptor este temporalmente no disponible."),
      sp(),
      infoBox("Por que mensajeria en lugar de HTTP directo?",
        "Con HTTP: si el Core no responde, la peticion falla y se pierde. Con mensajeria: el mensaje se guarda en una cola y se entrega cuando el Core vuelva a estar disponible. Esto es fundamental para operaciones que no pueden perderse, como registrar una venta. En desarrollo se usan archivos locales (.learningtransport/), en produccion se usa RabbitMQ.",
        VERDE_CLR, VERDE),
      sp(),
      h2("9.1 Comandos vs. Eventos"),
      twoColTable("Tipo", "Como funciona?", [
        ["Comando (IMessage/ICommand)", "Instruccion dirigida a UN destinatario especifico. Solo hay un handler. Ej: 'Aplica esta venta en el Core'."],
        ["Evento (IEvent)",            "Notificacion de algo que YA ocurrio. MULTIPLES suscriptores pueden reaccionar. Ej: 'El inventario se actualizo'."],
      ]),
      sp(),
      codeBlock([
        "// VentaRealizadaOfflineMessage (Comando/Mensaje)",
        "public class VentaRealizadaOfflineMessage : IMessage {",
        "    public string IdTransaccionLocal { get; set; } // GUID unico de la venta",
        "    public string CajeroId { get; set; }",
        "    public string SucursalId { get; set; }",
        "    public decimal MontoTotal { get; set; }",
        "    public string LineasJson { get; set; }  // Productos vendidos",
        "}",
        "",
        "// InventarioActualizadoEvent (Evento del Core)",
        "public class InventarioActualizadoEvent : IEvent {",
        "    public int ProductoId { get; set; }",
        "    public int NuevoStock { get; set; }",
        "    public string Motivo { get; set; }  // Venta | Reposicion | Ajuste",
        "}",
      ]),
      sp(),
      h2("9.2 Handler - Reaccionando a Eventos del Core"),
      codeBlock([
        "// InventarioActualizadoHandler.cs",
        "public class InventarioActualizadoHandler",
        "    : IHandleMessages<InventarioActualizadoEvent> {",
        "",
        "    public async Task Handle(InventarioActualizadoEvent message,",
        "        IMessageHandlerContext context) {",
        "        // El Core actualizo el inventario -> actualizar el mirror local",
        "        var producto = await _db.ProductosMirror",
        "            .FindAsync(message.ProductoId);",
        "",
        "        if (producto != null) {",
        "            producto.Stock = message.NuevoStock;",
        "            producto.UltimaSync = DateTime.UtcNow;",
        "            await _db.SaveChangesAsync();",
        "        }",
        "    }",
        "}",
      ]),
      sp(),
      h2("9.3 Saga - El Orquestador de Ventas Offline"),
      p("La SyncOfflineSaga es el proceso mas complejo del sistema. Orquesta la sincronizacion de una venta offline con el Core, incluyendo reintentos con backoff exponencial si el Core no responde."),
      sp(),
      codeBlock([
        "// Flujo de la SyncOfflineSaga:",
        "",
        "INICIO: llega VentaRealizadaOfflineMessage",
        "  |",
        "  v",
        "Paso 1: Guardar venta en BD local (estado: Pendiente)",
        "  |",
        "  v",
        "Paso 2: Intentar POST /api/ventas al Core",
        "  |-- Exito -> publicar VentaSincronizadaEvent, cerrar Saga",
        "  |-- Fallo -> Paso 3",
        "  |",
        "  v",
        "Paso 3: Programar reintento (backoff exponencial)",
        "  Intento 1: esperar  5 minutos",
        "  Intento 2: esperar 10 minutos",
        "  Intento 3: esperar 20 minutos",
        "  Intento 4: marcar como Rechazada, cerrar Saga (dead-letter)",
      ]),
      sp(),
      codeBlock([
        "public class SyncOfflineSaga : Saga<SyncOfflineSagaData>,",
        "    IAmStartedByMessages<VentaRealizadaOfflineMessage>,",
        "    IHandleTimeouts<RetryTimeout> {",
        "",
        "    public async Task Handle(VentaRealizadaOfflineMessage msg, ...) {",
        "        Data.IdTransaccionLocal = msg.IdTransaccionLocal;",
        "        Data.Intentos = 1;",
        "        await IntentarSincronizar(context);",
        "    }",
        "",
        "    public async Task Timeout(RetryTimeout timeout, ...) {",
        "        Data.Intentos++;",
        "        if (Data.Intentos > 3) {",
        "            await MarcarComoRechazada();",
        "            MarkAsComplete(); // Cerrar saga",
        "            return;",
        "        }",
        "        // Backoff exponencial: 5^1=5, 5^2=10, 5^3=20 minutos",
        "        var delay = TimeSpan.FromMinutes(5 * Math.Pow(2, Data.Intentos - 1));",
        "        await RequestTimeout<RetryTimeout>(context, delay);",
        "    }",
        "}",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 10 – MIDDLEWARE
      // ═════════════════════════════
      h1("10. Middleware - Codigo que se Ejecuta en Cada Peticion"),
      sp(),
      p("El middleware es como una cadena de filtros por donde pasa cada peticion HTTP antes de llegar al controlador. Piensalo como un aeropuerto: la peticion pasa por seguridad (autenticacion), luego por inmigración (autorizacion), y asi sucesivamente."),
      sp(),
      h2("10.1 CorrelationIdMiddleware.cs"),
      p("Asigna un ID unico (GUID) a cada peticion. Este ID viaja en el header X-Correlation-Id y permite rastrear una peticion a traves de multiples sistemas y logs."),
      sp(),
      codeBlock([
        "// Tomar el ID si ya viene en el header, o generar uno nuevo",
        "var correlationId = context.Request.Headers[\"X-Correlation-Id\"]",
        "    .FirstOrDefault() ?? Guid.NewGuid().ToString();",
        "",
        "// Guardarlo para que otros middlewares y controladores lo usen",
        "context.Items[\"CorrelationId\"] = correlationId;",
        "// Incluirlo en la respuesta para que el cliente pueda usarlo",
        "context.Response.Headers[\"X-Correlation-Id\"] = correlationId;",
        "",
        "// Pasar la peticion al siguiente middleware de la cadena",
        "await _next(context);",
      ]),
      sp(),
      infoBox("Para que sirve el Correlation ID?",
        "Cuando una peticion falla en produccion y el cliente dice 'hubo un error', puedes buscar el Correlation ID en Seq (sistema de logs centralizado) y ver exactamente todo lo que paso: que se recibio, que se envio al Core, que respondio, en que milisegundo. Sin esto, depurar errores en produccion seria casi imposible.",
        NARANJA_CLR, NARANJA),
      sp(),
      h2("10.2 IntegrationLoggingMiddleware.cs"),
      p("Registra todas las peticiones y respuestas en IntegrationLogs de PostgreSQL. Tiene caracteristicas importantes de seguridad:"),
      sp(),
      bullet("Sanitizacion de datos sensibles: NUNCA guarda passwords, tokens, numeros de tarjeta, CVV"),
      bullet("Limite de tamano: maximo 4000 caracteres en los bodies para no saturar la base de datos"),
      bullet("No bloquea el flujo: si el logging falla, la peticion continua normalmente"),
      sp(),
      codeBlock([
        "// Campos que NUNCA se guardan en los logs (seguridad)",
        "private static readonly string[] _camposSensibles = {",
        "    \"password\", \"token\", \"cardnumber\", \"cvv\",",
        "    \"refreshtoken\", \"accesstoken\"",
        "};",
        "",
        "// Los valores de estos campos se reemplazan con '***'",
        "// Ejemplo: { \"password\": \"mi_clave\" }",
        "//      ->  { \"password\": \"***\" }",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 11 – VALIDACION
      // ═════════════════════════════
      h1("11. Validadores - FluentValidation"),
      sp(),
      p("FluentValidation permite definir reglas de validacion de forma fluida y legible. Es mucho mas expresiva y testeable que los DataAnnotations tradicionales. Se ejecuta AUTOMATICAMENTE antes del controlador: si falla, devuelve 400 Bad Request con los mensajes de error sin que el controlador se ejecute."),
      sp(),
      codeBlock([
        "// RequestValidators.cs",
        "public class CrearVentaRequestValidator",
        "    : AbstractValidator<CrearVentaRequest> {",
        "",
        "    public CrearVentaRequestValidator() {",
        "        RuleFor(x => x.ClienteId)",
        "            .NotEmpty(); // No puede estar vacio",
        "",
        "        RuleFor(x => x.CajeroId)",
        "            .NotEmpty().MaximumLength(100); // Obligatorio, max 100 chars",
        "",
        "        RuleFor(x => x.MetodoPago)",
        "            .Must(m => m == \"Efectivo\" || m == \"Tarjeta\" || m == \"Transferencia\")",
        "            .WithMessage(\"Debe ser Efectivo, Tarjeta o Transferencia\");",
        "",
        "        RuleFor(x => x.MontoRecibido)",
        "            .GreaterThan(0); // Debe ser positivo",
        "",
        "        RuleFor(x => x.Lineas)",
        "            .NotEmpty() // Al menos un producto",
        "            .WithMessage(\"La venta debe tener al menos un producto\");",
        "",
        "        // Validar CADA linea de la venta",
        "        RuleForEach(x => x.Lineas).ChildRules(linea => {",
        "            linea.RuleFor(l => l.ProductoId).GreaterThan(0);",
        "            linea.RuleFor(l => l.Cantidad).GreaterThan(0);",
        "            linea.RuleFor(l => l.PrecioUnitario).GreaterThan(0);",
        "        });",
        "    }",
        "}",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 12 – SIGNALR
      // ═════════════════════════════
      h1("12. SignalR - Comunicacion en Tiempo Real"),
      sp(),
      p("SignalR permite comunicacion bidireccional en tiempo real entre el servidor y los clientes web usando WebSockets. En lugar de que el cliente pregunte cada segundo si hay novedades (polling), el servidor empuja (push) la informacion cuando hay algo nuevo."),
      sp(),
      codeBlock([
        "// NotificacionesHub.cs",
        "public class NotificacionesHub : Hub {",
        "",
        "    // El sitio web (P4) se suscribe para rastrear una orden especifica",
        "    public async Task SubscribeToOrden(string ordenId) {",
        "        await Groups.AddToGroupAsync(Context.ConnectionId, $\"orden-{ordenId}\");",
        "    }",
        "",
        "    // La caja POS (P1) se suscribe para recibir eventos de reconciliacion",
        "    public async Task SubscribeToSucursal(string sucursalId) {",
        "        await Groups.AddToGroupAsync(Context.ConnectionId, $\"sucursal-{sucursalId}\");",
        "    }",
        "}",
        "",
        "// Cuando el estado de una orden cambia, el servidor notifica a todos los suscritos:",
        "await _hub.Clients.Group($\"orden-{ordenId}\")",
        "    .SendAsync(\"EstadoOrdenActualizado\", nuevoEstado);",
      ]),
      sp(),
      twoColTable("Grupo SignalR", "Quien se suscribe y para que?", [
        ["orden-{id}",     "El sitio web. Muestra actualizaciones en tiempo real del estado de la orden sin que el usuario tenga que refrescar la pagina."],
        ["sucursal-{id}",  "La caja POS. Recibe notificaciones cuando las ventas offline se han sincronizado exitosamente con el Core."],
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 13 – PATRONES
      // ═════════════════════════════
      h1("13. Resumen de Patrones de Arquitectura"),
      sp(),
      p("Este proyecto implementa varios patrones de diseno reconocidos en la industria. Entender estos patrones te ayudara no solo con este proyecto, sino con cualquier sistema distribuido que encuentres en tu carrera."),
      sp(),
      twoColTable("Patron", "Como se implementa en este proyecto?", [
        ["API Gateway",           "IntegrationApp centraliza todas las peticiones de P1 y P4 hacia P2. Un punto de entrada que maneja autenticacion, validacion y enrutamiento."],
        ["Circuit Breaker",       "CircuitBreakerStateService + Polly v8. Previene fallos en cascada cuando el Core no esta disponible."],
        ["Graceful Degradation",  "Cuando el Core falla: productos desde mirror, ventas a BD local. La tienda sigue funcionando de forma limitada."],
        ["Idempotency Key",       "Header obligatorio en ventas. IdempotencyLog con indice UNIQUE previene procesamiento duplicado."],
        ["Event-Driven / CQRS",   "NServiceBus desacopla componentes. Comandos (instrucciones) y Eventos (notificaciones) son distintos."],
        ["Saga Pattern",          "SyncOfflineSaga orquesta la sincronizacion offline con reintentos y backoff exponencial."],
        ["Correlation ID",        "CorrelationIdMiddleware propaga un ID unico por toda la cadena para trazabilidad en logs."],
        ["Composite Health Check","Endpoint /health con estado agregado. Usado por Docker/Kubernetes para saber si la app esta sana."],
        ["Real-time Push",        "SignalR (WebSockets) notifica cambios sin que el cliente tenga que preguntar continuamente."],
        ["JWT Authentication",    "Tokens firmados para autenticacion stateless. El servidor no necesita guardar sesiones."],
        ["Structured Logging",    "Serilog -> Seq. Logs como objetos JSON buscables y filtrables por CorrelationId, Layer, etc."],
        ["Pipeline Validation",   "FluentValidation antes del controller. Si falla, el controller ni se ejecuta (400 Bad Request)."],
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 14 – FLUJOS COMPLETOS
      // ═════════════════════════════
      h1("14. Flujos Completos - De Punta a Punta"),
      sp(),
      h2("14.1 Flujo de una Venta Online (Core disponible)"),
      codeBlock([
        "1. Cajero presiona 'Registrar Venta' en la POS (P1)",
        "2. POS envia: POST /api/v1/ventas + Header: Idempotency-Key: {GUID}",
        "3. CorrelationIdMiddleware genera X-Correlation-Id",
        "4. FluentValidation valida los datos de la venta",
        "5. IntegrationLoggingMiddleware captura el request para auditoria",
        "6. VentasController verifica: CircuitBreaker.CoreAvailable == true",
        "7. Verifica IdempotencyLog: este GUID ya se proceso? Si: devolver resultado anterior",
        "8. CoreApiClient hace POST al Core (Polly maneja reintentos si falla)",
        "9. Core procesa la venta, genera factura, devuelve VentaResponse",
        "10. IntegrationApp guarda en IdempotencyLog (estado: Procesada)",
        "11. IntegrationLoggingMiddleware captura el response",
        "12. POS recibe 200 OK + factura -> cajero entrega recibo al cliente",
      ]),
      sp(),
      h2("14.2 Flujo de una Venta Offline (Core no disponible)"),
      codeBlock([
        "1. Cajero presiona 'Registrar Venta' en la POS (P1)",
        "2. POS envia: POST /api/v1/ventas + Header: Idempotency-Key: {GUID}",
        "3. VentasController: CircuitBreaker.CoreAvailable == false (MODO OFFLINE)",
        "4. Guarda VentaOfflinePendiente en PostgreSQL local (estado: Pendiente)",
        "5. Publica VentaRealizadaOfflineMessage en NServiceBus",
        "6. POS recibe 202 Accepted + 'Venta guardada en modo offline'",
        "7. La caja puede seguir vendiendo sin conexion",
        "",
        "--- Mas tarde, cuando el Core vuelve ---",
        "",
        "8. CoreHealthCheckService detecta que el Core responde",
        "9. Llama CircuitBreaker.MarkCoreAvailable()",
        "10. SyncOfflineSaga procesa VentaRealizadaOfflineMessage de la cola",
        "11. Hace POST al Core con la venta + idempotencyKey",
        "12. Core procesa y confirma",
        "13. Saga publica VentaSincronizadaEvent",
        "14. SignalR notifica a la sucursal: sus ventas se sincronizaron",
        "15. VentaOfflinePendiente se actualiza a estado: Sincronizada",
      ]),
      sp(),
      new Paragraph({ pageBreakBefore:true, children:[new TextRun({text:""})] }),

      // ═════════════════════════════
      // 15 – GUIA PRACTICA
      // ═════════════════════════════
      h1("15. Guia Practica para el Desarrollador Junior"),
      sp(),
      h2("15.1 Como Anadir un Nuevo Endpoint"),
      bullet("Paso 1: Crear el DTO en Contracts/Requests/ (lo que recibe) y Contracts/Responses/ (lo que devuelve)"),
      bullet("Paso 2: Anadir el validador en Validators/RequestValidators.cs"),
      bullet("Paso 3: Crear o usar un controlador existente en Controllers/"),
      bullet("Paso 4: Implementar logica: verificar CircuitBreaker, proxear al Core o usar datos locales"),
      bullet("Paso 5: Si necesitas nuevos datos: crear entidad en Domain/Entities/, agregarla al DbContext, generar migracion"),
      sp(),
      h2("15.2 Como Depurar con el Correlation ID"),
      bullet("Buscar el header X-Correlation-Id en la respuesta de cualquier peticion"),
      bullet("Ir a Seq (http://localhost:5341) y filtrar por ese Correlation ID"),
      bullet("Ver toda la cadena de logs de esa peticion especifica: request, Core, response"),
      sp(),
      h2("15.3 Stack de Tecnologias a Aprender"),
      twoColTable("Tecnologia", "Recurso Recomendado", [
        ["C# y .NET 9",        "docs.microsoft.com/dotnet - Tutorial C# oficial"],
        ["Entity Framework",   "docs.microsoft.com/ef - Getting Started"],
        ["ASP.NET Core",       "docs.microsoft.com/aspnet - Web API Tutorial"],
        ["JWT Authentication", "jwt.io - Introduction to JWT"],
        ["Polly",              "github.com/App-vNext/Polly - README y docs"],
        ["NServiceBus",        "docs.particular.net - Tutorial NServiceBus"],
        ["SignalR",            "docs.microsoft.com/aspnet/signalr - Getting Started"],
        ["FluentValidation",   "docs.fluentvalidation.net - Getting Started"],
        ["Serilog",            "serilog.net - Getting Started"],
        ["PostgreSQL",         "postgresql.org/docs - Tutorial SQL"],
      ]),
      sp(), sp(),
      new Table({
        width:{size:9360,type:WidthType.DXA}, columnWidths:[9360],
        rows:[new TableRow({children:[new TableCell({
          width:{size:9360,type:WidthType.DXA},
          shading:{fill:AZUL,type:ShadingType.CLEAR},
          margins:{top:200,bottom:200,left:300,right:300},
          borders:{
            top:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            bottom:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            left:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
            right:{style:BorderStyle.SINGLE,size:8,color:AZUL_MED},
          },
          children:[
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Sigue adelante, Jesus!", bold:true, size:36, font:"Arial", color:BLANCO})]}),
            sp(),
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Este proyecto implementa patrones que usan Netflix, Amazon y Uber.", size:22, font:"Arial", color:AZUL_CLARO})]}),
            new Paragraph({alignment:AlignmentType.CENTER, children:[new TextRun({text:"Entender cada pieza te pone muy por delante de muchos desarrolladores junior.", size:22, font:"Arial", color:AZUL_CLARO})]}),
          ],
        })]})],
      }),
    ],
  }],
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync(
    '/sessions/admiring-sharp-brown/mnt/RadiadoresSprings/Guia_Codigo_IntegrationApp.docx',
    buffer
  );
  console.log('Documento creado exitosamente');
}).catch(err => {
  console.error('Error:', err.message);
  process.exit(1);
});
