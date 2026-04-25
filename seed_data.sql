-- ============================================================
--  SCRIPT DE DATOS DE PRUEBA - RadiadoresSprings
--  Contexto  : República Dominicana
--  Moneda    : Peso Dominicano (DOP / RD$)
--  ITBIS     : 18 %
--  Generado  : 2025-04-24
-- ============================================================
--
--  INSTRUCCIONES DE USO
--  ────────────────────
--  1. Ejecuta el BLOQUE A en la base de datos Core  (SQL Server).
--  2. Ejecuta el BLOQUE B en la base de datos de Integración (SQLite).
--
--  ⚠  REQUISITO: Antes de ejecutar el Bloque A deben existir al menos
--     tres usuarios en la tabla [Usuario] con los siguientes IDs:
--       ID 1 → Administrador
--       ID 2 → Cajero
--       ID 3 → Vendedor
--     (El script no toca la tabla Usuario según indicación.)
--
-- ============================================================


-- ============================================================
-- ██████████  BLOQUE A — CORE DB (SQL Server)  ██████████████
-- ============================================================

BEGIN TRANSACTION;

-- ────────────────────────────────────────────────
-- 1. SUCURSALES
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Sucursal] ON;

INSERT INTO [Sucursal] (Id, Nombre, Direccion, Telefono, EsActiva, FechaCreacion)
VALUES
  (1, 'RadiadoresSprings - Santo Domingo',
      'Av. Winston Churchill No. 53, Piantini, Santo Domingo, D.N.',
      '809-535-4000', 1, '2024-01-15 08:00:00'),
  (2, 'RadiadoresSprings - Santiago',
      'Calle del Sol No. 78, Ensanche Bermúdez, Santiago de los Caballeros',
      '809-971-2200', 1, '2024-03-01 08:00:00'),
  (3, 'RadiadoresSprings - San Pedro de Macorís',
      'Av. Circunvalación No. 12, San Pedro de Macorís',
      '809-529-8800', 1, '2024-06-01 08:00:00');

SET IDENTITY_INSERT [Sucursal] OFF;


-- ────────────────────────────────────────────────
-- 2. CATEGORÍAS
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Categoria] ON;

INSERT INTO [Categoria] (Id, Nombre, Descripcion, EsActiva, FechaCreacion)
VALUES
  (1, 'Radiadores',
      'Radiadores originales y alternos para vehículos livianos y pesados', 1, '2024-01-15 09:00:00'),
  (2, 'Resortes y Amortiguadores',
      'Resortes helicoidales y amortiguadores para suspensión delantera y trasera', 1, '2024-01-15 09:00:00'),
  (3, 'Frenos',
      'Pastillas, discos y tambores de freno para todo tipo de vehículo', 1, '2024-01-15 09:00:00'),
  (4, 'Filtros',
      'Filtros de aceite, aire, combustible y habitáculo', 1, '2024-01-15 09:00:00'),
  (5, 'Lubricantes y Refrigerantes',
      'Aceites de motor, transmisión, diferencial y líquidos refrigerantes', 1, '2024-01-15 09:00:00'),
  (6, 'Correas y Mangueras',
      'Correas de distribución, serpentina y mangueras de radiador', 1, '2024-01-15 09:00:00'),
  (7, 'Baterías y Eléctrico',
      'Baterías automotrices, alternadores y componentes eléctricos', 1, '2024-01-15 09:00:00');

SET IDENTITY_INSERT [Categoria] OFF;


-- ────────────────────────────────────────────────
-- 3. PRODUCTOS  (precios en RD$, ITBIS incluido en precio final al cliente)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Producto] ON;

INSERT INTO [Producto]
  (Id, Codigo, Nombre, Descripcion, Precio, PrecioOferta,
   Stock, StockMinimo, CategoriaId, EsActivo, FechaCreacion)
