# Emergency system — setup guide (Windows)

Three programs, one database:

```
Phone app (MAUI)  ─┐
                   ├─►  Server (ASP.NET Core, port 5000)  ─►  MySQL
Desk app (WPF)    ─┘
```
Folders in this zip: `EmergencyDesk.Server` and `EmergencyDesk.Desktop`.
Your phone app (`EmergencyApp.User`) is unchanged.

## 1. Install (one time)
1. **Visual Studio 2022 Community** with workloads ".NET desktop development", "ASP.NET and web development" and ".NET Multi-platform App UI development".
2. **MySQL Community Server** (https://dev.mysql.com/downloads/installer/). Choose "Server only", and remember the **root password** you set.

## 2. Configure the server
Open `EmergencyDesk.Server\appsettings.json` and change:
- `Password=YOUR_MYSQL_PASSWORD` -> your MySQL root password
- `Jwt:Key` -> any long random text (32+ characters)

The database and tables are created automatically on first start.

## 3. Start the server (do this first, every time)
1. Open `EmergencyDesk.Server\EmergencyDesk.Server.csproj` in Visual Studio.
2. Press **F5** (or `dotnet run` in that folder).
3. Open http://localhost:5000/swagger in a browser. If you see the API list, it works.

First admin account (created automatically): **username `admin` / password `Admin123!`**. Change it in `appsettings.json` (`SeedAdmin`) BEFORE the first run.

## 4. Start the desk app
1. Open `EmergencyDesk.Desktop\EmergencyDesk.Desktop.csproj` in a second Visual Studio window.
2. `appsettings.json` already points to `http://localhost:5000/`.
3. Press **F5** and sign in with `admin` / `Admin123!`.
4. Go to **Responders** and add at least one (e.g. "Unit 1", unit "Ambulance 1").

## 5. Start the phone app
- **Emulator:** run `EmergencyApp.User` on an Android emulator. It already uses `http://10.0.2.2:5000/`, which means "this PC".
- **Real phone:** phone and PC on the same Wi-Fi. Find the PC address with `ipconfig` (e.g. 192.168.1.20). In the phone app set the server to `http://192.168.1.20:5000/`. Allow port 5000 in Windows Firewall when asked.

## 6. Test the whole flow
1. In the phone app: **Register**, then send an SOS.
2. In the desk app: within ~5 seconds it appears in Incidents with a sound alert.
3. Select it, **Assign** a responder, then **Update** status.
4. In the phone app: History shows the new status and responder name.
5. Desk app -> **App Users** (admin only): deactivate the user; their next login/SOS is refused.

## How the two apps' statuses line up
Desk statuses: Received, Dispatched, EnRoute, OnScene, Resolved, Cancelled.
The phone only knows Received / Dispatched / Resolved / Cancelled, so EnRoute and OnScene show as "Dispatched" on the phone.
Emergency types: Medical, Fire, Police, Rescue.

## Known limits (good next steps)
- Not compiled by me (no NuGet access here) — expect small build fixes; send me any error text.
- Only the seeded admin exists. Creating dispatcher accounts needs a staff-management screen (not built yet).
- HTTP only, for development. Use HTTPS before real use.
- Desk app refreshes by polling every 5 s (SignalR would make it instant).
