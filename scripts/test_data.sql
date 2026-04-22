-- ═══════════════════════════════════════════════════════════════════════════════
-- DATOS DE PRUEBA — RadiadoresSprings Core
-- Base de datos : RadiadoresSpringsCore (SQL Server)
--
-- PREREQUISITO : Ejecutar Core.API al menos una vez para que el seed inicial
--   cree: Sucursal Id=1, Categorias Id=1-4, Caja Id=1, Usuario admin Id=1
--
-- TABLAS CUBIERTAS:
--   Productos · Clientes · SesionesCaja · Ventas · LineasVenta
--   Ordenes · LineasOrden · CuentasCobrar · Pagos
--
-- COHERENCIA GARANTIZADA:
--   • Stock en Productos = stock inicial − unidades vendidas (ventas completadas)
--   • IVA = Subtotal × 16%, Total = Subtotal + IVA − Descuento
--   • TotalOrden = Σ (Cantidad × PrecioUnitario) en LineasOrden
--   • Cliente.SaldoPendiente = Σ SaldoPendiente de sus CuentasCobrar
--   • CuentaCobrar.SaldoPendiente = MontoOriginal − MontoPagado  (columna calculada)
--
-- EJECUCIÓN:
--   sqlcmd -S MSI -d RadiadoresSpringsCore -E -i scripts\test_data.sql
-- ═══════════════════════════════════════════════════════════════════════════════

USE RadiadoresSpringsCore;
GO
SET NOCOUNT ON;

PRINT '══════════════════════════════════════════════';
PRINT '  Insertando datos de prueba RadiadoresSprings';
PRINT '══════════════════════════════════════════════';

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. PRODUCTOS  (10 artículos — 3 con stock bajo para activar alertas en panel)
--
--    Stock refleja el estado DESPUÉS de las ventas de prueba del bloque 4.
--    CategoriaId:  1=Radiadores  2=Mangueras  3=Ventiladores  4=Accesorios
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Productos
    (Codigo,     Nombre,                            Descripcion,
     Precio,     PrecioOferta, Stock, StockMinimo, CategoriaId, EsActivo, FechaCreacion)
VALUES
--   Código       Nombre                             Descripción
    ('RAD-001', 'Radiador Universal 4 Filas',
     'Radiador de cobre 4 filas, compatible con motores 1.6 – 2.0 L',
     2500.00, NULL,      13,  5,  1, 1, GETUTCDATE()),   -- vendidos 2 en ventas

    ('RAD-002', 'Radiador Aluminio Chevrolet Aveo',
     'Radiador aluminio OEM para Chevrolet Aveo 2007-2015',
     3200.00, 2900.00,   5,  3,  1, 1, GETUTCDATE()),   -- vendidos 3; en oferta

    ('RAD-003', 'Radiador Toyota Corolla E150',
     'Radiador OEM Toyota Corolla E150 2007-2013',
     2800.00, NULL,      2,  5,  1, 1, GETUTCDATE()),   -- STOCK BAJO (2 < min 5)

    ('MAN-001', 'Manguera Superior Universal 40 mm',
     'Manguera de hule reforzado, diámetro 40 mm',
      180.00, NULL,     38, 10,  2, 1, GETUTCDATE()),   -- vendidas 2 unidades

    ('MAN-002', 'Manguera Inferior Nissan Tsuru',
     'Manguera inferior específica para Nissan Tsuru',
      220.00, NULL,     22,  8,  2, 1, GETUTCDATE()),   -- vendidas 3 unidades

    ('VEN-001', 'Ventilador Mecánico 12" 2 Aspas',
     'Ventilador mecánico 12 pulgadas, acero galvanizado',
      450.00, NULL,     16,  5,  3, 1, GETUTCDATE()),   -- vendidos 4 (2+2)

    ('VEN-002', 'Electroventilador Universal 12V',
     'Electroventilador universal 12V 2000 RPM, conectores JST',
      680.00, 599.00,   3,  5,  3, 1, GETUTCDATE()),   -- STOCK BAJO; en oferta

    ('ACC-001', 'Termostato Universal 82°C',
     'Termostato apertura 82 °C, junta de hule incluida',
      120.00, NULL,     48, 15,  4, 1, GETUTCDATE()),   -- vendidas 2 unidades

    ('ACC-002', 'Tapa Radiador 0.9 Bar',
     'Tapa de radiador presión 0.9 bar, apto para mayoría de modelos',
       95.00, NULL,     60, 20,  4, 1, GETUTCDATE()),   -- venta cancelada → stock íntegro

    ('ACC-003', 'Anticongelante Verde 1 L',
     'Anticongelante concentrado base etilén glicol, 1 litro',
       85.00, NULL,     95, 30,  4, 1, GETUTCDATE());   -- vendidas 5 unidades

