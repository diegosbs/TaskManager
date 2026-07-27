# Task Manager

A small, complete full-stack task management application built as a technical
interview exercise. It demonstrates Clean Architecture, ASP.NET Core, EF Core
with SQLite, JWT authentication, owner-scoped CRUD operations, React
integration, automated tests, and documented use of generative AI.

## User story

As a registered user, I want a private task list so that I can create, review,
update, complete, and remove my own work without exposing it to other users.

The repository includes a seeded demo account, two example tasks, a public
health endpoint, and a protected task API.

## Technology

- .NET 8 and ASP.NET Core Web API
- Entity Framework Core 8 with SQLite
- JWT bearer authentication
- PBKDF2-SHA256 password hashing
- xUnit, FluentAssertions, and `WebApplicationFactory`
- React 19 and Vite 8
- Plain CSS with responsive layouts

## Repository structure

```text
.
├── backend/
│   ├── TaskManager.sln
│   ├── .config/dotnet-tools.json
│   ├── src/
│   │   ├── TaskManager.Domain/
│   │   ├── TaskManager.Application/
│   │   ├── TaskManager.Infrastructure/
│   │   └── TaskManager.Api/
│   └── tests/TaskManager.Tests/
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── context/
│   │   ├── pages/
│   │   └── styles/
│   └── package.json
├── PRESENTATION.md
└── README.md
```

## Architecture

Dependencies point inward:

```text
API ───────────────► Application ◄──────── Infrastructure
 │                         │                      │
 └─────────────────────────┴──────────────────────▼
                                                Domain
```

- **Domain** contains `User`, `TaskItem`, the task status enum, and invariants.
  It has no ASP.NET Core or EF Core dependency.
- **Application** contains DTOs, use cases, validation, exceptions, and ports
  for repositories, time, password hashing, token generation, and user context.
  It does not reference EF Core.
- **Infrastructure** implements the ports with EF Core SQLite, PBKDF2, JWT,
  migrations, and seed data.
- **API** is the composition and HTTP layer. Controllers translate HTTP calls
  into application service calls; business and ownership rules do not live in
  controllers.
- **Frontend** consumes DTO-shaped JSON and keeps authentication/session logic
  separate from pages and reusable components.

Application abstractions are grouped by responsibility instead of being mixed
with their implementations:

```text
TaskManager.Application/
|-- Abstractions/
|   |-- Authentication/   # authenticated-user context
|   |-- Persistence/      # repositories and unit of work
|   |-- Security/         # password hashing and token generation
|   |-- Services/         # use-case contracts consumed by the API
|   `-- Time/             # clock abstraction
|-- Contracts/            # request and response DTOs by feature
|-- Exceptions/           # application-level failures
`-- Services/             # use-case implementations only
```

### Ownership and information disclosure

Every task query includes both task id and authenticated user id. Access to
another user's task returns `404 Not Found`, not `403 Forbidden`. This prevents
an attacker from determining whether a task id exists for another account. The
same behavior is used for genuinely missing tasks.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or a newer SDK
  capable of targeting .NET 8
- Node.js 22.12 or newer
- npm 10 or newer

No external database server is required.

## Backend setup

From the repository root:

```powershell
cd backend
dotnet tool restore
dotnet restore
dotnet run --project src/TaskManager.Api --launch-profile http
```

The API listens at `http://localhost:5116`. On startup it applies pending
migrations and idempotently seeds the demo user and tasks.

Verify the public endpoint:

```powershell
Invoke-RestMethod http://localhost:5116/api/public/health
```

### Database and migrations

The default connection string is:

```text
Data Source=taskmanager.db
```

Apply migrations explicitly:

```powershell
cd backend
dotnet tool restore
dotnet ef database update `
  --project src/TaskManager.Infrastructure `
  --startup-project src/TaskManager.Api
```

Create a future migration:

```powershell
dotnet ef migrations add MigrationName `
  --project src/TaskManager.Infrastructure `
  --startup-project src/TaskManager.Api `
  --output-dir Persistence/Migrations
```

For non-development environments, override configuration rather than editing
the committed development settings:

```powershell
$env:ConnectionStrings__TaskManager = "Data Source=C:\data\taskmanager.db"
$env:Jwt__Key = "replace-with-a-random-secret-containing-at-least-32-bytes"
```

The signing key in `appsettings.json` is intentionally a local-development
value and must not be used in production.

## Frontend setup

Keep the API running, then use another terminal:

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` requests to
`http://localhost:5116`.

