import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

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

type Species = { id: number; name: string; code?: string };
type Client = { id: number; name: string; code?: string };
type OwnerOption = { id: number; ownerCode?: string; name: string; mobile?: string };
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
  }[];
  initialPaymentAmount?: number | null;
  initialPaymentMethod?: PaymentMethodType | null;
  initialPaymentRef?: string | null;
};

@Component({
  standalone: true,
  selector: 'app-registration-form',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-1">Laboratory Registration</h2>
          <p class="text-muted mb-0">Owner, animals, tests / profiles / packages, and payment.</p>
        </div>
        <div class="d-flex gap-2">
          <a class="btn btn-outline-secondary btn-sm" routerLink="/registrations">Back</a>
          <button class="btn btn-outline-secondary btn-sm" type="button" (click)="resetForm()">Clear</button>
          <button class="btn btn-primary btn-sm" type="button" (click)="save()" [disabled]="saving">
            {{ saving ? 'Saving...' : 'Save Registration' }}
          </button>
        </div>
      </div>

      <div class="alert alert-danger" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="alert alert-success" *ngIf="successMessage">{{ successMessage }}</div>

      <div class="row g-3">
        <div class="col-lg-4">
          <div class="card p-3 h-100">
            <h5 class="mb-1">Owner & Visit</h5>
            <p class="small text-muted mb-3">Who is this registration for?</p>

            <div class="mb-3">
              <label class="form-label">Owner</label>
              <input
                class="form-control mb-2"
                [(ngModel)]="ownerSearch"
                (ngModelChange)="model.ownerId = null"
                name="ownerSearch"
                placeholder="Search existing owner by name or mobile…"
              />
              <select class="form-select" [(ngModel)]="model.ownerId" (ngModelChange)="onOwnerChange()" name="ownerId">
                <option [ngValue]="null">{{ ownerSearch ? 'No match — create new owner' : 'Walk-in / new owner' }}</option>
                <option *ngFor="let owner of filteredOwners" [ngValue]="owner.id">
                  {{ owner.name }}{{ owner.mobile ? ' • ' + owner.mobile : '' }}{{ owner.ownerCode ? ' • ' + owner.ownerCode : '' }}
                </option>
              </select>
              <div class="form-text">Type to search owners by name or mobile number.</div>
            </div>
            <div class="mb-3">
              <label class="form-label">Owner Name</label>
              <input class="form-control" [(ngModel)]="model.ownerName" name="ownerName" placeholder="Owner / patient name" />
            </div>
            <div class="mb-3">
              <label class="form-label">Owner Mobile</label>
              <input
                class="form-control"
                [(ngModel)]="model.ownerMobile"
                name="ownerMobile"
                placeholder="Mobile number (auto-filled from owner)"
              />
              <div class="form-text">Auto-filled when an existing owner is selected; can be edited for walk-in registrations.</div>
            </div>
            <div class="mb-3">
              <label class="form-label">Client (price list)</label>
              <select class="form-select" [(ngModel)]="model.clientId" (ngModelChange)="onClientChange()" name="clientId">
                <option [ngValue]="null">Walk-in / no client</option>
                <option *ngFor="let client of clients" [ngValue]="client.id">
                  {{ client.name }}{{ client.code ? ' • ' + client.code : '' }}
                </option>
              </select>
              <div class="form-text" [class.text-warning]="model.clientId === null">
                {{ model.clientId === null ? 'No client price list — items will be billed at 0.' : 'Prices below come from the selected client price list.' }}
              </div>
            </div>
            <div class="mb-3">
              <label class="form-label">Doctor</label>
              <select class="form-select" [(ngModel)]="model.doctorId" name="doctorId">
                <option [ngValue]="null">Select doctor</option>
                <option *ngFor="let doctor of doctors" [ngValue]="doctor.id">
                  {{ doctor.name }}{{ doctor.code ? ' • ' + doctor.code : '' }}
                </option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label">Reference Number</label>
              <input class="form-control" [(ngModel)]="model.referenceNumber" name="referenceNumber" placeholder="Optional external reference" />
            </div>
            <div class="mb-0">
              <label class="form-label">Clinical Notes</label>
              <textarea class="form-control" rows="4" [(ngModel)]="model.clinicalNotes" name="clinicalNotes" placeholder="Optional clinical notes"></textarea>
            </div>
          </div>
        </div>

        <div class="col-lg-8">
          <div class="card p-3 h-100">
            <div class="d-flex justify-content-between align-items-center mb-1">
              <h5 class="mb-0">Animals</h5>
              <span class="badge bg-primary">{{ model.animals.length }} added</span>
            </div>
            <p class="small text-muted">
              Pick a species and count, then Add — animal names auto-fill as 1, 2, 3… and stay editable. You can also add an existing animal from the owner.
            </p>

            <div class="border rounded p-2 mb-2">
              <div class="row g-2 align-items-end">
                <div class="col-md-4">
                  <label class="small fw-semibold">Species</label>
                  <select class="form-select form-select-sm" [(ngModel)]="speciesDropdownId" name="speciesDropdownId">
                    <option [ngValue]="null">Select species</option>
                    <option *ngFor="let s of species" [ngValue]="s.id">{{ s.name }}</option>
                  </select>
                </div>
                <div class="col-md-3">
                  <label class="small fw-semibold">Count</label>
                  <input
                    class="form-control form-control-sm"
                    type="number"
                    min="1"
                    step="1"
                    [(ngModel)]="speciesCount"
                    name="speciesCount"
                  />
                </div>
                <div class="col-md-5 d-flex gap-2">
                  <button
                    class="btn btn-sm btn-outline-success"
                    [disabled]="speciesDropdownId == null || speciesCount < 1"
                    type="button"
                    (click)="addAnimalsBySpecies()"
                  >+ Add animals</button>
                  <button class="btn btn-sm btn-outline-secondary" type="button" (click)="addAnimal()">+ Add manual row</button>
                </div>
              </div>
            </div>

            <div class="border rounded p-2 mb-2">
              <div class="row g-2 align-items-end">
                <div class="col">
                  <label class="small fw-semibold">Add existing animal</label>
                  <select class="form-select form-select-sm" [(ngModel)]="quickAnimalId" name="quickAnimalId">
                    <option [ngValue]="null">Select existing animal…</option>
                    <option *ngFor="let option of filteredAnimals" [ngValue]="option.id">
                      {{ option.name }}{{ option.animalCode ? ' • ' + option.animalCode : '' }}
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
                    <th style="width: 90px" class="text-end"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let animal of model.animals; let i = index">
                    <td>{{ i + 1 }}</td>
                    <td>
                      <select class="form-select form-select-sm" [(ngModel)]="animal.speciesId" [attr.name]="'speciesId' + i">
                        <option [ngValue]="null">Select</option>
                        <option *ngFor="let s of species" [ngValue]="s.id">{{ s.name }}</option>
                      </select>
                    </td>
                    <td>
                      <input class="form-control form-control-sm" placeholder="Animal name" [(ngModel)]="animal.animalName" [attr.name]="'animalName' + i" />
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
        </div>
      </div>

      <div class="card p-3 mt-3">
        <div class="d-flex justify-content-between align-items-center mb-1">
          <h5 class="mb-0">Tests, Profiles & Packages</h5>
          <span class="badge bg-primary">{{ selectedCount }} selected</span>
        </div>
        <p class="small text-muted">Pick a category, choose an item, then click Add. Quantity = number of animals the order applies to (e.g. 2 cats + 3 dogs = 5 × price for "Any (all animals)").</p>

        <div class="border rounded p-2 mb-2">
          <div class="row g-2 align-items-end">
            <div class="col-auto">
              <label class="small fw-semibold d-block">Category</label>
              <div class="btn-group btn-group-sm" role="group">
                <button
                  *ngFor="let t of itemTypeOptions"
                  class="btn btn-sm"
                  [class.btn-primary]="pickerType === t.value"
                  [class.btn-outline-primary]="pickerType !== t.value"
                  type="button"
                  (click)="setPickerType(t.value)"
                >{{ t.label }}</button>
              </div>
            </div>
            <div class="col">
              <label class="small fw-semibold d-block">Item</label>
              <select class="form-select form-select-sm" [(ngModel)]="pickerItemId" name="pickerItemId">
                <option [ngValue]="null">Select {{ typeLabel(pickerType) }}…</option>
                <option *ngFor="let o of orderablesByType(pickerType)" [ngValue]="o.id">
                  {{ o.name }}{{ o.code ? ' • ' + o.code : '' }} — {{ priceForItem(o.itemType, o.id) | currency:'AED' }} (incl VAT {{ inclForItem(o.itemType, o.id) | currency:'AED' }})
                </option>
              </select>
            </div>
            <div class="col-auto">
              <button class="btn btn-sm btn-outline-primary" [disabled]="!pickerItemId" type="button" (click)="addOrder()">+ Add</button>
            </div>
          </div>
          <input
            class="form-control form-control-sm mt-2"
            placeholder="Search tests, profiles, packages…"
            [(ngModel)]="orderableSearch"
            name="orderableSearch"
          />
        </div>

        <div class="table-responsive" *ngIf="model.items.length">
          <table class="table table-sm align-middle mb-0">
            <thead>
              <tr>
                <th style="width: 40px">#</th>
                <th>Type</th>
                <th>Item</th>
                <th>Species</th>
                <th class="text-end">Qty</th>
                <th>Sample Type</th>
                <th class="text-end">Net (each)</th>
                <th class="text-end">VAT</th>
                <th class="text-end">Total</th>
                <th style="width: 90px" class="text-end"></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of model.items; let i = index" [class.table-warning]="!item.itemId">
                <td>{{ i + 1 }}</td>
                <td>{{ typeLabel(item.itemType) }}</td>
                <td>
                  <span *ngIf="item.itemId">{{ itemName(item) }}</span>
                  <span *ngIf="!item.itemId" class="text-danger small">Select an item to add</span>
                </td>
                <td>
                  <select class="form-select form-select-sm" [(ngModel)]="item.speciesId" [attr.name]="'itemSpeciesId' + i">
                    <option [ngValue]="null">Any (all animals)</option>
                    <option *ngFor="let s of species" [ngValue]="s.id">{{ s.name }}</option>
                  </select>
                </td>
                <td class="text-end">
                  <span class="fw-semibold">{{ quantityFor(item) }}</span>
                  <span class="text-muted small">/ {{ animalCount }}</span>
                </td>
                <td>
                  <select class="form-select form-select-sm" [(ngModel)]="item.sampleTypeId" [attr.name]="'itemSampleTypeId' + i">
                    <option [ngValue]="null">Any</option>
                    <option *ngFor="let st of sampleTypes" [ngValue]="st.id">{{ st.name }}</option>
                  </select>
                </td>
                <td class="text-end">{{ priceFor(item) | currency:'AED' }}</td>
                <td class="text-end">{{ lineVat(item) | currency:'AED' }} <span class="text-muted small">({{ vatRateFor(item) }}%)</span></td>
                <td class="text-end fw-semibold">{{ lineTotal(item) | currency:'AED' }}</td>
                <td class="text-end">
                  <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeItem(i)">Remove</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="small text-muted mb-0" *ngIf="!model.items.length">No orders yet — pick a test, profile, or package above.</p>
      </div>

      <div class="row g-3 mt-0">
        <div class="col-lg-5">
          <div class="card p-3 h-100">
            <h5 class="mb-1">Initial Payment</h5>
            <p class="small text-muted mb-3">Payment mode — leave blank to pay later.</p>

            <label class="form-label">Payment Mode</label>
            <div class="btn-group w-100 mb-3" role="group">
              <button
                *ngFor="let m of paymentMethods"
                class="btn btn-sm"
                [class.btn-primary]="model.initialPaymentMethod === m.value"
                [class.btn-outline-primary]="model.initialPaymentMethod !== m.value"
                type="button"
                (click)="selectPaymentMethod(m.value)"
              >{{ m.label }}</button>
            </div>

            <div class="row g-2">
              <div class="col-md-6">
                <label class="form-label">Amount (AED)</label>
                <input
                  class="form-control"
                  type="number"
                  min="0"
                  step="0.01"
                  [ngModel]="model.initialPaymentAmount"
                  (ngModelChange)="onPaymentAmountChange($event)"
                  name="initialPaymentAmount"
                  placeholder="0.00"
                />
              </div>
              <div class="col-md-6">
                <label class="form-label">Reference</label>
                <input class="form-control" [(ngModel)]="model.initialPaymentRef" name="initialPaymentRef" placeholder="Cheque no / txn id" />
              </div>
            </div>

            <button class="btn btn-sm btn-outline-success mt-3" type="button" (click)="payInFull()" [disabled]="grandTotal <= 0">
              Pay in full ({{ grandTotal | currency:'AED' }})
            </button>
            <div class="small text-muted mt-2 mb-0">
              Balance due: <span class="fw-bold" [class.text-danger]="balanceDue > 0">{{ balanceDue | currency:'AED' }}</span>
            </div>
          </div>
        </div>

        <div class="col-lg-7">
          <div class="card p-3 h-100">
            <h5 class="mb-1">Order Summary</h5>
            <p class="small text-muted mb-2">Prices exclude VAT; totals below include VAT.</p>

            <p class="small text-muted" *ngIf="!selectedItems.length">No items selected yet.</p>
            <ul class="list-group list-group-flush mb-2" *ngIf="selectedItems.length">
              <li class="list-group-item d-flex justify-content-between px-0 py-1" *ngFor="let item of selectedItems">
                <span>{{ itemName(item) }} <span class="text-muted small">× {{ quantityFor(item) }}</span></span>
                <span>{{ lineTotal(item) | currency:'AED' }}</span>
              </li>
            </ul>

            <div class="border-top pt-2">
              <div class="d-flex justify-content-between">
                <span>Subtotal (excl. VAT)</span>
                <span>{{ subtotal | currency:'AED' }}</span>
              </div>
              <div class="d-flex justify-content-between">
                <span>VAT</span>
                <span>{{ vatTotal | currency:'AED' }}</span>
              </div>
              <div class="d-flex justify-content-between fw-bold">
                <span>Grand Total</span>
                <span>{{ grandTotal | currency:'AED' }}</span>
              </div>
              <div class="d-flex justify-content-between">
                <span>Paid</span>
                <span>{{ paidAmount | currency:'AED' }}</span>
              </div>
              <div class="d-flex justify-content-between fw-bold" [class.text-danger]="balanceDue > 0">
                <span>Balance Due</span>
                <span>{{ balanceDue | currency:'AED' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class RegistrationFormComponent {
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly regItemType = RegItemType;
  readonly paymentMethod = PaymentMethodType;

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
  ownerSearch = '';
  pickerType: RegItemType = RegItemType.Test;
  pickerItemId: number | null = null;
  orderableSearch = '';

  errorMessage = '';
  successMessage = '';
  saving = false;

  model: CreateDto = this.createEmptyModel();

  get selectedItems() {
    return this.model.items.filter((item) => item.itemId);
  }

  get selectedCount() {
    return this.selectedItems.length;
  }

  get animalCount() {
    return this.model.animals.length;
  }

  get filteredOwners(): OwnerOption[] {
    const q = (this.ownerSearch ?? '').trim().toLowerCase();
    if (!q) {
      return this.owners;
    }
    return this.owners.filter(
      (owner) =>
        owner.name.toLowerCase().includes(q) ||
        (owner.mobile ?? '').toLowerCase().includes(q) ||
        (owner.ownerCode ?? '').toLowerCase().includes(q),
    );
  }

  animalCountOf(speciesId: number | null | undefined) {
    if (!speciesId) {
      return this.model.animals.length;
    }
    return this.model.animals.filter((animal) => animal.speciesId === speciesId).length;
  }

  quantityFor(item: { speciesId?: number | null }) {
    return this.animalCountOf(item.speciesId);
  }

  get subtotal() {
    return this.model.items.reduce((sum, item) => sum + this.lineNet(item), 0);
  }

  get vatTotal() {
    return this.model.items.reduce((sum, item) => sum + this.lineVat(item), 0);
  }

  get grandTotal() {
    return this.subtotal + this.vatTotal;
  }

  get paidAmount() {
    return this.model.initialPaymentAmount ?? 0;
  }

  get balanceDue() {
    return Math.max(this.grandTotal - this.paidAmount, 0);
  }

  get filteredAnimals(): AnimalOption[] {
    if (!this.model.ownerId) {
      return [];
    }
    return this.animals.filter((animal) => animal.ownerId === this.model.ownerId);
  }

  ngOnInit() {
    this.loadLookups();
  }

  private loadLookups() {
    this.http.get<LookupResponse>('/api/registrations/lookups').subscribe((res) => {
      this.owners = res.owners ?? [];
      this.animals = res.animals ?? [];
      this.doctors = res.doctors ?? [];
      this.clients = res.clients ?? [];
      this.species = res.species ?? [];
      this.sampleTypes = res.sampleTypes ?? [];
      this.orderables = res.orderables ?? [];
    });
  }

  private createEmptyModel(): CreateDto {
    return {
      ownerName: '',
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
    };
  }

  resetForm() {
    this.errorMessage = '';
    this.successMessage = '';
    this.speciesDropdownId = null;
    this.speciesCount = 1;
    this.quickAnimalId = null;
    this.ownerSearch = '';
    this.pickerItemId = null;
    this.orderableSearch = '';
    this.priceMap.clear();
    this.model = this.createEmptyModel();
  }

  onOwnerChange() {
    const owner = this.owners.find((entry) => entry.id === this.model.ownerId);
    if (owner) {
      this.model.ownerName = owner.name;
      this.model.ownerMobile = owner.mobile ?? '';
      this.ownerSearch = '';
    }
  }

  onClientChange() {
    this.priceMap.clear();
    const clientId = this.model.clientId;
    if (!clientId) {
      return;
    }
    this.http
      .get<{ itemType: number; itemId: number; rate: number; vatRate: number }[]>('/api/registrations/prices', {
        params: { clientId: String(clientId) },
      })
      .subscribe((rates) => {
        this.priceMap = new Map(
          rates.map((rate) => [`${rate.itemType}:${rate.itemId}`, { rate: rate.rate, vatRate: rate.vatRate }]),
        );
      });
  }

  addAnimalsBySpecies() {
    const speciesId = this.speciesDropdownId;
    const count = this.speciesCount;
    if (!speciesId || !count || count < 1) {
      return;
    }
    const existing = this.model.animals.filter((animal) => animal.speciesId === speciesId).length;
    for (let i = 1; i <= count; i++) {
      this.model.animals.push({ speciesId, animalName: String(existing + i), animalId: null });
    }
    this.speciesDropdownId = null;
    this.speciesCount = 1;
  }

  addExistingAnimal() {
    const animal = this.animals.find((entry) => entry.id === this.quickAnimalId);
    if (!animal) {
      return;
    }
    this.model.animals.push({ speciesId: animal.speciesId, animalName: animal.name, animalId: animal.id });
    this.quickAnimalId = null;
  }

  addAnimal() {
    this.model.animals.push({ speciesId: null, animalName: '', animalId: null });
  }

  removeAnimal(index: number) {
    this.model.animals.splice(index, 1);
  }

  setPickerType(itemType: RegItemType) {
    this.pickerType = itemType;
    this.pickerItemId = null;
  }

  typeLabel(itemType: RegItemType) {
    return this.itemTypeOptions.find((t) => t.value === itemType)?.label ?? 'item';
  }

  addOrder() {
    if (!this.pickerItemId) {
      return;
    }
    this.model.items.push({
      itemType: this.pickerType,
      itemId: this.pickerItemId,
      speciesId: null,
      sampleTypeId: null,
    });
    this.pickerItemId = null;
  }

  removeItem(index: number) {
    this.model.items.splice(index, 1);
  }

  orderablesByType(itemType: RegItemType) {
    const q = (this.orderableSearch ?? '').trim().toLowerCase();
    return this.orderables.filter((option) => {
      if (option.itemType !== itemType) {
        return false;
      }
      if (!q) {
        return true;
      }
      return option.name.toLowerCase().includes(q) || (option.code ?? '').toLowerCase().includes(q);
    });
  }

  priceInfo(itemType: RegItemType, itemId: number | null): PriceInfo {
    if (!itemId) {
      return { rate: 0, vatRate: 0 };
    }
    return this.priceMap.get(`${itemType}:${itemId}`) ?? { rate: 0, vatRate: 0 };
  }

  priceForItem(itemType: RegItemType, itemId: number | null) {
    return this.priceInfo(itemType, itemId).rate;
  }

  vatRateForItem(itemType: RegItemType, itemId: number | null) {
    return this.priceInfo(itemType, itemId).vatRate;
  }

  vatAmountForItem(itemType: RegItemType, itemId: number | null) {
    const info = this.priceInfo(itemType, itemId);
    if (info.rate <= 0) {
      return 0;
    }
    return Math.round(info.rate * info.vatRate) / 100;
  }

  inclForItem(itemType: RegItemType, itemId: number | null) {
    return this.priceForItem(itemType, itemId) + this.vatAmountForItem(itemType, itemId);
  }

  priceFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.priceForItem(item.itemType, item.itemId ?? null);
  }

  vatRateFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.vatRateForItem(item.itemType, item.itemId ?? null);
  }

  vatAmountFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.vatAmountForItem(item.itemType, item.itemId ?? null);
  }

  inclFor(item: { itemType: RegItemType; itemId?: number | null }) {
    return this.inclForItem(item.itemType, item.itemId ?? null);
  }

  lineNet(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    return this.priceFor(item) * this.quantityFor(item);
  }

  lineVat(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    return this.vatAmountFor(item) * this.quantityFor(item);
  }

  lineTotal(item: { itemType: RegItemType; itemId?: number | null; speciesId?: number | null }) {
    return this.lineNet(item) + this.lineVat(item);
  }

  itemName(item: { itemType: RegItemType; itemId?: number | null }) {
    const found = this.orderables.find((option) => option.itemType === item.itemType && option.id === item.itemId);
    return found ? found.name : '—';
  }

  onPaymentAmountChange(value: number | null) {
    this.model.initialPaymentAmount = typeof value === 'number' && !Number.isNaN(value) ? value : null;
  }

  selectPaymentMethod(method: PaymentMethodType) {
    this.model.initialPaymentMethod = this.model.initialPaymentMethod === method ? null : method;
  }

  payInFull() {
    if (this.grandTotal > 0) {
      this.model.initialPaymentAmount = this.grandTotal;
    }
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
    if (amount && amount > 0 && this.grandTotal > 0 && amount > this.grandTotal) {
      errors.push('Initial payment cannot exceed the grand total.');
    }

    if (errors.length) {
      this.errorMessage = errors.join(' ');
      return;
    }

    this.saving = true;
    this.http.post<{ barcode?: string }>('/api/registrations', this.model).subscribe({
      next: (res) => {
        this.saving = false;
        this.successMessage = res?.barcode ? `Registration created: ${res.barcode}` : 'Registration created.';
        void this.router.navigate(['/registrations']);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err?.error ?? 'Failed to save registration.';
      },
    });
  }
}
