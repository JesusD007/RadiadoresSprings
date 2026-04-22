// Este handler fue eliminado porque el comando AplicarTransaccionesOfflineCommand
// nunca se envía en el flujo vigente. La sincronización offline se realiza
// mediante HTTP POST individual con Idempotency-Key desde la Saga SyncOfflineSaga
// en IntegrationApp, no por comandos batch en el bus.
//
// Si en el futuro se desea un canal batch por NServiceBus, se puede restaurar
// este handler usando SharedContracts.Commands como fuente de contratos.