PRINT '  [OK] Productos     : 10 registros';

-- ── Variables de producto ─────────────────────────────────────────────────────
DECLARE @pRAD001 INT = (SELECT Id FROM Productos WHERE Codigo = 'RAD-001');
DECLARE @pRAD002 INT = (SELECT Id FROM Productos WHERE Codigo = 'RAD-002');
DECLARE @pRAD003 INT = (SELECT Id FROM Productos WHERE Codigo = 'RAD-003');
DECLARE @pMAN001 INT = (SELECT Id FROM Productos WHERE Codigo = 'MAN-001');
DECLARE @pMAN002 INT = (SELECT Id FROM Productos WHERE Codigo = 'MAN-002');
DECLARE @pVEN001 INT = (SELECT Id FROM Productos WHERE Codigo = 'VEN-001');
DECLARE @pVEN002 INT = (SELECT Id FROM Productos WHERE Codigo = 'VEN-002');
DECLARE @pACC001 INT = (SELECT Id FROM Productos WHERE Codigo = 'ACC-001');
DECLARE @pACC002 INT = (SELECT Id FROM Productos WHERE Codigo = 'ACC-002');
DECLARE @pACC003 INT = (SELECT Id FROM Productos WHERE Codigo = 'ACC-003');

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. CLIENTES  (5 clientes — tipos y estados variados)
--
--    SaldoPendiente = suma de cuentas por cobrar pendientes del cliente.
--       @cTaller  → 1 CC PagoParcial  saldo $3,500
--       @cAuto    → 2 CC (Pendiente $3,944 + Vencida $8,556) = $12,500
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Clientes
    (Nombre,               Apellido,            Email,
     Telefono,   Direccion,                                     RFC,
     Tipo,        LimiteCredito, SaldoPendiente, EsActivo, FechaCreacion)
VALUES
    ('Juan',               'Pérez Martínez',    'juan.perez@email.com',
     '555-1001', 'Calle Roble 45, Col. Centro',                 'PEMJ850312HDF',
     'Regular',   0.00,         0.00,     1, GETUTCDATE()),

    ('Taller Mecánico',    'García e Hijos',    'taller.garcia@gmail.com',
     '555-2002', 'Blvd. Industrial 120, Col. Industrial',       'GAHI760820MXL',
     'Mayorista', 15000.00,    3500.00,   1, GETUTCDATE()),

    ('Auto Express',       'SA de CV',          'compras@autoexpress.mx',
     '555-3003', 'Av. Insurgentes 890, Col. Doctores',          'AEX930101AAA',
     'VIP',       50000.00,  12500.00,   1, GETUTCDATE()),

    ('María',              'López Hernández',   'maria.lopez@hotmail.com',
     '555-4004', 'Privada Pinos 12, Col. Las Flores',           NULL,
     'Regular',   0.00,         0.00,     1, GETUTCDATE()),

    ('Distribuidora Norte','del Bajío SA',       'ventas@disnorte.com',
     '555-5005', 'Carretera Panamericana Km 34',                'DNB881115HJK',
     'Mayorista', 25000.00,     0.00,     1, GETUTCDATE());

PRINT '  [OK] Clientes      : 5 registros';