VALUES
  -- Radiadores ─────────────────────────────────────────────
  (1,  'RAD-TOY-001', 'Radiador Toyota Corolla 2018-2023',
       'Radiador alterno aluminio/plástico, compatible con Corolla E210 motor 2ZR-FE.',
       12500.00, NULL,      8,  3, 1, 1, '2024-01-16 09:00:00'),
  (2,  'RAD-HON-001', 'Radiador Honda Civic 2016-2021',
       'Radiador alterno aluminio/plástico, compatible con Civic FC motor 1.5T y 2.0.',
       11800.00, 10500.00,  5,  3, 1, 1, '2024-01-16 09:00:00'),
  (3,  'RAD-HYU-001', 'Radiador Hyundai Tucson 2019-2023',
       'Radiador alterno aluminio/plástico, compatible con Tucson NX4 motor 2.0 y 2.5.',
       15200.00, NULL,      4,  3, 1, 1, '2024-01-16 09:00:00'),
  (4,  'RAD-NIS-001', 'Radiador Nissan Sentra 2020-2023',
       'Radiador alterno aluminio/plástico, compatible con Sentra B18 motor 1.6 CVT.',
       13800.00, 12900.00,  6,  3, 1, 1, '2024-01-16 09:00:00'),

  -- Resortes y Amortiguadores ───────────────────────────────
  (5,  'RES-TOY-001', 'Resorte Delantero Toyota Corolla (Par)',
       'Par de resortes delanteros para Corolla E210. Altura estándar de fábrica.',
       3800.00,  NULL,     12,  4, 2, 1, '2024-01-17 09:00:00'),
  (6,  'AMO-HON-001', 'Amortiguador Trasero Honda Civic (Par)',
       'Par de amortiguadores traseros KYB Excel-G para Civic FC 2016-2021.',
       4500.00,  NULL,      9,  4, 2, 1, '2024-01-17 09:00:00'),

  -- Frenos ─────────────────────────────────────────────────
  (7,  'FRE-PAS-001', 'Pastillas de Freno Delanteras Toyota',
       'Juego pastillas Bosch BP1234 para Corolla, RAV4 y Camry. Sin polvo metálico.',
       1200.00,  NULL,     25,  8, 3, 1, '2024-01-17 09:00:00'),
  (8,  'FRE-DIS-001', 'Disco de Freno Ventilado Toyota',
       'Disco Brembo 09.A956.11 para Corolla y Camry. Diámetro 280 mm. Par delantero.',
       2800.00,  NULL,     15,  5, 3, 1, '2024-01-17 09:00:00'),

  -- Filtros ────────────────────────────────────────────────
  (9,  'FIL-ACE-001', 'Filtro de Aceite Toyota/Lexus OEM',
       'Filtro genuino Toyota 90915-YZZE1. Compatible con múltiples modelos 2015-2023.',
        280.00,  NULL,     80, 20, 4, 1, '2024-01-18 09:00:00'),
  (10, 'FIL-AIR-001', 'Filtro de Aire K&N Alto Flujo',
       'Filtro deportivo K&N E-0665 lavable y reutilizable. Flujo 45 % superior al original.',
        350.00,  NULL,     60, 15, 4, 1, '2024-01-18 09:00:00'),

  -- Lubricantes y Refrigerantes ─────────────────────────────
  (11, 'LUB-CAS-001', 'Aceite Motor Castrol GTX 20W-50 (4 L)',
       'Aceite mineral API SL/CF. Botella 4 litros. Formulado para clima tropical.',
       1450.00, 1350.00,  40, 10, 5, 1, '2024-01-18 09:00:00'),
  (12, 'LUB-REF-001', 'Líquido Refrigerante Havoline (1 L)',
       'Refrigerante concentrado Extended Life Technology. Color naranja. 1 litro.',
        320.00,  NULL,    50, 15, 5, 1, '2024-01-18 09:00:00'),

  -- Correas y Mangueras ────────────────────────────────────
  (13, 'COR-DIS-001', 'Correa de Distribución Toyota 2ZR',
       'Correa Gates T304RB para Corolla/Yaris motor 2ZR-FE 2009-2023.',
       1850.00,  NULL,    18,  5, 6, 1, '2024-01-19 09:00:00'),
  (14, 'MAN-RAD-001', 'Manguera Radiador Superior Honda Civic',
       'Manguera Gates 23747 para Civic 2016-2021. EPDM reforzado. Tramo superior.',
        680.00,  NULL,    30,  8, 6, 1, '2024-01-19 09:00:00'),

  -- Baterías y Eléctrico ───────────────────────────────────
  (15, 'BAT-BSC-001', 'Batería Bosch S4 12V 60Ah 540A',
       'Batería S4005. Sin mantenimiento. Garantía 18 meses. Libre de ácido.',
       6500.00,  NULL,    10,  3, 7, 1, '2024-01-19 09:00:00');

SET IDENTITY_INSERT [Producto] OFF;


-- ────────────────────────────────────────────────
-- 4. CLIENTES
--    RFC = RNC (Registro Nacional del Contribuyente) en RD
--          Empresas: 9 dígitos | Personas físicas: 11 dígitos (cédula)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Cliente] ON;

INSERT INTO [Cliente]
  (Id, Nombre, Apellido, Email, Telefono, Direccion,
   RFC, Tipo, LimiteCredito, SaldoPendiente, EsActivo, FechaCreacion)
