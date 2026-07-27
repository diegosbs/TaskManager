# Task Manager interview walkthrough

This outline is designed for a five-to-seven-minute technical presentation,
followed by a code walkthrough.

## 1. User story

A user registers or signs in, then manages a private list of tasks with title,
description, status, and due date. Every task belongs to exactly one user.
Other authenticated users cannot discover, read, modify, or delete it.

The demo account is:

```text
demo@taskmanager.local / Demo123!
```

## 2. Architecture

The solution follows Clean Architecture dependency direction:

```text
Domain
  ▲
Application
  ▲             ▲
API       Infrastructure
```

- Domain owns entities and invariants.
- Application owns use cases, DTOs, validation, and abstractions.
- Infrastructure owns EF Core, SQLite, password hashing, JWT creation,
  migrations, and seed data.
- API owns HTTP, authentication middleware, CORS, status codes, and dependency
  composition.
- React is an independent client consuming the HTTP contract.

The fastest boundary proof is to inspect the project references: Domain has no
outgoing project reference, and Application references only Domain.

## 3. Design choices to highlight

### Owner-scoped queries

Task retrieval uses `(task id, authenticated user id)` in one repository query.
Cross-user access therefore returns the same `404` as a nonexistent id. This
avoids leaking resource existence and removes the risk of forgetting a
post-query owner check.

### Password and token security

Passwords use PBKDF2-SHA256 with a random salt and constant-time verification.
JWT validation checks signature, issuer, audience, and expiry. The committed
key is for local development only.

### Thin controllers

Controllers delegate to application services and choose HTTP success responses.
A middleware maps application/domain exceptions to validation problems,
`401`, `404`, or `409`.

### Small dependency surface

Manual validation and platform cryptography keep the exercise explainable.
SQLite eliminates external setup. No state-management or component framework
is needed for the React scope.

## 4. Testing strategy

The 30-test suite forms four focused groups:

1. Domain tests prove task invariants.
2. Application tests exercise `AuthService` and `TaskService` directly with
   substituted ports, proving business rules independently from HTTP and EF.
3. Infrastructure tests run repositories and seed recovery against real SQLite
   in-memory and verify salted hashing.
4. API tests use `WebApplicationFactory`, migrations, seed data, real JWTs, and
   HTTP requests.

High-value endpoint cases include:

- registration, duplicate email, and login;
- invalid login and missing JWT;
- valid and invalid task creation;
- cross-user access;
- missing update/delete;
- full create/read/update/delete lifecycle.

Run:

```powershell
cd backend
dotnet test TaskManager.sln
```

## 5. Frontend

The React client separates API calls, authentication context, pages, reusable
components, and styling. It demonstrates:

- login and registration;
- local JWT persistence and bearer headers;
- responsive task list and status filters;
- create/edit forms with labels and field limits;
- loading, API error, empty, saving, and deleting states;
- token clearing on logout or an expired-session `401`.

## 6. Generative AI usage

AI was used to scaffold and accelerate implementation. Human review focused on
dependency direction, cryptography, JWT settings, owner-scoped data access,
problem responses, real-provider testing, and build/runtime verification.

The exact scaffold and implementation prompts, a representative generated-code
sample, validation evidence, corrections, edge cases, and authentication
decisions are recorded in the README rather than presented as an unreviewed AI
transcript.

One concrete correction was configuring string-enum deserialization in the API
test client after the first test run exposed a client/contract mismatch.

The main lesson: a precise prompt naming security rules, boundary constraints,
negative tests, and completion checks produces output that is substantially
easier to validate than “build a task app.”

## 7. Known trade-offs

- Local-storage JWTs meet the exercise requirement but hardened cookies may be
  preferable for a production browser deployment.
- There are no refresh tokens, lockout, rate limiting, or revocation.
- SQLite is ideal locally but not the intended high-write production store.
- Concurrent duplicate registration at the unique-index boundary could receive
  more specialized exception translation.
- Pagination, optimistic concurrency, OpenAPI, browser E2E tests, CI, and
  observability are natural next increments.

## Suggested live demo

1. Call the public health endpoint.
2. Show `/api/tasks` returning `401` without a token.
3. Sign in with the demo account.
4. Create, edit, complete, and delete a task in React.
5. Run the test suite.
6. Open `TaskService`, its contract under `Abstractions/Services`, a direct
   Application test, `TaskRepository`, and a `WebApplicationFactory` test to
   connect the behavior to the architecture.
