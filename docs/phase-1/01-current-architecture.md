# Current Architecture — Phase 1 Assessment

**Assessment date:** 2026-08-02  
**Scope:** Read-only source and migration analysis. No runtime database inspection was performed, so the schema description reflects the EF Core model and migrations in this repository.

## 1. Executive summary

Vet LIMS is an ASP.NET Core 9 / Angular 20 application with a working laboratory-information-management foundation. It supports identity login, role checks, laboratory master data, client pricing, registration/payment, barcode generation, result entry, and clinic/branch records.

The solution has projects named for Clean Architecture, but its implementation is currently a two-layer application: Web API controllers directly depend on `VetLimsDbContext`, entities live in Infrastructure, and Domain/Application contain only placeholders. This is a safe base for incremental modernization; it should not be rewritten.

## 2. Solution structure

| Path | Current responsibility | Assessment |
| --- | --- | --- |
| `backend/src/VetLims.Domain` | Empty `Class1`; no domain model or invariants. | Placeholder only. |
| `backend/src/VetLims.Application` | Empty `Class1`; references Domain. | Placeholder only. |
| `backend/src/VetLims.Infrastructure` | EF Core DbContext, identity models, entities, migrations, seeders, and three services. | Currently holds both persistence and domain concepts. |
| `backend/src/VetLims.WebApi` | Composition root, JWT/Identity configuration, middleware, controllers, DTOs, validation. | Contains API and application orchestration. |
| `frontend` | Angular 20 standalone application. | Feature-oriented folders, but most views contain direct HTTP calls. |

Dependency direction is currently `WebApi → Infrastructure`; `Application → Domain` exists only at project-reference level. Infrastructure does not reference Domain/Application because those layers have no implementation.

## 3. Backend architecture and implemented modules

### 3.1 Runtime composition

`Program.cs` configures Npgsql/EF Core, ASP.NET Identity (`AppUser`, `IdentityRole<int>`), JWT bearer authentication, two role policies (`RequireAdmin`, `RequireManager`), development CORS, Swagger in Development, Serilog console logging, a global exception middleware, and automatic migration/seeding at application startup.

`VetLimsDbContext` is an `IdentityDbContext` and supplies audit timestamps/soft-delete field mutation in `SaveChangesAsync`. It does **not** currently add global query filters for `IsDeleted`, clinic, or branch.

### 3.2 Controllers and API modules

| Area | Existing controller capability |
| --- | --- |
| Authentication and users | Login with JWT; admin user/role management; role assignment; user active status. |
| Authorization configuration | Role policies plus persisted permission/menu/screen/button catalog endpoints. Persisted permissions are not enforced by an authorization handler. |
| Organization | Admin CRUD/status management for `Clinic` and `Branch`. |
| Laboratory masters | Species, departments, units, tests, parameters, test reference ranges, profiles, packages, and a generic master-catalog endpoint. |
| Commercial | Clients, client rates and rates. |
| LIMS workflow | Registration creation/list/detail, barcode assignment, payments, basic dashboard counts, result worklist, result entry and completion. |
| Non-production sample | `WeatherForecastController` remains. |

Controllers commonly use `VetLimsDbContext` directly, include query and mapping logic, and return a mixture of raw responses and DTOs. `BaseApiController` defines an authenticated default, but most controllers inherit `ControllerBase` directly, so its default authorization is not consistently applied.

### 3.3 Services, repositories, DTOs, validation

There is no repository, unit-of-work abstraction, or application command/query handler layer. The three Infrastructure services are:

- `BarcodeService`: increments one shared sequence row and generates values such as `VL-00000001`.
- `PricingService`: resolves a client-specific price, then a null-client fallback.
- `ReferenceRangeService`: selects a species/age range and evaluates high/low flags.

`MastersDtos.cs` centralizes most HTTP request/response records, while some request types are declared inside controllers. FluentValidation is referenced and `MastersValidators.cs` exists, but `Program.cs` does not register FluentValidation services/auto-validation.

