# SmartScheduler

SmartScheduler is an enterprise scheduling and appointment management platform built with Blazor, ASP.NET Core, Entity Framework Core, SQL Server, SignalR, and ASP.NET Core Identity.

The project is designed as a C# portfolio application that demonstrates a realistic business workflow: authenticated users manage schedules, appointments, availability, notifications, reports, audit trails, and administrative configuration from a role-aware Blazor Server interface.

Live demo: https://smartscheduler-3751.onrender.com

## Tech Stack

- .NET 9 Blazor Web App with server interactivity
- C# and Razor components
- ASP.NET Core Identity
- Role-based access control
- Entity Framework Core
- SQL Server / LocalDB
- SignalR real-time updates
- Bootstrap 5
- Custom Teal & Slate Enterprise theme

## Demo Credentials

Seeded development users:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@smartscheduler.local` | `SmartScheduler!2026` |
| Manager | `manager@smartscheduler.local` | `SmartScheduler!2026` |
| Scheduler | `scheduler@smartscheduler.local` | `SmartScheduler!2026` |

## Features By Module

### Dashboard

- Today&apos;s appointments
- Weekly utilization
- Pending requests
- Team availability
- Upcoming meetings
- Unread notifications
- Real-time SignalR refresh status

### Calendar

- Weekly appointment view
- Status-colored appointment events
- Availability overlays
- Busy and time-off indicators
- Real-time refresh on schedule changes

### Appointments

- Create and edit appointments
- Cancel, complete, and reschedule appointments
- Recurring appointment flag
- Employee, appointment type, and location dropdowns
- Conflict detection for overlapping bookings
- Working-hours validation
- Time-off validation
- Suggested next available time

### Availability

- Availability, busy, and time-off blocks
- Employee-attached availability records
- CRUD workflow
- Calendar overlays

### Team Scheduling

- Employee directory
- Department management
- Working hours
- Active/inactive employee scheduling status

### Reports

- EF-backed report metrics
- Daily appointments
- Weekly utilization
- Monthly completion
- Cancelled appointments
- Employee workload
- Date range, department, employee, and status filters
- Monthly trend table
- Employee utilization table
- Status breakdown
- Department summary
- CSV export at `/reports/export.csv`

### Administration

- Appointment type management
- Location management
- Business hours
- Holiday and closure blocks
- Audit log visibility
- Role-protected configuration pages

### Authentication And RBAC

- Login, register, forgot password, and account management from ASP.NET Core Identity
- Roles: Admin, Manager, Scheduler, Employee
- Role-aware navigation
- Styled access denied page

## Route Map

| Route | Purpose | Roles |
| --- | --- | --- |
| `/` | Dashboard | Public/demo visible |
| `/calendar` | Calendar | Admin, Manager, Scheduler |
| `/appointments` | Appointment management | Admin, Manager, Scheduler |
| `/availability` | Availability management | Admin, Manager, Scheduler |
| `/team` | Team scheduling overview | Admin, Manager |
| `/employees` | Employee CRUD | Admin, Manager |
| `/departments` | Department CRUD | Admin, Manager |
| `/reports` | Reporting dashboard | Admin, Manager |
| `/admin` | Admin dashboard and audit logs | Admin |
| `/appointment-types` | Appointment type CRUD | Admin |
| `/locations` | Location CRUD | Admin |
| `/business-hours` | Business hours and closures | Admin |
| `/project-info` | Portfolio project overview | Public/demo visible |

## Local Setup

Prerequisites:

- .NET 9 SDK
- SQL Server LocalDB or SQL Server

Run the app:

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

The default development connection string targets SQL Server LocalDB in `appsettings.json`.

Development startup also seeds demo users, roles, appointments, availability blocks, business hours, and sample configuration. This seeding runs only when `ASPNETCORE_ENVIRONMENT` is `Development`.

## EF Core Commands

Create a migration:

```powershell
dotnet ef migrations add MigrationName
```

Apply migrations:

```powershell
dotnet ef database update
```

Remove the last unapplied migration:

```powershell
dotnet ef migrations remove
```

## Azure App Service And Azure SQL

SmartScheduler is designed to deploy cleanly to Azure App Service with Azure SQL Database.

Recommended Azure resources:

- Azure App Service running the .NET 9 stack
- Azure SQL Database
- App Service connection string named `DefaultConnection`

Required App Service configuration:

| Setting | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string |
| `Database__ApplyMigrationsOnStartup` | `false` by default |

Example Azure SQL connection string shape:

```text
Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Production Seeding

