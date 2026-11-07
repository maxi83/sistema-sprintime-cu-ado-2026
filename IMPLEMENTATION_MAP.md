# Mapa de implementación SprinTime V1

| Blueprint / pedido | Implementación |
|---|---|
| Home premium | `Views/Home/Index.cshtml` + `wwwroot/css/site.css` |
| Carreras / ficha pública | `EventsController` + `Views/Events/*` |
| Distancias + categorías | `EventDistance`, `Category` |
| Inscripción | `RegistrationController` |
| Mercado Pago | `MercadoPagoService` + `MercadoPagoController` |
| Webhook verificado | HMAC-SHA256 + consulta de pago server-to-server |
| Cierre por fecha/hora | `RegistrationClosureHostedService` |
| PDF / CSV / TXT | `ExportService` |
| Race Pass QR | `QrCodeService` + `Views/Registration/RacePass.cshtml` |
| Check-in | `RaceDayController.CheckIn` |
| Live Timing básico | `RaceDayController.Timing/RegisterPass/Sync` |
| Offline Race Day | `wwwroot/js/site.js` + `localStorage` |
| Seguimiento público | `FollowController` |
| Resultados estructurados | `Result` + `ResultsController` |
| Ranking | `RankingService` |
| Finisher Card | `Views/Results/Finisher.cshtml` |
| Diploma PDF | `ExportService.BuildDiploma` |
| Espacio ciclista | `CyclistController` |
| Admin Cockpit | `AdminController` + `Views/Admin/*` |
| PWA básica | `manifest.webmanifest` + `sw.js` |
