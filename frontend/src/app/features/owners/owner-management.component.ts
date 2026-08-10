import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

interface OwnerListItem {
  id: number;
  code: string;
  name: string;
  mobile?: string | null;
  whatsApp?: string | null;
  email?: string | null;
  address?: string | null;
  city?: string | null;
  isActive: boolean;
  animalCount: number;
}

interface AnimalSummary {
  id: number;
  code: string;
  name: string;
  speciesId: number;
  speciesName: string;
  dateOfBirth?: string | null;
  ageInDays?: number | null;
  weightKg?: number | null;
  vaccinationStatus?: string | null;
  isActive: boolean;
}

interface OwnerProfile extends Omit<OwnerListItem, 'animalCount'> {
  animals: AnimalSummary[];
}

interface MedicalHistoryItem {
  id: number;
  occurredAt: string;
  entryType: string;
  diagnosis?: string | null;
  treatment?: string | null;
  medication?: string | null;
  notes?: string | null;
}

interface AnimalProfile {
  id: number;
  ownerId: number;
  ownerName: string;
  code: string;
  name: string;
  speciesId: number;
  speciesName: string;
  breedId?: number | null;
  breedName?: string | null;
  genderId?: number | null;
  genderName?: string | null;
  colorId?: number | null;
  colorName?: string | null;
  dateOfBirth?: string | null;
  ageInDays?: number | null;
  weightKg?: number | null;
  microchipNumber?: string | null;
  photoUrl?: string | null;
  vaccinationStatus?: string | null;
  isActive: boolean;
  medicalHistory: MedicalHistoryItem[];
}

interface LookupItem {
  id: number;
  code: string;
  name: string;
}

interface LookupsResponse {
  species: LookupItem[];
  breeds: LookupItem[];
  genders: LookupItem[];
  colors: LookupItem[];
}

interface OwnerForm {
  code: string;
  name: string;
  mobile: string;
  whatsApp: string;
  email: string;
  address: string;
  city: string;
  isActive: boolean;
}

interface AnimalForm {
  code: string;
  name: string;
  speciesId: number | null;
  breedId: number | null;
  genderId: number | null;
  colorId: number | null;
  dateOfBirth: string | null;
  weightKg: number | null;
  microchipNumber: string;
  photoUrl: string;
  vaccinationStatus: string;
  isActive: boolean;
}

interface HistoryForm {
  occurredAt: string;
  entryType: string;
  diagnosis: string;
  treatment: string;
  medication: string;
  notes: string;
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3" *ngIf="!ownerId; else profileView">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <div>
          <h2 class="mb-0">Owner Master</h2>
          <small class="text-muted">One owner can manage multiple animal profiles.</small>
        </div>
        <button *ngIf="canManage()" class="btn btn-primary" (click)="openCreateOwner()">+ Owner</button>
      </div>

      <div class="input-group mb-3">
        <input
          class="form-control"
          [(ngModel)]="search"
          placeholder="Search by owner code, name, or mobile"
          (keyup.enter)="loadOwners()"
        />
        <button class="btn btn-outline-secondary" (click)="loadOwners()">Search</button>
      </div>

      <div *ngIf="message()" class="alert alert-success">{{ message() }}</div>
      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

