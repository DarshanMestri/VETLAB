# Incremental Upgrade Plan — Phase 1 Assessment

## Guardrails for every implementation phase

1. Preserve existing routes and payloads. Add versioned/additive endpoints or optional fields first; deprecate only after consumer migration.
2. Before modifying a module, publish a short change note covering: current implementation, required change, database impact, API impact, and UI impact.
3. Use an EF Core migration per coherent schema change. Never duplicate an existing table or business concept.
4. Use expand → backfill → validate → enforce migrations for live data. Every migration requires PostgreSQL rollback/backup considerations.
5. Add tests at the slice being changed: domain/application unit tests, API integration tests against PostgreSQL, and Angular component/API-service tests.
6. Keep the current LIMS registration/result flow operational throughout; place new behavior behind compatible defaults where necessary.

## Step 1 — establish a safe production baseline

**Current implementation:** Secrets/default credentials are committed, migrations run automatically, and no test/CI baseline exists.

**Required changes:** externalize configuration/secrets; replace default-admin seeding with controlled bootstrap; add health checks, correlation/structured logging, ProblemDetails, secure CORS/environment configuration; introduce CI for build, tests, formatting, dependency/security scans, and migration validation. Make migration execution a release activity rather than API startup behavior.

**Database impact:** None initially. Validate migration history against each database before future changes.

**API impact:** Add unauthenticated health/readiness endpoints; preserve existing APIs. Normalize errors additively where clients tolerate `ProblemDetails`.

**UI impact:** Improve common error/session-expiry handling only; no business-screen breakage.

## Step 2 — introduce Clean Architecture by vertical slice

**Current implementation:** Controllers directly use EF Core and Domain/Application are placeholders.

**Required changes:** Establish Domain (entities/value objects/events), Application (use cases, interfaces, validators, DTO contracts), Infrastructure (EF/Identity implementations), and API (transport) boundaries. Begin with a low-risk master-data slice, then migrate registrations/results only when each is covered by tests. Do not add a generic repository merely for pattern compliance; use focused repositories/query services where aggregate boundaries demand them.

**Database impact:** None for the structural move.

**API impact:** Preserve existing paths/contracts; internal handlers replace controller logic.

**UI impact:** None unless API responses are normalized; use compatibility adapters if needed.

## Step 3 — make organization scope real (single clinic through multi-site)

**Current implementation:** Clinic/branch tables and nullable foreign keys exist, but they are neither populated nor enforced consistently.

**Required changes:** Define the organization model and scope rules: platform/system admin, clinic, branch/site, shared central lab, and active user assignment. Extend current-user context/JWT/claims or server-side session lookup with permitted clinic/branch scope. Add authorization handlers and tenant-aware query/command services. Backfill legacy records to a selected default clinic/branch before enforcement.

**Database impact:** Prefer additive organization assignment/history and explicit ownership fields only where absent. Backfill existing nullable transactional keys, then add composite indexes/constraints in later migrations. Review global master-code indexes before allowing local catalogues.

**API impact:** Existing APIs derive scope server-side. Optional `clinicId`/`branchId` filters are limited to authorized cross-site users; never trust arbitrary client ownership fields.

**UI impact:** Add a clinic/branch context selector for authorized users and organization administration screens; retain a transparent default for a single-clinic deployment.

## Step 4 — security, permissions, and auditability

**Current implementation:** JWT roles work in part; permission tables and audit tables are not operational.

**Required changes:** Establish permission keys as the authorization source, map roles to permissions, enforce permission policies/handlers, use a fallback authorization policy, implement account lifecycle/refresh token revocation, write login/activity/audit events, and add optimistic concurrency for mutable clinical/laboratory records.

**Database impact:** Reuse existing permission/audit/refresh-token tables. Add only fields needed for token family/revocation, audit correlation, and concurrency; no duplicate security tables.

**API impact:** Role-based access remains temporarily compatible while permission checks are introduced; add token refresh/logout endpoints and standard 401/403 responses.

**UI impact:** Drive navigation/actions from effective permissions, add user/role/organization-assignment administration.

## Step 5 — stabilize the LIMS order-to-report workflow

**Current implementation:** Registrations contain animals/items/payments; results can be entered/completed for direct test items.

**Required changes:** Formalize order/accession, patient/specimen, collection/receipt/rejection, barcode/label lifecycle, package/profile expansion, result draft/review/approval/release/amendment, reference-range versioning, and printable/signed reports. Verify every parameter belongs to its ordered test and every state transition is authorized/audited.

**Database impact:** Evolve existing registrations, registration animals/items, results, result values, barcode sequences, tests, profiles, packages, and ranges. Add specimen/order/report entities only when no existing table represents that concept; migrate legacy registrations as an initial order state.

**API impact:** Keep registration/result endpoints functional; add dedicated order/specimen/report endpoints and compatible read models. Avoid changing existing barcode identifiers.

**UI impact:** Incrementally enhance registration, then add specimen collection, worklists, result review/release, and report views.

## Step 6 — add veterinary hospital clinical operations

**Current implementation:** The application is LIMS-oriented with only registration-level owner/animal details.

**Required changes:** Introduce durable owner/patient records first, then appointments/queue, encounters, clinical records, prescriptions/treatments, vaccination, hospitalization/surgery, and discharge. Each slice must integrate with the existing laboratory order path rather than duplicate it.

**Database impact:** Add normalized clinical aggregates and foreign keys to existing registration/order records through migrations. Do not repurpose free-text legacy fields destructively; retain them for history/mapping.

**API impact:** New `/api/v1` clinical endpoints can coexist with legacy unversioned LIMS endpoints; publish OpenAPI contracts and deprecation timelines.

**UI impact:** Add patient chart and reception/clinical workspaces by feature route, keeping LIMS navigation intact.

## Step 7 — billing, inventory, integrations, and operations

**Current implementation:** Basic rates/payments exist; medicine/instrument/supplier records are masters only.

**Required changes:** Build invoice/ledger/payment reconciliation, discounts/tax/refunds, inventory transactions/lot/expiry, purchasing/dispensing, analyzer integration, notifications, reports, and external interoperability in separately testable modules.

**Database impact:** Add transactional accounting/inventory tables rather than overloading rates/payments/medicine masters. Link them to existing registrations/orders with immutable reference IDs.

**API impact:** Add versioned endpoints and webhooks/integration adapters; maintain reads of legacy payment/rate data.

**UI impact:** Introduce billing, inventory, integration monitoring, and reporting screens with permission-based actions.

## Step 8 — rollout discipline

Pilot each organization/workflow slice with a single clinic and branch, then enable multi-branch and central-lab scenarios. For each release, capture migration verification, backup/restore test, security test, performance thresholds, audit evidence, user training, feature flags, and rollback procedure.

