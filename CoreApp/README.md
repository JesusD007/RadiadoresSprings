# Core (P2) — RadiadoresSprings

Capa de negocio principal del sistema de tienda de radiadores.  
Corre en `https://localhost:5001` y es consultada por **IntegrationApp (P3)**.

---

## Arquitectura — N-Tier

```
Core.Domain      → Entidades, Enums, Interfaces
Core.Data        → DbContext EF Core (SQL Server) + Repositorios  
Core.API         → Web API + Servicios + Handlers NServiceBus
Core.Console     → Consola interactiva Spectre.Console
```

## Base de datos

**SQL Server local:** `Server=MSI;Database=RadiadoresSpringsCore`  
Las migraciones se aplican automáticamente al iniciar.

## Levantar el API (P2)

```bash
cd CoreApp/Core.API
dotnet run
# API disponible en https://localhost:5001
# OpenAPI en https://localhost:5001/openapi/v1.json
```

Usuario por defecto: `admin` / `Admin123!`

## Levantar la Consola

```bash
cd CoreApp/Core.Console
dotnet run
```

La consola se conecta directamente a SQL Server y muestra un dashboard
interactivo con todos los módulos del negocio.

## Bus de mensajes (NServiceBus)

El Core comparte el directorio `.learningtransport/` con IntegrationApp.

| Rol     | Mensaje                               |
|---------|---------------------------------------|
| Suscribe | `AplicarTransaccionesOfflineCommand` |
| Publica | `InventarioActualizadoEvent`          |
| Publica | `OrdenCambioEstadoEvent`              |
| Publica | `VentaAplicadaEnCoreEvent`            |

## CI/CD

El workflow `.github/workflows/core-ci.yml` corre **independientemente** de
IntegrationApp. Se dispara solo cuando hay cambios en `CoreApp/`.

**El Core NO se despliega junto a IntegrationApp.**