-- ── Variables de cliente ──────────────────────────────────────────────────────
DECLARE @cJuan    INT = (SELECT Id FROM Clientes WHERE Nombre = 'Juan'               AND Apellido = 'Pérez Martínez');
DECLARE @cTaller  INT = (SELECT Id FROM Clientes WHERE Nombre = 'Taller Mecánico'   AND Apellido = 'García e Hijos');
DECLARE @cAuto    INT = (SELECT Id FROM Clientes WHERE Nombre = 'Auto Express'      AND Apellido = 'SA de CV');
DECLARE @cMaria   INT = (SELECT Id FROM Clientes WHERE Nombre = 'María'             AND Apellido = 'López Hernández');
DECLARE @cDistrib INT = (SELECT Id FROM Clientes WHERE Nombre = 'Distribuidora Norte' AND Apellido = 'del Bajío SA');

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. SESIONES DE CAJA  (3 sesiones: 2 cerradas + 1 activa)
--
--    CajaId=1  UsuarioId=1 (admin del seed)
--    Sesión 1 (hace 2 días): Cerrada — faltante de $100
--    Sesión 2 (ayer)       : Cuadrada — sin diferencia
--    Sesión 3 (hoy)        : Abierta  — sesión activa actual
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @hoy     DATETIME = CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME);
DECLARE @ayer    DATETIME = DATEADD(DAY, -1, @hoy);
DECLARE @dosDias DATETIME = DATEADD(DAY, -2, @hoy);

-- Sesión 1 — hace 2 días, cerrada con faltante
INSERT INTO SesionesCaja
    (CajaId, UsuarioId, FechaApertura,            FechaCierre,
     MontoApertura, MontoCierre, MontoSistema, Diferencia, Estado, Observaciones)
VALUES
    (1, 1,
     DATEADD(HOUR,  8, @dosDias), DATEADD(HOUR, 18, @dosDias),
     1500.00, 6320.00, 6420.00, -100.00, 'Cerrada',
     'Faltaron $100 en efectivo — revisar con cajero turno matutino');
DECLARE @s1 INT = SCOPE_IDENTITY();

-- Sesión 2 — ayer, cuadrada
INSERT INTO SesionesCaja
    (CajaId, UsuarioId, FechaApertura,            FechaCierre,
     MontoApertura, MontoCierre, MontoSistema, Diferencia, Estado, Observaciones)
VALUES
    (1, 1,
     DATEADD(HOUR,  8, @ayer), DATEADD(HOUR, 18, @ayer),
     2000.00, 9480.00, 9480.00, 0.00, 'Cuadrada', NULL);
DECLARE @s2 INT = SCOPE_IDENTITY();

-- Sesión 3 — hoy, abierta
INSERT INTO SesionesCaja
    (CajaId, UsuarioId, FechaApertura,           FechaCierre,
     MontoApertura, MontoCierre, MontoSistema, Diferencia, Estado, Observaciones)
VALUES
    (1, 1,
     DATEADD(HOUR, 8, @hoy), NULL,
     1500.00, NULL, NULL, NULL, 'Abierta', NULL);
DECLARE @s3 INT = SCOPE_IDENTITY();

PRINT '  [OK] SesionesCaja  : 3 registros (IDs: ' +
      CAST(@s1 AS NVARCHAR) + ', ' + CAST(@s2 AS NVARCHAR) + ', ' + CAST(@s3 AS NVARCHAR) + ')';

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. VENTAS  (8 ventas — distribuidas entre las 3 sesiones)
--
--    Cálculo: IVA = Subtotal × 0.16  |  Total = Subtotal + IVA − Descuento
--    ─────────────────────────────────────────────────────────────────────
--    V1  Efectivo     Juan Pérez        S1  1×RAD-001@2500             = 2500 | +400  = 2900
--    V2  TarjetaDb    Taller García     S1  1×RAD-002@3200             = 3200 | +512  = 3712
--    V3  Efectivo     (anónimo)         S1  2×MAN-001@180              =  360 | +57.6 =  417.60
--    V4  Transferen.  Auto Express      S2  1×RAD-001@2500+2×VEN-001@450= 3400| +544  = 3944
--    V5  Crédito      Taller García     S2  2×RAD-002@3200             = 6400 | +1024 = 7424  ← genera CC
--    V6  Efectivo     María López       S2  3×MAN-002@220+2×ACC-001@120=  900 | +144  = 1044
--    V7  Efectivo     (anónimo)         S2  1×ACC-002@95               =   95 | +15.2 =  110.20  ← CANCELADA
--    V8  TarjetaCr    Auto Express      S3  2×VEN-001@450+5×ACC-003@85 = 1325 | +212  = 1537
-- ─────────────────────────────────────────────────────────────────────────────

