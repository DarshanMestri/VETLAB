# Technical Debt Report — Phase 1 Assessment

## Priority 0 — remediate before production exposure

| Finding | Evidence | Risk | Incremental remediation |
| --- | --- | --- |
| Secrets/default credentials in source and startup seeding | Development PostgreSQL credential/JWT key are committed; the seeder creates `admin / Admin@123`. | Immediate unauthorized access if deployed or reused. | Remove secrets/default passwords from source, use user secrets/environment/secret store, require bootstrap admin creation, rotate known credentials. |
| Organization data isolation is absent | Clinic/branch keys are nullable and queries/controllers do not scope by them; JWT has no organization claims. | Cross-clinic data disclosure/modification. | Introduce organization context, populate it transactionally, enforce filters and branch authorization in a compatibility-safe feature flag/rollout. |
| Authorization is inconsistent | Several controllers inherit `ControllerBase`, not authenticated `BaseApiController`; no global fallback policy; permissions are stored but unused. | Anonymous access or excessive access to business data. | Add a global fallback authorization policy, explicitly allow login/health endpoints, then replace role literals incrementally with permissions. |
| Barcode generation is race-prone and organization-agnostic | One sequence row is read/incremented/saved without a transaction/concurrency control. | Duplicate barcode under concurrent use; cross-site collisions. | Add scoped unique sequence identity and database-side atomic increment/concurrency strategy; migrate safely while retaining format. |
| Automatic migrations at web-app startup | `Program.cs` calls `MigrateAsync()`. | Uncontrolled production schema changes, startup races, and insufficient deployment audit. | Move migrations to a controlled release job/command; retain local-development convenience only. |

## Priority 1 — production quality and correctness

| Finding | Evidence | Risk | Incremental remediation |
| --- | --- | --- |
| Clean Architecture is structural only | Domain/Application are empty; entities and business services are in Infrastructure; controllers orchestrate persistence. | Hard-to-test business rules, high change coupling. | Add feature slices to Application first (interfaces, commands/queries, validation); move only touched concepts over phase by phase. |
| Soft delete is incomplete | `SaveChangesAsync` changes delete to `IsDeleted`, but queries lack global filters and uniqueness indexes include deleted rows. | Deleted data remains visible; re-creation conflicts. | Introduce filters and repository/query specifications; plan partial unique indexes where needed. |
| Audit model is unused | `AuditLog` has a mapping but no interceptor/service writes business changes. | No non-repudiation or regulatory traceability. | Add an EF save interceptor/audit service with exclusions and immutable event design. |
| Inconsistent persistence/API contracts | `RateDto` exists but no `Rates` DbSet/entity mapping appears in the current model; `PricingService` uses `ClientRates` with nullable client though `ClientRate.ClientId` is non-null; API response/pagination styles vary. | Runtime failures, incompatible UI behavior, unclear contract ownership. | Reconcile model/DTO/controller contracts under integration tests before extending pricing. |
| Potential result integrity gaps | Result entry accepts parameter IDs without verifying they belong to the registration item's test; completion does not require values/review. | Incorrect results can be written/released. | Add invariant checks and a staged draft/review/approved state machine. |
| Runtime side effects on read | GET result detail creates a `Result` record when missing. | GET is non-idempotent; audit/concurrency surprises. | Create draft result on order/sample acceptance or first save, while retaining existing read endpoint behavior initially. |
| Missing cancellation tokens, concurrency tokens, transactions | Most controller/service operations omit cancellation/concurrency; multi-step registration and result workflows rely on implicit save behavior. | Poor cancellation, lost updates, partial workflow changes. | Add request cancellation tokens, PostgreSQL concurrency tokens/ETags where appropriate, and explicit transactions for aggregate workflows. |
| Error handling is not RFC-standard | Middleware always returns HTTP 500 and serializes exception messages in `errors`. | Client inconsistency and information leakage. | Adopt `ProblemDetails`, map known domain/validation/database conflicts, add correlation IDs. |
| FluentValidation is not wired | Package and validator file exist but registration is absent. | Expected validation may never run. | Register validators and add API validation tests. |
| Duplicate/unused frontend plumbing | Both JWT interceptors exist but one is unregistered; duplicate API services and API base constants exist. | Divergent behavior and maintenance cost. | Consolidate on one API client/interceptor and typed feature services while preserving endpoint paths. |
| Browser token storage | JWT is in `localStorage`. | XSS token theft risk. | Evaluate BFF/httpOnly secure-cookie approach; as an interim, strengthen CSP/XSS controls and short-lived rotated tokens. |

## Priority 2 — maintainability and delivery

- No test projects/spec files, CI workflow, container/deployment configuration, or architecture test enforcement.
- No OpenAPI versioning configuration despite the versioning package reference; controller routes are unversioned.
- Swagger is Development-only and lacks auth schema/API contract governance.
- The database uses generated EF defaults with inconsistent temporal properties (`CreatedDate`/`ModifiedDate` and some domain `CreatedAt`/`UpdatedAt`), requiring a canonical audit strategy.
- `AppRole` exists but Identity is configured around `IdentityRole<int>`; role type usage should be standardized when the auth slice is touched.
- Migrations show rapid foundational changes on 2026-08-02. Before adding features, apply a migration-history/actual-database drift check in each environment; this audit did not connect to a PostgreSQL instance.
- Migration destructive behavior must be assessed per deployed database. No new table should be created for concepts already represented; schema evolution should use additive nullable fields, backfill, validation, then enforcement.

## Current database considerations

The EF model has strong basic primary/foreign keys and several useful unique indexes, including organization branch codes, result values, registration barcodes, and reference-range keys. Gaps relevant to scale and isolation include:

- no composite tenant-filter indexes such as `(ClinicId, BranchId, CreatedAt/Status)` for high-volume worklists;
- no filtered/partial indexes accounting for soft-deleted records;
- nullable clinic/branch ownership allows unscoped legacy rows and must be backfilled before making constraints non-null;
- client rate uniqueness omits clinic/branch although those columns exist, preventing truly organization-specific pricing;
- many master code uniqueness constraints are global, which conflicts with a potential organization-specific catalogue model.

