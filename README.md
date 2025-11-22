# GameHub Store

A web storefront for video games built with ASP.NET MVC 5 targeting .NET Framework 4.8. GameHub lets users browse and search game listings, view details and screenshots, add games to a wishlist/library, and manage games from an admin UI. This repository contains the MVC web app (server-rendered Razor views, Entity Framework models, and MVC controllers).

Status: DRAFT — review configuration, secrets, and deployment before publishing to production.

---

## Table of contents
- [Features](#features)
- [Tech stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick local run](#quick-local-run)
- [Database](#database)
- [Authentication & Google OAuth](#authentication--google-oauth)
- [Security notes](#security-notes)
- [Project structure (relevant)](#project-structure-relevant)
- [Publishing & deployment](#publishing--deployment)
- [Contributing](#contributing)
- [Roadmap](#roadmap)
- [License](#license)
- [Contact](#contact)

---

## Features
- Game catalog with genres, filters and pagination
- Product detail pages with screenshots and metadata
- Search and pagination for large catalogs
- Wishlist / library management for users
- User registration, login, and profile management
- Admin area with CRUD for games, discounts, and users
- Entity Framework 6 for data access (Database-first or Code-first)
- OWIN middleware for external authentication (Google OAuth)

---

## Tech stack
- Backend / UI: ASP.NET MVC 5 on .NET Framework 4.8  
- ORM: Entity Framework 6  
- Authentication: Session-based auth, optional OWIN Google OAuth  
- Database: SQL Server / LocalDB  
- Frontend: Razor views, Bootstrap, jQuery  
- Dev tooling: Visual Studio 2019 / 2022, NuGet

---

## Prerequisites
- Visual Studio 2019 or 2022 (ASP.NET workload installed)  
- .NET Framework 4.8 installed  
- SQL Server or LocalDB instance  
- NuGet package restore enabled

---

## Quick local run

1. Clone the repository:
```bash
git clone https://github.com/Abdul-Rafy2005/GameHub-store.git
cd GameHub-store
```

2. Restore NuGet packages:  
Open the solution (`.sln`) in Visual Studio — NuGet restore will run automatically. Or from the command line:
```bash
nuget restore GameHub.sln
```

3. Update connection string:  
Edit `Web.config` and set the connection string named `GameManagementMISEntities` to point at your SQL Server / LocalDB instance.

4. Build & run:
- In Visual Studio: set the web project as Startup Project, choose Debug or Release, press F5 (IIS Express) or Ctrl+F5.
- Confirm the app runs at the IIS Express URL shown in the toolbar.

---

## Database
- The project uses EF models in the `Models` folder and expects a connection string named `GameManagementMISEntities` in `Web.config`.
- If using Database-First: ensure the database schema exists and matches the EDMX / model.
- If using Code-First: use your preferred EF migrations or seeding approach to create and seed the database.
- For quick seeding, add SQL scripts or seed logic and run them against your database.

---

## Authentication & Google OAuth
- OWIN Google OAuth is configured in `App_Start/Startup.cs`.
- For local development you may store Google client ID/secret in `Web.config` (appSettings) — do NOT commit secrets to source control.
- For production, store secrets in environment variables or the host's configuration (IIS App Settings).
- Callback path: `/signin-google` — ensure this is configured in the Google Cloud Console.

---

## Security notes
- Remove or avoid known vulnerable packages (e.g., outdated OWIN/Identity packages). Use minimal required OWIN packages for Google OAuth.
- Never commit secrets (Google client secret, DB passwords) to the repo.
- Use HTTPS in production and set cookies as Secure and HttpOnly.
- Verify how password hashes are implemented; ensure modern hashing (bcrypt/argon2). The project stores a PasswordHash field — review before production.
- Keep packages up to date and review advisories (NuGet security advisories).

---

## Project structure (relevant)
- Controllers/ — MVC controllers (Games, Discounts, Account, Admin, etc.)
- Models/ — Entity Framework models and DbContext
- Views/ — Razor views and shared layouts
- Content/ — CSS, images, cover art and banners
- Scripts/ — client-side scripts (jQuery, Bootstrap)
- App_Start/ — OWIN Startup, routing, bundles
- Global.asax — application lifecycle events
- Web.config — site configuration, app settings and connection strings

---

## Publishing & deployment (high level)
1. Build in Release configuration.
2. Publish the web project (Visual Studio → Publish → File System or Web Deploy).
3. Deploy the publish output to IIS, including:
   - bin/ (assemblies)
   - Views/
   - Content/ (CSS/images)
   - Global.asax
   - Web.config (transform for production; set compilation debug="false")
4. Configure IIS:
   - App Pool: .NET CLR v4.0, Integrated pipeline
   - Provide environment variables or App Settings for Google client secrets and connection strings
5. Ensure production URLs are registered in Google Cloud Console for OAuth redirect (e.g., https://yourdomain.com/signin-google).

---

## Contributing
- Fork → feature branch → open a pull request.
- Keep sensitive data out of commits.
- Add tests for non-trivial logic where possible.

---

## Roadmap
- Harden authentication flows and secret management
- Add unit & integration tests
- Add CI for build & basic tests
- Audit and update NuGet packages to address advisories

---

## License
Add a LICENSE file (MIT or preferred) before publishing.

---

## Contact
Maintainer: Abdul-Rafy2005 — https://github.com/Abdul-Rafy2005

For issues or feature requests, please open an issue in this repository.
