import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';

interface MasterDefinition {
  type: string;
  title: string;
  codeLabel: string;
  codeRequired?: boolean;
  fields: Array<keyof MasterForm>;
}

interface MasterForm {
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  departmentId: number | null;
  licenseNumber: string;
  phone: string;
  email: string;
  manufacturer: string;
  model: string;
  contactPerson: string;
  address: string;
  genericName: string;
  category: string;
  unit: string;
}

interface DepartmentOption {
  id: number;
  name: string;
}

const definitions: MasterDefinition[] = [
  { type: 'species', title: 'Species Master', codeLabel: 'Code', fields: [] },
  { type: 'breeds', title: 'Breed Master', codeLabel: 'Code', fields: ['description'] },
  {
    type: 'animalcolors',
    title: 'Animal Color Master',
    codeLabel: 'Code',
    fields: ['description'],
  },
  { type: 'genders', title: 'Gender Master', codeLabel: 'Code', fields: ['description'] },
  {
    type: 'doctors',
    title: 'Doctor Master',
    codeLabel: 'Code',
    fields: ['departmentId', 'licenseNumber', 'phone', 'email'],
  },
  {
    type: 'departments',
    title: 'Department Master',
    codeLabel: 'Code',
    codeRequired: false,
    fields: [],
  },
  { type: 'sampletypes', title: 'Sample Type Master', codeLabel: 'Code', fields: ['description'] },
  { type: 'containers', title: 'Container Master', codeLabel: 'Code', fields: ['description'] },
  { type: 'units', title: 'Unit Master', codeLabel: 'Symbol', fields: [] },
  { type: 'methods', title: 'Method Master', codeLabel: 'Code', fields: ['description'] },
  {
    type: 'instruments',
    title: 'Instrument Master',
    codeLabel: 'Code',
    fields: ['manufacturer', 'model'],
  },
  {
    type: 'suppliers',
    title: 'Supplier Master',
    codeLabel: 'Code',
    fields: ['contactPerson', 'phone', 'email', 'address'],
  },
  {
    type: 'medicines',
    title: 'Medicine Master',
    codeLabel: 'Code',
    fields: ['genericName', 'category', 'unit'],
  },
  {
    type: 'inventorycategories',
    title: 'Inventory Category Master',
    codeLabel: 'Code',
    fields: ['description'],
  },
];

