# GameHub Store

A web storefront for video games built with ASP.NET MVC 5 targeting .NET Framework 4.8. GameHub lets users browse and search game listings, view details and screenshots, add games to a wishlist/library, and manage games from an admin UI. This repository contains the MVC web app (server-rendered Razor views, Entity Framework models, and MVC controllers).

**Status:** DRAFT — review configuration, secrets, and deployment before publishing to production.

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
- [What was removed from earlier drafts](#what-was-removed-from-earlier-drafts)  
- [Contributing](#contributing)  
- [Roadmap](#roadmap)  
- [License](#license)  
- [Contact](#contact)

---

## Features
- Game catalog with genres and filters  
- Product detail pages with screenshots and metadata  
- Search and pagination for large catalogs  
- Wishlist / library management for users  
- User registration and login  
- Admin CRUD for games, discounts, and users  
- Entity Framework for data access (EF6 — Database-first or Code-first)  
- OWIN middleware for external authentication (Google OAuth)

---

## Tech stack
- Backend / UI: ASP.NET MVC 5 on .NET Framework 4.8  
- ORM: Entity Framework 6  
- Authentication: Session-based auth and optional OWIN Google OAuth  
- Database: SQL Server / LocalDB  
- Frontend: Razor views, Bootstrap, jQuery  
- Dev tooling: Visual Studio 2019 / 2022, NuGet

---

## Prerequisites
- Visual Studio 2019 or 2022 (ASP.NET workload)  
- .NET Framework 4.8 installed  
- SQL Server or LocalDB instance  
- NuGet package restore enabled

---

## Quick local run
1. Clone the repository:
```bash
git clone https://github.com/Abdul-Rafy2005/-GameHub-store.git
cd GameHub-store
```

2. Restore NuGet packages: open the solution in Visual Studio (NuGet restore runs automatically), or:
```bash
nuget restore <YourSolution>.sln
```

3. Update the connection string in `Web.config` to point to your SQL Server or LocalDB instance. The project's EF connection string is named `GameManagementMISEntities`. Example snippet (replace placeholders):
```xml
<connectionStrings>
  <add name="GameManagementMISEntities"
       connectionString="metadata=res://*/Models.GameModel.csdl|res://*/Models.GameModel.ssdl|res://*/Models.GameModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=GameHubDb;Integrated Security=True;MultipleActiveResultSets=True&quot;"
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

4. Build & run:
- In Visual Studio: set the web project as the Startup Project, choose Debug or Release, press F5 (IIS Express) or Ctrl+F5.
- The app will run under the IIS Express URL shown in Visual Studio.

---

## Database
- Entity Framework models live in the `Models` folder. The connection string name is `GameManagementMISEntities`.
- If using Database-First: ensure the database schema exists and matches the EDMX/model metadata.
- If using Code-First: apply EF migrations or use your preferred seeding approach to create and seed the database.
- Provide SQL seed scripts or EF seed logic when convenient for dev onboarding.

---

## Authentication & Google OAuth
- OWIN Google OAuth is configured in `App_Start/Startup.cs`.
- For local development you may temporarily store Google client ID/secret in `Web.config` (appSettings) — do NOT commit secrets to source control.
- For production, use environment variables / IIS App Settings to keep secrets out of source control.
- Callback path: `/signin-google` — register this URI in the Google Cloud Console for both local and production redirect URIs.

---

## Security notes
- Avoid known vulnerable packages (review NuGet advisories). For example, evaluate any old Microsoft.AspNet.Identity.* or OWIN packages and update or minimize usage.
- Never commit secrets (Google client secret, DB passwords) to the repository.
- Use HTTPS in production and set cookies as `Secure` and `HttpOnly`.
- Review password hashing implementation; ensure modern hashing (bcrypt/argon2) or confirm the framework's secure approach. The project currently uses a `PasswordHash` field — audit before production.
- Keep NuGet packages updated and monitor advisories.

---

## Project structure (relevant)
- Controllers/ — Games, Discounts, Account, Admin, etc.  
- Models/ — Entity Framework models and DbContext (EDMX or Code-First classes)  
- Views/ — Razor views and shared layouts  
- Content/ — CSS, images, cover art, banners  
- Scripts/ — client-side scripts (jQuery, Bootstrap)  
- App_Start/ — OWIN Startup, routing, bundles  
- Global.asax — application lifecycle events  
- Web.config — site configuration, app settings and connection strings

---

## Publishing & deployment (high level)
1. Build in Release configuration.  
2. Publish the web project (Visual Studio → Publish → File System or Web Deploy).  
3. Deploy the publish output to IIS; ensure you include:
   - bin/ (assemblies)
   - Views/
   - Content/ (CSS, images)
   - Global.asax
   - Web.config (apply transforms for production; set compilation debug="false")

4. IIS configuration:
   - App Pool: .NET CLR v4.0, Integrated pipeline
   - Provide connection strings and Google client secrets via environment variables or IIS App Settings (avoid Web.config secrets)
5. Register production redirect URIs in Google Cloud Console (e.g., `https://yourdomain.com/signin-google`).

---

## What was removed from earlier drafts
- Node / React / Sequelize / Postgres instructions (not part of this repo)  
- Docker / docker-compose steps (not included here)  
- Node-specific environment variables and package.json scripts

If you later add a separate frontend or an API microservice, add dedicated docs for those components.

---

## Contributing
- Fork → create a feature branch → open a pull request.  
- Keep secrets out of commits.  
- Include tests for non-trivial changes where possible.

---

## Roadmap
- Harden authentication flows and secret management  
- Add unit & integration tests  
- Add CI for build and basic tests  
- Audit and update NuGet packages to resolve advisories

---

## Contact
Maintainer: Abdul-Rafy2005 — https://github.com/Abdul-Rafy2005

For issues or feature requests, please open an issue in this repository.
