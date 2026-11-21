# EmergencyApp.User (.NET 8 MAUI, Android)

Open `EmergencyApp.User.csproj` in Visual Studio 2022 (17.8+) with the ".NET MAUI" workload, pick an Android emulator, run.

## Architecture
MVVM (CommunityToolkit.Mvvm) · Shell navigation · sqlite-net-pcl (local cache, offline-first) · HttpClient + JWT bearer.
- `Services/DatabaseService` local SQLite (user, contacts, requests)
- `Services/ApiService` REST client; base URL stored in Preferences (default `http://10.0.2.2:5000/` = host PC from emulator)
- `Services/SyncService` pushes unsent SOS requests, pulls status updates
- SOS is always saved locally first, then sent; unsent requests retry on History refresh

## Server API contract (ASP.NET Core) the app expects (JSON, camelCase)
| Method | Route | Body | Returns |
|---|---|---|---|
| POST | `/api/auth/register` | `{fullName, phone, email, password}` | `{userId, fullName, phone, email, token}` |
| POST | `/api/auth/login` | `{email, password}` | same as above |
| POST | `/api/emergencies` (Bearer) | `{clientId, userId, type, description, latitude, longitude, createdAtUtc}` | `{serverId, clientId, status, responderName}` |
| GET | `/api/emergencies/mine` (Bearer) | – | `[{serverId, clientId, status, responderName}]` |

Status values: `Received`, `Dispatched`, `Resolved`, `Cancelled`. The server should treat `clientId` as an idempotency key so retries don't create duplicates.

## Dev notes
- `usesCleartextTraffic="true"` in the manifest is for http dev only; use HTTPS in production.
- DTOs live in `Models/Models.cs`; mirror them in your shared/server project.