-- V1
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00001', 1, 1, @s1, @cJuan,   1, DATEADD(HOUR, 10, @dosDias),
    2500.00, 400.00, 2900.00, 0.00, 'Efectivo', 'Completada', 0);
DECLARE @v1 INT = SCOPE_IDENTITY();

-- V2
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00002', 1, 1, @s1, @cTaller, 1, DATEADD(HOUR, 12, @dosDias),
    3200.00, 512.00, 3712.00, 0.00, 'TarjetaDebito', 'Completada', 0);
DECLARE @v2 INT = SCOPE_IDENTITY();

-- V3
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00003', 1, 1, @s1, NULL,     1, DATEADD(HOUR, 15, @dosDias),
    360.00, 57.60, 417.60, 0.00, 'Efectivo', 'Completada', 0);
DECLARE @v3 INT = SCOPE_IDENTITY();

-- V4
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00004', 1, 1, @s2, @cAuto,   1, DATEADD(HOUR,  9, @ayer),
    3400.00, 544.00, 3944.00, 0.00, 'Transferencia', 'Completada', 0);
DECLARE @v4 INT = SCOPE_IDENTITY();

-- V5 (crédito → cuenta por cobrar)
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00005', 1, 1, @s2, @cTaller, 1, DATEADD(HOUR, 11, @ayer),
    6400.00, 1024.00, 7424.00, 0.00, 'Credito', 'Completada', 0);
DECLARE @v5 INT = SCOPE_IDENTITY();

-- V6
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00006', 1, 1, @s2, @cMaria,  1, DATEADD(HOUR, 14, @ayer),
    900.00, 144.00, 1044.00, 0.00, 'Efectivo', 'Completada', 0);
DECLARE @v6 INT = SCOPE_IDENTITY();

-- V7 (cancelada — stock ACC-002 ya fue devuelto en el conteo de productos)
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline, Observaciones)
VALUES ('F-202504-00007', 1, 1, @s2, NULL,     1, DATEADD(HOUR, 16, @ayer),
    95.00, 15.20, 110.20, 0.00, 'Efectivo', 'Cancelada', 0,
    'CANCELADA: Cliente se arrepintió — devuelto en caja');
DECLARE @v7 INT = SCOPE_IDENTITY();

-- V8 (sesión activa de hoy)
INSERT INTO Ventas (NumeroFactura, SucursalId, CajaId, SesionCajaId, ClienteId, UsuarioId,
    Fecha, Subtotal, IVA, Total, Descuento, MetodoPago, Estado, EsOffline)
VALUES ('F-202504-00008', 1, 1, @s3, @cAuto,   1,
    DATEADD(MINUTE, 30, DATEADD(HOUR, 8, @hoy)),
    1325.00, 212.00, 1537.00, 0.00, 'TarjetaCredito', 'Completada', 0);
DECLARE @v8 INT = SCOPE_IDENTITY();