### 3.4 Authentication and authorization

- The login endpoint issues an 8-hour signed JWT with user ID, user name, and roles. It does not put clinic/branch context or permissions in the token.
- The JWT configuration source is `Jwt`, but production `appsettings.json` incorrectly nests a separate JWT block under `Logging`; a development JSON JWT block is present. Development values contain credentials/secrets in source control.
- Identity lockout is disabled at password check time and no refresh-token/rotation endpoint exists, despite refresh-token tables being modeled.
- API authorization is role-only where applied. `ClientController` and several other controllers do not declare `[Authorize]`; the application has no global fallback authorization policy.

### 3.5 Persistence model

The tracked EF model maps ASP.NET Identity tables plus these application table groups:

- Organization/security/audit: `clinics`, `branches`, `permissions`, `role_permissions`, `user_refresh_tokens`, `login_histories`, `user_activity_logs`, `menu_permissions`, `screen_permissions`, `button_permissions`, `audit_logs`.
- Master data: `species`, `breeds`, `animal_colors`, `genders`, `doctors`, `sample_types`, `containers`, `methods`, `instruments`, `suppliers`, `medicines`, `inventory_categories`, `departments`, `units`.
- Laboratory catalogue: `tests`, `test_parameters`, `test_reference_ranges`, `profiles`, `profile_tests`, `packages`, `package_items`.
- Client/pricing: `clients`, `client_rates`.
- LIMS transactions: `registrations`, `registration_animals`, `registration_items`, `payments`, `barcode_sequences`, `results`, `result_values`.

The most recent migrations add clinic/branch nullable foreign keys to users, client rates, registrations, registration items, payments, results, and result values. Controllers and services do not consistently populate or filter those keys, therefore the schema is multi-clinic-ready only in part.

Important modeled relationships/constraints include unique clinic code, unique `(ClinicId, Branch.Code)`, unique test/profile/package codes, composite profile-test key, unique package item, unique registration/item barcodes, and unique `(ResultId, TestParameterId)`. A concise database-impact inventory and risks are in the technical-debt report.

## 4. Frontend architecture

The Angular application uses standalone components, lazy component routes, Bootstrap 5, RxJS, Angular signals in several lists, template-driven forms, and an Angular development proxy to port 5035.

### Existing navigation/features

- Login and token persistence in browser `localStorage`.
- Dashboard, laboratory masters, clients/rates, registrations, result worklist, and result entry routes.
- A navbar conditionally shows master-data links according to decoded role claims.
- `authGuard` protects routed features. A `roleGuard` exists but is not used on routes.

### State and API handling

There is no global state store. `AuthService` owns identity state through `BehaviorSubject`; page-local signals and component fields hold feature state. HTTP interaction is split between a few thin API services and many direct `HttpClient` calls inside components. Two authentication interceptors exist, but only `authInterceptor` is registered; the registered one attaches a bearer token but does not handle 401/403. The API base is duplicated (`/api` and an unused hard-coded `http://localhost:5035/api`).

The package file does not list Angular Material, despite the target stack naming it. There are no frontend unit-test spec files in the repository.

## 5. Coding conventions observed

- C# uses nullable reference types, primary constructors in some controllers, PascalCase public members, async EF Core APIs, and concise comments.
- Angular uses standalone components, single quotes, 100-character Prettier configuration, lower-case kebab filenames, and predominantly inline templates/styles.
- Conventions are inconsistent: duplicate imports exist, names such as `ClientController.cs` versus `ClientsController`, different response shapes/pagination implementations, duplicated frontend API wrappers, and direct persistence access from controllers.

## 6. Baseline verification

`dotnet build VetLims.sln --no-restore -v:minimal` completed successfully during this assessment. No test project, test files, GitHub Actions workflow, Dockerfile, docker-compose configuration, editor configuration for backend, or deployment/IaC artifacts were found.