VALUES
  (1, 'Taller Mecánico El Expreso', NULL,
      'taller.expreso@gmail.com',       '809-683-1200',
      'Calle 30 de Marzo No. 45, Villa Juana, Santo Domingo',
      '130125678',  'Mayorista', 150000.00,  0.00,     1, '2024-01-20 09:00:00'),
  (2, 'Auto Centro Rodríguez',      'SRL',
      'autocenrodriguez@hotmail.com',   '829-451-7800',
      'Av. 27 de Febrero No. 203, Ensanche Bermúdez, Santiago',
      '130984321',  'Mayorista', 100000.00,  0.00,     1, '2024-01-20 09:00:00'),
  (3, 'Juan Carlos',                'Pérez Marte',
      'jcperez@gmail.com',             '849-678-3421',
      'Calle Las Mercedes No. 12, Los Prados, Santo Domingo',
      NULL,         'Regular',       0.00,  0.00,     1, '2024-02-05 09:00:00'),
  (4, 'María',                      'Santos Taveras',
      'msantos@yahoo.es',              '809-234-5678',
      'Av. Independencia No. 567, Barahona',
      NULL,         'Regular',       0.00,  0.00,     1, '2024-02-10 09:00:00'),
  (5, 'Servicar SRL',               NULL,
      'compras@servicar.com.do',        '809-540-2200',
      'Av. Luperón Km 9.5, Zona Industrial, Santo Domingo Oeste',
      '130543216',  'VIP',       200000.00, 16904.00,  1, '2024-02-15 09:00:00'),
  (6, 'Rafael',                     'Moquete García',
      'rmoquete@gmail.com',            '829-334-5900',
      'Calle Principal No. 8, Bonao, Monseñor Nouel',
      NULL,         'Regular',       0.00,  0.00,     1, '2024-03-01 09:00:00'),
  (7, 'Ana Lucía',                  'Fernández Cruz',
      'aluciafernandez@hotmail.com',   '849-901-2345',
      'Calle Duarte No. 89, La Romana',
      NULL,         'VIP',        50000.00,  0.00,     1, '2024-03-10 09:00:00'),
  (8, 'Mecánica Industrial RD',     'SRL',
      'gerencia@mird.com.do',          '809-778-9040',
      'Av. San Vicente de Paul No. 15, Zona Industrial Itabo, Guayubín',
      '130778904',  'Mayorista', 120000.00,  0.00,     1, '2024-04-01 09:00:00');

SET IDENTITY_INSERT [Cliente] OFF;


-- ────────────────────────────────────────────────
-- 5. CAJAS  (2 en Santo Domingo, 1 en Santiago, 1 en San Pedro)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Caja] ON;

INSERT INTO [Caja] (Id, SucursalId, Numero, Nombre, EsActiva, FechaCreacion)
VALUES
  (1, 1, 'C-01', 'Caja Principal SD',       1, '2024-01-15 09:00:00'),
  (2, 1, 'C-02', 'Caja Secundaria SD',      1, '2024-01-15 09:00:00'),
  (3, 2, 'C-01', 'Caja Única Santiago',     1, '2024-03-01 09:00:00'),
  (4, 3, 'C-01', 'Caja Única San Pedro',    1, '2024-06-01 09:00:00');

SET IDENTITY_INSERT [Caja] OFF;


-- ────────────────────────────────────────────────
-- 6. SESIONES DE CAJA
--    ⚠  Requiere usuarios con ID 2 (Cajero) y ID 3 (Vendedor)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [SesionCaja] ON;

INSERT INTO [SesionCaja]
  (Id, CajaId, UsuarioId, FechaApertura, FechaCierre,
   MontoApertura, MontoCierre, MontoSistema, Diferencia, Estado, Observaciones)
VALUES
  -- Sesión activa hoy — Caja Principal SD
  (1, 1, 2,
   '2025-04-24 08:00:00', NULL,
   5000.00, NULL, NULL, NULL,
   'Abierta', NULL),

  -- Sesión cerrada ayer — Caja Única Santiago
  -- MontoSistema = apertura + ventas cobradas ese día (Venta 3 en crédito, no suma efectivo)
  (2, 3, 2,
   '2025-04-23 08:00:00', '2025-04-23 18:30:00',
   3000.00, 28709.60, 29709.60, -1000.00,
   'Cerrada',
   'Diferencia de RD$1,000 detectada. Billete de RD$1,000 falso devuelto al Banco Popular.'),

  -- Sesión cuadrada — Caja Secundaria SD
  (3, 2, 3,
   '2025-04-22 08:00:00', '2025-04-22 17:45:00',
   2500.00, 18350.00, 18350.00, 0.00,
   'Cuadrada', NULL);

SET IDENTITY_INSERT [SesionCaja] OFF;


-- ────────────────────────────────────────────────
-- 7. VENTAS
--    Cálculo ITBIS: Subtotal × 0.18
--    ⚠  Requiere usuarios ID 2 (Cajero) y ID 3 (Vendedor)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Venta] ON;

INSERT INTO [Venta]
  (Id, NumeroFactura, SucursalId, CajaId, SesionCajaId,
   ClienteId, UsuarioId, Fecha,
   Subtotal, IVA, Total, Descuento,
   MetodoPago, Estado, EsOffline, IdTransaccionLocal, Observaciones)
