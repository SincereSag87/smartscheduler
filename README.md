# SmartScheduler

SmartScheduler is an enterprise scheduling and appointment management platform built with Blazor, ASP.NET Core, Entity Framework Core, SQL Server, SignalR, and ASP.NET Core Identity.

The project is designed as a C# portfolio application that demonstrates a realistic business workflow: authenticated users manage schedules, appointments, availability, notifications, reports, audit trails, and administrative configuration from a role-aware Blazor Server interface.

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
