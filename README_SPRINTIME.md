# SprinTime V1 · NeoMax

Reconstrucción sobre un proyecto ASP.NET Core MVC limpio, orientada al blueprint V1 de SprinTime y al pedido de Lucho sobre cierre de inscripciones + exportación de nómina.

## Stack

- ASP.NET Core MVC · .NET 9
- SQL Server + Entity Framework Core 9
- Razor Views
- CSS/JavaScript propios, responsive y mobile-first
- QRCoder para Race Pass
- Mercado Pago Checkout Pro mediante Preferences API + Webhooks
- PWA básica (manifest + service worker)

## Módulos incluidos

1. **Web pública**
   - Home premium
   - Calendario de carreras
   - Ficha de evento
   - Distancias, categorías, cupos, precio y recorrido
   - Seguimiento público por nombre/dorsal

2. **Inscripción**
   - Datos del ciclista
   - Distancia + categoría
   - Control de cupo
   - Una inscripción por ciclista/evento
   - Estados de inscripción y pago
   - Modo demo local para aprobar pagos durante desarrollo

3. **Mercado Pago**
   - Creación de Preference
   - `external_reference` única por inscripción
   - `notification_url`
   - Webhook server-to-server
   - Consulta posterior del pago a Mercado Pago
   - Validación HMAC-SHA256 del Webhook cuando se configura `WebhookSecret`
   - El retorno del checkout **no** confirma por sí solo la inscripción

4. **Cierre de inscripción**
   - Fecha/hora configurable
   - Cierre automático mediante servicio alojado
   - Bloqueo de nuevas altas después del límite
   - Cierre manual desde Control Center
   - Exportación solo de inscriptos con pago aprobado
   - PDF, CSV y TXT

5. **Race Pass QR**
   - Pase digital
   - Dorsal, categoría, distancia y estado de pago
   - QR individual
   - Check-in desde teléfono

6. **Race Day**
   - Dashboard operativo
   - Check-in / acreditación
   - Salida, checkpoints y meta
   - Registro por dorsal
   - Cola offline en `localStorage`
   - Sincronización al recuperar señal
   - Idempotencia mediante `ClientEntryId`
   - Cálculo básico de tiempo, posición general y categoría

7. **Resultados**
   - Tabla estructurada
   - Búsqueda y categorías
   - Finisher Card
   - Diploma PDF

8. **Ranking**
   - Puntos de temporada
   - Participaciones
   - Podios
   - Categoría/equipo

9. **Espacio ciclista**
   - Carreras
   - Bicicletas
   - Kilometraje
   - Progreso de mantenimiento

10. **Control Center**
   - Métricas del evento
   - Participantes
   - Estados de pago
   - Dorsales
   - Cierre y exportación
   - Acceso a Race Day
   - Edición del evento

## Primer arranque

1. Instalar **.NET 9 SDK** y SQL Server / LocalDB.
2. Abrir una terminal dentro de la carpeta del proyecto.
3. Ejecutar:

```bash
dotnet restore
dotnet run
```

La base `SprinTimeV2` se crea automáticamente con `EnsureCreated()` y se cargan datos demo la primera vez.

### Credenciales de desarrollo

Solo en `Development`:

- Usuario: `admin@sprintime.local`
- Contraseña: `SprinTime2026!`

En producción configurar credenciales reales mediante variables de entorno o secret manager. No usar las credenciales demo.

## Configurar SQL Server

Editar `ConnectionStrings:DefaultConnection` en `appsettings.json` o usar una variable de entorno:

```text
ConnectionStrings__DefaultConnection=...
```

La cadena incluida usa LocalDB de Windows:

```text
Server=(localdb)\MSSQLLocalDB;Database=SprinTimeV2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

## Configurar Mercado Pago

No hay credenciales reales dentro del ZIP. Configurar de forma segura:

```text
MercadoPago__AccessToken=APP_USR-...
MercadoPago__PublicKey=APP_USR-...
MercadoPago__WebhookSecret=...
```

Para Webhooks, la aplicación debe publicarse con HTTPS. El endpoint es:

```text
/ MercadoPago / Webhook
```

(sin los espacios: `/MercadoPago/Webhook`)

La firma se valida contra `x-signature`, `x-request-id` y el `data.id` recibido. Después de validar la notificación, SprinTime consulta el pago en Mercado Pago y recién entonces actualiza el estado local.

## Exportación pedida por Lucho

Desde Control Center se puede descargar:

- `*-inscriptos.pdf`
- `*-inscriptos.csv`
- `*-inscriptos.txt`

La exportación incluye únicamente inscripciones `Confirmed` con pago `Approved`.

### Pendiente externo

Lucho no recordó el nombre/formato exacto del archivo de texto que requiere su software de presentismo/cronometraje. Por eso el TXT actual usa un layout interoperable base separado por `|`:

```text
DORSAL|DNI|APELLIDO|NOMBRE|DISTANCIA|CATEGORIA|PAGO|QR
```

Cuando se conozca el software o una muestra del archivo esperado, solamente hay que adaptar `Services/ExportService.cs`; el resto del flujo ya está preparado.

## Datos demo

Se crea una carrera futura (`Desafío MTB Potrero 2026`) y una carrera finalizada (`Rally Bike La Florida 2026`) para poder ver:

- home y fichas de evento;
- inscriptos y dorsales;
- resultados;
- ranking;
- Finisher Card;
- espacio ciclista.

## Producción

Antes de publicar una carrera real:

- reemplazar imágenes demo;
- configurar SQL Server de producción y backups;
- cargar credenciales reales de Mercado Pago mediante secretos;
- configurar `WebhookSecret`;
- comprobar URL pública HTTPS;
- probar un pago de prueba de punta a punta;
- definir el formato TXT exacto del sistema externo de Lucho;
- hacer prueba completa de Race Day desde celulares reales;
- migrar de `EnsureCreated()` a migraciones EF Core versionadas antes de evolucionar el esquema en producción.