PRINT '  [OK] Ventas        : 8 registros (IDs: ' +
      CAST(@v1 AS NVARCHAR) + '–' + CAST(@v8 AS NVARCHAR) + ')';

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. LÍNEAS DE VENTA
--    Subtotal es columna calculada (ignorada por EF), no se inserta.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO LineasVenta (VentaId, ProductoId, Cantidad, PrecioUnitario, Descuento)
VALUES
    -- V1: 1 × RAD-001 @ 2500  → 2500 ✓
    (@v1, @pRAD001, 1, 2500.00, 0.00),

    -- V2: 1 × RAD-002 @ 3200  → 3200 ✓
    (@v2, @pRAD002, 1, 3200.00, 0.00),

    -- V3: 2 × MAN-001 @ 180   → 360 ✓
    (@v3, @pMAN001, 2,  180.00, 0.00),

    -- V4: 1 × RAD-001 @ 2500 + 2 × VEN-001 @ 450  → 2500+900 = 3400 ✓
    (@v4, @pRAD001, 1, 2500.00, 0.00),
    (@v4, @pVEN001, 2,  450.00, 0.00),

    -- V5: 2 × RAD-002 @ 3200  → 6400 ✓
    (@v5, @pRAD002, 2, 3200.00, 0.00),

    -- V6: 3 × MAN-002 @ 220 + 2 × ACC-001 @ 120  → 660+240 = 900 ✓
    (@v6, @pMAN002, 3,  220.00, 0.00),
    (@v6, @pACC001, 2,  120.00, 0.00),

    -- V7 (cancelada): 1 × ACC-002 @ 95  → 95 ✓
    (@v7, @pACC002, 1,   95.00, 0.00),

    -- V8: 2 × VEN-001 @ 450 + 5 × ACC-003 @ 85  → 900+425 = 1325 ✓
    (@v8, @pVEN001, 2,  450.00, 0.00),
    (@v8, @pACC003, 5,   85.00, 0.00);

PRINT '  [OK] LineasVenta   : 11 registros';

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. ÓRDENES  (4 órdenes — ciclo completo de estados)
--
--    O1 Taller García  → Entregada  (hace 5 días)   total $5,900
--    O2 Auto Express   → Enviada    (hace 2 días)   total $4,125
--    O3 Distrib. Norte → EnProceso  (ayer)          total $7,500
--    O4 Juan Pérez     → Recibida   (hoy)           total $1,185
-- ─────────────────────────────────────────────────────────────────────────────
-- O1 Entregada
INSERT INTO Ordenes
    (NumeroOrden, ClienteId, Estado, Fecha, FechaEstimadaEntrega, FechaEntrega,
     TotalOrden, MetodoPago, DireccionEnvio, Notas)
VALUES
    ('ORD-20250415-TG001', @cTaller, 'Entregada',
     DATEADD(DAY, -5, GETUTCDATE()),
     DATEADD(DAY, -3, GETUTCDATE()),
     DATEADD(DAY, -2, GETUTCDATE()),
     5900.00, 'Transferencia',
     'Blvd. Industrial 120, Col. Industrial — Taller García',
     'Entrega en almacén, turno matutino. Recibe Sr. García.');
DECLARE @o1 INT = SCOPE_IDENTITY();

-- O2 Enviada
INSERT INTO Ordenes
    (NumeroOrden, ClienteId, Estado, Fecha, FechaEstimadaEntrega, FechaEntrega,
     TotalOrden, MetodoPago, DireccionEnvio, Notas)
VALUES
    ('ORD-20250418-AE001', @cAuto, 'Enviada',
     DATEADD(DAY, -2, GETUTCDATE()),
     DATEADD(DAY,  1, GETUTCDATE()),
     NULL,
     4125.00, 'TarjetaCredito',
     'Av. Insurgentes 890, Col. Doctores — Auto Express SA',
     'Paquetería Estafeta, guía #EST2025044891. Llegada estimada: mañana AM.');
DECLARE @o2 INT = SCOPE_IDENTITY();

-- O3 EnProceso
INSERT INTO Ordenes
    (NumeroOrden, ClienteId, Estado, Fecha, FechaEstimadaEntrega, FechaEntrega,
     TotalOrden, MetodoPago, DireccionEnvio, Notas)
VALUES
    ('ORD-20250419-DN001', @cDistrib, 'EnProceso',
     DATEADD(DAY, -1, GETUTCDATE()),
     DATEADD(DAY,  3, GETUTCDATE()),
     NULL,
     7500.00, 'Transferencia',
     'Carretera Panamericana Km 34 — Distribuidora Norte',
     'Pedido mayorista. Empacar radiadores individualmente con burbuja.');
