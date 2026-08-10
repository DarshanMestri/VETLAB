# Missing Features — Phase 1 Assessment

This list distinguishes schema foundation from delivered workflow. A modeled table/field is not treated as an implemented feature unless it is populated, scoped, and exposed safely through the API/UI.

## 1. Critical production capabilities

1. **Enforced organization isolation.** Clinic/branch foreign keys exist on some transactional tables, but no current-user organization context, query filters, request scope, or authorization rules enforce them. The system cannot yet safely operate as multi-clinic/multi-branch.
2. **Tenant-aware master data.** Most master tables have no clinic/branch ownership or controlled shared-catalog model. Tests, reference ranges, inventory masters, prices, and clients require a clear global-versus-organization ownership policy.
3. **Complete authorization model.** Persisted permission tables exist but have no runtime enforcement. Roles, screen permissions, and API authorization must be unified through policies/claims/handlers.
4. **Secure operational identity lifecycle.** Missing user provisioning UI/API workflow, password reset, lockout policy, refresh/rotation/revocation, login history writing, MFA/SSO strategy, and secure secret management.
5. **Auditable business events.** `AuditLog` is mapped but is not automatically written. Results need draft/review/approval/amendment lifecycle, immutable sign-off metadata, and a reason-for-change record.
6. **Reliability controls.** Missing health checks, structured correlated logs, metrics/tracing, backup/restore runbook, centralized configuration/secrets, and an error-response standard based on `ProblemDetails`.
7. **Automated test and delivery baseline.** No backend/frontend tests, integration-test database, CI pipeline, quality gates, or deployment artifacts are present.

## 2. Veterinary hospital workflow gaps

- Owner/pet master records: owner is currently a free-text registration value; animals are registration-scoped and only capture species/name/age. Missing durable owner, animal, breed, sex, color, date-of-birth, microchip, allergies, consent, medical history, and attachment records.
- Clinical care: appointments, schedules, queues, encounters/consultations, SOAP notes, diagnoses, prescriptions, treatments, vaccinations, hospitalization/kennels, surgery, discharge, referrals, and medical documents.
- Finance and reception: estimates, invoices, line-item taxes/discounts, refunds/voids, payment reconciliation, cashier sessions, insurance/corporate billing, credit limits, and receivables reporting.
- Inventory/pharmacy: stock locations, lot/batch, expiry, purchase orders, goods receipt, adjustments, dispensing, controlled-drug controls, reorder levels, and supplier transactions. Current medicine/supplier/category records are master data only.

## 3. LIMS workflow gaps

- Order lifecycle from accession to collection, acceptance/rejection, aliquoting, routing, transport, and chain of custody.
- Sample records with sample type/container, quantity, collection metadata, collector, storage location, stability, and barcode labels. Existing registration items are not specimen records.
- Test/order expansion for profiles/packages: profiles and packages exist but registration/result workflows process test items only; package/profile expansion and de-duplication are missing.
- Instrument integration, analyzer worklists, QC/calibration, reagent/lot traceability, method versioning, delta checks, critical-result escalation, and turnaround-time tracking.
- Result review, technical validation, clinical authorization, report generation/versioning, digital signature, result release, correction/amendment, and external delivery (email/portal/HL7/FHIR where required).
- Rich reference ranges: sex, breed, specimen/method/instrument, units/conversions, qualitative/categorical interpretation, effective dates, and versioning.

## 4. Multi-organization/hospital gaps

- Organization hierarchy beyond Clinic → Branch (legal entity, organization type, site/laboratory, department/location).
- Explicit operating model: shared central laboratory versus branch laboratory, inter-branch referrals, ownership of patients/orders/results/invoices, and cross-site reporting permissions.
- User organization assignments with effective dates and permitted branch scope; current `AppUser` has only one nullable clinic and branch.
- Organization-specific numbering, report templates/branding, currencies/taxes, business hours, roles/permissions, and master-data overrides.
- A safe single-clinic default/legacy organization migration strategy.

## 5. Frontend/product gaps

- Clinic/branch administration is exposed by API but not routed in the Angular UI.
- No owner/patient chart, appointment, encounter, invoice, inventory, sample-collection, approval, reporting, or configuration administration workflows.
- No shared typed API client, DTO model library, centralized error handling, loading/empty/error pattern, route-level authorization, or accessibility/i18n strategy.
- No Angular Material dependency/component use, despite the intended target stack.

