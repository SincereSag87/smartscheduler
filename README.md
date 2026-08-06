# SmartScheduler

Enterprise scheduling and appointment management platform built with Blazor, ASP.NET Core, Entity Framework Core, SQL Server, and SignalR.

## Tech Stack

- Blazor Server / Blazor Web App on .NET 9
- C# and Razor components
- ASP.NET Core Identity with role-ready authentication
- Entity Framework Core with SQL Server
- SignalR for real-time scheduling updates
- Bootstrap 5 plus a custom Teal & Slate Enterprise theme

## Features

- Dashboard with KPI cards, today's appointments, upcoming meetings, team availability, and notifications
- Calendar week view with status-colored appointment cards
- Appointment booking workflow for creating and managing appointments
- Team scheduling with departments, working hours, time off, and availability states
- Reports for daily appointments, weekly utilization, monthly trends, and completion performance
- Admin area for users, roles, departments, business hours, locations, and audit controls

## Visual Identity

SmartScheduler uses a Teal & Slate Enterprise palette:

- Sidebar: `#0F4C5C`
- Primary: `#14B8A6`
- Accent: `#5EEAD4`
- Background: `#F8FAFC`
- Header: `#334155`
- Text: `#1E293B`

Appointment statuses are color-coded for confirmed, scheduled, pending, cancelled, and completed events.

## Development Roadmap

1. Project setup, authentication, layout, and dashboard
2. Employees, departments, and calendar
3. Appointment management, availability, and scheduling rules
4. SignalR real-time updates, notifications, and reports
5. Admin panel, deployment, and documentation

## Run Locally

```powershell
dotnet restore
dotnet run
```

The default development connection string targets SQL Server LocalDB.
