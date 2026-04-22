// El comando VentaRealizadaOfflineMessage (Caja → Integration) está definido en
// SharedContracts.Commands para que Caja (P1), Integration (P3) y cualquier
// consumidor futuro compartan el mismo contrato de bus.
//
// El patrón de sincronización vigente usa HTTP POST con Idempotency-Key
// (saga SyncOfflineSaga → POST /api/v1/ventas en Core.API).
// No se utiliza un comando batch por NServiceBus en la versión actual.
