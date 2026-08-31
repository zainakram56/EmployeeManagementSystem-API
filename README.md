# WebInterface — Leave Management System API

A RESTful Web API built with ASP.NET Core, serving as the backend for the Leave Management System. Handles all data access, business logic, and authentication, while client applications (like the MVC web app) consume it over HTTP.

## Overview

This API was built to decouple business logic and data access from the client application, following an API-first architecture. Instead of the frontend directly accessing the database, it communicates with this API via HTTP requests — the same pattern used by real-world systems where a single backend serves multiple clients (web, mobile, admin panels, etc.).

## Tech Stack

- **ASP.NET Core Web API** (.NET 10)
- **Entity Framework Core** — data access (SQL Server)
- **ASP.NET Core Identity** — user management
- **JWT (JSON Web Tokens)** — authentication & authorization
- **MailKit** — email delivery (invite system)
- **Swagger / OpenAPI** — API documentation and testing

## Features

- **Employee, Department, Leave Type, Leave Request, and Leave Balance management** — full CRUD via REST endpoints
- **Role-based authorization** (Employee, Manager, HR) enforced at the API level — not just the client
- **Data-level filtering** — employees only see their own records, managers see their team, HR sees everything
- **Two-stage leave approval workflow** — Manager approval → HR approval, with balance tracking
- **JWT-based authentication** — stateless, token-based login, suitable for multiple client types
- **Invite-based user registration** — admins invite users by email; users set their own password via a secure, time-limited link
- **Password policy & account lockout** — enforced via ASP.NET Core Identity configuration

## Architecture
Client (WebAppMVC, or any other client)
│ HTTP + JWT Bearer Token
▼
WebInterface API
│
▼
SQL Server Database


Each controller enforces its own authorization rules — role checks and data filtering happen server-side, so no client can bypass business rules by calling the API directly.

## Getting Started

1. Update the connection string in `appsettings.json`
2. Configure JWT settings (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`) in `appsettings.json`
3. Configure SMTP settings for the invite email feature
4. Run the project — Swagger UI will be available at `/swagger` in development

## API Endpoints (summary)

| Resource | Base Route |
|---|---|
| Auth (login, invite, register) | `/api/Auth` |
| Employees | `/api/Employees` |
| Departments | `/api/Departments` |
| Leave Types | `/api/LeaveTypes` |
| Leave Requests | `/api/LeaveRequests` |
| Leave Balances | `/api/LeaveBalances` |

All endpoints (except login/register) require a valid JWT Bearer token.