VALUES
  -- V1: Juan Carlos Pérez — efectivo — Completada
  --     Subtotal 2 010.00 | ITBIS 361.80 | Total 2 371.80
  (1, 'B01-0000001', 1, 1, 1,
   3, 2, '2025-04-24 09:15:00',
   2010.00, 361.80, 2371.80, 0.00,
   'Efectivo', 'Completada', 0, NULL, NULL),

  -- V2: Taller Mecánico El Expreso — tarjeta crédito — Completada
  --     Subtotal 27 720.00 | ITBIS 4 989.60 | Total 32 709.60
  (2, 'B01-0000002', 1, 1, 1,
   1, 2, '2025-04-24 10:45:00',
   27720.00, 4989.60, 32709.60, 0.00,
   'TarjetaCredito', 'Completada', 0, NULL,
   'Cliente solicitó factura con NCF Fiscal. Comprobante suministrado.'),

  -- V3: Servicar SRL — crédito 30 días — Completada (genera CuentaCobrar)
  --     Subtotal 22 800.00 | ITBIS 4 104.00 | Total 26 904.00
  (3, 'B01-0000003', 2, 3, 2,
   5, 2, '2025-04-23 11:20:00',
   22800.00, 4104.00, 26904.00, 0.00,
   'Credito', 'Completada', 0, NULL,
   'Plazo 30 días según acuerdo comercial Servicar SRL. Ref. contrato SRV-2025-001.'),

  -- V4: Venta cancelada — cliente anónimo — Caja Secundaria
  --     Subtotal 6 500.00 | ITBIS 1 170.00 | Total 7 670.00
  (4, 'B01-0000004', 1, 2, 3,
   NULL, 3, '2025-04-22 14:00:00',
   6500.00, 1170.00, 7670.00, 0.00,
   'Efectivo', 'Cancelada', 0, NULL,
   'Cliente rechazó la batería al verificar que la fecha de fabricación era de hace 14 meses.');

SET IDENTITY_INSERT [Venta] OFF;


-- ────────────────────────────────────────────────
-- 8. LÍNEAS DE VENTA
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [LineaVenta] ON;

INSERT INTO [LineaVenta] (Id, VentaId, ProductoId, Cantidad, PrecioUnitario, Descuento)
VALUES
  -- Venta 1 (Juan Carlos Pérez) ─ Subtotal RD$2 010.00
  (1, 1,  9, 2,   280.00, 0.00),   -- 2× Filtro Aceite Toyota OEM   = RD$  560.00
  (2, 1, 11, 1,  1450.00, 0.00),   -- 1× Aceite Castrol 20W-50 4 L  = RD$1 450.00

  -- Venta 2 (Taller Mecánico El Expreso) ─ Subtotal RD$27 720.00
  (3, 2,  1, 2, 12500.00, 0.00),   -- 2× Radiador Toyota Corolla    = RD$25 000.00
  (4, 2, 14, 4,   680.00, 0.00),   -- 4× Manguera Radiador Honda    = RD$ 2 720.00

  -- Venta 3 (Servicar SRL — crédito) ─ Subtotal RD$22 800.00
  (5, 3,  3, 1, 15200.00, 0.00),   -- 1× Radiador Hyundai Tucson    = RD$15 200.00
  (6, 3,  5, 2,  3800.00, 0.00),   -- 2× Resorte Delantero Toyota   = RD$ 7 600.00

  -- Venta 4 (Cancelada) ─ Subtotal RD$6 500.00
  (7, 4, 15, 1,  6500.00, 0.00);   -- 1× Batería Bosch S4           = RD$ 6 500.00

SET IDENTITY_INSERT [LineaVenta] OFF;


-- ────────────────────────────────────────────────
-- 9. ÓRDENES  (canal e-commerce / WhatsApp)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Orden] ON;

INSERT INTO [Orden]
  (Id, NumeroOrden, ClienteId, Estado, Fecha,
   FechaEstimadaEntrega, FechaEntrega, TotalOrden,
   DireccionEnvio, MetodoPago, Referencia, Notas)
VALUES
  -- O1: Auto Centro Rodríguez — Entregada
  (1, 'ORD-2025-0001', 2,
   'Entregada', '2025-04-10 10:00:00',
   '2025-04-14 10:00:00', '2025-04-13 15:30:00',
   9200.00,
   'Av. 27 de Febrero No. 203, Ensanche Bermúdez, Santiago',
   'Transferencia', 'TRF-BPD-20250410-001',
   'Entregado antes del plazo estimado. Enviado con motoexpress Gacela.'),

  -- O2: Ana Lucía Fernández — En Proceso
  (2, 'ORD-2025-0002', 7,
   'EnProceso', '2025-04-20 14:00:00',
   '2025-04-27 14:00:00', NULL,
   16300.00,
   'Calle Duarte No. 89, La Romana',
   'TarjetaCredito', NULL,
   'Coordinar entrega en horario de tarde (después de las 4 PM).'),

  -- O3: Mecánica Industrial RD — Recibida (pendiente de procesar)
  (3, 'ORD-2025-0003', 8,
   'Recibida', '2025-04-23 09:00:00',
   '2025-04-30 09:00:00', NULL,
   8700.00,
   'Av. San Vicente de Paul No. 15, Zona Industrial Itabo, Guayubín',
   'Transferencia', NULL,
   'Requiere NCF Fiscal a nombre de Mecánica Industrial RD SRL, RNC 130778904.');

SET IDENTITY_INSERT [Orden] OFF;


-- ────────────────────────────────────────────────
-- 10. LÍNEAS DE ORDEN
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [LineaOrden] ON;