DECLARE @o3 INT = SCOPE_IDENTITY();

-- O4 Recibida
INSERT INTO Ordenes
    (NumeroOrden, ClienteId, Estado, Fecha, FechaEstimadaEntrega, FechaEntrega,
     TotalOrden, MetodoPago, DireccionEnvio, Notas)
VALUES
    ('ORD-20250420-JP001', @cJuan, 'Recibida',
     GETUTCDATE(),
     DATEADD(DAY, 5, GETUTCDATE()),
     NULL,
     1185.00, 'Efectivo',
     'Calle Roble 45, Col. Centro — Juan Pérez Martínez',
     NULL);
DECLARE @o4 INT = SCOPE_IDENTITY();

PRINT '  [OK] Ordenes       : 4 registros';

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. LÍNEAS DE ORDEN
--    ─────────────────────────────────────────────────────────────────────────
--    O1: 2×RAD-001@2500 + 3×MAN-001@180 + 3×ACC-001@120  = 5000+540+360 = 5900 ✓
--    O2: 1×RAD-003@2800 + 2×VEN-001@450 + 5×ACC-003@85   = 2800+900+425 = 4125 ✓
--    O3: 2×RAD-002@3200 + 5×MAN-002@220                  = 6400+1100    = 7500 ✓
--    O4: 2×VEN-001@450  + 3×ACC-002@95                   = 900+285      = 1185 ✓
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO LineasOrden (OrdenId, ProductoId, Cantidad, PrecioUnitario)
VALUES
    -- O1 Entregada
    (@o1, @pRAD001, 2, 2500.00),
    (@o1, @pMAN001, 3,  180.00),
    (@o1, @pACC001, 3,  120.00),

    -- O2 Enviada
    (@o2, @pRAD003, 1, 2800.00),
    (@o2, @pVEN001, 2,  450.00),
    (@o2, @pACC003, 5,   85.00),

    -- O3 EnProceso
    (@o3, @pRAD002, 2, 3200.00),
    (@o3, @pMAN002, 5,  220.00),

    -- O4 Recibida
    (@o4, @pVEN001, 2,  450.00),
    (@o4, @pACC002, 3,   95.00);

PRINT '  [OK] LineasOrden   : 10 registros';

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. CUENTAS POR COBRAR  (3 cuentas — estados variados)
--
--    CC1  Taller García / V5   PagoParcial  MontoOrig=$7,424  Pagado=$3,924  Saldo=$3,500
--    CC2  Auto Express  / V4   Pendiente    MontoOrig=$3,944  Pagado=$0      Saldo=$3,944
--    CC3  Auto Express  / ---  Vencida      MontoOrig=$8,556  Pagado=$0      Saldo=$8,556
--
--    Verificación saldos por cliente:
--       @cTaller.SaldoPendiente = 3,500          ✓
--       @cAuto.SaldoPendiente   = 3,944 + 8,556 = 12,500  ✓
-- ─────────────────────────────────────────────────────────────────────────────
-- CC1 PagoParcial
INSERT INTO CuentasCobrar
    (ClienteId, VentaId, NumeroFactura, MontoOriginal, MontoPagado,
     FechaEmision, FechaVencimiento, Estado, Notas)
VALUES
    (@cTaller, @v5, 'F-202504-00005',
     7424.00, 3924.00,
     DATEADD(DAY, -1, GETUTCDATE()),
     DATEADD(DAY, 29, GETUTCDATE()),
     'PagoParcial',
     'Abono inicial del 52.86 %. Saldo pendiente: $3,500. Próximo vencimiento en 29 días.');
DECLARE @cc1 INT = SCOPE_IDENTITY();

-- CC2 Pendiente (vence mañana — urgente)
INSERT INTO CuentasCobrar
    (ClienteId, VentaId, NumeroFactura, MontoOriginal, MontoPagado,
     FechaEmision, FechaVencimiento, Estado, Notas)