/** Step 5: reusable CRUD screen for the existing non-clinical master catalog tables. */
@Component({
  selector: 'app-master-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <div>
          <h2 class="mb-0">{{ definition().title }}</h2>
          <small class="text-muted">Master management</small>
        </div>
        <div class="d-flex gap-2">
          <select
            class="form-select"
            [ngModel]="selectedType()"
            (ngModelChange)="changeType($event)"
          >
            <option *ngFor="let item of masterDefinitions" [value]="item.type">
              {{ item.title }}
            </option>
          </select>
          <button class="btn btn-primary text-nowrap" (click)="openCreate()">+ Add</button>
        </div>
      </div>

      <div class="input-group mb-3">
        <input
          class="form-control"
          [(ngModel)]="search"
          placeholder="Search by code or name"
          (keyup.enter)="searchRecords()"
          aria-label="Search master records"
        />
        <button class="btn btn-outline-secondary" (click)="searchRecords()">Search</button>
      </div>

      <div *ngIf="error()" class="alert alert-danger" role="alert">{{ error() }}</div>
      <div *ngIf="loading()" class="text-muted py-3">Loading…</div>

      <div class="table-responsive" *ngIf="!loading()">
        <table
          class="table table-striped table-hover align-middle"
          *ngIf="rows().length; else emptyState"
        >
          <thead>
            <tr>
              <th>{{ codeLabel() }}</th>
              <th>Name</th>
              <th>Details</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of rows()">
              <td>{{ displayCode(row) }}</td>
              <td>{{ row.name }}</td>
              <td>{{ details(row) }}</td>
              <td>
                <span
                  class="badge"
                  [class.text-bg-success]="row.isActive"
                  [class.text-bg-secondary]="!row.isActive"
                  >{{ row.isActive ? 'Active' : 'Inactive' }}</span
                >
              </td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-primary me-1" (click)="openEdit(row)">
                  Edit
                </button>
                <button class="btn btn-sm btn-outline-danger" (click)="remove(row)">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
        <ng-template #emptyState
          ><p class="text-muted py-3">
            No {{ definition().title.toLowerCase() }} records found.
          </p></ng-template
        >
      </div>

      <div class="d-flex justify-content-between align-items-center" *ngIf="total()">
        <small class="text-muted">{{ total() }} record(s) · page {{ page() }}</small>
        <div class="btn-group">
          <button
            class="btn btn-outline-secondary"
            [disabled]="page() === 1"
            (click)="goToPage(page() - 1)"
          >
            Previous
          </button>
          <button
            class="btn btn-outline-secondary"
            [disabled]="page() * pageSize >= total()"
            (click)="goToPage(page() + 1)"
          >
            Next
          </button>
        </div>
      </div>

      <section class="card mt-4" *ngIf="showForm()">
        <div class="card-header">
          {{ editingId() ? 'Edit' : 'Create' }} {{ definition().title }}
        </div>
        <div class="card-body">
          <form #masterForm="ngForm" (ngSubmit)="save(masterForm.valid ?? false)" novalidate>
            <div class="row g-3">
              <div class="col-md-4">
                <label class="form-label" for="code"
                  >{{ codeLabel() }}{{ isCodeRequired() ? ' *' : '' }}</label
                ><input
                  id="code"
                  class="form-control"
                  maxlength="50"
                  [required]="isCodeRequired()"
                  [(ngModel)]="form.code"
                  name="code"
                  #code="ngModel"
                />
                <div class="invalid-feedback d-block" *ngIf="code.invalid && code.touched">
                  {{ codeLabel() }} is required.
                </div>
              </div>
              <div class="col-md-8">
                <label class="form-label" for="name">Name *</label
                ><input
                  id="name"
                  class="form-control"
                  maxlength="200"
                  required
                  [(ngModel)]="form.name"
                  name="name"
                  #name="ngModel"
                />
                <div class="invalid-feedback d-block" *ngIf="name.invalid && name.touched">
                  Name is required.
                </div>
              </div>

              <div class="col-12" *ngIf="hasField('description')">
                <label class="form-label">Description</label
                ><textarea
                  class="form-control"
                  rows="2"
                  [(ngModel)]="form.description"
                  name="description"
                ></textarea>
              </div>
              <div class="col-md-6" *ngIf="hasField('departmentId')">
                <label class="form-label">Department</label
                ><select class="form-select" [(ngModel)]="form.departmentId" name="departmentId">
                  <option [ngValue]="null">Not assigned</option>
                  <option *ngFor="let department of departments()" [ngValue]="department.id">
                    {{ department.name }}
                  </option>
                </select>
              </div>
              <div class="col-md-6" *ngIf="hasField('licenseNumber')">
                <label class="form-label">License number</label
                ><input
                  class="form-control"
                  [(ngModel)]="form.licenseNumber"
                  name="licenseNumber"
                />
              </div>
              <div class="col-md-6" *ngIf="hasField('manufacturer')">
                <label class="form-label">Manufacturer</label
                ><input class="form-control" [(ngModel)]="form.manufacturer" name="manufacturer" />
              </div>
              <div class="col-md-6" *ngIf="hasField('model')">
                <label class="form-label">Model</label
                ><input class="form-control" [(ngModel)]="form.model" name="model" />
              </div>
              <div class="col-md-6" *ngIf="hasField('contactPerson')">
                <label class="form-label">Contact person</label
                ><input
                  class="form-control"
                  [(ngModel)]="form.contactPerson"
                  name="contactPerson"
                />
              </div>
              <div class="col-md-6" *ngIf="hasField('phone')">
                <label class="form-label">Phone</label
                ><input class="form-control" [(ngModel)]="form.phone" name="phone" />
              </div>
              <div class="col-md-6" *ngIf="hasField('email')">
                <label class="form-label">Email</label
                ><input
                  type="email"
                  class="form-control"
                  [(ngModel)]="form.email"
                  name="email"
                  #email="ngModel"
                  email
                />
                <div class="invalid-feedback d-block" *ngIf="email.invalid && email.touched">
                  Enter a valid email address.
                </div>
              </div>
              <div class="col-12" *ngIf="hasField('address')">
                <label class="form-label">Address</label
                ><textarea
                  class="form-control"
                  rows="2"
                  [(ngModel)]="form.address"
                  name="address"
                ></textarea>
              </div>
              <div class="col-md-4" *ngIf="hasField('genericName')">
                <label class="form-label">Generic name</label
                ><input class="form-control" [(ngModel)]="form.genericName" name="genericName" />
              </div>
              <div class="col-md-4" *ngIf="hasField('category')">
                <label class="form-label">Category</label
                ><input class="form-control" [(ngModel)]="form.category" name="category" />
              </div>
              <div class="col-md-4" *ngIf="hasField('unit')">
                <label class="form-label">Unit</label
                ><input class="form-control" [(ngModel)]="form.unit" name="unit" />
              </div>
              <div class="col-12">
                <div class="form-check">
                  <input
                    id="isActive"
                    class="form-check-input"
                    type="checkbox"
                    [(ngModel)]="form.isActive"
                    name="isActive"
                  /><label class="form-check-label" for="isActive">Active</label>
                </div>
              </div>
            </div>
            <div class="text-end mt-4">
              <button type="button" class="btn btn-secondary me-2" (click)="showForm.set(false)">
                Cancel</button
              ><button class="btn btn-primary" [disabled]="saving()">
                {{ saving() ? 'Saving…' : 'Save' }}
              </button>
            </div>
          </form>
        </div>
      </section>
    </div>
  `,
})
export class MasterListComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly masterDefinitions = definitions;
  readonly selectedType = signal('species');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly rows = signal<any[]>([]);
  readonly departments = signal<DepartmentOption[]>([]);
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  search = '';
  form: MasterForm = this.emptyForm();

  readonly definition = () =>
    definitions.find((x) => x.type === this.selectedType()) ?? definitions[0];

  ngOnInit(): void {
    const routeType = this.route.snapshot.paramMap.get('type') ?? this.route.snapshot.data['type'];
    if (routeType && definitions.some((x) => x.type === routeType)) {
      this.selectedType.set(routeType);
    }
    this.load();
  }

  changeType(type: string): void {
    this.selectedType.set(type);
    this.search = '';
    this.page.set(1);
    this.showForm.set(false);
    this.router.navigate(['/masters', type]);
  }
  searchRecords(): void {
    this.page.set(1);
    this.load();
  }
  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }
  hasField(field: keyof MasterForm): boolean {
    return this.definition().fields.includes(field);
  }
  codeLabel(): string {
    return this.definition().codeLabel;
  }
  isCodeRequired(): boolean {
    return this.definition().codeRequired !== false;
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.error.set('');
    this.showForm.set(true);
    if (this.hasField('departmentId')) this.loadDepartments();
  }
  openEdit(row: any): void {
    this.editingId.set(row.id);
    this.form = { ...this.emptyForm(), ...row, departmentId: row.departmentId ?? null };
    this.error.set('');
    this.showForm.set(true);
    if (this.hasField('departmentId')) this.loadDepartments();
  }

  save(isValid: boolean): void {
    if (!isValid || (this.isCodeRequired() && !this.form.code.trim())) {
      this.error.set('Correct the highlighted validation errors.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    const payload = this.trimPayload();
    const base = `/api/mastercatalog/${this.selectedType()}`;
    const request = this.editingId()
      ? this.http.put(`${base}/${this.editingId()}`, payload)
      : this.http.post(base, payload);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      },
    });
  }

  remove(row: any): void {
    if (!confirm(`Delete ${row.name}? This will hide it from master lists.`)) return;
    this.http
      .delete(`/api/mastercatalog/${this.selectedType()}/${row.id}`)
      .subscribe({
        next: () => this.load(),
        error: (err) => this.error.set(this.errorMessage(err)),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');
    this.http
      .get<any>(`/api/mastercatalog/${this.selectedType()}`, {
        params: { search: this.search, page: this.page(), pageSize: this.pageSize },
      })
      .subscribe({
        next: (res) => {
          this.rows.set((res.items ?? []).map((row: any) => this.normalizeRow(row)));
          this.total.set(res.total ?? 0);
          this.loading.set(false);
        },
        error: (err) => {
          this.rows.set([]);
          this.loading.set(false);
          this.error.set(this.errorMessage(err));
        },
      });
  }
  private loadDepartments(): void {
    this.http
      .get<any>('/api/departments', { params: { page: 1, pageSize: 100 } })
      .subscribe({
        next: (r) => this.departments.set(r.items ?? r ?? []),
        error: () => this.departments.set([]),
      });
  }
  private emptyForm(): MasterForm {
    return {
      code: '',
      name: '',
      description: '',
      isActive: true,
      departmentId: null,
      licenseNumber: '',
      phone: '',
      email: '',
      manufacturer: '',
      model: '',
      contactPerson: '',
      address: '',
      genericName: '',
      category: '',
      unit: '',
    };
  }
  private trimPayload(): MasterForm {
    return Object.fromEntries(
      Object.entries(this.form).map(([key, value]) => [
        key,
        typeof value === 'string' ? value.trim() : value,
      ]),
    ) as MasterForm;
  }
  displayCode(row: any): string {
    return row.code ?? '';
  }
  details(row: any): string {
    const fragments = [
      row.description,
      row.licenseNumber,
      row.phone,
      row.email,
      row.manufacturer,
      row.model,
      row.contactPerson,
      row.address,
      row.genericName,
      row.category,
      row.unit,
    ].filter((value) => !!value);
    return fragments.join(' · ') || '-';
  }
  private normalizeRow(row: any): any {
    if (this.selectedType() === 'units') {
      return { ...row, code: row.symbol ?? '' };
    }
    return row;
  }
  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) {
        return firstMessage;
      }
    }

    return (
      error?.error?.message ??
      error?.error?.title ??
      error?.error ??
      'The request could not be completed.'
    );
  }
}
