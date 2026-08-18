# PCMS — Pet Cremation Management System

## Project Overview

PCMS is a Pet Cremation Management System.

Primary business workflow:

Clientes → Mascotas → Recepciones → Cremaciones

Keep these responsibilities separate:

- Clientes: customer information only.
- Mascotas: pet information only.
- Recepciones: intake and chain of custody, QR assignment, weight verification,
  photos, personal belongings, and optional veterinary referral information.
- Cremaciones: cremation service/package information, urn, paw print,
  certificate, status, scheduling, completion, and history.

Veterinary directory/referral functionality is also part of the system.
Veterinary clinic and veterinarian associations may be optional where the
domain allows them.

---

## Repository

Repository root:

~/Desktop/pcms

Main directories:

backend/
frontend/

Backend layered architecture:

backend/src/pcms.Domain
backend/src/pcms.Application
backend/src/pcms.Infrastructure
backend/src/pcms.Api

Preserve the existing layered architecture and dependency direction.

Do not move business logic into controllers.

---

## Technology Stack

Backend:
- C#
- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- JWT authentication

Frontend:
- React
- TypeScript
- Vite
- React Router
- TanStack Query
- Axios
- React Hook Form / Zod where currently used
- Tailwind CSS

Infrastructure:
- Docker
- Git

---

## Language Rules

These rules are important.

User-facing frontend/UI:
- Spanish (Mexico)

Examples:
- Clientes
- Mascotas
- Recepciones
- Cremaciones
- Veterinarias
- Veterinarios

C#:
- English

Class names:
- English

DTO names:
- English

Service names:
- English

API routes:
- English

Database-facing/user-facing terminology:
- Spanish where practical and consistent with the existing schema.

Do not translate existing C# architecture into Spanish.

---

## Coding Principles

Preserve existing project conventions before introducing new patterns.

Before changing a module:
1. Inspect the related entity.
2. Inspect DTOs.
3. Inspect interfaces.
4. Inspect service implementation.
5. Inspect EF Core mapping.
6. Inspect controller/API.
7. Inspect frontend types/API/hooks/components when relevant.

Avoid duplicate implementations.

Do not create a second DbContext, service, DTO, API client, or model when one
already exists.

Prefer focused changes over broad rewrites.

Do not modify unrelated files.

Keep nullable/optional domain relationships nullable throughout all layers.

Preserve soft-delete behavior where already implemented.

Preserve controlled status-transition rules.

Do not bypass domain validation simply to make a build pass.

---

## Database / EF Core

Use PostgreSQL through the existing EF Core infrastructure.

Inspect existing migrations before creating new ones.

Do not delete or recreate the database unless explicitly instructed.

Do not create a migration unless a schema change actually requires one.

Keep entity configuration consistent with existing conventions.

---

## Frontend

All visible labels, buttons, validation messages, headings, table columns,
forms, dialogs, and status descriptions should be Spanish (Mexico).

Keep TypeScript code and internal identifiers in English.

Reuse:
- existing API clients
- hooks
- shared components
- form conventions
- query keys
- routing conventions

Do not introduce a second frontend architecture.

---

## Git Safety

Always inspect:

git status

before significant modifications.

Never run destructive commands such as:

git reset --hard
git clean -fd
git checkout -- .
git restore .

unless explicitly instructed.

Do not commit automatically unless explicitly requested.

Do not push automatically unless explicitly requested.

Before a large change, recommend a Git checkpoint if there are uncommitted
changes.

---

## Validation

After backend changes, run the appropriate build from the repository:

dotnet build

Use the actual solution/project paths discovered in the repository.

Run formatting when appropriate.

After frontend changes, run the existing frontend build command, normally:

npm run build

Run relevant tests when available.

Do not claim a task is complete when the build is failing.

Report:
- files changed
- important design decisions
- build/test results
- remaining warnings/errors

---

## Working With The User

The user works primarily in Visual Studio Code on Ubuntu.

Codex has direct repository access, so inspect files directly instead of
asking the user to print or paste large source files.

When explaining work:
- identify the current step
- explain significant architectural decisions
- make changes directly when authorized
- show a concise summary afterward

Do not overwhelm the user with unnecessary low-level edits when Codex can
make and verify them directly.

For substantial changes, first inspect the existing implementation so the
change fits the codebase rather than replacing working architecture.

---

## Current Migration Checkpoint

Repository state reviewed and stabilized on 2026-08-18. The repository remains
the source of truth; older ChatGPT checkpoints must not override the files, Git
history, or current build results.

Committed baseline:

- Branch `develop` is at `e1a290a` and aligned with `origin/develop`.
- Cremation management, veterinary clinics and veterinarians, payments,
  veterinary requests, reception conversion, and pet collections are committed.
- Veterinarians support an optional `VeterinaryClinicId`, an optional
  `SecondLastName`, and unassigned-clinic workflows.

Current uncommitted phase:

- Adds cremation package, urn, and weight-based pricing catalogs; explicit
  package-to-urn assignments; cremation quote snapshots; payment-total
  integration; EF Core migrations; and the corresponding frontend modules.
- Package-to-urn choices are explicitly mapped and constrained. Historical
  package, urn, inclusion, quote, and payment data are preserved when catalog
  records later change or become inactive.
- Payment accounts derive their service total from the stored cremation quote.
  Payments preserve history, reject overpayment, and calculate paid and pending
  balances on the backend.
- Legacy cremations without a package, quote, or payment account can receive an
  explicit one-time quote through the edit workflow; they are not silently
  backfilled with guessed historical pricing.
- `Urn.Price` remains catalog metadata and is not added to the weight-based
  package quote. Treating it as a surcharge requires an explicit product and
  snapshot-model decision in a later phase.

Current validation baseline:

- The backend solution and frontend production build succeed.
- Frontend lint succeeds with one unrelated existing veterinarian hook warning.
- EF Core reports no pending model changes.
- The local PostgreSQL development database is migrated through
  `20260818194622_AddCremationPackageUrnAssignments`.
- An isolated PostgreSQL workflow test passed customer, pet, reception,
  cremation, allowed-urn selection, 5.00/5.01 kg boundary quotes, quote snapshot,
  partial payment, overpayment rejection, inactive-record protection, restore,
  payoff, and remaining-balance checks. Its temporary schema was removed.
- No permanent backend or frontend automated test project is currently present.

Next task:

- Review and commit this stabilized phase only when explicitly requested.
- After the checkpoint, choose the next development phase explicitly; do not
  fold urn surcharges or quote-revision history into this checkpoint without a
  business decision.

---

## Long-Term PCMS Direction

Main workflow remains:

Clientes
  ↓
Mascotas
  ↓
Recepciones
  ↓
Cremaciones

Additional veterinary functionality should integrate with that workflow
without changing the core ownership boundaries.

Future development should continue incrementally in clear phases/sprints.

Prefer:
PLAN → IMPLEMENT → BUILD/TEST → REVIEW → COMMIT