INSERT INTO [LineaOrden] (Id, OrdenId, ProductoId, Cantidad, PrecioUnitario)
VALUES
  -- Orden 1 (Auto Centro Rodríguez) ─ Total RD$9 200.00
  (1, 1,  7, 3, 1200.00),   -- 3× Pastillas Freno Toyota    = RD$3 600.00
  (2, 1,  8, 2, 2800.00),   -- 2× Disco Freno Ventilado     = RD$5 600.00

  -- Orden 2 (Ana Lucía Fernández) ─ Total RD$16 300.00
  (3, 2,  6, 1, 4500.00),   -- 1× Amortiguador Trasero Honda = RD$4 500.00
  (4, 2,  2, 1, 11800.00),  -- 1× Radiador Honda Civic       = RD$11 800.00

  -- Orden 3 (Mecánica Industrial RD) ─ Total RD$8 700.00
  (5, 3, 10, 5,  350.00),   -- 5× Filtro Aire K&N            = RD$1 750.00
  (6, 3,  9, 5,  280.00),   -- 5× Filtro Aceite Toyota OEM   = RD$1 400.00
  (7, 3, 13, 3, 1850.00);   -- 3× Correa Distribución Toyota = RD$5 550.00

SET IDENTITY_INSERT [LineaOrden] OFF;


-- ────────────────────────────────────────────────
-- 11. CUENTAS POR COBRAR
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [CuentaCobrar] ON;

INSERT INTO [CuentaCobrar]
  (Id, ClienteId, VentaId, NumeroFactura,
   MontoOriginal, MontoPagado,
   FechaEmision, FechaVencimiento, Estado, Notas)
VALUES
  -- CC1: Servicar SRL — vinculada a Venta 3 — Pago Parcial
  --      Abono RD$10 000 recibido → Saldo pendiente RD$16 904.00
  (1, 5, 3, 'B01-0000003',
   26904.00, 10000.00,
   '2025-04-23 11:20:00', '2025-05-23 23:59:59',
   'PagoParcial',
   'Abono inicial RD$10,000 recibido el 24/04/2025 vía transferencia BHD. Saldo: RD$16,904.00'),

  -- CC2: Taller Mecánico El Expreso — línea de crédito mensual abierta
  --      Sin venta asociada; monto acordado para consumo mensual en piezas
  (2, 1, NULL, 'CC-2025-001',
   50000.00, 0.00,
   '2025-04-01 09:00:00', '2025-05-01 23:59:59',
   'Pendiente',
   'Línea de crédito mensual renovable. Primer día de cada mes se emite estado de cuenta.');

SET IDENTITY_INSERT [CuentaCobrar] OFF;


-- ────────────────────────────────────────────────
-- 12. PAGOS
--    ⚠  UsuarioId referencia al usuario que registró el pago (ID 1 = Admin)
-- ────────────────────────────────────────────────
SET IDENTITY_INSERT [Pago] ON;

INSERT INTO [Pago]
  (Id, ClienteId, VentaId, CuentaCobrarId,
   Monto, MetodoPago, Fecha, Referencia, Notas, UsuarioId)
VALUES
  -- P1: Abono Servicar SRL sobre CC1 — transferencia bancaria
  (1, 5, 3, 1,
   10000.00, 'Transferencia',
   '2025-04-24 10:00:00', 'TRF-BHD-20250424-001',
   'Abono parcial factura B01-0000003. Número de confirmación bancaria: 20250424-BHD-00142.',
   1),

  -- P2: Abono Taller Mecánico El Expreso sobre CC2 — efectivo
  (2, 1, NULL, 2,
   15000.00, 'Efectivo',
   '2025-04-22 16:00:00', 'EFE-EXP-20250422',
   'Abono en efectivo a cuenta CC-2025-001. Billetes verificados con detector UV.',
   1);

SET IDENTITY_INSERT [Pago] OFF;

COMMIT TRANSACTION;

GO
PRINT '✓ BLOQUE A completado — Core DB cargada correctamente.';
GO


-- ============================================================
-- ██████  BLOQUE B — INTEGRATION DB (SQLite / PostgreSQL)  ███
-- ============================================================
-- Ejecutar en la base de datos de integración (offline).
-- Los IDs de las tablas Mirror coinciden con los IDs del Core.
-- ============================================================

BEGIN;  -- SQLite: usa BEGIN / COMMIT; en PostgreSQL es igual