Demo users and demo data are intentionally limited to the Development environment. In Production, the application does not create:

- Demo admin, manager, or scheduler accounts
- Demo appointments
- Demo availability blocks
- Demo business hours or closures

Create production users through the Identity UI or a controlled administrative process.

### Migration Strategy

Preferred production strategy:

```powershell
dotnet ef database update
```

Run migrations as a release step against the Azure SQL connection string.

Optional App Service startup strategy:

```text
Database__ApplyMigrationsOnStartup=true
```

Use this only when you explicitly want the app to apply pending EF Core migrations during startup. The default is `false` to avoid surprise schema changes in Production.

## Render Demo Deployment

SmartScheduler can also run as a live demo on Render without Azure by using Docker and SQLite.

This mode is intended for portfolio/demo hosting. The free-tier blueprint uses:

- Render Web Service
- Dockerfile in this repository
- SQLite database file at `/tmp/smartscheduler.db`
- Development seeding for demo users and sample data

Render environment variables:

| Key | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `Database__Provider` | `Sqlite` |
| `Database__ApplyMigrationsOnStartup` | `true` |
| `ConnectionStrings__DefaultConnection` | `Data Source=/tmp/smartscheduler.db` |
| `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE` | `false` |

The included `render.yaml` defines the same settings for Render&apos;s free tier.

### Deploy To Render

1. Push this repository to GitHub.
2. In Render, create a new Blueprint or Web Service from the GitHub repository.
3. If using the Blueprint flow, Render reads `render.yaml`.
4. If creating the Web Service manually:
   - Environment: Docker
   - Dockerfile path: `./Dockerfile`
   - Add the environment variables listed above
5. Deploy the service.

After deployment, open the Render URL and log in with:

```text
admin@smartscheduler.local
SmartScheduler!2026
```

Notes:

- SQLite mode uses `EnsureCreated` instead of SQL Server migrations.
- This avoids Azure SQL for demo hosting.
- Free-tier SQLite data is ephemeral and can reset when Render restarts the service.
- Config reload file watchers are disabled to avoid Render free-tier inotify limits.
- For persistent demo data, upgrade the Render service and mount a disk at `/data`, then use `Data Source=/data/smartscheduler.db`.
- Do not use this SQLite demo configuration for a real production scheduling system.

## What This Demonstrates

- Building an enterprise Blazor Server application with reusable Razor components
- Implementing ASP.NET Core Identity and role-based access control
- Designing EF Core entities, migrations, relationships, and seed data
- Creating real-time UX updates with SignalR
- Enforcing scheduling business rules in application services
- Building reporting features with LINQ and SQL-backed filters
- Writing audit logs and persistent notifications
- Creating admin configuration workflows for business users
- Designing a polished enterprise UI with a custom visual identity

## Screenshots

Add screenshots to this section as the portfolio entry is finalized.

| Screen | Placeholder |
| --- | --- |
| Dashboard | `docs/screenshots/dashboard.png` |
| Calendar | `docs/screenshots/calendar.png` |
| Appointments | `docs/screenshots/appointments.png` |
| Reports | `docs/screenshots/reports.png` |
| Admin | `docs/screenshots/admin.png` |

## Completed Phases

- [x] Project setup
- [x] Authentication
- [x] Teal & Slate Enterprise layout
- [x] Dashboard
- [x] Employees and departments
- [x] Calendar
- [x] Appointment management
- [x] Availability management
- [x] Scheduling conflict detection
- [x] SignalR real-time updates
- [x] Persistent notifications
- [x] EF-backed reports
- [x] Audit logs
- [x] Admin configuration
- [x] Role-based access control
- [x] Portfolio documentation

## Visual Identity

SmartScheduler uses a Teal & Slate Enterprise palette:

- Sidebar: `#0F4C5C`
- Primary: `#14B8A6`
- Accent: `#5EEAD4`
- Background: `#F8FAFC`
- Header: `#334155`
- Text: `#1E293B`

Appointment statuses are color-coded for confirmed, scheduled, pending, cancelled, and completed events.
