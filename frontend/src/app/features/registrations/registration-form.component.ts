import { Component, inject, NgZone, ChangeDetectorRef, AfterViewInit, ElementRef, ViewChildren, QueryList } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import JsBarcode from 'jsbarcode';

enum RegItemType {
  Test = 1,
  Profile = 2,
  Package = 3,
}

enum PaymentMethodType {
  Cash = 1,
  Card = 2,
  Cheque = 3,
}

enum DiscountType {
  Percentage = 1,
  Amount = 2,
}

enum AgeUnit {
  Days = 1,
  Months = 2,
  Years = 3,
}

type Species = { id: number; name: string; code?: string };
type Client = { id: number; name: string; code?: string };
type OwnerOption = { id: number; code?: string; name: string; nameAr?: string; mobile?: string };
type AnimalOption = { id: number; ownerId: number; animalCode?: string; name: string; speciesId: number };
type DoctorOption = { id: number; code?: string; name: string };
type SampleTypeOption = { id: number; code?: string; name: string };
type OrderableOption = {
  id: number;
  code?: string;
  name: string;
  itemType: RegItemType;
  departmentId?: number | null;
  sampleTypeId?: number | null;
};
type PriceInfo = { rate: number; vatRate: number };

type BarcodePopupData = {
  registrationId: number;
  registrationBarcode: string;
  ownerName: string;
  ownerCode?: string;
  animals: { id: number; animalName: string; speciesName: string; barcode: string; ageInDays?: number | null; printCount: number; animalCode?: string }[];
};

type SampleCollectionData = {
  registrationId: number;
  registrationBarcode: string;
  ownerName: string;
  ownerCode?: string;
  items: { id: number; itemBarcode: string; sampleStatus: string; speciesId?: number; speciesName?: string; collectedAt?: string }[];
  animals: { id: number; animalName: string; speciesName: string; barcode: string; speciesId?: number; collectedAt?: string; allCollected?: boolean; printCount: number; animalCode?: string }[];
};

type LookupResponse = {
  owners: OwnerOption[];
  animals: AnimalOption[];
  doctors: DoctorOption[];
  clients: Client[];
  species: Species[];
  sampleTypes: SampleTypeOption[];
  orderables: OrderableOption[];
};

type CreateDto = {
  ownerName: string;
  ownerNameAr?: string | null;
  ownerMobile?: string | null;
  clientId?: number | null;
  ownerId?: number | null;
  doctorId?: number | null;
  referenceNumber?: string | null;
  clinicalNotes?: string | null;
  animals: { speciesId?: number | null; animalName?: string | null; animalId?: number | null }[];
  items: {
    itemType: RegItemType;
    itemId?: number | null;
    speciesId?: number | null;
    sampleTypeId?: number | null;
    registrationItemId?: number | null;
    isNew?: boolean;
    price?: number;
    vatAmount?: number;
  }[];
  initialPaymentAmount?: number | null;
  initialPaymentMethod?: PaymentMethodType | null;
  initialPaymentRef?: string | null;
  discountType?: DiscountType | null;
  discountValue?: number | null;
  ageValue?: number | null;
  ageUnit?: AgeUnit | null;
};