VALUES
    (@cAuto, @v4, 'F-202504-00004',
     3944.00, 0.00,
     DATEADD(DAY, -1, GETUTCDATE()),
     DATEADD(DAY,  1, GETUTCDATE()),
     'Pendiente',
     'Crédito 2 días. Pago acordado vía SPEI CLABE 0000-1234-5678-90. Vence mañana.');
DECLARE @cc2 INT = SCOPE_IDENTITY();

-- CC3 Vencida (crédito anterior sin liquidar)
INSERT INTO CuentasCobrar
    (ClienteId, VentaId, NumeroFactura, MontoOriginal, MontoPagado,
     FechaEmision, FechaVencimiento, Estado, Notas)
VALUES
    (@cAuto, NULL, 'CC-20250315-001',
     8556.00, 0.00,
     DATEADD(DAY, -36, GETUTCDATE()),
     DATEADD(DAY,  -6, GETUTCDATE()),
     'Vencida',
     'Crédito 30 días vencido hace 6 días. Pendiente gestión de cobranza.');
DECLARE @cc3 INT = SCOPE_IDENTITY();

PRINT '  [OK] CuentasCobrar : 3 registros (IDs: ' +
      CAST(@cc1 AS NVARCHAR) + ', ' + CAST(@cc2 AS NVARCHAR) + ', ' + CAST(@cc3 AS NVARCHAR) + ')';

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. PAGOS  (3 pagos vinculados a clientes y cuentas)
--
--    P1  Taller García → CC1   $3,924  Transferencia   (abono parcial F-202504-00005)
--    P2  Distrib. Norte         $4,500  Transferencia  (anticipo 60 % orden DN001)
--    P3  Auto Express           $500   Efectivo        (abono parcial a cuenta vencida)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Pagos
    (ClienteId, CuentaCobrarId, Monto, MetodoPago, Fecha, Referencia, Notas, UsuarioId)
VALUES
    -- P1: abono de Taller García a CC1
    (@cTaller, @cc1,
     3924.00, 'Transferencia',
     DATEADD(HOUR, 9, @ayer),
     'SPEI-20250419-TG-001',
     'Abono parcial F-202504-00005. Saldo restante: $3,500.', 1),

    -- P2: anticipo de Distribuidora Norte (sin cuenta cobrar específica)
    (@cDistrib, NULL,
     4500.00, 'Transferencia',
     DATEADD(HOUR, 11, @ayer),
     'SPEI-20250419-DN-001',
     'Anticipo 60 % orden ORD-20250419-DN001. Saldo a contra-entrega: $3,000.', 1),

    -- P3: pago parcial de Auto Express en efectivo sobre CC3
    (@cAuto, @cc3,
     500.00, 'Efectivo',
     DATEADD(MINUTE, 15, DATEADD(HOUR, 8, @hoy)),
     NULL,
     'Pago parcial en efectivo a cuenta CC-20250315-001. Saldo restante: $8,056.', 1);

PRINT '  [OK] Pagos         : 3 registros';

-- ═══════════════════════════════════════════════════════════════════════════════
-- RESUMEN FINAL
-- ═══════════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '══════════════════════════════════════════════════════════════════';
PRINT '  ✅ Datos de prueba insertados correctamente';
PRINT '──────────────────────────────────────────────────────────────────';
PRINT '  Tabla             Registros  Notas';
PRINT '  ─────────────────────────────────────────────────────────────';
PRINT '  Productos              10    3 con stock bajo (alertas activas)';
PRINT '  Clientes                5    2 mayoristas · 1 VIP · 2 regulares';
PRINT '  SesionesCaja            3    1 abierta (sesión activa de hoy)  ';
PRINT '  Ventas                  8    6 completadas · 1 cancelada · 1 crédito';
PRINT '  LineasVenta            11    ';
PRINT '  Ordenes                 4    Recibida→EnProceso→Enviada→Entregada';
PRINT '  LineasOrden            10    ';
PRINT '  CuentasCobrar           3    PagoParcial · Pendiente · Vencida ';
PRINT '  Pagos                   3    ';
PRINT '══════════════════════════════════════════════════════════════════';
GO
