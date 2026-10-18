# DMS Project Documentation

## Run the Sprint 2 submission

Prerequisite: Docker Desktop (Linux containers) with Docker Compose. Run these
commands from the extracted repository root. No local Node.js or .NET SDK is
needed for the Docker setup.

```powershell
# First run only: create local demo settings; keep an existing .env.
Copy-Item DMS/.env.example DMS/.env
docker compose -f DMS/docker-compose.yml build
docker compose -f DMS/docker-compose.yml up -d --wait
```

On Linux/macOS, use `cp DMS/.env.example DMS/.env` for the first command.
Allow Keycloak extra time on first startup; `--wait` does not check its login
readiness. Inspect startup with `docker compose -f DMS/docker-compose.yml logs keycloak`.

Open [DMS](http://localhost/), choose **Log in**, and sign in with
**student / student**. The dashboard lists documents. Use **Upload** to upload a
PDF, then select its filename to view details, edit its description, download it,
or manage tags and collections. The browser sends `/api` requests through nginx
to the API container.

| Service | Address |
|---------|---------|
| Frontend | http://localhost/ |
| API health | http://localhost:8081/health |
| Keycloak admin | http://localhost:8082/admin/ (admin / admin with example settings) |
| PostgreSQL | localhost:5432 (dms / dms with example settings) |

Ports 80, 5432, 8081, and 8082 must be available. The API applies database
migrations automatically. Database records, uploaded files, and Keycloak state
persist in Docker volumes. Stop the application without deleting data:

```powershell
docker compose -f DMS/docker-compose.yml stop
```

Demo credentials are for local assessment only. Keycloak provides frontend
login and logout. The API validates Keycloak access tokens. Document, tag,
and collection endpoints require authentication; missing or invalid tokens
return 401. See [how-to-start.txt](how-to-start.txt) for startup and local
development instructions.

## Contributors
 - **at-fhtw** - Abdulah Tepsurkaev (if24b293)
 - **gitmanPrime** - Michael Kovacevic (if23b015)
 - **domaaron** - Aaron Domingo (if24b504)

## Architecture Decisions

### ADR-001: One solution with separate projects
We use one solution for the backend, tests, and future worker applications.
This keeps development organized while allowing separate applications
to run in their own containers.

### ADR-002: Layered architecture
We separate responsibilities into four layers:

- DMS.Domain: Business entities and framework-independent business rules.
- DMS.BLL: Application services, use cases, DTOs, mapping, and repository interfaces.
- DMS.DAL: Database access and repository implementations.
- DMS.API: HTTP controllers, configuration, and dependency registration.

Project references:
- BLL → Domain
- DAL → BLL and Domain
- API → BLL and DAL
- UnitTests → BLL and Domain

Repository interfaces belong in BLL, with implementations in DAL.
This allows business logic to be tested without a production database.

### ADR-003: Technology baseline
We use C# with .NET 10 and ASP.NET Core controllers.
This meets the project requirements.
We use xUnit for unit testing and Visual Studio 2026 for development.

### ADR-004: Document collections as the additional use case
Users can organize documents into named collections.
A document can belong to multiple collections.
We represent membership through a DocumentCollection entity.
Removing a membership does not delete the document.

### ADR-005: Entity property access
Identifiers and creation timestamps have private setters.
Editable metadata has public setters, with validation performed
by BLL services. Entities must not be bound directly to API requests;
the API uses DTOs.

### ADR-006: Document collection membership
DocumentCollection connects documents and collections using their IDs.
The pair (DocumentId, CollectionId) will be the composite primary key,
preventing the same document from appearing twice in one collection.
Membership IDs have private setters and are supplied through a constructor.

### ADR-007: Onion Architecture
We structure the solution following the Onion Architecture pattern. 
Dependencies point strictly inward: outer layers depend on inner layers, 
never the reverse.

```mermaid
graph TD
    A[DMS.API<br/>Controllers, DI config] --> B[DMS.BLL<br/>Services, DTOs, Mapping]
    B --> C[DMS.Domain<br/>Entities, core business rules]
    D[DMS.DAL<br/>EF Core, Repositories] --> B
    D --> C
```

- **DMS.Domain** (center): Contains entities and core business rules with no 
  dependencies on any other layer or external framework.
- **DMS.BLL**: Contains application services, use cases, DTOs, mapping profiles, 
  and repository interfaces. Depends only on Domain.
- **DMS.DAL**: Implements the repository interfaces defined in BLL using EF Core. 
  Depends on BLL and Domain, but BLL/Domain have no knowledge of EF Core or 
  PostgreSQL specifics.
- **DMS.API** (outermost): Exposes HTTP endpoints, wires up dependency injection, 
  and translates between HTTP and the application layer.

This keeps the domain and business logic independent of infrastructure concerns 
(database, web framework), making them easier to test and allowing infrastructure 
components (e.g. the file storage backend) to be swapped without touching business 
logic — relevant later in Sprint 4 when local file storage is replaced by MinIO.

### ADR-008: Angular frontend
The frontend is a separate Angular application in `DMS/frontend`.
It uses standalone components, Angular Router, and SCSS.

Components are grouped into feature folders, with shared pages
under `shared`. Route components are loaded lazily.

This provides a common foundation for parallel frontend development.
REST API integration and container deployment will follow.

## Progress
- Created the layered solution and unit test project.
- Added project references.
- Removed generated example classes.
- Successfully built the solution.
- Added API health endpoint (GET /health returns "Healthy").
- Sprint 1: implemented domain model, EF Core + Npgsql persistence, repositories,
  business services, REST API with Swagger UI, local file storage and unit tests.
- Sprint 2: Angular dashboard, PDF upload, document details, downloads,
  description editing, deletion, tags, collections, and Keycloak frontend login.
  Docker Compose runs nginx, the API, PostgreSQL, and Keycloak together.
- Frontend installation, development, build, and testing instructions
  are available in [DMS/frontend/README.md](DMS/frontend/README.md).

## Versioning

We tag the repository after each sprint submission using semantic versioning 
(`vMAJOR.MINOR.0`), e.g. `v0.1.0` for Sprint 1. Tags mark the exact code state 
submitted for grading.

| Tag | Sprint | Description |
|-----|--------|-------------|
| v0.1.0 | Sprint 1 | REST API, EF Core persistence, repository pattern, AutoMapper |

### Technology versions (introduced in Sprint 1)
- .NET 10 SDK 10.0.401, target framework net10.0.
- EF Core 10.0.12 (Microsoft.EntityFrameworkCore + Microsoft.EntityFrameworkCore.Relational).
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3.
- Microsoft.EntityFrameworkCore.Design 10.0.12 (API project, for `dotnet ef`).
- Swashbuckle.AspNetCore 10.2.3 (Swagger UI at /swagger).
- Moq 4.20.72 (unit test mocking).
- xUnit 2.9.3 / xunit.runner.visualstudio 3.1.4 / Microsoft.NET.Test.Sdk 17.14.1.

## Testing Strategy
- Use xUnit to test business rules and application services.
- Use repository mocks or test doubles so unit tests do not require PostgreSQL.
- Verify database persistence separately against PostgreSQL.
- Add a full user-story integration test in Sprint 6.
- Achieve greater than 70% code coverage for code reviews.
- Record test commands, results, and coverage scope as tests are implemented.
- Use Vitest for Angular component tests.
- Frontend CI runs dependency installation, production build,
  and tests through GitHub Actions.

## Planned Technologies
These technologies are planned but are not yet integrated:

- RabbitMQ for asynchronous processing.
- MinIO for document storage.
- OCR tooling for extracting document text.
- Elasticsearch for full-text and fuzzy search.
- A GenAI API for generating summaries.

## Local authentication server

We use Keycloak 26.8.0 as the authentication server.
It runs in development mode with a built-in database for local use.

With Docker Desktop running, execute this from the repository root:

```powershell
docker compose -f DMS/docker-compose.yml up -d keycloak
```

Wait for startup, then use these local demo accounts:

| Account | Username | Password |
|---------|----------|----------|
| Administrator | admin | admin |
| Test user | student | student |

- [Admin console](http://localhost:8082/admin/)
- [Test user account page](http://localhost:8082/realms/dms/account/)
- Application realm: `dms`
- Frontend client: `dms-frontend`, a public OpenID Connect client

The file `DMS/keycloak/dms-realm.json` creates the realm, frontend
client, and test user automatically on a fresh installation.
The test user receives `default-roles-dms` for account-page access.

Settings persist in the `keycloak_data` Docker volume.
Startup import skips existing realms, so editing the JSON does
not update a realm that already exists.

For an existing realm, add an Audience mapper to the dms-frontend
dedicated client scope: name dms-api-audience, Included Custom Audience
dms-api, Add to access token enabled, and Add to ID token disabled.
Log out and log back in afterward.

## Postman authentication

Import `postman/DMS.postman_collection.json` into Postman.
The collection uses OAuth 2.0 Authorization Code with PKCE.

For an existing Keycloak realm, open the admin console, select
realm `dms`, then Clients → dms-frontend → Settings.
Add this Valid redirect URI and save:

https://oauth.pstmn.io/v1/browser-callback

Fresh realms receive this callback from the realm import file.
Startup import does not update existing realms.

With the application running:

1. Open the DMS API collection's Authorization tab.
2. Confirm Authorization Code (With PKCE), SHA-256, and client
   `dms-frontend`. Leave Client Secret empty.
3. Enable Authorize using browser and confirm the callback above.
4. Select Get New Access Token and log in with student / student.
5. Select Use Token.
6. Send GET http://localhost:8081/api/documents with authorization
   inherited from the collection. Expect HTTP 200.
7. Temporarily select No Auth on that request. Expect HTTP 401,
   then restore Inherit auth from parent.

Do not commit generated access or refresh tokens.

Verification status: collection import and interactive authentication
are pending manual testing.

## Team workflow

We track our work on the [Kanban board](https://github.com/users/gitmanPrime/projects/2).

- Assign each issue to the responsible team member.
- Move it to Ready when it can be started, then In progress when work begins.
- Create a branch from the latest main for each issue.
- Make small commits with clear messages.
- Open a pull request into main and include `Closes #<issue-number>` in its description.
- Move the issue to Review / Verification and ask another team member to review it.
- After review, successful checks, and merging, move the issue to Done.