For a production build:

```powershell
npm run build
```

Set `VITE_API_URL` when the API is hosted on another origin. See
`frontend/.env.example`.

## Demo credentials

```text
Email:    demo@taskmanager.local
Password: Demo123!
```

Passwords are never stored as plaintext. The seed process hashes the demo
password with PBKDF2-SHA256, a random 128-bit salt, and 210,000 iterations.

## API endpoints

| Method | Endpoint | Authorization | Success |
|---|---|---:|---:|
| `GET` | `/api/public/health` | Public | `200` |
| `POST` | `/api/auth/register` | Public | `201` |
| `POST` | `/api/auth/login` | Public | `200` + JWT |
| `GET` | `/api/tasks` | Bearer token | `200` |
| `GET` | `/api/tasks/{id}` | Bearer token | `200` |
| `POST` | `/api/tasks` | Bearer token | `201` |
| `PUT` | `/api/tasks/{id}` | Bearer token | `200` |
| `DELETE` | `/api/tasks/{id}` | Bearer token | `204` |

Important error responses:

- `400` for invalid request data.
- `401` for missing/invalid JWT or invalid login.
- `404` for missing tasks and tasks owned by another user.
- `409` for duplicate email registration.

### Task payload

```json
{
  "title": "Prepare the interview demo",
  "description": "Walk through architecture and tests.",
  "status": "Pending",
  "dueDate": "2026-08-01"
}
```

Allowed statuses are `Pending`, `InProgress`, and `Completed`.

## Running tests

```powershell
cd backend
dotnet test TaskManager.sln
```

The 30-test suite covers:

- valid task construction;
- empty and over-length titles;
- direct `AuthService` registration, validation, normalization, login, and
  persistence behavior;
- direct `TaskService` validation, user scoping, mapping, create, update, and
  delete behavior;
- registration and duplicate emails;
- valid and invalid login;
- protected endpoints without JWT;
- cross-user task isolation;
- missing-task update and delete;
- complete authenticated CRUD;
- salted password hashing;
- idempotent recovery of missing demo tasks when the demo user already exists;
- EF Core repository behavior against SQLite in-memory;
- API behavior through `WebApplicationFactory`.

## Main design decisions

1. **Application services instead of logic in controllers.** Controllers remain
   easy to read and test, while use cases own validation and authorization
   decisions.
2. **Repository queries are owner-scoped.** A task is never loaded by id and
   checked later; the user id is part of the database predicate.
3. **DTOs at the boundary.** EF entities never leave controllers directly.
4. **Manual validation.** The exercise avoids an additional validation
   framework while still returning standard validation-problem responses.
5. **PBKDF2 from the platform.** Secure hashing is implemented with
   `Rfc2898DeriveBytes.Pbkdf2`, random salts, and constant-time verification,
   avoiding plaintext or reversible storage.
6. **SQLite migrations plus startup seeding.** Local setup remains one command,
   while the schema history remains explicit and reviewable.
7. **JWT in local storage.** This directly demonstrates the requested token
   storage/header flow. A production browser application may prefer an
   `HttpOnly`, `Secure`, `SameSite` cookie depending on its threat model.

## Trade-offs and possible improvements

- Add refresh-token rotation and token revocation.
- Move browser authentication to hardened cookies if the deployment model
  permits it.
- Add rate limiting and account lockout for login attempts.
- Catch and translate the unique-index race that can occur between an email
  existence check and commit under concurrent registrations.
- Add pagination, search, sorting options, and optimistic concurrency for task
  updates.
- Replace the development signing key with a managed secret and asymmetric
  signing in production.
- Add OpenAPI documentation, frontend component tests, Playwright end-to-end
  tests, CI, containers, and production observability.
- Use a production database such as PostgreSQL when concurrent write volume
  outgrows SQLite.

## Generative AI usage

Generative AI was used as an implementation accelerator, not as a substitute
for engineering review.

### Scaffold prompt

The initial scaffold was generated from this focused prompt:

```text
Create a single-repository .NET 8 task management exercise. Start only with
backend/TaskManager.sln and the Domain, Application, Infrastructure, Api, and
xUnit test projects under the requested src/tests folders. Configure Clean
Architecture project references, create an empty frontend folder, remove
template WeatherForecast code, initialize Git, and prove the empty solution
builds. Do not implement business logic in this scaffold step.
```

