# Calendar LAN

A desktop **calendar application** (Google Calendar style) that runs **locally over a LAN**,
with no dependency on the internet or cloud services.

Multiple machines on the same network can schedule, view and edit shared events in real time.
One machine acts as the **central server** that stores all events; the others connect to it as
**clients**.

## Goals

> Target feature set — see [Status](#status) for what already works.

- Schedule events with date, start/end time, title and description.
- Calendar views: **month**, **week** and **day**.
- Create, edit and delete events.
- **Real-time** synchronization across all LAN clients.
- Zero cloud configuration — everything lives on the local network.
- Cross-platform: **Windows, Linux and macOS**.

## Tech Stack

| Component | Technology |
|---|---|
| UI (client) | Avalonia UI (MVVM) |
| Central server | ASP.NET Core (Minimal API) |
| Real-time | SignalR |
| Database | SQLite + EF Core |
| Language | C# / .NET 9 |

## Architecture

Client–server model over the LAN. One machine runs the server (storing events in SQLite);
clients connect to it via its local IP (e.g. `http://192.168.1.50:5000`). CRUD operations go
over REST, and changes are broadcast to every client via SignalR.

```
   Client A     Client B     Client C      ← Avalonia UI
       │            │            │
       └────────────┼────────────┘
             REST + SignalR (LAN)
                    │
            Central server                  ← ASP.NET Core + SQLite
```

## Project Structure

```
Calendar/
├── Calendar.Server/     → ASP.NET Core: REST API + SignalR Hub + SQLite/EF Core
├── Calendar.Client/     → Avalonia UI: calendar views (MVVM)
├── Calendar.Shared/     → Shared models and contracts (Event, DTOs)
│   └── Models/Event.cs  → the Event model, defined once and used by both sides
├── global.json          → pins the .NET SDK to 9.0.3xx
└── Calendar.sln         → Solution grouping the three projects
```

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download) — the build is pinned to `9.0.3xx`
  through `global.json`.
- An IDE: **Visual Studio 2022 (17.12 or newer)**, VS Code + C# Dev Kit, or Rider.
  Older Visual Studio builds cannot load a .NET 9 solution.
- Avalonia templates are **not** needed to build this repository. They are only required to
  scaffold *new* Avalonia projects: `dotnet new install Avalonia.Templates`.

## Getting Started — Visual Studio 2022

1. Open `Calendar.sln` (double-click it, or `File → Open → Project/Solution`).
2. **Wait for the NuGet restore to finish** — the status bar shows *"Restoring NuGet
   packages…"*. The first restore pulls all of Avalonia and takes a while. Building before it
   finishes produces misleading reference errors.
3. Configure both apps to launch together:
   - Right-click the **solution** node → **Configure Startup Projects…**
   - Select **Multiple startup projects**
   - `Calendar.Server` → **Start**, `Calendar.Client` → **Start**, `Calendar.Shared` → **None**
   - Keep `Calendar.Server` above `Calendar.Client` so the server boots first
4. Build with **Ctrl+Shift+B**.
5. Run with **F5**.

You should get a console window logging `Now listening on: http://0.0.0.0:5000` and the
Avalonia client window.

> `Calendar.Shared` is a class library and cannot be run. If the startup project dropdown
> only offers `Calendar.Shared`, set the startup project first (right-click
> `Calendar.Server` → **Set as Startup Project**).

## Getting Started — CLI

```powershell
git clone <your-repo-url>
cd calendar

dotnet build

# Run the server (on the machine acting as central)
dotnet run --project Calendar.Server

# Run the client (on each machine, in a separate terminal)
dotnet run --project Calendar.Client
```

## Verifying It Works

With the server running:

| Check | Expected |
|---|---|
| `http://localhost:5000/health` | `{"status":"ok"}` |
| `http://localhost:5000/openapi/v1.json` | The OpenAPI document (Development only) |
| Client window | Opens and renders |

`Calendar.Server/Calendar.Server.http` holds the same health request, runnable straight from
Visual Studio.

### Reaching the server from other machines

The server binds to `0.0.0.0:5000`, so it is reachable across the LAN.

1. Find the host's local IP: `ipconfig` (Windows) or `ip addr` (Linux/macOS).
2. From another machine on the same network, open `http://<host-ip>:5000/health`.
3. On the first run, Windows Firewall prompts for `dotnet` — allow it on **private networks
   only**, never public ones. If LAN clients cannot connect, this rule is the usual cause.

The server speaks **plain HTTP** by design: it is a LAN-only service, so there is no HTTPS
redirection and no `https` launch profile.

## Troubleshooting

**`dotnet --version` reports "No .NET SDKs were found"** — the 32-bit .NET install
(`C:\Program Files (x86)\dotnet`) ships runtimes only. If it precedes
`C:\Program Files\dotnet` on `PATH`, the CLI finds no SDK. Remove the x86 entry from the
machine `PATH` (elevated shell required) and restart your terminal and IDE.

**Port 5000 already in use** — change `applicationUrl` in
`Calendar.Server/Properties/launchSettings.json`, and update the client's server address to
match.

## Status

🚧 **In development.** The solution skeleton is in place: the three projects build, the
server exposes a health endpoint, and the client opens. Persistence (SQLite/EF Core),
the REST event API, SignalR sync and the calendar views are still to come.

## License

TBD
