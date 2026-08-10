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
          <p class="text-muted mb-0">Owner, animal, doctor, clinical notes, and test ordering.</p>
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
            <h5>Owner & Visit</h5>
            <div class="mb-3">
              <label class="form-label">Owner</label>
              <select class="form-select" [(ngModel)]="model.ownerId" (ngModelChange)="onOwnerChange()" name="ownerId">
                <option [ngValue]="null">Select owner</option>
                <option *ngFor="let owner of owners" [ngValue]="owner.id">
                  {{ owner.name }}{{ owner.ownerCode ? ' • ' + owner.ownerCode : '' }}
                </option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label">Owner Name</label>
              <input class="form-control" [(ngModel)]="model.ownerName" name="ownerName" />
            </div>
            <div class="mb-3">
              <label class="form-label">Client</label>
              <select class="form-select" [(ngModel)]="model.clientId" name="clientId">
                <option [ngValue]="null">Walk-in / no client</option>
                <option *ngFor="let client of clients" [ngValue]="client.id">
                  {{ client.name }}{{ client.code ? ' • ' + client.code : '' }}
                </option>
              </select>
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
              <input class="form-control" [(ngModel)]="model.referenceNumber" name="referenceNumber" />
            </div>
            <div class="mb-0">
              <label class="form-label">Clinical Notes</label>
              <textarea class="form-control" rows="6" [(ngModel)]="model.clinicalNotes" name="clinicalNotes"></textarea>
            </div>
          </div>
        </div>

        <div class="col-lg-4">
          <div class="card p-3 mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <h5 class="mb-0">Animals</h5>
              <button class="btn btn-sm btn-outline-primary" type="button" (click)="addAnimal()">Add Animal</button>
            </div>

            <div class="border rounded p-2 mb-2" *ngFor="let animal of model.animals; let i = index">
              <div class="row g-2">
                <div class="col-md-6">
                  <label class="form-label">Existing Animal</label>
                  <select class="form-select form-select-sm" [(ngModel)]="animal.animalId" (ngModelChange)="onAnimalSelect(i)" [name]="'animalId' + i">
                    <option [ngValue]="null">Manual entry</option>
                    <option *ngFor="let option of filteredAnimals" [ngValue]="option.id">
                      {{ option.name }}{{ option.animalCode ? ' • ' + option.animalCode : '' }}
                    </option>
                  </select>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Species</label>
                  <select class="form-select form-select-sm" [(ngModel)]="animal.speciesId" [name]="'speciesId' + i">
                    <option [ngValue]="null">Select</option>
                    <option *ngFor="let species of species" [ngValue]="species.id">{{ species.name }}</option>
                  </select>
                </div>
                <div class="col-md-2 d-grid">
                  <label class="form-label d-block">&nbsp;</label>
                  <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeAnimal(i)">Remove</button>
                </div>
                <div class="col-12">
                  <label class="form-label">Animal Name</label>
                  <input class="form-control form-control-sm" [(ngModel)]="animal.animalName" [name]="'animalName' + i" />
                </div>
              </div>
            </div>
          </div>

          <div class="card p-3">
            <h5>Initial Payment</h5>
            <div class="row g-2">
              <div class="col-md-4">
                <label class="form-label">Amount</label>
                <input class="form-control" type="number" min="0" [(ngModel)]="model.initialPaymentAmount" name="initialPaymentAmount" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Method</label>
                <select class="form-select" [(ngModel)]="model.initialPaymentMethod" name="initialPaymentMethod">
                  <option [ngValue]="null">Select</option>
                  <option [ngValue]="paymentMethod.Cash">Cash</option>
                  <option [ngValue]="paymentMethod.Card">Card</option>
                  <option [ngValue]="paymentMethod.Cheque">Cheque</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Reference</label>
                <input class="form-control" [(ngModel)]="model.initialPaymentRef" name="initialPaymentRef" />
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-4">
          <div class="card p-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <h5 class="mb-0">Test Orders</h5>
              <button class="btn btn-sm btn-outline-primary" type="button" (click)="addItem()">Add Order</button>
            </div>

            <div class="border rounded p-2 mb-2" *ngFor="let item of model.items; let i = index">
              <div class="row g-2">
                <div class="col-md-4">
                  <label class="form-label">Type</label>
                  <select class="form-select form-select-sm" [(ngModel)]="item.itemType" (ngModelChange)="resetOrderable(i)" [name]="'itemType' + i">
                    <option [ngValue]="regItemType.Test">Test</option>
                    <option [ngValue]="regItemType.Profile">Profile</option>
                    <option [ngValue]="regItemType.Package">Package</option>
                  </select>
                </div>
                <div class="col-md-8">
                  <label class="form-label">Orderable</label>
                  <select class="form-select form-select-sm" [(ngModel)]="item.itemId" [name]="'itemId' + i">
                    <option [ngValue]="null">Select</option>
                    <option *ngFor="let option of orderablesByType(item.itemType)" [ngValue]="option.id">
                      {{ option.name }}{{ option.code ? ' • ' + option.code : '' }}
                    </option>
                  </select>
                </div>
                <div class="col-md-5">
                  <label class="form-label">Species</label>
                  <select class="form-select form-select-sm" [(ngModel)]="item.speciesId" [name]="'itemSpeciesId' + i">
                    <option [ngValue]="null">Any</option>
                    <option *ngFor="let option of species" [ngValue]="option.id">{{ option.name }}</option>
                  </select>
                </div>
                <div class="col-md-5">
                  <label class="form-label">Sample Type</label>
                  <select class="form-select form-select-sm" [(ngModel)]="item.sampleTypeId" [name]="'sampleTypeId' + i">
                    <option [ngValue]="null">Select</option>
                    <option *ngFor="let sampleType of sampleTypes" [ngValue]="sampleType.id">
                      {{ sampleType.name }}
                    </option>
                  </select>
                </div>
                <div class="col-md-2 d-grid">
                  <label class="form-label d-block">&nbsp;</label>
                  <button class="btn btn-sm btn-outline-danger" type="button" (click)="removeItem(i)">Remove</button>
                </div>
              </div>
            </div>

            <div class="small text-muted" *ngIf="!model.items.length">Add at least one test, profile, or package.</div>
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

  species: Species[] = [];
  clients: Client[] = [];
  owners: OwnerOption[] = [];
  animals: AnimalOption[] = [];
  doctors: DoctorOption[] = [];
  sampleTypes: SampleTypeOption[] = [];
  orderables: OrderableOption[] = [];

  errorMessage = '';
  successMessage = '';
  saving = false;

  model: CreateDto = this.createEmptyModel();

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
      clientId: null,
      ownerId: null,
      doctorId: null,
      referenceNumber: '',
      clinicalNotes: '',
      animals: [{ speciesId: null, animalName: '', animalId: null }],
      items: [{ itemType: RegItemType.Test, itemId: null, speciesId: null, sampleTypeId: null }],
      initialPaymentAmount: null,
      initialPaymentMethod: null,
      initialPaymentRef: '',
    };
  }

  resetForm() {
    this.errorMessage = '';
    this.successMessage = '';
    this.model = this.createEmptyModel();
  }

  onOwnerChange() {
    const owner = this.owners.find((entry) => entry.id === this.model.ownerId);
    if (owner) {
      this.model.ownerName = owner.name;
      this.model.animals = this.model.animals.map((animal) => ({ ...animal, animalId: null }));
    }
  }

  onAnimalSelect(index: number) {
    const selectedId = this.model.animals[index]?.animalId;
    const selected = this.filteredAnimals.find((animal) => animal.id === selectedId);
    if (!selected) {
      return;
    }

    this.model.animals[index] = {
      ...this.model.animals[index],
      animalId: selected.id,
      animalName: selected.name,
      speciesId: selected.speciesId,
    };
  }

  addAnimal() {
    this.model.animals.push({ speciesId: null, animalName: '', animalId: null });
  }

  removeAnimal(index: number) {
    this.model.animals.splice(index, 1);
    if (!this.model.animals.length) {
      this.addAnimal();
    }
  }

  addItem() {
    this.model.items.push({ itemType: RegItemType.Test, itemId: null, speciesId: null, sampleTypeId: null });
  }

  removeItem(index: number) {
    this.model.items.splice(index, 1);
    if (!this.model.items.length) {
      this.addItem();
    }
  }

  resetOrderable(index: number) {
    this.model.items[index].itemId = null;
  }

  orderablesByType(itemType: RegItemType) {
    return this.orderables.filter((option) => option.itemType === itemType);
  }

  save() {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.model.ownerName.trim()) {
      this.errorMessage = 'Owner name is required.';
      return;
    }

    if (!this.model.animals.every((animal) => animal.speciesId && (animal.animalName || animal.animalId))) {
      this.errorMessage = 'Each animal row needs species and animal name.';
      return;
    }

    if (!this.model.items.every((item) => item.itemId)) {
      this.errorMessage = 'Each order row needs a selected test, profile, or package.';
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