@Component({
  standalone: true,
  selector: 'app-registration-form',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="reg-container">
      <!-- Header -->
      <div class="reg-header">
        <div>
          <h2 class="reg-title">{{ editMode ? 'Edit Registration' : 'Laboratory Registration' }}</h2>
          <p class="reg-subtitle">{{ editMode ? 'Update an existing patient registration order.' : 'Create a new patient registration order.' }}</p>
        </div>
        <div class="d-flex gap-2">
          <a class="btn btn-outline-secondary btn-sm" routerLink="/registrations">Back</a>
          <button class="btn btn-outline-secondary btn-sm" type="button" (click)="resetForm()">Clear</button>
          <button *ngIf="successMessage && !editMode" class="btn btn-outline-info btn-sm" type="button" (click)="openSampleCollectionPopup()">Collect Sample</button>
          <button class="btn btn-primary btn-sm" type="button" (click)="save()" [disabled]="saving">
            {{ saving ? 'Saving...' : (editMode ? 'Update Registration' : 'Save Registration') }}
          </button>
        </div>
      </div>

      <div class="alert alert-danger" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="alert alert-success" *ngIf="successMessage">{{ successMessage }}</div>

      <div class="row g-3">
        <!-- ===== LEFT COLUMN: Owner & Patient ===== -->
        <div class="col-lg-4">
          <div class="card reg-card h-100" style="border-top: 3px solid var(--color-primary);">
            <h5 class="reg-card-title">Owner &amp; Patient</h5>
            <p class="reg-card-desc">Search for an existing owner or enter walk-in details.</p>

            <!-- Owner typeahead search -->
            <div class="mb-3 position-relative">
              <label class="form-label fw-semibold">Search Owner</label>
              <input
                class="form-control"
                [ngModel]="ownerSearch"
                (ngModelChange)="onOwnerSearchInput($event)"
                name="ownerSearch"
                placeholder="Type name or mobile..."
                autocomplete="off"
              />
              <div class="form-text" *ngIf="ownerSearchLoading">Searching...</div>
              <div class="form-text" *ngIf="!ownerSearchLoading">Min 2 characters for search.</div>
              <div class="owner-results" *ngIf="ownerSearchResults.length > 0">
                <button type="button" class="owner-result-item"
                        *ngFor="let o of ownerSearchResults"
                        (click)="selectOwnerFromSearch(o)">
                  <span class="fw-semibold">{{ o.name }}</span>
                  <span class="text-muted" *ngIf="o.mobile">{{ o.mobile }}</span>
                  <span class="text-muted" *ngIf="o.code">{{ o.code }}</span>
                </button>
              </div>
            </div>

            <div class="mb-3">
              <label class="form-label fw-semibold">Owner Name</label>
              <input class="form-control" [(ngModel)]="model.ownerName" name="ownerName" placeholder="Owner name" />
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Owner Name (Arabic)</label>
              <input class="form-control" dir="rtl" lang="ar" [(ngModel)]="model.ownerNameAr" name="ownerNameAr" placeholder="&#1575;&#1604;&#1573;&#1587;&#1605;" />
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Patient Age <small class="text-muted">(applies to all species)</small></label>
              <div class="input-group">
                <input class="form-control" type="number" min="0" step="1"
                       [(ngModel)]="ageValue" name="ageValue"
                       placeholder="e.g. 5" />
                <select class="form-select" style="max-width: 130px;" [(ngModel)]="ageUnit" name="ageUnit">
                  <option *ngFor="let u of ageUnitOptions" [ngValue]="u.value">{{ u.label }}</option>
                </select>
              </div>
              <div class="form-text">Used for species-wise reference range matching.</div>
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Mobile</label>
              <input class="form-control" [(ngModel)]="model.ownerMobile" name="ownerMobile" placeholder="Mobile number" />
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Client (price list)</label>
              <select class="form-select" [(ngModel)]="model.clientId" (ngModelChange)="onClientChange()" name="clientId">
                <option [ngValue]="null">Walk-in / no client</option>
                <option *ngFor="let client of clients" [ngValue]="client.id">
                  {{ client.name }}<span *ngIf="client.code"> &middot; {{ client.code }}</span>
                </option>
              </select>
              <div class="form-text" [class.text-warning]="model.clientId === null">
                {{ model.clientId === null ? 'No price list — items billed at 0.' : 'Prices from selected client.' }}
              </div>
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Doctor</label>
              <select class="form-select" [(ngModel)]="model.doctorId" name="doctorId">
                <option [ngValue]="null">Select doctor</option>
                <option *ngFor="let doctor of doctors" [ngValue]="doctor.id">
                  {{ doctor.name }}<span *ngIf="doctor.code"> &middot; {{ doctor.code }}</span>
                </option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Reference Number</label>
              <input class="form-control" [(ngModel)]="model.referenceNumber" name="referenceNumber" placeholder="Optional reference" />
            </div>
            <div class="mb-0">
              <label class="form-label fw-semibold">Clinical Notes</label>
              <textarea class="form-control" rows="3" [(ngModel)]="model.clinicalNotes" name="clinicalNotes" placeholder="Optional notes"></textarea>
            </div>
          </div>
        </div>

        <!-- ===== RIGHT COLUMN ===== -->
        <div class="col-lg-8">

          <!-- CARD: Patients / Animals -->
          <div class="card reg-card mb-3" style="border-top: 3px solid var(--color-secondary);">
            <div class="d-flex justify-content-between align-items-center mb-1">
              <h5 class="reg-card-title mb-0">Patients / Animals</h5>
              <span class="badge bg-primary">{{ model.animals.length }} added</span>
            </div>
            <p class="reg-card-desc">Add animals by species. Species groups drive test ordering below.</p>

            <div class="border rounded p-2 mb-2">
              <div class="row g-2 align-items-end">
                <div class="col-md-3">
                  <label class="small fw-semibold">Species</label>
                  <select class="form-select form-select-sm" [(ngModel)]="speciesDropdownId" name="speciesDropdownId">
                    <option [ngValue]="null">Select</option>
                    <option *ngFor="let s of species" [ngValue]="s.id">{{ s.name }}</option>
                  </select>
                </div>
                <div class="col-md-2">
                  <label class="small fw-semibold">Count</label>
                  <input class="form-control form-control-sm" type="number" min="1" max="50" step="1" [(ngModel)]="speciesCount" name="speciesCount" />
                </div>
                <div class="col-md-4 d-flex gap-2">
                  <button class="btn btn-sm btn-outline-success" [disabled]="speciesDropdownId == null || speciesCount < 1"
                          type="button" (click)="addAnimalsBySpecies()">+ Add</button>
                  <button class="btn btn-sm btn-outline-secondary" type="button" (click)="addAnimal()">+ Manual</button>
                </div>
                <div class="col-md-3">
                  <label class="small fw-semibold">Existing animal</label>
                  <select class="form-select form-select-sm" [(ngModel)]="quickAnimalId" name="quickAnimalId">
                    <option [ngValue]="null">Select...</option>
                    <option *ngFor="let option of filteredAnimals" [ngValue]="option.id">
                      {{ option.name }}<span *ngIf="option.animalCode"> &middot; {{ option.animalCode }}</span>
                    </option>
                  </select>
                </div>
                <div class="col-auto">
                  <button class="btn btn-sm btn-outline-primary" [disabled]="!quickAnimalId" type="button" (click)="addExistingAnimal()">Add</button>
                </div>
              </div>
            </div>

            <div class="table-responsive" *ngIf="model.animals.length">
              <table class="table table-sm align-middle mb-0">
                <thead>
                  <tr>
                    <th style="width: 40px">#</th>
                    <th>Species</th>
                    <th>Animal Name</th>
                    <th style="width: 80px" class="text-end"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let animal of model.animals; let i = index">
                    <td>{{ i + 1 }}</td>
                    <td>
                      <select class="form-select form-select-sm" [(ngModel)]="animal.speciesId" [attr.name]="'sp' + i">
                        <option [ngValue]="null">Select</option>
                        <option *ngFor="let s of species" [ngValue]="s.id">{{ s.name }}</option>
                      </select>
                    </td>
                    <td>
                      <input class="form-control form-control-sm" placeholder="Name" [(ngModel)]="animal.animalName" [attr.name]="'an' + i" />
                    </td>
                    <td class="text-end">
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeAnimal(i)">Remove</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <p class="small text-muted mb-0 mt-1" *ngIf="!model.animals.length">Add at least one animal.</p>
          </div>

          <!-- CARD: Test Orders (species-grouped) -->
          <div class="card reg-card mb-3" style="border-top: 3px solid var(--color-accent);">
            <div class="d-flex justify-content-between align-items-center mb-1">
              <h5 class="reg-card-title mb-0">Test Orders</h5>
              <span class="badge bg-primary">{{ selectedCount }} selected</span>
            </div>
            <p class="reg-card-desc">Add tests under each species group. Species and sample type are inherited automatically.</p>

            <!-- Per-species groups -->
            <div *ngFor="let sp of uniqueSpecies" class="species-group mb-3">
              <div class="species-group-header">
                <h6 class="mb-0">
                  <span class="species-label">{{ sp.name }}</span>
                  <span class="badge bg-secondary ms-2">{{ animalCountOf(sp.id) }} animals</span>
                  <span class="badge bg-info text-dark ms-1" *ngIf="ageDisplayText">Age: {{ ageDisplayText }}</span>
                </h6>
              </div>
              <div class="species-group-body">
                <div class="row g-2 align-items-end mb-2">
                  <div class="col-auto">
                    <div class="btn-group btn-group-sm" role="group">
                      <button *ngFor="let t of itemTypeOptions" class="btn"
                              [class.btn-primary]="getPickerType(sp.id) === t.value"
                              [class.btn-outline-primary]="getPickerType(sp.id) !== t.value"
                              type="button" (click)="setPickerTypeForSpecies(sp.id, t.value)">{{ t.label }}</button>
                    </div>
                  </div>
                  <div class="col">
                    <select class="form-select form-select-sm"
                            [ngModel]="getPickerItemId(sp.id)"
                            (ngModelChange)="setPickerItemIdForSpecies(sp.id, $event)"
                            [attr.name]="'picker_' + sp.id">
                      <option [ngValue]="null">Select {{ typeLabel(getPickerType(sp.id)) }}...</option>
                      <option *ngFor="let o of orderablesByType(getPickerType(sp.id))" [ngValue]="o.id">
                        {{ o.name }}<span *ngIf="o.code"> &middot; {{ o.code }}</span>
                      </option>
                    </select>
                  </div>
                  <div class="col-auto">
                    <button class="btn btn-sm btn-outline-primary" [disabled]="!getPickerItemId(sp.id)" type="button"
                            (click)="addOrder(sp.id)">+ Add</button>
                  </div>
                </div>

                <div class="table-responsive" *ngIf="itemsForSpecies(sp.id).length">
                  <table class="table table-sm align-middle mb-0">
                    <thead>
                      <tr>
                        <th>Type</th>
                        <th>Item</th>
                        <th class="text-center">Qty</th>
                        <th>Sample Type</th>
                        <th class="text-end">Net</th>
                        <th class="text-end">VAT</th>
                        <th class="text-end">Total</th>
                        <th style="width: 70px" class="text-end"></th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let item of itemsForSpecies(sp.id)">
                        <td><span class="badge bg-light text-dark">{{ typeLabel(item.itemType) }}</span></td>
                        <td>{{ itemName(item) }}</td>
                        <td class="text-center fw-semibold">{{ quantityFor(item) }}</td>
                        <td><span class="text-muted small">{{ sampleTypeName(item.sampleTypeId) }}</span></td>
                        <td class="text-end">{{ priceFor(item) | currency:'AED' }}</td>
                        <td class="text-end"><span class="text-muted">{{ lineVat(item) | currency:'AED' }}</span> <small class="text-muted">({{ vatRateFor(item) }}%)</small></td>
                        <td class="text-end fw-semibold">{{ lineTotal(item) | currency:'AED' }}</td>
                        <td class="text-end">
                          <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeItemByRef(item)">Remove</button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p class="small text-muted mb-0" *ngIf="!itemsForSpecies(sp.id).length">No tests added for {{ sp.name }} yet.</p>
              </div>
            </div>

            <!-- Orders without species (profiles & packages) -->
            <div class="species-group mb-2" *ngIf="itemsForSpecies(null).length || uniqueSpecies.length === 0">
              <div class="species-group-header">
                <h6 class="mb-0"><span class="species-label">Profiles &amp; Packages</span></h6>
              </div>
              <div class="species-group-body">
                <div class="row g-2 align-items-end mb-2">
                  <div class="col-auto">
                    <div class="btn-group btn-group-sm" role="group">
                      <button *ngFor="let t of itemTypeOptions" class="btn"
                              [class.btn-primary]="getPickerType(null) === t.value"
                              [class.btn-outline-primary]="getPickerType(null) !== t.value"
                              type="button" (click)="setPickerTypeForSpecies(null, t.value)">{{ t.label }}</button>
                    </div>
                  </div>
                  <div class="col">
                    <select class="form-select form-select-sm"
                            [ngModel]="getPickerItemId(null)"
                            (ngModelChange)="setPickerItemIdForSpecies(null, $event)"
                            name="picker_global">
                      <option [ngValue]="null">Select {{ typeLabel(getPickerType(null)) }}...</option>
                      <option *ngFor="let o of orderablesByType(getPickerType(null))" [ngValue]="o.id">
                        {{ o.name }}<span *ngIf="o.code"> &middot; {{ o.code }}</span>
                      </option>
                    </select>
                  </div>
                  <div class="col-auto">
                    <button class="btn btn-sm btn-outline-primary" [disabled]="!getPickerItemId(null)" type="button"
                            (click)="addOrder(null)">+ Add</button>
                  </div>
                </div>

                <div class="table-responsive" *ngIf="itemsForSpecies(null).length">
                  <table class="table table-sm align-middle mb-0">
                    <thead>
                      <tr>
                        <th>Type</th>
                        <th>Item</th>
                        <th class="text-center">Qty</th>
                        <th>Sample Type</th>
                        <th class="text-end">Net</th>
                        <th class="text-end">VAT</th>
                        <th class="text-end">Total</th>
                        <th style="width: 70px" class="text-end"></th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let item of itemsForSpecies(null)">
                        <td><span class="badge bg-light text-dark">{{ typeLabel(item.itemType) }}</span></td>
                        <td>{{ itemName(item) }}</td>
                        <td class="text-center fw-semibold">{{ quantityFor(item) }}</td>
                        <td><span class="text-muted small">{{ sampleTypeName(item.sampleTypeId) }}</span></td>
                        <td class="text-end">{{ priceFor(item) | currency:'AED' }}</td>
                        <td class="text-end"><span class="text-muted">{{ lineVat(item) | currency:'AED' }}</span> <small class="text-muted">({{ vatRateFor(item) }}%)</small></td>
                        <td class="text-end fw-semibold">{{ lineTotal(item) | currency:'AED' }}</td>
                        <td class="text-end">
                          <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeItemByRef(item)">Remove</button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
            <p class="small text-muted mb-0" *ngIf="!model.items.length">No orders yet — pick a species group above and add tests.</p>
          </div>

          <!-- CARD: Registration Summary (no test details) -->
          <div class="card reg-card mb-3" style="border-top: 3px solid var(--color-info);">
            <h5 class="reg-card-title mb-2">Registration Summary</h5>
            <div class="row">
              <div class="col-md-4">
                <div class="summary-label">Owner</div>
                <div class="summary-value">{{ model.ownerName || '(not set)' }}</div>
                <div class="summary-label" *ngIf="model.ownerNameAr">Name (Arabic)</div>
                <div class="summary-value" *ngIf="model.ownerNameAr" dir="rtl">{{ model.ownerNameAr }}</div>
                <div class="summary-label">Mobile</div>
                <div class="summary-value-last">{{ model.ownerMobile || '-' }}</div>
              </div>
              <div class="col-md-4">
                <div class="summary-label">Patients</div>
                <div class="summary-value">{{ model.animals.length }} animals</div>
                <div class="summary-label">Species</div>
                <div class="summary-value-last">{{ uniqueSpecies.length }} unique</div>
              </div>
              <div class="col-md-4">
                <div class="summary-label">Doctor</div>
                <div class="summary-value">{{ selectedDoctorName || '(none)' }}</div>
                <div class="summary-label">Client</div>
                <div class="summary-value-last">{{ selectedClientName || '(walk-in)' }}</div>
                <div class="summary-label" *ngIf="editMode">Payment Status</div>
                <div *ngIf="editMode" class="mt-1">
                  <span class="badge"
                    [class.bg-danger]="paymentStatus === 'Unpaid'"
                    [class.bg-warning text-dark]="paymentStatus === 'Partially Paid'"
                    [class.bg-success]="paymentStatus === 'Paid'">
                    {{ paymentStatus }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- CARD: Discount & Billing -->
          <div class="card reg-card mb-3" style="border-top: 3px solid var(--color-warning);">
            <h5 class="reg-card-title mb-2">Discount &amp; Billing</h5>
            <div class="row g-3">
              <div class="col-md-5">
                <label class="form-label fw-semibold">Discount Type</label>
                <div class="btn-group w-100" role="group">
                  <button class="btn btn-sm"
                          [class.btn-warning]="discountType === DiscountType.Percentage"
                          [class.btn-outline-warning]="discountType !== DiscountType.Percentage"
                          type="button" (click)="toggleDiscountType(DiscountType.Percentage)">Percentage %</button>
                  <button class="btn btn-sm"
                          [class.btn-warning]="discountType === DiscountType.Amount"
                          [class.btn-outline-warning]="discountType !== DiscountType.Amount"
                          type="button" (click)="toggleDiscountType(DiscountType.Amount)">Amount (AED)</button>
                  <button class="btn btn-sm btn-outline-secondary" type="button" (click)="clearDiscount()">Clear</button>
                </div>
              </div>
              <div class="col-md-4">
                <label class="form-label fw-semibold">Discount Value</label>
                <input class="form-control" type="number" min="0" step="0.01"
                       [disabled]="!discountType"
                       [placeholder]="discountType === DiscountType.Amount ? '0.00' : '0'"
                       [(ngModel)]="discountValue" name="discountValue" />
                <div class="form-text" *ngIf="discountType === DiscountType.Percentage">0-100%</div>
                <div class="form-text" *ngIf="discountType === DiscountType.Amount">Fixed AED amount</div>
              </div>
              <div class="col-md-3 d-flex align-items-end">
                <div *ngIf="discountAmount > 0" class="alert alert-warning py-1 px-2 mb-0 w-100 text-center">
                  <small>Discount: <strong>-{{ discountAmount | currency:'AED' }}</strong></small>
                </div>
              </div>
              <div class="col-12">
                <div class="border-top pt-2">
                  <div class="d-flex justify-content-between">
                    <span>Subtotal (excl. VAT)</span>
                    <span>{{ subtotal | currency:'AED' }}</span>
                  </div>
                  <div class="d-flex justify-content-between">
                    <span>VAT Total</span>
                    <span>{{ vatTotal | currency:'AED' }}</span>
                  </div>
                  <div class="d-flex justify-content-between text-warning" *ngIf="discountAmount > 0">
                    <span>Discount</span>
                    <span>-{{ discountAmount | currency:'AED' }}</span>
                  </div>
                  <div class="d-flex justify-content-between fw-bold border-top pt-1 mt-1" style="font-size: 1.05rem;">
                    <span>Net Total</span>
                    <span style="color: var(--color-primary);">{{ netTotal | currency:'AED' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- CARD: Payment -->
          <div class="card reg-card mb-3" style="border-top: 3px solid var(--color-success);">
            <h5 class="reg-card-title mb-2">Payment</h5>
            <div class="mb-3">
              <label class="form-label fw-semibold">Payment Mode</label>
              <div class="btn-group w-100" role="group">
                <button *ngFor="let m of paymentMethods" class="btn btn-sm"
                        [class.btn-success]="model.initialPaymentMethod === m.value"
                        [class.btn-outline-success]="model.initialPaymentMethod !== m.value"
                        type="button" (click)="selectPaymentMethod(m.value)">{{ m.label }}</button>
              </div>
            </div>
            <div class="row g-2">
              <div class="col-md-6">
                <label class="form-label fw-semibold">Amount (AED)</label>
                <input class="form-control" type="number" min="0" step="0.01"
                       [ngModel]="model.initialPaymentAmount"
                       (ngModelChange)="onPaymentAmountChange($event)"
                       name="initialPaymentAmount" placeholder="0.00" />
              </div>
              <div class="col-md-6">
                <label class="form-label fw-semibold">Reference</label>
                <input class="form-control" [(ngModel)]="model.initialPaymentRef" name="initialPaymentRef" placeholder="Cheque / txn ID" />
              </div>
            </div>
            <div class="d-flex justify-content-between align-items-center mt-3">
              <button class="btn btn-sm btn-outline-success" type="button" (click)="payInFull()" [disabled]="grandTotal <= 0">
                Pay in full ({{ grandTotal | currency:'AED' }})
              </button>
              <div>
                <span class="text-muted me-2">Balance:</span>
                <span class="fw-bold" [class.text-danger]="balanceDue > 0" [class.text-success]="balanceDue <= 0" style="font-size: 1.1rem;">
                  {{ balanceDue | currency:'AED' }}
                </span>
              </div>
            </div>
            <!-- Payment History -->
            <div *ngIf="existingPayments.length" class="mt-3">
              <label class="form-label fw-semibold small">Payment History</label>
              <table class="table table-sm table-bordered mb-0">
                <thead class="table-light">
                  <tr><th>#</th><th>Method</th><th class="text-end">Amount</th><th>Date</th><th>Reference</th></tr>
                </thead>
                <tbody>
                  <tr *ngFor="let p of existingPayments; let i = index">
                    <td>{{ i + 1 }}</td>
                    <td>{{ p.method }}</td>
                    <td class="text-end">{{ p.amount | currency:'AED' }}</td>
                    <td>{{ p.paidAt | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td>{{ p.reference || '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

        </div><!-- /col-lg-8 -->
      </div><!-- /row -->

      <!-- BARCODE POPUP -->
      <div class="modal-backdrop" *ngIf="showBarcodePopup" (click)="closeBarcodePopup()"></div>
      <div class="modal d-block" *ngIf="showBarcodePopup" tabindex="-1">
        <div class="modal-dialog modal-lg modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Generated Animal Barcodes</h5>
              <button type="button" class="btn-close" (click)="closeBarcodePopup()"></button>
            </div>
            <div class="modal-body">
              <div class="mb-2">
                <small class="text-muted">Registration: <strong>{{ barcodePopupData?.registrationBarcode }}</strong> | Owner: <strong>{{ barcodePopupData?.ownerName }}</strong> <span *ngIf="barcodePopupData?.ownerCode">({{ barcodePopupData?.ownerCode }})</span></small>
              </div>
              <div class="table-responsive">
                <table class="table table-sm table-bordered align-middle mb-0">
                  <thead class="table-light">
                    <tr><th>#</th><th>Animal ID</th><th>Animal</th><th>Species</th><th>Barcode</th><th>Preview</th><th>Count</th><th>Actions</th></tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let a of barcodePopupData?.animals; let i = index">
                      <td>{{ i + 1 }}</td>
                      <td><code>{{ a.animalCode || '-' }}</code></td>
                      <td>{{ a.animalName }}</td>
                      <td>{{ a.speciesName }}</td>
                      <td><code>{{ a.barcode }}</code></td>
                      <td><svg #barcodeSvg [attr.data-code]="a.barcode" width="120" height="30"></svg></td>
                      <td><input type="number" class="form-control form-control-sm" style="width:60px" min="1" [(ngModel)]="a.printCount" name="count_{{a.id}}"></td>
                      <td>
                        <button class="btn btn-outline-secondary btn-sm" type="button" (click)="printSingleBarcode(a.id, barcodePopupData!.registrationId, a.printCount)">Print</button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="printAllBarcodes(2)">Print All Labels</button>
              <button type="button" class="btn btn-info" (click)="openSampleCollectionPopup()">Collect Sample</button>
              <button type="button" class="btn btn-primary" (click)="closeBarcodePopup()">Done</button>
            </div>
          </div>
        </div>
      </div>

      <!-- SAMPLE COLLECTION POPUP -->
      <div class="modal-backdrop" *ngIf="showSamplePopup" (click)="closeSamplePopup()"></div>
      <div class="modal d-block" *ngIf="showSamplePopup" tabindex="-1">
        <div class="modal-dialog modal-lg modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Sample Collection</h5>
              <button type="button" class="btn-close" (click)="closeSamplePopup()"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <small class="text-muted">Registration: <strong>{{ sampleCollectionData?.registrationBarcode }}</strong> | Owner: <strong>{{ sampleCollectionData?.ownerName }}</strong> <span *ngIf="sampleCollectionData?.ownerCode">({{ sampleCollectionData?.ownerCode }})</span></small>
              </div>
              <h6 class="mb-2">Animals &amp; Barcodes</h6>
              <div class="table-responsive mb-3">
                <table class="table table-sm table-bordered align-middle mb-0">
                  <thead class="table-light">
                    <tr><th>#</th><th>Animal ID</th><th>Animal</th><th>Species</th><th>Barcode</th><th>Preview</th><th>Count</th><th>Actions</th></tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let a of sampleCollectionData?.animals; let i = index">
                      <td>{{ i + 1 }}</td>
                      <td><code>{{ a.animalCode || '-' }}</code></td>
                      <td>{{ a.animalName }}</td>
                      <td>{{ a.speciesName }}</td>
                      <td><code>{{ a.barcode }}</code></td>
                      <td><svg #printBarcodeSvg [attr.data-code]="a.barcode" width="120" height="30"></svg></td>
                      <td><input type="number" class="form-control form-control-sm" style="width:60px" min="1" [(ngModel)]="a.printCount" name="scount_{{a.id}}"></td>
                      <td>
                        <button class="btn btn-outline-secondary btn-sm" type="button" (click)="printSingleBarcode(a.id, sampleCollectionData!.registrationId, a.printCount)">Print</button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <h6 class="mb-2">Sample Status</h6>
              <div class="table-responsive">
                <table class="table table-sm table-bordered align-middle mb-0">
                  <thead class="table-light">
                    <tr><th>Item Barcode</th><th>Species</th><th>Status</th><th>Actions</th></tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of sampleCollectionData?.items">
                      <td><code>{{ item.itemBarcode }}</code></td>
                      <td>{{ item.speciesName || '-' }}</td>
                      <td><span class="badge" [ngClass]="sampleStatusColor(item.sampleStatus)">{{ item.sampleStatus }}</span></td>
                      <td>
                        <button *ngIf="item.sampleStatus === 'NotCollected'" class="btn btn-outline-success btn-sm" type="button" (click)="collectSample(item.id)">Collect</button>
                        <button *ngIf="item.sampleStatus === 'Collected'" class="btn btn-outline-primary btn-sm" type="button" (click)="receiveSample(item.id)">Receive</button>
                        <span *ngIf="item.sampleStatus === 'Received' || item.sampleStatus === 'Processing' || item.sampleStatus === 'Processed'" class="text-muted small">—</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-success" (click)="collectAllSamples()">Collect All Samples</button>
              <button type="button" class="btn btn-outline-secondary" (click)="printAllBarcodes(2)">Print All Labels</button>
              <button type="button" class="btn btn-primary" (click)="closeSamplePopup()">Close</button>
            </div>
          </div>
        </div>
      </div>

    </div><!-- /reg-container -->
  `,
  styles: [`
    .reg-container { padding: 1rem 0 2rem; }
    .reg-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.5rem; }
    .reg-title { margin-bottom: 0.15rem; font-weight: 700; color: var(--color-main-text); }
    .reg-subtitle { margin-bottom: 0; color: var(--color-secondary-text); font-size: 0.875rem; }
    .reg-card { background: var(--color-card-background); border-color: var(--color-border); padding: 1rem; }
    .reg-card-title { font-weight: 600; font-size: 1rem; color: var(--color-main-text); margin-bottom: 0.15rem; }
    .reg-card-desc { font-size: 0.8rem; color: var(--color-secondary-text); margin-bottom: 0.75rem; }
    .species-group { border: 1px solid var(--color-border); border-radius: 0.5rem; overflow: hidden; }
    .species-group-header { background: var(--color-tint); padding: 0.5rem 0.75rem; border-bottom: 1px solid var(--color-border); }
    .species-group-body { padding: 0.75rem; }
    .species-label { color: var(--color-primary); font-weight: 600; }
    .summary-label { font-size: 0.75rem; color: var(--color-secondary-text); text-transform: uppercase; letter-spacing: 0.03em; margin-bottom: 0.1rem; }
    .summary-value { font-weight: 600; color: var(--color-main-text); margin-bottom: 0.5rem; }
    .summary-value-last { font-weight: 600; color: var(--color-main-text); }
    .owner-results { position: absolute; z-index: 10; background: white; border: 1px solid var(--color-border); border-radius: 0.375rem; width: 100%; max-height: 320px; overflow-y: auto; box-shadow: 0 4px 12px rgba(0,0,0,0.1); margin-top: 2px; }
    .owner-result-item { display: flex; gap: 0.5rem; align-items: center; width: 100%; padding: 0.5rem 0.75rem; border: none; background: none; text-align: left; cursor: pointer; font-size: 0.875rem; }
    .owner-result-item:hover { background: var(--color-tint); }
    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1040; }
    .modal { z-index: 1050; }
  `],
})
export class RegistrationFormComponent implements AfterViewInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private ngZone = inject(NgZone);
  private cdr = inject(ChangeDetectorRef);

  editMode = false;
  editId: number | null = null;

  readonly DiscountType = DiscountType;
  readonly regItemType = RegItemType;

  readonly paymentMethods = [
    { value: PaymentMethodType.Cash, label: 'Cash' },
    { value: PaymentMethodType.Card, label: 'Card' },
    { value: PaymentMethodType.Cheque, label: 'Cheque' },
  ];

  readonly itemTypeOptions = [
    { value: RegItemType.Test, label: 'Test' },
    { value: RegItemType.Profile, label: 'Profile' },
    { value: RegItemType.Package, label: 'Package' },
  ];

  species: Species[] = [];
  clients: Client[] = [];
  owners: OwnerOption[] = [];
  animals: AnimalOption[] = [];
  doctors: DoctorOption[] = [];
  sampleTypes: SampleTypeOption[] = [];
  orderables: OrderableOption[] = [];
  private priceMap = new Map<string, PriceInfo>();

  speciesDropdownId: number | null = null;
  speciesCount = 1;
  quickAnimalId: number | null = null;
  speciesPickerState = new Map<number | null, { type: RegItemType; itemId: number | null }>();
  orderableSearch = '';

  // Owner typeahead
  ownerSearch = '';
  ownerSearchResults: OwnerOption[] = [];
  ownerSearchLoading = false;
  private ownerSearchTimer: ReturnType<typeof setTimeout> | null = null;

  // Barcode popup after save
  showBarcodePopup = false;
  barcodePopupData: BarcodePopupData | null = null;

  // Sample collection popup
  showSamplePopup = false;
  sampleCollectionData: SampleCollectionData | null = null;

  // Payment history (edit mode)
  existingPayments: { id: number; method: string; amount: number; paidAt: string; reference?: string; currency: string }[] = [];
  paymentStatus = 'Unpaid';

  // Stored amounts from the loaded registration (edit mode) — preserved on save.
  storedTotalAmount = 0;
  storedPaidAmount = 0;
  storedBalance = 0;

  @ViewChildren('barcodeSvg') barcodeSvgs!: QueryList<ElementRef<SVGSVGElement>>;
  @ViewChildren('printBarcodeSvg') printBarcodeSvgs!: QueryList<ElementRef<SVGSVGElement>>;

  // Discount
  discountType: DiscountType | null = null;
  discountValue: number | null = null;

  // Age (registration-level)
  ageValue: number | null = null;
  ageUnit: AgeUnit = AgeUnit.Years;
  readonly ageUnitOptions = [
    { value: AgeUnit.Days, label: 'Days' },
    { value: AgeUnit.Months, label: 'Months' },
    { value: AgeUnit.Years, label: 'Years' },
  ];

  errorMessage = '';
  successMessage = '';
  saving = false;

  model: CreateDto = this.createEmptyModel();

  // --- Cached Computed State ---
  cachedSubtotal = 0;
  cachedVatTotal = 0;
  cachedDiscountAmount = 0;
  cachedNetTotal = 0;
  cachedBalanceDue = 0;
  cachedUniqueSpecies: { id: number; name: string }[] = [];
  cachedItemsBySpecies = new Map<number | null, { itemType: RegItemType; itemId?: number | null; speciesId?: number | null; sampleTypeId?: number | null; registrationItemId?: number | null; isNew?: boolean; price?: number; vatAmount?: number }[]>();
  cachedAnimalCountBySpecies = new Map<number, number>();
  cachedTotalAnimals = 0;
  cachedOrderableMap = new Map<string, string>();
  cachedSampleTypeMap = new Map<number, string>();

  // --- Computed (cheap lookups only) ---

  get selectedCount() {
    return this.model.items.filter((item) => item.itemId).length;
  }

  get animalCount() {
    return this.cachedTotalAnimals;
  }

  get uniqueSpecies() {
    return this.cachedUniqueSpecies;
  }

  get subtotal() {
    return this.cachedSubtotal;
  }

  get vatTotal() {
    return this.cachedVatTotal;
  }

  get discountAmount() {
    return this.cachedDiscountAmount;
  }

  get netTotal() {
    return this.cachedNetTotal;
  }

  get grandTotal() {
    return this.cachedNetTotal;
  }

  get balanceDue() {
    return this.cachedBalanceDue;
  }

  get filteredAnimals(): AnimalOption[] {
    if (!this.model.ownerId) return [];
    return this.animals.filter((animal) => animal.ownerId === this.model.ownerId);
  }

  get selectedDoctorName(): string | null {
    if (!this.model.doctorId) return null;
    return this.doctors.find((d) => d.id === this.model.doctorId)?.name ?? null;
  }

  get selectedClientName(): string | null {
    if (!this.model.clientId) return null;
    return this.clients.find((c) => c.id === this.model.clientId)?.name ?? null;
  }

  get ageDisplayText(): string {
    if (!this.ageValue || this.ageValue <= 0) return '';
    const unit = this.ageUnitOptions.find((u) => u.value === this.ageUnit)?.label ?? '';
    return `${this.ageValue} ${unit}`;
  }

  get paidAmount() {
    return this.editMode ? this.storedPaidAmount : (this.model.initialPaymentAmount ?? 0);
  }

  // --- Cache Rebuild Methods ---

  private rebuildCaches() {
    this.rebuildSpeciesCache();
    this.rebuildItemsBySpeciesCache();
    this.rebuildOrderableMaps();
    this.recalculateTotals();
  }

  private rebuildSpeciesCache() {
    const speciesMap = new Map<number, string>();
    const countMap = new Map<number, number>();
    let total = 0;
    for (const a of this.model.animals) {
      total++;
      if (a.speciesId) {
        countMap.set(a.speciesId, (countMap.get(a.speciesId) ?? 0) + 1);
        if (!speciesMap.has(a.speciesId)) {
          const sp = this.species.find((s) => s.id === a.speciesId);
          if (sp) speciesMap.set(a.speciesId, sp.name);
        }
      }
    }
    this.cachedTotalAnimals = total;
    this.cachedAnimalCountBySpecies = countMap;
    this.cachedUniqueSpecies = Array.from(speciesMap.entries()).map(([id, name]) => ({ id, name }));
  }

  private rebuildItemsBySpeciesCache() {
    const map = new Map<number | null, typeof this.model.items>();
    for (const item of this.model.items) {
      if (!item.itemId) continue;
      const key = item.speciesId ?? null;
      const arr = map.get(key);
      if (arr) {
        arr.push(item);
      } else {
        map.set(key, [item]);
      }
    }
    this.cachedItemsBySpecies = map;
  }

  private rebuildOrderableMaps() {
    this.cachedOrderableMap.clear();
    for (const o of this.orderables) {
      this.cachedOrderableMap.set(`${o.itemType}:${o.id}`, o.name);
    }
    this.cachedSampleTypeMap.clear();
    for (const s of this.sampleTypes) {
      this.cachedSampleTypeMap.set(s.id, s.name);
    }
  }

  recalculateTotals() {
    let subtotal = 0;
    let vatTotal = 0;
    for (const item of this.model.items) {
      if (!item.itemId) continue;
      const qty = this.cachedAnimalCountBySpecies.get(item.speciesId ?? 0) ?? this.cachedTotalAnimals;

      if (this.editMode && !item.isNew && item.registrationItemId) {
        // Pre-existing tests keep their original stored price / VAT. Only the
        // quantity reflects animals added or removed during this edit.
        subtotal += (item.price ?? 0) * qty;
        vatTotal += (item.vatAmount ?? 0) * qty;
      } else {
        const rate = this.priceInfo(item.itemType, item.itemId ?? null);
        subtotal += rate.rate * qty;
        if (rate.rate > 0) {
          vatTotal += Math.round(rate.rate * rate.vatRate) / 100 * qty;
        }
      }
    }
    this.cachedSubtotal = subtotal;
    this.cachedVatTotal = vatTotal;

    const base = subtotal + vatTotal;
    let discount = 0;
    if (this.discountType && this.discountValue && this.discountValue > 0) {
      if (this.discountType === DiscountType.Percentage) {
        discount = this.discountValue > 100 ? base : Math.round(base * this.discountValue * 100) / 10000;
      } else {
        discount = Math.min(this.discountValue, base);
      }
    }
    this.cachedDiscountAmount = discount;
    this.cachedNetTotal = Math.max(base - discount, 0);
    this.cachedBalanceDue = Math.max(this.cachedNetTotal - this.paidAmount, 0);
  }

  // --- Lifecycle ---

  ngAfterViewInit() {
    // Barcode rendering is triggered after popups open via renderBarcodes()
  }

  private renderBarcodes() {
    setTimeout(() => {
      this.barcodeSvgs?.forEach((el) => {
        try {
          JsBarcode(el.nativeElement, el.nativeElement.getAttribute('data-code') || '', {
            format: 'CODE128',
            width: 1.5,
            height: 40,
            displayValue: false,
            margin: 0,
          });
        } catch {}
      });
      this.printBarcodeSvgs?.forEach((el) => {
        try {
          JsBarcode(el.nativeElement, el.nativeElement.getAttribute('data-code') || '', {
            format: 'CODE128',
            width: 2,
            height: 50,
            displayValue: false,
            margin: 0,
          });
        } catch {}
      });
    }, 50);
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.editMode = true;
      this.editId = Number(id);
      // Fetch registration + lookups concurrently so dropdowns never wait
      // behind the detail request (no second-wave population).
      this.loadRegistrationForEdit();
      this.loadLookups();
    } else {
      this.loadLookups();
    }
  }

  private loadRegistrationForEdit() {
    this.http.get<any>(`/api/registrations/${this.editId}`).subscribe({
      next: (reg) => {
        this.ngZone.run(() => {
          this.model.ownerName = reg.ownerName ?? '';
          this.model.ownerNameAr = reg.ownerNameAr ?? '';
          this.model.ownerMobile = reg.ownerMobile ?? '';
          this.model.clientId = reg.clientId ?? null;
          this.model.ownerId = reg.ownerId ?? null;
          this.model.doctorId = reg.doctorId ?? null;
          this.model.referenceNumber = reg.referenceNumber ?? '';
          this.model.clinicalNotes = reg.clinicalNotes ?? '';
          this.model.ageValue = reg.ageValue ?? null;
          this.ageValue = reg.ageValue ?? null;
          this.ageUnit = reg.ageUnit ?? AgeUnit.Years;
          this.discountType = reg.discountType ?? null;
          this.discountValue = reg.discountValue ?? null;

          // Stored amounts are preserved for edit; only newly added items
          // contribute fresh pricing on top.
          this.storedTotalAmount = reg.totalAmount ?? 0;
          this.storedPaidAmount = reg.paidAmount ?? 0;
          this.storedBalance = reg.balance ?? 0;

          // Load payment history
          this.existingPayments = (reg.payments ?? []).map((p: any) => ({
            id: p.id,
            method: p.method,
            amount: p.amount,
            paidAt: p.paidAt,
            reference: p.reference,
            currency: p.currency,
          }));
          this.paymentStatus = reg.paymentStatus ?? 'Unpaid';

          if (reg.animals?.length) {
            this.model.animals = reg.animals.map((a: any) => ({
              speciesId: a.speciesId,
              animalName: a.animalName,
              animalId: a.animalId,
            }));
          }
          if (reg.items?.length) {
            this.model.items = reg.items.map((i: any) => ({
              itemType: i.itemType,
              itemId: i.itemId,
              speciesId: i.speciesId,
              sampleTypeId: i.sampleTypeId,
              registrationItemId: i.id,
              isNew: false,
              price: i.price ?? 0,
              vatAmount: (i.vatAmount ?? 0) / ((i.quantity ?? 1) || 1),
            }));
          }

          this.rebuildCaches();
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Failed to load registration for editing.';
          this.cdr.detectChanges();
        });
      },
    });
  }

  // --- Data Loading ---

  private loadLookups() {
    this.http.get<LookupResponse>('/api/registrations/lookups').subscribe((res) => {
      this.owners = res.owners ?? [];
      this.animals = res.animals ?? [];
      this.doctors = res.doctors ?? [];
      this.clients = res.clients ?? [];
      this.species = res.species ?? [];
      this.sampleTypes = res.sampleTypes ?? [];
      this.orderables = res.orderables ?? [];
      this.rebuildCaches();
    });
  }

  // --- Owner Search (Server-side typeahead) ---

  private lastSelectedOwnerName = '';

  onOwnerSearchInput(term: string) {
    this.ownerSearch = term;

    if (this.ownerSearchTimer) {
      clearTimeout(this.ownerSearchTimer);
    }

    const trimmed = (term ?? '').trim();
    if (trimmed.length < 2) {
      this.ownerSearchResults = [];
      return;
    }

    this.ownerSearchLoading = true;
    this.ownerSearchTimer = setTimeout(() => {
      this.http
        .get<{ total: number; items: OwnerOption[] }>('/api/owners', {
          params: { search: trimmed, pageSize: '20' },
        })
        .subscribe({
          next: (res) => {
            this.ngZone.run(() => {
              this.ownerSearchResults = res.items ?? [];
              this.ownerSearchLoading = false;
              this.cdr.detectChanges();
            });
          },
          error: () => {
            this.ngZone.run(() => {
              this.ownerSearchResults = [];
              this.ownerSearchLoading = false;
              this.cdr.detectChanges();
            });
          },
        });
    }, 350);
  }

  selectOwnerFromSearch(owner: OwnerOption) {
    this.model.ownerId = owner.id;
    this.model.ownerName = owner.name;
    this.model.ownerNameAr = owner.nameAr ?? '';
    this.model.ownerMobile = owner.mobile ?? '';
    this.ownerSearch = owner.name;
    this.lastSelectedOwnerName = owner.name;
    this.ownerSearchResults = [];
  }

  // --- Client & Pricing ---

  onClientChange() {
    this.priceMap.clear();
    const clientId = this.model.clientId;
    if (!clientId) { this.recalculateTotals(); return; }
    this.http
      .get<{ itemType: number; itemId: number; rate: number; vatRate: number }[]>('/api/registrations/prices', {
        params: { clientId: String(clientId) },
      })
      .subscribe((rates) => {
        this.priceMap = new Map(
          rates.map((rate) => [`${rate.itemType}:${rate.itemId}`, { rate: rate.rate, vatRate: rate.vatRate }]),
        );
        this.recalculateTotals();
      });
  }

  // --- Animal Management ---

  addAnimalsBySpecies() {
    const speciesId = this.speciesDropdownId;
    const count = this.speciesCount;
    if (!speciesId || !count || count < 1) return;
    const sp = this.species.find((s) => s.id === speciesId);
    const speciesName = sp?.name ?? 'Animal';
    const existing = this.model.animals.filter((animal) => animal.speciesId === speciesId).length;
    for (let i = 1; i <= count; i++) {
      this.model.animals.push({ speciesId, animalName: `${speciesName} ${existing + i}`, animalId: null });
    }
    this.speciesDropdownId = null;
    this.speciesCount = 1;
    this.rebuildSpeciesCache();
    this.rebuildItemsBySpeciesCache();
    this.recalculateTotals();
  }

  addExistingAnimal() {
    const animal = this.animals.find((entry) => entry.id === this.quickAnimalId);
    if (!animal) return;
    const alreadyAdded = this.model.animals.some((a) => a.animalId === animal.id);
    if (alreadyAdded) {
      alert('This animal is already added to the registration.');
      return;
    }
    this.model.animals.push({ speciesId: animal.speciesId, animalName: animal.name, animalId: animal.id });
    this.quickAnimalId = null;
    this.rebuildSpeciesCache();
    this.rebuildItemsBySpeciesCache();
    this.recalculateTotals();
  }

  addAnimal() {
    this.model.animals.push({ speciesId: null, animalName: '', animalId: null });
    this.rebuildSpeciesCache();
    this.rebuildItemsBySpeciesCache();
    this.recalculateTotals();
  }

  removeAnimal(index: number) {
    this.model.animals.splice(index, 1);
    this.rebuildSpeciesCache();
    this.rebuildItemsBySpeciesCache();
    this.recalculateTotals();
  }

  animalCountOf(speciesId: number | null | undefined) {
    if (!speciesId) return this.cachedTotalAnimals;
    return this.cachedAnimalCountBySpecies.get(speciesId) ?? 0;
  }

  // --- Test Order Management ---

  private getPickerState(speciesId: number | null): { type: RegItemType; itemId: number | null } {
    let state = this.speciesPickerState.get(speciesId ?? null);
    if (!state) {
      state = { type: RegItemType.Test, itemId: null };
      this.speciesPickerState.set(speciesId ?? null, state);
    }
    return state;
  }

  getPickerType(speciesId: number | null): RegItemType {
    return this.getPickerState(speciesId).type;
  }

  getPickerItemId(speciesId: number | null): number | null {
    return this.getPickerState(speciesId).itemId;
  }

  setPickerTypeForSpecies(speciesId: number | null, type: RegItemType) {
    const state = this.getPickerState(speciesId);
    state.type = type;
    state.itemId = null;
  }

  setPickerItemIdForSpecies(speciesId: number | null, itemId: number | null) {
    this.getPickerState(speciesId).itemId = itemId;
  }

  typeLabel(itemType: RegItemType) {
    return this.itemTypeOptions.find((t) => t.value === itemType)?.label ?? 'item';
  }

  addOrder(speciesId: number | null) {
    const state = this.getPickerState(speciesId);
    if (!state.itemId) return;
    const exists = this.model.items.some(
      (i) => i.itemType === state.type && i.itemId === state.itemId && (i.speciesId ?? null) === (speciesId ?? null),
    );
    if (exists) {
      this.errorMessage = 'This item is already ordered for this species.';
      return;
    }
    this.errorMessage = '';
    const orderable = this.orderables.find(
      (o) => o.itemType === state.type && o.id === state.itemId,
    );
    const info = this.priceInfo(state.type, state.itemId);
    this.model.items.push({
      itemType: state.type,
      itemId: state.itemId,
      speciesId: speciesId,
      sampleTypeId: orderable?.sampleTypeId ?? null,
      registrationItemId: null,
      isNew: true,
      price: info.rate,
      vatAmount: info.rate > 0 ? Math.round(info.rate * info.vatRate) / 100 : 0,
    });
    state.itemId = null;
    this.rebuildItemsBySpeciesCache();
    this.recalculateTotals();
  }

  removeItemByRef(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    const idx = this.model.items.findIndex(
      (i) => i.itemType === item.itemType && i.itemId === item.itemId && i.speciesId === item.speciesId,
    );
    if (idx >= 0) {
      this.model.items.splice(idx, 1);
      this.rebuildItemsBySpeciesCache();
      this.recalculateTotals();
    }
  }

  itemsForSpecies(speciesId: number | null) {
    return this.cachedItemsBySpecies.get(speciesId ?? null) ?? [];
  }

  orderablesByType(itemType: RegItemType) {
    const q = (this.orderableSearch ?? '').trim().toLowerCase();
    return this.orderables.filter((option) => {
      if (option.itemType !== itemType) return false;
      if (!q) return true;
      return option.name.toLowerCase().includes(q) || (option.code ?? '').toLowerCase().includes(q);
    });
  }

  // --- Pricing Helpers (cheap lookups) ---

  priceInfo(itemType: RegItemType, itemId: number | null): PriceInfo {
    if (!itemId) return { rate: 0, vatRate: 0 };
    return this.priceMap.get(`${itemType}:${itemId}`) ?? { rate: 0, vatRate: 0 };
  }

  priceFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.priceInfo(item.itemType, item.itemId ?? null).rate;
  }

  vatRateFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.priceInfo(item.itemType, item.itemId ?? null).vatRate;
  }

  quantityFor(item: { speciesId?: number | null }) {
    return this.animalCountOf(item.speciesId);
  }

  lineVat(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    const info = this.priceInfo(item.itemType, item.itemId ?? null);
    if (info.rate <= 0) return 0;
    return Math.round(info.rate * info.vatRate) / 100 * this.quantityFor(item);
  }

  lineTotal(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    return this.priceFor(item) * this.quantityFor(item) + this.lineVat(item);
  }

  itemName(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.cachedOrderableMap.get(`${item.itemType}:${item.itemId}`) ?? '\u2014';
  }

  sampleTypeName(sampleTypeId: number | null | undefined): string {
    if (!sampleTypeId) return 'Any';
    return this.cachedSampleTypeMap.get(sampleTypeId) ?? 'Any';
  }

  // --- Discount Management ---

  toggleDiscountType(type: DiscountType) {
    this.discountType = this.discountType === type ? null : type;
    if (!this.discountType) this.discountValue = null;
    this.recalculateTotals();
  }

  clearDiscount() {
    this.discountType = null;
    this.discountValue = null;
    this.recalculateTotals();
  }

  // --- Payment ---

  onPaymentAmountChange(value: number | null) {
    this.model.initialPaymentAmount = typeof value === 'number' && !Number.isNaN(value) ? value : null;
    this.recalculateTotals();
  }

  selectPaymentMethod(method: PaymentMethodType) {
    this.model.initialPaymentMethod = this.model.initialPaymentMethod === method ? null : method;
  }

  payInFull() {
    if (this.cachedNetTotal > 0) {
      this.model.initialPaymentAmount = this.cachedNetTotal;
      this.recalculateTotals();
    }
  }

  // --- Form ---

  private extractError(err: any, fallback: string): string {
    if (typeof err?.error === 'string') return err.error;
    return err?.error?.title
      || (err?.error?.errors ? Object.values(err.error.errors).flat().join(' ') : null)
      || err?.error?.message
      || fallback;
  }

  private createEmptyModel(): CreateDto {
    return {
      ownerName: '',
      ownerNameAr: '',
      ownerMobile: '',
      clientId: null,
      ownerId: null,
      doctorId: null,
      referenceNumber: '',
      clinicalNotes: '',
      animals: [],
      items: [],
      initialPaymentAmount: null,
      initialPaymentMethod: null,
      initialPaymentRef: '',
      discountType: null,
      discountValue: null,
      ageValue: null,
      ageUnit: null,
    };
  }

  resetForm() {
    this.errorMessage = '';
    this.successMessage = '';
    this.speciesDropdownId = null;
    this.speciesCount = 1;
    this.quickAnimalId = null;
    this.ownerSearch = '';
    this.ownerSearchResults = [];
    this.speciesPickerState.clear();
    this.orderableSearch = '';
    this.discountType = null;
    this.discountValue = null;
    this.ageValue = null;
    this.ageUnit = AgeUnit.Years;
    this.priceMap.clear();
    this.model = this.createEmptyModel();
    this.rebuildCaches();
  }

  save() {
    this.errorMessage = '';
    this.successMessage = '';

    const errors: string[] = [];
    if (!this.model.ownerName.trim()) {
      errors.push('Owner name is required.');
    }
    if (!this.model.animals.length) {
      errors.push('Add at least one animal.');
    }
    for (const animal of this.model.animals) {
      if (!animal.speciesId) {
        errors.push('Every animal needs a species.');
        break;
      }
      if (!(animal.animalName?.trim() || animal.animalId)) {
        errors.push('Every animal needs a name.');
        break;
      }
    }
    if (!this.model.items.length) {
      errors.push('Add at least one test, profile, or package.');
    }
    for (const item of this.model.items) {
      if (!item.itemId) {
        errors.push('Every order needs a selected item.');
        break;
      }
    }
    const amount = this.model.initialPaymentAmount;
    if (amount && amount > 0 && !this.model.initialPaymentMethod) {
      errors.push('Select a payment mode for the initial payment.');
    }
    if (amount && amount > 0 && this.cachedNetTotal > 0 && amount > this.cachedNetTotal) {
      errors.push('Initial payment cannot exceed the net total.');
    }
    if (this.discountType && this.discountValue != null) {
      if (this.discountType === DiscountType.Percentage && (this.discountValue < 0 || this.discountValue > 100)) {
        errors.push('Discount percentage must be between 0 and 100.');
      }
      if (this.discountType === DiscountType.Amount && this.discountValue < 0) {
        errors.push('Discount amount cannot be negative.');
      }
    }
    if (this.ageValue !== null && this.ageValue !== undefined) {
      if (this.ageValue < 0) {
        errors.push('Age cannot be negative.');
      }
    }

    if (errors.length) {
      this.errorMessage = errors.join(' ');
      return;
    }

    this.model.discountType = this.discountType;
    this.model.discountValue = this.discountValue ?? 0;
    this.model.ageValue = this.ageValue;
    this.model.ageUnit = this.ageValue ? this.ageUnit : null;

    this.saving = true;
    const url = this.editMode ? `/api/registrations/${this.editId}` : '/api/registrations';
    const request = this.editMode
      ? this.http.put<{ barcode?: string }>(url, this.model)
      : this.http.post<{ barcode?: string }>(url, this.model);

    request.subscribe({
      next: (res) => {
        this.ngZone.run(() => {
          this.saving = false;
          const regId = (res as any)?.id ?? this.editId;
          this.editId = regId;
          this.successMessage = res?.barcode
            ? `Registration ${this.editMode ? 'updated' : 'created'}: ${res.barcode}`
            : `Registration ${this.editMode ? 'updated' : 'created'}.`;
          this.recalculateTotals();
          this.cdr.detectChanges();

          if (!this.editMode && regId) {
            this.loadBarcodesAndShowPopup(regId);
          }
        });
      },
      error: (err) => {
        this.ngZone.run(() => {
          this.saving = false;
          this.errorMessage = this.extractError(err, 'Failed to save registration.');
          this.cdr.detectChanges();
        });
      },
    });
  }

  // --- Barcode Popup ---

  loadBarcodesAndShowPopup(regId: number) {
    this.http.get<any>(`/api/registrations/${regId}/barcodes`).subscribe({
      next: (data) => {
        this.ngZone.run(() => {
          this.barcodePopupData = {
            registrationId: regId,
            registrationBarcode: data.registrationBarcode,
            ownerName: data.ownerName,
            ownerCode: data.ownerCode,
            animals: (data.animals ?? []).map((a: any) => ({ ...a, printCount: 2 })),
          };
          this.showBarcodePopup = true;
          this.cdr.detectChanges();
          this.renderBarcodes();
        });
      },
      error: () => {},
    });
  }

  closeBarcodePopup() {
    this.showBarcodePopup = false;
    this.barcodePopupData = null;
  }

  // --- Sample Collection Popup ---

  openSampleCollectionPopup() {
    if (!this.editId) return;
    this.http.get<any>(`/api/registrations/${this.editId}`).subscribe({
      next: (reg) => {
        this.ngZone.run(() => {
          const items = (reg.items ?? []).map((i: any) => ({
            id: i.id,
            itemBarcode: i.itemBarcode,
            sampleStatus: i.sampleStatus,
            speciesId: i.speciesId,
            speciesName: i.speciesId ? this.species.find(s => s.id === i.speciesId)?.name : undefined,
            collectedAt: i.sampleCollectedAt ?? undefined,
          }));
          this.sampleCollectionData = {
            registrationId: reg.id,
            registrationBarcode: reg.barcode,
            ownerName: reg.ownerName,
            ownerCode: reg.ownerCode,
            items,
            animals: (reg.animals ?? []).map((a: any) => {
              const animalSpeciesItems = items.filter((i: any) => i.speciesId === a.speciesId);
              const allCollected = animalSpeciesItems.length > 0 && animalSpeciesItems.every((i: any) => i.sampleStatus !== 'NotCollected');
              const lastCollected = animalSpeciesItems
                .filter((i: any) => i.collectedAt)
                .map((i: any) => i.collectedAt)
                .sort()
                .pop();
              return {
                id: a.id,
                animalName: a.animalName,
                speciesName: this.species.find(s => s.id === a.speciesId)?.name ?? 'Unknown',
                barcode: a.barcode ?? '',
                speciesId: a.speciesId,
                collectedAt: lastCollected,
                allCollected,
                printCount: 2,
                animalCode: a.animalCode,
              };
            }),
          };
          this.showSamplePopup = true;
          this.showBarcodePopup = false;
          this.cdr.detectChanges();
          this.renderBarcodes();
        });
      },
    });
  }

  closeSamplePopup() {
    this.showSamplePopup = false;
    this.sampleCollectionData = null;
  }

  collectSample(itemId: number) {
    this.http.post(`/api/registrations/items/${itemId}/collect`, { notes: null }).subscribe({
      next: () => {
        this.ngZone.run(() => {
          const regId = this.sampleCollectionData?.registrationId ?? this.editId;
          this.showSamplePopup = false;
          this.sampleCollectionData = null;
          this.cdr.detectChanges();
          if (regId) this.loadBarcodesAndShowPopup(regId);
        });
      },
      error: (err) => alert(this.extractError(err, 'Failed to collect sample.')),
    });
  }

  receiveSample(itemId: number) {
    this.http.post(`/api/registrations/items/${itemId}/receive`, { notes: null }).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.openSampleCollectionPopup();
        });
      },
      error: (err) => alert(this.extractError(err, 'Failed to receive sample.')),
    });
  }

  collectAllSamples() {
    if (!this.sampleCollectionData) return;
    const uncollected = this.sampleCollectionData.items.filter(i => i.sampleStatus === 'NotCollected');
    if (!uncollected.length) {
      alert('All samples are already collected.');
      return;
    }
    let completed = 0;
    for (const item of uncollected) {
      this.http.post(`/api/registrations/items/${item.id}/collect`, { notes: null }).subscribe({
        next: () => {
          completed++;
          if (completed === uncollected.length) {
            this.ngZone.run(() => {
              const regId = this.sampleCollectionData?.registrationId ?? this.editId;
              this.showSamplePopup = false;
              this.sampleCollectionData = null;
              this.cdr.detectChanges();
              if (regId) this.loadBarcodesAndShowPopup(regId);
            });
          }
        },
        error: () => {
          completed++;
          if (completed === uncollected.length) {
            this.ngZone.run(() => {
              const regId = this.sampleCollectionData?.registrationId ?? this.editId;
              this.showSamplePopup = false;
              this.sampleCollectionData = null;
              this.cdr.detectChanges();
              if (regId) this.loadBarcodesAndShowPopup(regId);
            });
          }
        },
      });
    }
  }

  // --- Barcode Printing ---

  printAllBarcodes(count: number = 2) {
    const regId = this.barcodePopupData?.registrationId ?? this.sampleCollectionData?.registrationId;
    if (!regId) return;
    this.http.get(`/api/registrations/${regId}/barcodes/pdf`, {
      params: { count },
      responseType: 'blob',
    }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => alert('Failed to generate barcode PDF.'),
    });
  }

  printSingleBarcode(animalId: number, registrationId: number, count: number = 2) {
    this.http.get(`/api/registrations/animals/${animalId}/barcode/pdf`, {
      params: { count },
      responseType: 'blob',
    }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => alert('Failed to generate barcode PDF.'),
    });
  }

  printAllBarcodesFromPopup() {
    this.printAllBarcodes(2);
  }

  // --- Helpers for popups ---

  sampleStatusColor(status: string): string {
    switch (status) {
      case 'NotCollected': return 'bg-secondary';
      case 'Collected': return 'bg-info text-dark';
      case 'Received': return 'bg-primary';
      case 'Processing': return 'bg-warning text-dark';
      case 'Processed': return 'bg-success';
      default: return 'bg-secondary';
    }
  }
}