      <div class="table-responsive" *ngIf="owners().length; else emptyOwners">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>Owner Code</th>
              <th>Name</th>
              <th>Mobile</th>
              <th>City</th>
              <th>Animals</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of owners()">
              <td>{{ item.code }}</td>
              <td>{{ item.name }}</td>
              <td>{{ item.mobile || '-' }}</td>
              <td>{{ item.city || '-' }}</td>
              <td>{{ item.animalCount }}</td>
              <td>
                <span class="badge" [class.text-bg-success]="item.isActive" [class.text-bg-secondary]="!item.isActive">
                  {{ item.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="text-end">
                <a class="btn btn-sm btn-outline-primary me-1" [routerLink]="['/owners', item.id]">Profile</a>
                <button *ngIf="canManage()" class="btn btn-sm btn-outline-secondary" (click)="openEditOwner(item)">Edit</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <ng-template #emptyOwners>
        <p class="text-muted">No owners found.</p>
      </ng-template>

      <div class="d-flex justify-content-between align-items-center" *ngIf="total()">
        <small class="text-muted">{{ total() }} record(s) · page {{ page() }}</small>
        <div class="btn-group">
          <button class="btn btn-outline-secondary" [disabled]="page() === 1" (click)="changePage(-1)">Previous</button>
          <button class="btn btn-outline-secondary" [disabled]="page() * pageSize >= total()" (click)="changePage(1)">Next</button>
        </div>
      </div>

      <section class="card mt-4" *ngIf="showOwnerForm()">
        <div class="card-header">{{ editingOwnerId() ? 'Edit owner' : 'New owner' }}</div>
        <div class="card-body">
          <form (ngSubmit)="saveOwner()" novalidate>
            <div class="row g-3">
              <div class="col-md-4">
                <label class="form-label">Owner code *</label>
                <input class="form-control" maxlength="50" [(ngModel)]="ownerForm.code" name="ownerCode" />
              </div>
              <div class="col-md-8">
                <label class="form-label">Name *</label>
                <input class="form-control" maxlength="200" [(ngModel)]="ownerForm.name" name="ownerName" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Mobile</label>
                <input class="form-control" maxlength="30" [(ngModel)]="ownerForm.mobile" name="mobile" />
              </div>
              <div class="col-md-4">
                <label class="form-label">WhatsApp</label>
                <input class="form-control" maxlength="30" [(ngModel)]="ownerForm.whatsApp" name="whatsApp" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Email</label>
                <input type="email" class="form-control" maxlength="200" [(ngModel)]="ownerForm.email" name="email" />
              </div>
              <div class="col-md-8">
                <label class="form-label">Address</label>
                <input class="form-control" [(ngModel)]="ownerForm.address" name="address" />
              </div>
              <div class="col-md-4">
                <label class="form-label">City</label>
                <input class="form-control" maxlength="100" [(ngModel)]="ownerForm.city" name="city" />
              </div>
              <div class="col-12">
                <div class="form-check">
                  <input id="ownerActive" class="form-check-input" type="checkbox" [(ngModel)]="ownerForm.isActive" name="ownerIsActive" />
                  <label class="form-check-label" for="ownerActive">Active</label>
                </div>
              </div>
            </div>
            <div class="text-end mt-3">
              <button type="button" class="btn btn-secondary me-2" (click)="closeOwnerForm()">Cancel</button>
              <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save Owner' }}</button>
            </div>
          </form>
        </div>
      </section>
    </div>

    <ng-template #profileView>
      <div class="container py-3">
        <a routerLink="/owners" class="btn btn-link px-0">← Owners</a>
        <div *ngIf="message()" class="alert alert-success">{{ message() }}</div>
        <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

        <ng-container *ngIf="profileData() as owner">
          <div class="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-3">
            <div>
              <h2 class="mb-1">{{ owner.name }}</h2>
              <div class="text-muted">{{ owner.code }} · {{ owner.mobile || 'No mobile' }} · {{ owner.city || 'No city' }}</div>
            </div>
            <div class="d-flex gap-2" *ngIf="canManage()">
              <button class="btn btn-outline-secondary" (click)="openEditOwner(owner)">Edit Owner</button>
              <button class="btn btn-primary" (click)="openCreateAnimal()">+ Animal</button>
            </div>
          </div>

          <div class="row g-3">
            <div class="col-lg-4">
              <div class="card h-100">
                <div class="card-header">Owner Profile</div>
                <div class="card-body">
                  <dl class="row mb-0">
                    <dt class="col-sm-4">Mobile</dt>
                    <dd class="col-sm-8">{{ owner.mobile || '-' }}</dd>
                    <dt class="col-sm-4">WhatsApp</dt>
                    <dd class="col-sm-8">{{ owner.whatsApp || '-' }}</dd>
                    <dt class="col-sm-4">Email</dt>
                    <dd class="col-sm-8">{{ owner.email || '-' }}</dd>
                    <dt class="col-sm-4">Address</dt>
                    <dd class="col-sm-8">{{ owner.address || '-' }}</dd>
                    <dt class="col-sm-4">City</dt>
                    <dd class="col-sm-8">{{ owner.city || '-' }}</dd>
                    <dt class="col-sm-4">Status</dt>
                    <dd class="col-sm-8">{{ owner.isActive ? 'Active' : 'Inactive' }}</dd>
                  </dl>
                </div>
              </div>
            </div>

            <div class="col-lg-8">
              <div class="card h-100">
                <div class="card-header d-flex justify-content-between align-items-center">
                  <span>Animal Profiles</span>
                  <small class="text-muted">{{ owner.animals.length }} animal(s)</small>
                </div>
                <div class="list-group list-group-flush" *ngIf="owner.animals.length; else noAnimals">
                  <a
                    *ngFor="let animal of owner.animals"
                    class="list-group-item list-group-item-action"
                    [class.active]="animal.id === animalId"
                    [routerLink]="['/owners', owner.id, 'animals', animal.id]"
                  >
                    <div class="d-flex justify-content-between align-items-center">
                      <div>
                        <div class="fw-semibold">{{ animal.name }}</div>
                        <small>{{ animal.code }} · {{ animal.speciesName }} · {{ ageLabel(animal.ageInDays, animal.dateOfBirth) }}</small>
                      </div>
                      <span class="badge" [class.text-bg-success]="animal.isActive" [class.text-bg-secondary]="!animal.isActive">
                        {{ animal.isActive ? 'Active' : 'Inactive' }}
                      </span>
                    </div>
                  </a>
                </div>
                <ng-template #noAnimals>
                  <div class="card-body text-muted">No animal profiles found for this owner.</div>
                </ng-template>
              </div>
            </div>
          </div>

          <section class="mt-4" *ngIf="animalId && animalData() as animal; else selectAnimal">
            <div class="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
              <div>
                <h4 class="mb-1">Animal Profile — {{ animal.name }}</h4>
                <small class="text-muted">{{ animal.code }} · {{ animal.speciesName }} · {{ ageLabel(animal.ageInDays, animal.dateOfBirth) }}</small>
              </div>
              <div class="d-flex gap-2" *ngIf="canManage()">
                <button class="btn btn-outline-secondary btn-sm" (click)="openEditAnimal(animal)">Edit Animal</button>
                <button class="btn btn-outline-danger btn-sm" (click)="deleteAnimal(animal)">Delete Animal</button>
              </div>
            </div>

            <div class="row g-3">
              <div class="col-lg-8">
                <div class="card">
                  <div class="card-header">Animal Details</div>
                  <div class="card-body">
                    <dl class="row mb-0">
                      <dt class="col-sm-4">Species</dt>
                      <dd class="col-sm-8">{{ animal.speciesName }}</dd>
                      <dt class="col-sm-4">Breed</dt>
                      <dd class="col-sm-8">{{ animal.breedName || '-' }}</dd>
                      <dt class="col-sm-4">Gender</dt>
                      <dd class="col-sm-8">{{ animal.genderName || '-' }}</dd>
                      <dt class="col-sm-4">Color</dt>
                      <dd class="col-sm-8">{{ animal.colorName || '-' }}</dd>
                      <dt class="col-sm-4">DOB</dt>
                      <dd class="col-sm-8">{{ animal.dateOfBirth || '-' }}</dd>
                      <dt class="col-sm-4">Age</dt>
                      <dd class="col-sm-8">{{ ageLabel(animal.ageInDays, animal.dateOfBirth) }}</dd>
                      <dt class="col-sm-4">Weight</dt>
                      <dd class="col-sm-8">{{ animal.weightKg ? (animal.weightKg + ' kg') : '-' }}</dd>
                      <dt class="col-sm-4">Microchip</dt>
                      <dd class="col-sm-8">{{ animal.microchipNumber || '-' }}</dd>
                      <dt class="col-sm-4">Vaccination</dt>
                      <dd class="col-sm-8">{{ animal.vaccinationStatus || '-' }}</dd>
                      <dt class="col-sm-4">Status</dt>
                      <dd class="col-sm-8">{{ animal.isActive ? 'Active' : 'Inactive' }}</dd>
                    </dl>
                  </div>
                </div>
              </div>
              <div class="col-lg-4">
                <div class="card h-100">
                  <div class="card-header">Photo</div>
                  <div class="card-body text-center">
                    <img *ngIf="animal.photoUrl; else noPhoto" [src]="animal.photoUrl" [alt]="animal.name" class="img-fluid rounded border" />
                    <ng-template #noPhoto>
                      <div class="text-muted py-5">No photo available.</div>
                    </ng-template>
                  </div>
                </div>
              </div>
            </div>

            <div class="d-flex justify-content-between align-items-center mt-4 mb-2">
              <h5 class="mb-0">Complete Medical History</h5>
              <button *ngIf="canManage()" class="btn btn-primary btn-sm" (click)="openCreateHistory()">+ Entry</button>
            </div>

            <div class="card mb-2" *ngFor="let entry of animal.medicalHistory">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-start gap-2">
                  <div>
                    <div class="fw-semibold">{{ entry.entryType }}</div>
                    <small class="text-muted">{{ entry.occurredAt | date:'medium' }}</small>
                  </div>
                  <div class="d-flex gap-2" *ngIf="canManage()">
                    <button class="btn btn-outline-secondary btn-sm" (click)="openEditHistory(entry)">Edit</button>
                    <button class="btn btn-outline-danger btn-sm" (click)="deleteHistory(entry)">Delete</button>
                  </div>
                </div>
                <div class="mt-2" *ngIf="entry.diagnosis"><strong>Diagnosis:</strong> {{ entry.diagnosis }}</div>
                <div *ngIf="entry.treatment"><strong>Treatment:</strong> {{ entry.treatment }}</div>
                <div *ngIf="entry.medication"><strong>Medication:</strong> {{ entry.medication }}</div>
                <div *ngIf="entry.notes"><strong>Notes:</strong> {{ entry.notes }}</div>
              </div>
            </div>
            <p class="text-muted" *ngIf="!animal.medicalHistory.length">No medical history entries recorded yet.</p>
          </section>

          <ng-template #selectAnimal>
            <div class="alert alert-light border mt-4">Select an animal profile to review its full details and medical history.</div>
          </ng-template>

          <section class="card mt-4" *ngIf="showOwnerForm()">
            <div class="card-header">{{ editingOwnerId() ? 'Edit owner' : 'New owner' }}</div>
            <div class="card-body">
              <form (ngSubmit)="saveOwner()" novalidate>
                <div class="row g-3">
                  <div class="col-md-4"><label class="form-label">Owner code *</label><input class="form-control" maxlength="50" [(ngModel)]="ownerForm.code" name="profileOwnerCode" /></div>
                  <div class="col-md-8"><label class="form-label">Name *</label><input class="form-control" maxlength="200" [(ngModel)]="ownerForm.name" name="profileOwnerName" /></div>
                  <div class="col-md-4"><label class="form-label">Mobile</label><input class="form-control" maxlength="30" [(ngModel)]="ownerForm.mobile" name="profileMobile" /></div>
                  <div class="col-md-4"><label class="form-label">WhatsApp</label><input class="form-control" maxlength="30" [(ngModel)]="ownerForm.whatsApp" name="profileWhatsApp" /></div>
                  <div class="col-md-4"><label class="form-label">Email</label><input type="email" class="form-control" maxlength="200" [(ngModel)]="ownerForm.email" name="profileEmail" /></div>
                  <div class="col-md-8"><label class="form-label">Address</label><input class="form-control" [(ngModel)]="ownerForm.address" name="profileAddress" /></div>
                  <div class="col-md-4"><label class="form-label">City</label><input class="form-control" maxlength="100" [(ngModel)]="ownerForm.city" name="profileCity" /></div>
                  <div class="col-12"><div class="form-check"><input id="profileOwnerActive" class="form-check-input" type="checkbox" [(ngModel)]="ownerForm.isActive" name="profileOwnerIsActive" /><label class="form-check-label" for="profileOwnerActive">Active</label></div></div>
                </div>
                <div class="text-end mt-3">
                  <button type="button" class="btn btn-secondary me-2" (click)="closeOwnerForm()">Cancel</button>
                  <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save Owner' }}</button>
                </div>
              </form>
            </div>
          </section>

          <section class="card mt-4" *ngIf="showAnimalForm()">
            <div class="card-header">{{ editingAnimalId() ? 'Edit animal' : 'New animal' }}</div>
            <div class="card-body">
              <form (ngSubmit)="saveAnimal()" novalidate>
                <div class="row g-3">
                  <div class="col-md-4"><label class="form-label">Animal code *</label><input class="form-control" maxlength="50" [(ngModel)]="animalForm.code" name="animalCode" /></div>
                  <div class="col-md-8"><label class="form-label">Name *</label><input class="form-control" maxlength="200" [(ngModel)]="animalForm.name" name="animalName" /></div>
                  <div class="col-md-3"><label class="form-label">Species *</label><select class="form-select" [(ngModel)]="animalForm.speciesId" name="speciesId"><option [ngValue]="null">Select species</option><option *ngFor="let item of lookups().species" [ngValue]="item.id">{{ item.name }}</option></select></div>
                  <div class="col-md-3"><label class="form-label">Breed</label><select class="form-select" [(ngModel)]="animalForm.breedId" name="breedId"><option [ngValue]="null">Select breed</option><option *ngFor="let item of lookups().breeds" [ngValue]="item.id">{{ item.name }}</option></select></div>
                  <div class="col-md-3"><label class="form-label">Gender</label><select class="form-select" [(ngModel)]="animalForm.genderId" name="genderId"><option [ngValue]="null">Select gender</option><option *ngFor="let item of lookups().genders" [ngValue]="item.id">{{ item.name }}</option></select></div>
                  <div class="col-md-3"><label class="form-label">Color</label><select class="form-select" [(ngModel)]="animalForm.colorId" name="colorId"><option [ngValue]="null">Select color</option><option *ngFor="let item of lookups().colors" [ngValue]="item.id">{{ item.name }}</option></select></div>
                  <div class="col-md-3"><label class="form-label">DOB</label><input type="date" class="form-control" [(ngModel)]="animalForm.dateOfBirth" name="dateOfBirth" /></div>
                  <div class="col-md-3"><label class="form-label">Age</label><input class="form-control" [value]="ageLabel(null, animalForm.dateOfBirth)" readonly /></div>
                  <div class="col-md-3"><label class="form-label">Weight (kg)</label><input type="number" step="0.001" min="0" class="form-control" [(ngModel)]="animalForm.weightKg" name="weightKg" /></div>
                  <div class="col-md-3"><label class="form-label">Microchip</label><input class="form-control" maxlength="100" [(ngModel)]="animalForm.microchipNumber" name="microchipNumber" /></div>
                  <div class="col-md-6"><label class="form-label">Photo URL</label><input class="form-control" [(ngModel)]="animalForm.photoUrl" name="photoUrl" /></div>
                  <div class="col-md-6"><label class="form-label">Vaccination status</label><input class="form-control" maxlength="100" [(ngModel)]="animalForm.vaccinationStatus" name="vaccinationStatus" /></div>
                  <div class="col-12"><div class="form-check"><input id="animalActive" class="form-check-input" type="checkbox" [(ngModel)]="animalForm.isActive" name="animalIsActive" /><label class="form-check-label" for="animalActive">Active</label></div></div>
                </div>
                <div class="text-end mt-3">
                  <button type="button" class="btn btn-secondary me-2" (click)="closeAnimalForm()">Cancel</button>
                  <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save Animal' }}</button>
                </div>
              </form>
            </div>
          </section>

          <section class="card mt-4" *ngIf="showHistoryForm()">
            <div class="card-header">{{ editingHistoryId() ? 'Edit history entry' : 'Medical history entry' }}</div>
            <div class="card-body">
              <form (ngSubmit)="saveHistory()" novalidate>
                <div class="row g-3">
                  <div class="col-md-4"><label class="form-label">Date *</label><input type="datetime-local" class="form-control" [(ngModel)]="historyForm.occurredAt" name="occurredAt" /></div>
                  <div class="col-md-8"><label class="form-label">Entry type *</label><input class="form-control" maxlength="100" [(ngModel)]="historyForm.entryType" name="entryType" placeholder="Consultation, diagnosis, vaccination..." /></div>
                  <div class="col-md-6"><label class="form-label">Diagnosis</label><input class="form-control" [(ngModel)]="historyForm.diagnosis" name="diagnosis" /></div>
                  <div class="col-md-6"><label class="form-label">Medication</label><input class="form-control" [(ngModel)]="historyForm.medication" name="medication" /></div>
                  <div class="col-12"><label class="form-label">Treatment</label><textarea class="form-control" rows="2" [(ngModel)]="historyForm.treatment" name="treatment"></textarea></div>
                  <div class="col-12"><label class="form-label">Notes</label><textarea class="form-control" rows="3" [(ngModel)]="historyForm.notes" name="notes"></textarea></div>
                </div>
                <div class="text-end mt-3">
                  <button type="button" class="btn btn-secondary me-2" (click)="closeHistoryForm()">Cancel</button>
                  <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save Entry' }}</button>
                </div>
              </form>
            </div>
          </section>
        </ng-container>
      </div>
    </ng-template>
  `,
})
export class OwnerManagementComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly owners = signal<OwnerListItem[]>([]);
  readonly total = signal(0);
  readonly error = signal('');
  readonly message = signal('');
  readonly saving = signal(false);
  readonly profileData = signal<OwnerProfile | null>(null);
  readonly animalData = signal<AnimalProfile | null>(null);
  readonly showOwnerForm = signal(false);
  readonly showAnimalForm = signal(false);
  readonly showHistoryForm = signal(false);
  readonly editingOwnerId = signal<number | null>(null);
  readonly editingAnimalId = signal<number | null>(null);
  readonly editingHistoryId = signal<number | null>(null);
  readonly lookups = signal<LookupsResponse>({ species: [], breeds: [], genders: [], colors: [] });

  ownerId: number | null = null;
  animalId: number | null = null;
  search = '';

  ownerForm: OwnerForm = this.emptyOwner();
  animalForm: AnimalForm = this.emptyAnimal();
  historyForm: HistoryForm = this.emptyHistory();

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.ownerId = this.parseId(params.get('ownerId'));
      this.animalId = this.parseId(params.get('animalId'));
      this.error.set('');
      this.message.set('');

      if (this.ownerId) {
        this.loadLookups();
        this.loadProfile();
        return;
      }

      this.profileData.set(null);
      this.animalData.set(null);
      this.showAnimalForm.set(false);
      this.showHistoryForm.set(false);
      this.loadOwners();
    });
  }

  canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  loadOwners(): void {
    this.http.get<any>('/api/owners', { params: { search: this.search, page: this.page(), pageSize: this.pageSize } }).subscribe({
      next: (response) => {
        this.owners.set(response.items ?? []);
        this.total.set(response.total ?? 0);
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  loadProfile(): void {
    if (!this.ownerId) return;

    this.http.get<OwnerProfile>(`/api/owners/${this.ownerId}`).subscribe({
      next: (owner) => {
        this.profileData.set(owner);
        if (this.animalId) {
          this.loadAnimalProfile();
        } else {
          this.animalData.set(null);
        }
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  loadAnimalProfile(): void {
    if (!this.ownerId || !this.animalId) return;

    this.http.get<AnimalProfile>(`/api/owners/${this.ownerId}/animals/${this.animalId}`).subscribe({
      next: (animal) => this.animalData.set(animal),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  loadLookups(): void {
    this.http.get<LookupsResponse>('/api/owners/lookups').subscribe({
      next: (response) => this.lookups.set(response),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  changePage(offset: number): void {
    this.page.update((value) => Math.max(1, value + offset));
    this.loadOwners();
  }

  openCreateOwner(): void {
    this.editingOwnerId.set(null);
    this.ownerForm = this.emptyOwner();
    this.showOwnerForm.set(true);
    this.error.set('');
  }

  openEditOwner(owner: OwnerProfile | OwnerListItem): void {
    this.editingOwnerId.set(owner.id);
    this.ownerForm = {
      code: owner.code,
      name: owner.name,
      mobile: owner.mobile ?? '',
      whatsApp: owner.whatsApp ?? '',
      email: owner.email ?? '',
      address: owner.address ?? '',
      city: owner.city ?? '',
      isActive: owner.isActive
    };
    this.showOwnerForm.set(true);
    this.error.set('');
  }

  closeOwnerForm(): void {
    this.showOwnerForm.set(false);
    this.ownerForm = this.emptyOwner();
  }

  saveOwner(): void {
    const validationMessage = this.validateOwnerForm();
    if (validationMessage) {
      this.error.set(validationMessage);
      return;
    }

    const payload = {
      code: this.ownerForm.code.trim(),
      name: this.ownerForm.name.trim(),
      mobile: this.cleanString(this.ownerForm.mobile),
      whatsApp: this.cleanString(this.ownerForm.whatsApp),
      email: this.cleanString(this.ownerForm.email),
      address: this.cleanString(this.ownerForm.address),
      city: this.cleanString(this.ownerForm.city),
      isActive: this.ownerForm.isActive
    };

    this.saving.set(true);
    this.error.set('');
    const request = this.editingOwnerId()
      ? this.http.put<OwnerProfile>(`/api/owners/${this.editingOwnerId()}`, payload)
      : this.http.post<OwnerProfile>('/api/owners', payload);

    request.subscribe({
      next: (owner) => {
        this.saving.set(false);
        this.showOwnerForm.set(false);
        this.message.set(`Owner ${this.editingOwnerId() ? 'updated' : 'created'} successfully.`);
        if (this.ownerId) {
          this.profileData.set(owner);
        } else if (!this.editingOwnerId()) {
          this.router.navigate(['/owners', owner.id]);
          return;
        } else {
          this.loadOwners();
        }
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      }
    });
  }

  openCreateAnimal(): void {
    this.editingAnimalId.set(null);
    this.animalForm = this.emptyAnimal();
    this.showAnimalForm.set(true);
    this.error.set('');
  }

  openEditAnimal(animal: AnimalProfile): void {
    this.editingAnimalId.set(animal.id);
    this.animalForm = {
      code: animal.code,
      name: animal.name,
      speciesId: animal.speciesId,
      breedId: animal.breedId ?? null,
      genderId: animal.genderId ?? null,
      colorId: animal.colorId ?? null,
      dateOfBirth: animal.dateOfBirth ?? null,
      weightKg: animal.weightKg ?? null,
      microchipNumber: animal.microchipNumber ?? '',
      photoUrl: animal.photoUrl ?? '',
      vaccinationStatus: animal.vaccinationStatus ?? '',
      isActive: animal.isActive
    };
    this.showAnimalForm.set(true);
    this.error.set('');
  }

  closeAnimalForm(): void {
    this.showAnimalForm.set(false);
    this.animalForm = this.emptyAnimal();
  }

  saveAnimal(): void {
    if (!this.ownerId) return;

    const validationMessage = this.validateAnimalForm();
    if (validationMessage) {
      this.error.set(validationMessage);
      return;
    }

    const payload = {
      code: this.animalForm.code.trim(),
      name: this.animalForm.name.trim(),
      speciesId: this.animalForm.speciesId,
      breedId: this.animalForm.breedId,
      genderId: this.animalForm.genderId,
      colorId: this.animalForm.colorId,
      dateOfBirth: this.animalForm.dateOfBirth,
      weightKg: this.animalForm.weightKg,
      microchipNumber: this.cleanString(this.animalForm.microchipNumber),
      photoUrl: this.cleanString(this.animalForm.photoUrl),
      vaccinationStatus: this.cleanString(this.animalForm.vaccinationStatus),
      isActive: this.animalForm.isActive
    };

    this.saving.set(true);
    this.error.set('');

    const request = this.editingAnimalId()
      ? this.http.put<AnimalProfile>(`/api/owners/${this.ownerId}/animals/${this.editingAnimalId()}`, payload)
      : this.http.post<AnimalProfile>(`/api/owners/${this.ownerId}/animals`, payload);

    request.subscribe({
      next: (animal) => {
        this.saving.set(false);
        this.showAnimalForm.set(false);
        this.message.set(`Animal ${this.editingAnimalId() ? 'updated' : 'created'} successfully.`);
        this.router.navigate(['/owners', this.ownerId, 'animals', animal.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      }
    });
  }

  deleteAnimal(animal: AnimalProfile): void {
    if (!this.ownerId) return;
    if (!confirm(`Delete animal "${animal.name}"?`)) return;

    this.http.delete(`/api/owners/${this.ownerId}/animals/${animal.id}`).subscribe({
      next: () => {
        this.message.set('Animal deleted successfully.');
        this.router.navigate(['/owners', this.ownerId]);
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  openCreateHistory(): void {
    this.editingHistoryId.set(null);
    this.historyForm = this.emptyHistory();
    this.showHistoryForm.set(true);
    this.error.set('');
  }

  openEditHistory(entry: MedicalHistoryItem): void {
    this.editingHistoryId.set(entry.id);
    this.historyForm = {
      occurredAt: this.toDateTimeLocal(entry.occurredAt),
      entryType: entry.entryType,
      diagnosis: entry.diagnosis ?? '',
      treatment: entry.treatment ?? '',
      medication: entry.medication ?? '',
      notes: entry.notes ?? ''
    };
    this.showHistoryForm.set(true);
    this.error.set('');
  }

  closeHistoryForm(): void {
    this.showHistoryForm.set(false);
    this.historyForm = this.emptyHistory();
  }

  saveHistory(): void {
    if (!this.ownerId || !this.animalId) return;

    const validationMessage = this.validateHistoryForm();
    if (validationMessage) {
      this.error.set(validationMessage);
      return;
    }

    const payload = {
      occurredAt: new Date(this.historyForm.occurredAt).toISOString(),
      entryType: this.historyForm.entryType.trim(),
      diagnosis: this.cleanString(this.historyForm.diagnosis),
      treatment: this.cleanString(this.historyForm.treatment),
      medication: this.cleanString(this.historyForm.medication),
      notes: this.cleanString(this.historyForm.notes)
    };

    this.saving.set(true);
    this.error.set('');

    const request = this.editingHistoryId()
      ? this.http.put<MedicalHistoryItem>(`/api/owners/${this.ownerId}/animals/${this.animalId}/medical-history/${this.editingHistoryId()}`, payload)
      : this.http.post<MedicalHistoryItem>(`/api/owners/${this.ownerId}/animals/${this.animalId}/medical-history`, payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.showHistoryForm.set(false);
        this.message.set(`Medical history ${this.editingHistoryId() ? 'updated' : 'added'} successfully.`);
        this.loadProfile();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      }
    });
  }

  deleteHistory(entry: MedicalHistoryItem): void {
    if (!this.ownerId || !this.animalId) return;
    if (!confirm(`Delete medical history entry "${entry.entryType}"?`)) return;

    this.http.delete(`/api/owners/${this.ownerId}/animals/${this.animalId}/medical-history/${entry.id}`).subscribe({
      next: () => {
        this.message.set('Medical history entry deleted successfully.');
        this.loadProfile();
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  ageLabel(ageInDays?: number | null, dob?: string | null): string {
    const days = ageInDays ?? this.calculateAgeInDays(dob);
    if (days == null) return '-';
    if (days < 31) return `${days} day(s)`;
    if (days < 365) return `${Math.floor(days / 30.44)} month(s)`;
    return `${Math.floor(days / 365.25)} year(s)`;
  }

  private calculateAgeInDays(dob?: string | null): number | null {
    if (!dob) return null;
    const birth = new Date(dob);
    const diff = Math.floor((Date.now() - birth.getTime()) / 86400000);
    return diff < 0 ? 0 : diff;
  }

  private validateOwnerForm(): string | null {
    if (!this.ownerForm.code.trim()) return 'Owner code is required.';
    if (!this.ownerForm.name.trim()) return 'Owner name is required.';
    if (this.ownerForm.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.ownerForm.email.trim())) return 'Enter a valid owner email address.';
    return null;
  }

  private validateAnimalForm(): string | null {
    if (!this.animalForm.code.trim()) return 'Animal code is required.';
    if (!this.animalForm.name.trim()) return 'Animal name is required.';
    if (!this.animalForm.speciesId) return 'Species is required.';
    if (this.animalForm.weightKg != null && this.animalForm.weightKg <= 0) return 'Weight must be greater than zero.';
    return null;
  }

  private validateHistoryForm(): string | null {
    if (!this.historyForm.occurredAt) return 'History date is required.';
    if (!this.historyForm.entryType.trim()) return 'Entry type is required.';
    return null;
  }

  private parseId(value: string | null): number | null {
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  private toDateTimeLocal(value: string): string {
    return new Date(value).toISOString().slice(0, 16);
  }

  private emptyOwner(): OwnerForm {
    return { code: '', name: '', mobile: '', whatsApp: '', email: '', address: '', city: '', isActive: true };
  }

  private emptyAnimal(): AnimalForm {
    return {
      code: '',
      name: '',
      speciesId: null,
      breedId: null,
      genderId: null,
      colorId: null,
      dateOfBirth: null,
      weightKg: null,
      microchipNumber: '',
      photoUrl: '',
      vaccinationStatus: '',
      isActive: true
    };
  }

  private emptyHistory(): HistoryForm {
    return {
      occurredAt: new Date().toISOString().slice(0, 16),
      entryType: '',
      diagnosis: '',
      treatment: '',
      medication: '',
      notes: ''
    };
  }

  private cleanString(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) return firstMessage;
    }

    return error?.error?.message ?? error?.error?.title ?? error?.error ?? 'Request failed.';
  }
}
