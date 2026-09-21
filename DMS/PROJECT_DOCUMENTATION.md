# DMS Project Documentation

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

## Progress
- Created the layered solution and unit test project.
- Added project references.
- Removed generated example classes.
- Successfully built the solution.

## Testing Strategy
- Use xUnit to test business rules and application services.
- Use repository mocks or test doubles so unit tests do not require PostgreSQL.
- Verify database persistence separately against PostgreSQL.
- Add a full user-story integration test in Sprint 6.
- Achieve greater than 70% code coverage for code reviews.
- Record test commands, results, and coverage scope as tests are implemented.

### Current Verification
The initial solution builds successfully.
- Manually verified GET /health returns HTTP 200 OK with body "Healthy".
- 
No meaningful automated tests have been implemented yet.

## Time Tracking
Record actual time spent by each team member.
Keep estimates separate from actual time.

| Date | Team Member | Task | Actual Duration | Result / Commit |
| 2026-09-20 | Michael | Installed VS 2026, created layered projects and references, added initial documentation | ~1 h 30 min | Solution builds; commited |
| 2026-09-21 | Michael | Created API health endpoint | ~10 min | returns Healthy |

## Planned Technologies
These technologies are planned but are not yet integrated:

- EF Core and Npgsql for PostgreSQL persistence.
- Docker Compose for running the API and supporting services.
- RabbitMQ for asynchronous processing.
- MinIO for document storage.
- OCR tooling for extracting document text.
- Elasticsearch for full-text and fuzzy search.
- A GenAI API for generating summaries.

Record exact versions and reasons for technology choices when introduced.