// Los eventos cross-service (publicados por Core y consumidos por Integration)
// están definidos en el proyecto SharedContracts para garantizar que ambos extremos
// del bus de RabbitMQ usen exactamente el mismo tipo y namespace.
//
//   SharedContracts.Events.InventarioActualizadoEvent
//   SharedContracts.Events.OrdenCambioEstadoEvent
//   SharedContracts.Events.VentaAplicadaEnCoreEvent
//
// Este archivo queda intencionalmente vacío de definiciones.
// Para publicar eventos desde los servicios del Core, usa:
//   using SharedContracts.Events;