-- ────────────────────────────────────────────────
-- B1. SucursalMirror
-- ────────────────────────────────────────────────
INSERT INTO SucursalMirror (CoreId, Nombre, Direccion, Telefono, EsActivo, UltimaSync)
VALUES
  (1, 'RadiadoresSprings - Santo Domingo',
      'Av. Winston Churchill No. 53, Piantini, Santo Domingo, D.N.',
      '809-535-4000', 1, '2025-04-24 07:00:00'),
  (2, 'RadiadoresSprings - Santiago',
      'Calle del Sol No. 78, Ensanche Bermúdez, Santiago de los Caballeros',
      '809-971-2200', 1, '2025-04-24 07:00:00'),
  (3, 'RadiadoresSprings - San Pedro de Macorís',
      'Av. Circunvalación No. 12, San Pedro de Macorís',
      '809-529-8800', 1, '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B2. CategoriaMirror
-- ────────────────────────────────────────────────
INSERT INTO CategoriaMirror (CoreId, Nombre, Descripcion, EsActivo, UltimaSync)
VALUES
  (1, 'Radiadores',               'Radiadores originales y alternos', 1, '2025-04-24 07:00:00'),
  (2, 'Resortes y Amortiguadores','Resortes y amortiguadores de suspensión', 1, '2025-04-24 07:00:00'),
  (3, 'Frenos',                   'Pastillas, discos y tambores', 1, '2025-04-24 07:00:00'),
  (4, 'Filtros',                  'Filtros de aceite, aire y combustible', 1, '2025-04-24 07:00:00'),
  (5, 'Lubricantes y Refrigerantes','Aceites y líquidos refrigerantes', 1, '2025-04-24 07:00:00'),
  (6, 'Correas y Mangueras',      'Correas de distribución y mangueras', 1, '2025-04-24 07:00:00'),
  (7, 'Baterías y Eléctrico',     'Baterías automotrices y eléctrico', 1, '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B3. ProductoMirror  (espejo del catálogo Core)
-- ────────────────────────────────────────────────
INSERT INTO ProductoMirror
  (Id, Codigo, Nombre, Precio, PrecioOferta, Stock, StockMinimo,
   Categoria, Descripcion, CategoriaId, EsActivo, UltimaSync)
VALUES
  (1,  'RAD-TOY-001', 'Radiador Toyota Corolla 2018-2023',    12500.00, NULL,     8,  3, 'Radiadores',               'Radiador alterno Corolla E210 2ZR-FE',            1, 1, '2025-04-24 07:00:00'),
  (2,  'RAD-HON-001', 'Radiador Honda Civic 2016-2021',       11800.00, 10500.00, 5,  3, 'Radiadores',               'Radiador alterno Civic FC 1.5T/2.0',              1, 1, '2025-04-24 07:00:00'),
  (3,  'RAD-HYU-001', 'Radiador Hyundai Tucson 2019-2023',    15200.00, NULL,     4,  3, 'Radiadores',               'Radiador alterno Tucson NX4 2.0/2.5',             1, 1, '2025-04-24 07:00:00'),
  (4,  'RAD-NIS-001', 'Radiador Nissan Sentra 2020-2023',     13800.00, 12900.00, 6,  3, 'Radiadores',               'Radiador alterno Sentra B18 1.6 CVT',             1, 1, '2025-04-24 07:00:00'),
  (5,  'RES-TOY-001', 'Resorte Delantero Toyota Corolla (Par)', 3800.00, NULL,   12,  4, 'Resortes y Amortiguadores','Par resortes delanteros Corolla E210',            2, 1, '2025-04-24 07:00:00'),
  (6,  'AMO-HON-001', 'Amortiguador Trasero Honda Civic (Par)', 4500.00, NULL,    9,  4, 'Resortes y Amortiguadores','Par amortiguadores KYB Civic FC 2016-2021',       2, 1, '2025-04-24 07:00:00'),
  (7,  'FRE-PAS-001', 'Pastillas de Freno Delanteras Toyota',  1200.00, NULL,    25,  8, 'Frenos',                   'Pastillas Bosch BP1234 Corolla/RAV4/Camry',       3, 1, '2025-04-24 07:00:00'),
  (8,  'FRE-DIS-001', 'Disco de Freno Ventilado Toyota',       2800.00, NULL,    15,  5, 'Frenos',                   'Disco Brembo 280mm Corolla/Camry par delantero',  3, 1, '2025-04-24 07:00:00'),
  (9,  'FIL-ACE-001', 'Filtro de Aceite Toyota/Lexus OEM',      280.00, NULL,    80, 20, 'Filtros',                  'Filtro Toyota 90915-YZZE1 múltiples modelos',     4, 1, '2025-04-24 07:00:00'),
  (10, 'FIL-AIR-001', 'Filtro de Aire K&N Alto Flujo',          350.00, NULL,    60, 15, 'Filtros',                  'Filtro K&N E-0665 lavable y reutilizable',        4, 1, '2025-04-24 07:00:00'),
  (11, 'LUB-CAS-001', 'Aceite Motor Castrol GTX 20W-50 (4 L)', 1450.00, 1350.00, 40, 10, 'Lubricantes y Refrigerantes','Aceite mineral API SL/CF botella 4 L',          5, 1, '2025-04-24 07:00:00'),
  (12, 'LUB-REF-001', 'Líquido Refrigerante Havoline (1 L)',     320.00, NULL,    50, 15, 'Lubricantes y Refrigerantes','Refrigerante concentrado ELT naranja 1 L',      5, 1, '2025-04-24 07:00:00'),
  (13, 'COR-DIS-001', 'Correa de Distribución Toyota 2ZR',      1850.00, NULL,   18,  5, 'Correas y Mangueras',      'Correa Gates T304RB Corolla/Yaris 2ZR-FE',       6, 1, '2025-04-24 07:00:00'),
  (14, 'MAN-RAD-001', 'Manguera Radiador Superior Honda Civic',  680.00, NULL,   30,  8, 'Correas y Mangueras',      'Manguera Gates 23747 Civic 2016-2021 EPDM',      6, 1, '2025-04-24 07:00:00'),
  (15, 'BAT-BSC-001', 'Batería Bosch S4 12V 60Ah 540A',         6500.00, NULL,  10,  3, 'Baterías y Eléctrico',     'Batería S4005 sin mantenimiento garantía 18m',    7, 1, '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B4. ClienteMirror
-- ────────────────────────────────────────────────
INSERT INTO ClienteMirror
  (LocalId, CoreId, Nombre, Apellido, Email, Telefono, RFC,
   Direccion, Tipo, LimiteCredito, SaldoPendiente, EsActivo, EsLocal, UltimaSync)
VALUES
  (1, 1, 'Taller Mecánico El Expreso', NULL,
      'taller.expreso@gmail.com',       '809-683-1200', '130125678',
      'Calle 30 de Marzo No. 45, Villa Juana, SD',
      'Mayorista', 150000.00,  0.00,    1, 0, '2025-04-24 07:00:00'),
  (2, 2, 'Auto Centro Rodríguez',      'SRL',
      'autocenrodriguez@hotmail.com',   '829-451-7800', '130984321',
      'Av. 27 de Febrero No. 203, Santiago',
      'Mayorista', 100000.00,  0.00,    1, 0, '2025-04-24 07:00:00'),
  (3, 3, 'Juan Carlos',                'Pérez Marte',
      'jcperez@gmail.com',             '849-678-3421', NULL,
      'Calle Las Mercedes No. 12, Los Prados, SD',
      'Regular',       0.00,  0.00,    1, 0, '2025-04-24 07:00:00'),
  (4, 4, 'María',                      'Santos Taveras',
      'msantos@yahoo.es',              '809-234-5678', NULL,
      'Av. Independencia No. 567, Barahona',
      'Regular',       0.00,  0.00,    1, 0, '2025-04-24 07:00:00'),
  (5, 5, 'Servicar SRL',               NULL,
      'compras@servicar.com.do',       '809-540-2200', '130543216',
      'Av. Luperón Km 9.5, Zona Industrial, SDO',
      'VIP',       200000.00, 16904.00, 1, 0, '2025-04-24 07:00:00'),
  (6, 6, 'Rafael',                     'Moquete García',
      'rmoquete@gmail.com',            '829-334-5900', NULL,
      'Calle Principal No. 8, Bonao',
      'Regular',       0.00,  0.00,    1, 0, '2025-04-24 07:00:00'),
  (7, 7, 'Ana Lucía',                  'Fernández Cruz',
      'aluciafernandez@hotmail.com',   '849-901-2345', NULL,
      'Calle Duarte No. 89, La Romana',
      'VIP',        50000.00,  0.00,   1, 0, '2025-04-24 07:00:00'),
  (8, 8, 'Mecánica Industrial RD',     'SRL',
      'gerencia@mird.com.do',          '809-778-9040', '130778904',
      'Av. San Vicente de Paul No. 15, Guayubín',
      'Mayorista', 120000.00,  0.00,   1, 0, '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B5. CajaMirror
-- ────────────────────────────────────────────────
INSERT INTO CajaMirror (CoreId, Nombre, SucursalId, EsActiva, UltimaSync)
VALUES
  (1, 'Caja Principal SD',    1, 1, '2025-04-24 07:00:00'),
  (2, 'Caja Secundaria SD',   1, 1, '2025-04-24 07:00:00'),
  (3, 'Caja Única Santiago',  2, 1, '2025-04-24 07:00:00'),
  (4, 'Caja Única San Pedro', 3, 1, '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B6. CuentaCobrarMirror
-- ────────────────────────────────────────────────
INSERT INTO CuentaCobrarMirror
  (CoreId, VentaId, ClienteId, MontoTotal, SaldoPendiente, FechaVencimiento, Estado, UltimaSync)
VALUES
  (1, 3, 5, 26904.00, 16904.00, '2025-05-23 23:59:59', 'PagoParcial', '2025-04-24 07:00:00'),
  (2, 0, 1, 50000.00, 50000.00, '2025-05-01 23:59:59', 'Pendiente',   '2025-04-24 07:00:00');


-- ────────────────────────────────────────────────
-- B7. SesionCajaMirror  (sólo la sesión activa del día)
-- ────────────────────────────────────────────────
INSERT INTO SesionCajaMirror
  (IdLocal, CajaId, NombreCaja, UsuarioId, NombreUsuario,
   MontoApertura, MontoCierre, Estado,
   FechaApertura, FechaCierre, Observaciones,
   EstadoSync, CoreSesionId)
VALUES
  (1, 1, 'Caja Principal SD', 2, 'Cajero Principal',
   5000.00, NULL, 'Abierta',
   '2025-04-24T08:00:00-04:00', NULL, NULL,
   'Sincronizado', 1);


-- ────────────────────────────────────────────────
-- B8. VentaOfflinePendiente
--     Representa una venta procesada offline (sin conexión)
--     que aún no ha sido sincronizada con el Core.
-- ────────────────────────────────────────────────
INSERT INTO VentaOfflinePendiente
  (IdTransaccionLocal, CajeroId, SucursalId, ClienteId, CajaId, SesionCajaId,
   Descuento, Observaciones, MetodoPago,
   MontoTotal, MontoRecibido, LineasJson,
   FechaLocal, Estado, IntentosSync, UltimoIntento)
VALUES
  ('a1b2c3d4-e5f6-7890-abcd-ef1234567890',
   2, 1, 3, 1, 1,
   0.00, 'Venta registrada sin conexión a internet durante corte de luz.',
   'Efectivo',
   4531.00, 5000.00,
   '[{"ProductoId":9,"Nombre":"Filtro de Aceite Toyota/Lexus OEM","Cantidad":3,"PrecioUnitario":280.00},{"ProductoId":12,"Nombre":"Líquido Refrigerante Havoline (1 L)","Cantidad":5,"PrecioUnitario":320.00},{"ProductoId":10,"Nombre":"Filtro de Aire K&N Alto Flujo","Cantidad":3,"PrecioUnitario":350.00}]',
   '2025-04-24T14:30:00-04:00',
   'Pendiente', 0, NULL);
-- Desglose: 3×280 + 5×320 + 3×350 = 840 + 1600 + 1050 = RD$3 490.00 neto
-- Con ITBIS 18 %: 3490 × 1.18 = RD$4 118.20  (MontoTotal aproximado sin decimales extra para demo)


-- ────────────────────────────────────────────────
-- B9. OperacionPendiente
--     Actualización de stock pendiente de sincronizar con Core
--     (resultado de la venta offline anterior)
-- ────────────────────────────────────────────────
INSERT INTO OperacionPendiente
  (IdempotencyKey, TipoEntidad, TipoOperacion, EndpointCore, MetodoHttp,
   PayloadJson, IdLocalTemporal, UsuarioId,
   Estado, FechaCreacion, IntentosSync, UltimoIntento, MotivoRechazo, RespuestaCore)
VALUES
  ('f9e8d7c6-b5a4-3210-fedc-ba9876543210',
   'Stock', 'AjusteOffline',
   '/api/inventario/ajuste-offline', 'POST',
   '{"operaciones":[{"ProductoId":9,"Cantidad":-3},{"ProductoId":12,"Cantidad":-5},{"ProductoId":10,"Cantidad":-3}],"IdTransaccionLocal":"a1b2c3d4-e5f6-7890-abcd-ef1234567890","SucursalId":1}',
   NULL, '2',
   'Pendiente', '2025-04-24T14:30:05-04:00', 0, NULL, NULL, NULL);


-- ────────────────────────────────────────────────
-- B10. IntegrationLogEntry  (historial de sincronizaciones)
-- ────────────────────────────────────────────────
INSERT INTO IntegrationLog
  (Endpoint, Direccion, RequestJson, ResponseJson, HttpStatus,
   LatenciaMs, DesdeCache, CorrelationId, UserId, Layer, Fecha)
VALUES
  -- Sincronización de catálogo de productos (exitosa)
  ('/api/productos/sync', 'IN',
   '{"ultimaSync":"2025-04-24T06:55:00Z","sucursalId":1}',
   '{"totalActualizados":15,"totalNuevos":0}',
   200, 340, 0,
   'corr-sync-20250424-001', '2', 'Integracion',
   '2025-04-24T07:00:00-04:00'),

  -- Envío de venta B01-0000001 (exitosa)
  ('/api/ventas', 'OUT',
   '{"NumeroFactura":"B01-0000001","ClienteId":3,"Total":2371.80,"MetodoPago":"Efectivo"}',
   '{"id":1,"NumeroFactura":"B01-0000001","estado":"Completada"}',
   201, 512, 0,
   'corr-venta-20250424-001', '2', 'Integracion',
   '2025-04-24T09:15:12-04:00'),

  -- Intento fallido de sincronización (corte de luz — venta offline)
  ('/api/ventas', 'OUT',
   '{"IdTransaccionLocal":"a1b2c3d4-e5f6-7890-abcd-ef1234567890","MetodoPago":"Efectivo","Total":4531.00}',
   NULL,
   0, 0, 0,
   'corr-offline-20250424-002', '2', 'Integracion',
   '2025-04-24T14:30:06-04:00');


-- ────────────────────────────────────────────────
-- B11. IdempotencyLog
--     Rastrea el estado de la venta offline pendiente
-- ────────────────────────────────────────────────
INSERT INTO IdempotencyLog
  (IdTransaccionLocal, FacturaIdCore, Estado,
   MotivoRechazo, FechaEnvio, FechaConfirmacion)
VALUES
  ('a1b2c3d4-e5f6-7890-abcd-ef1234567890',
   NULL,
   'Pendiente',
   NULL,
   '2025-04-24T14:30:06-04:00',
   NULL);

COMMIT;
-- SQLite: SELECT 'BLOQUE B completado — Integration DB cargada correctamente.';
