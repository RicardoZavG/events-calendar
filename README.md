# Calendar LAN

A desktop **calendar application** (Google Calendar style) that runs **locally over a LAN**,
with no dependency on the internet or cloud services.

Multiple machines on the same network can schedule, view and edit shared events in real time.
One machine acts as the **central server** that stores all events; the others connect to it as
**clients**.

## Features

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
└── Calendar.sln         → Solution grouping the three projects
```

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- Avalonia templates: `dotnet new install Avalonia.Templates`
- An IDE: Visual Studio 2022 Community / VS Code + C# Dev Kit / Rider

## Getting Started

> The solution skeleton is being set up. Commands will be finalized once it exists.

```powershell
# Clone the repository
git clone <your-repo-url>
cd calendar

# Run the server (on the machine acting as central)
dotnet run --project Calendar.Server

# Run the client (on each machine)
dotnet run --project Calendar.Client
```

## Status

🚧 **Bootstrapping** — defining the base structure.

## License

TBD