### Implementation prompt

After the scaffold built successfully, the implementation was driven by this
prompt:

```text
Continue in the existing single repository and implement a production-minded
task manager without changing the established .NET 8 Clean Architecture
project boundaries.

Backend requirements:
- Model User and TaskItem. A task has id, title, description, status, due date,
  owner id, created timestamp, and updated timestamp.
- Keep Domain independent. Keep Application dependent only on Domain. Put
  interfaces in responsibility-specific Application/Abstractions folders and
  implementations in Infrastructure or Application/Services as appropriate.
- Use ASP.NET Core controllers, EF Core SQLite migrations, JWT bearer
  authentication, and salted PBKDF2 password hashes. Never store plaintext
  passwords.
- Implement public health, register, and login endpoints plus authenticated
  CRUD endpoints for tasks. Scope every task query by both task id and current
  user id; return 404 for missing and cross-user resources.
- Validate required fields, title/description limits, enum values, dates,
  duplicate email, invalid credentials, and malformed authentication. Return
  appropriate 201, 204, 400, 401, 404, and 409 responses.
- Seed one demo user and two tasks idempotently.

Frontend requirements:
- Build a responsive React client with login, registration, logout, task
  listing/filtering, create, edit, complete, and delete flows.
- Separate API clients, auth state, pages, reusable components, and styles.
- Handle loading, empty, validation, API-error, save/delete, confirmation, and
  expired-session states without browser-console warnings.

Testing requirements:
- Add focused unit tests for Domain invariants and Application services.
- Test Infrastructure repositories with the real SQLite provider in memory.
- Test public/authenticated endpoints, registration/login, validation, full
  CRUD, missing records, and cross-user isolation through WebApplicationFactory.

Documentation and completion:
- Write README setup, architecture, endpoint, credentials, trade-off, testing,
  and GenAI sections plus a concise presentation outline.
- Show a representative generated-code sample and document human validation,
  corrections, edge cases, authentication, and validation decisions.
- Finish only after dotnet build has zero warnings/errors, all tests pass, the
  frontend production build succeeds, npm audit reports no vulnerabilities,
  and manual HTTP smoke checks pass.

Before editing, inspect existing files and preserve established conventions.
Implement in small verifiable steps and report assumptions or trade-offs.
```

### Representative AI-generated code

This repository query is intentionally scoped by both identifiers:

```csharp
return dbContext.Tasks.SingleOrDefaultAsync(
    task => task.Id == id && task.UserId == userId,
    cancellationToken);
```

It is short, but it captures an important security decision: callers cannot
accidentally retrieve another user's task and authorization is enforced at the
data-access boundary.

### Validation and corrections

AI output was validated with compiler feedback, 30 automated tests, a
production frontend build, package audit output, startup migrations, demo login,
and HTTP CRUD checks.

Corrections and improvements made during review included:

- removing generated WeatherForecast and placeholder test code;
- keeping Domain and Application independent from EF Core;
- separating service contracts from implementations and grouping application
  ports by responsibility;
- emitting enum names consistently and correcting the integration-test JSON
  enum converter;
- adding a minimum JWT key length and strict issuer/audience/lifetime checks;
- using salted PBKDF2 and fixed-time comparison rather than weak hashing;
- filtering task reads by owner and documenting the `404` disclosure policy;
- testing the actual SQLite provider instead of EF's non-relational in-memory
  provider;
- adding loading, validation, expired-session, and destructive-action states to
  the frontend.

Authentication, invalid dates/statuses, missing tasks, duplicate users,
cross-user access, empty and oversized fields, logout, and token propagation
were handled explicitly rather than assumed from generated happy-path code.

### Weak prompt versus improved prompt

A weak prompt such as _"build a task app with .NET and React"_ leaves security,
layer boundaries, status codes, ownership, test cases, and completion criteria
ambiguous. It tends to produce a large happy-path scaffold that is difficult to
review.

The improved prompt named the exact fields, endpoints, dependency constraints,
validation limits, authorization rule, expected error cases, test matrix,
frontend states, and required verification commands. That specificity made the
AI output smaller, testable, and easier for a human engineer to challenge.

## Interview presentation

See [PRESENTATION.md](PRESENTATION.md) for a concise walkthrough covering the
story, architecture, design choices, test strategy, AI usage, and known
trade-offs.
