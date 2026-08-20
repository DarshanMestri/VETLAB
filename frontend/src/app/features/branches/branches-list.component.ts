import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BranchesService, BranchListItem, ClinicOption, LaboratoryOption } from './branches.service';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-branches-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [DatePipe],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <div>
          <h2 class="mb-0">Branches</h2>
          <small class="text-muted">Manage branch locations for your organization</small>
        </div>
        <button class="btn btn-primary" (click)="openCreate()">+ Add Branch</button>
      </div>

      <div class="row g-3 align-items-center mb-3">
        <div class="col-md-6">
          <div class="input-group">
            <input
              class="form-control"
              [(ngModel)]="search"
              (keyup.enter)="load()"
              placeholder="Search by code, name, clinic, or laboratory"
            />
            <button class="btn btn-outline-secondary" (click)="load()">Search</button>
          </div>
        </div>
        <div class="col-md-6 text-md-end">
          <div class="btn-group" role="group" aria-label="Filter by status">
            <button
              class="btn btn-sm"
              [class.btn-primary]="statusFilter() === 'all'"
              [class.btn-outline-secondary]="statusFilter() !== 'all'"
              type="button"
              (click)="statusFilter.set('all')"
            >
              All
            </button>
            <button
              class="btn btn-sm"
              [class.btn-primary]="statusFilter() === 'active'"
              [class.btn-outline-secondary]="statusFilter() !== 'active'"
              type="button"
              (click)="statusFilter.set('active')"
            >
              Active
            </button>
            <button
              class="btn btn-sm"
              [class.btn-primary]="statusFilter() === 'inactive'"
              [class.btn-outline-secondary]="statusFilter() !== 'inactive'"
              type="button"
              (click)="statusFilter.set('inactive')"
            >
              Inactive
            </button>
          </div>
        </div>
      </div>

      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>
      <div *ngIf="loading()" class="text-muted py-3">Loading…</div>

      <div class="table-responsive" *ngIf="!loading()">
        <table class="table table-striped table-hover align-middle" *ngIf="filteredRows().length; else empty">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Clinic</th>
              <th>Laboratory</th>
              <th>Address</th>
              <th>Phone</th>
              <th>Email</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of filteredRows()">
              <td>{{ row.code }}</td>
              <td>{{ row.name }}</td>
              <td>{{ row.clinicName || getClinicName(row.clinicId) }}</td>
              <td>{{ row.laboratoryName || getLaboratoryName(row.laboratoryId) }}</td>
              <td>{{ row.address || '—' }}</td>
              <td>{{ row.phone || '—' }}</td>
              <td>{{ row.email || '—' }}</td>
              <td>
                <span class="badge" [class.text-bg-success]="row.isActive" [class.text-bg-secondary]="!row.isActive">
                  {{ row.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="text-end">
                <div class="btn-group btn-group-sm" role="group">
                  <button class="btn btn-outline-primary" (click)="openEdit(row)">Edit</button>
                  <button class="btn btn-outline-info" (click)="openDepartmentManager(row)">Departments</button>
                  <button class="btn btn-outline-warning" (click)="openTestManager(row)">Tests</button>
                  <button class="btn btn-outline-secondary" (click)="toggleStatus(row)">
                    {{ row.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <ng-template #empty>
          <p class="text-muted py-3">No branches found.</p>
        </ng-template>
      </div>

      <div class="d-flex justify-content-between align-items-center mt-3" *ngIf="total() > pageSize">
        <small class="text-muted">Page {{ page() }} of {{ Math.ceil(total() / pageSize) }}</small>
        <div class="btn-group btn-group-sm">
          <button class="btn btn-outline-secondary" [disabled]="page() === 1" (click)="goToPage(page() - 1)">Previous</button>
          <button
            class="btn btn-outline-secondary"
            [disabled]="page() >= Math.ceil(total() / pageSize)"
            (click)="goToPage(page() + 1)"
          >
            Next
          </button>
        </div>
      </div>

      <section class="card mt-4" *ngIf="selectedBranch()">
        <div class="card-header d-flex justify-content-between align-items-center">
          <span>Assignments for {{ selectedBranch()?.name }}</span>
          <button class="btn btn-sm btn-outline-secondary" (click)="closeDepartmentManager()">Close</button>
        </div>
        <div class="card-body">
          <h6 class="border-bottom pb-2">Departments</h6>
          <div class="row g-3 mb-4">
            <div class="col-md-6">
              <div class="list-group">
                <div class="list-group-item d-flex justify-content-between align-items-center" *ngFor="let item of assignedDepartments()">
                  <div>
                    <div class="fw-semibold">{{ item.departmentName }}</div>
                    <small class="text-muted">{{ item.departmentCode || 'No code' }}</small>
                  </div>
                  <div class="btn-group btn-group-sm">
                    <button class="btn btn-outline-secondary" (click)="toggleAssignedDepartment(item)">
                      {{ item.isActive ? 'Disable' : 'Enable' }}
                    </button>
                    <button class="btn btn-outline-danger" (click)="removeAssignedDepartment(item)">Remove</button>
                  </div>
                </div>
                <div class="list-group-item text-muted" *ngIf="!assignedDepartments().length">No departments assigned.</div>
              </div>
            </div>

            <div class="col-md-6">
              <div class="input-group">
                <select class="form-select" [(ngModel)]="selectedDepartmentId">
                  <option [ngValue]="null">Select a department</option>
                  <option *ngFor="let dept of availableDepartments()" [ngValue]="dept.id">
                    {{ dept.name }}{{ dept.code ? ' (' + dept.code + ')' : '' }}
                  </option>
                </select>
                <button class="btn btn-primary" type="button" [disabled]="selectedDepartmentId == null" (click)="assignSelectedDepartment()">
                  Assign
                </button>
              </div>
              <div class="text-muted small mt-1" *ngIf="!availableDepartments().length">All active departments are already assigned.</div>
            </div>
          </div>

          <h6 class="border-bottom pb-2">Tests</h6>
          <div class="row g-3">
            <div class="col-md-6">
              <div class="list-group">
                <div class="list-group-item d-flex justify-content-between align-items-center" *ngFor="let item of assignedTests()">
                  <div>
                    <div class="fw-semibold">{{ item.testName }}</div>
                    <small class="text-muted">{{ item.testCode || 'No code' }}</small>
                  </div>
                  <div class="btn-group btn-group-sm">
                    <button class="btn btn-outline-secondary" (click)="toggleAssignedTest(item)">
                      {{ item.isActive ? 'Disable' : 'Enable' }}
                    </button>
                    <button class="btn btn-outline-danger" (click)="removeAssignedTest(item)">Remove</button>
                  </div>
                </div>
                <div class="list-group-item text-muted" *ngIf="!assignedTests().length">No tests assigned.</div>
              </div>
            </div>

            <div class="col-md-6">
              <div class="input-group">
                <select class="form-select" [(ngModel)]="selectedTestId">
                  <option [ngValue]="null">Select a test</option>
                  <option *ngFor="let test of availableTests()" [ngValue]="test.id">
                    {{ test.name }}{{ test.code ? ' (' + test.code + ')' : '' }}
                  </option>
                </select>
                <button class="btn btn-primary" type="button" [disabled]="selectedTestId == null" (click)="assignSelectedTest()">
                  Assign
                </button>
              </div>
              <div class="text-muted small mt-1" *ngIf="!availableTests().length">All active tests are already assigned.</div>
            </div>
          </div>
        </div>
      </section>

      <section class="card mt-4" *ngIf="showForm()">
        <div class="card-header">{{ editingId() ? 'Edit' : 'Create' }} Branch</div>
        <div class="card-body">
          <form #branchForm="ngForm" (ngSubmit)="save(branchForm.valid ?? false)" novalidate>
            <div class="row g-3">
              <div class="col-md-6">
                <label class="form-label">Clinic <span class="text-danger">*</span></label>
                <select class="form-select" [(ngModel)]="form.clinicId" name="clinicId" required>
                  <option [ngValue]="null">Select Clinic</option>
                  @for(c of clinicOptions; track c.id) {
                    <option [ngValue]="c.id">{{c.name}} ({{c.code}})</option>
                  }
                </select>
              </div>
              <div class="col-md-6">
                <label class="form-label">Laboratory <span class="text-danger">*</span></label>
                <select class="form-select" [(ngModel)]="form.laboratoryId" name="laboratoryId" required>
                  <option [ngValue]="null">Select Laboratory</option>
                  @for(l of laboratoryOptions; track l.id) {
                    <option [ngValue]="l.id">{{l.name}} ({{l.code}})</option>
                  }
                </select>
              </div>

              <div class="col-md-6">
                <label class="form-label">Name *</label>
                <input class="form-control" name="name" [(ngModel)]="form.name" maxlength="200" required #name="ngModel" />
                <div class="invalid-feedback d-block" *ngIf="name.invalid && name.touched">Name is required.</div>
              </div>

              <div class="col-md-3">
                <label class="form-label">Code *</label>
                <input class="form-control" name="code" [(ngModel)]="form.code" maxlength="100" required #code="ngModel" />
                <div class="invalid-feedback d-block" *ngIf="code.invalid && code.touched">Code is required.</div>
              </div>

              <div class="col-md-3">
                <label class="form-label">Status</label>
                <div class="form-check mt-2">
                  <input id="branchIsActive" class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
                  <label class="form-check-label" for="branchIsActive">Active</label>
                </div>
              </div>

              <div class="col-md-6">
                <label class="form-label">Address</label>
                <input class="form-control" name="address" [(ngModel)]="form.address" maxlength="500" />
              </div>

              <div class="col-md-3">
                <label class="form-label">Phone</label>
                <input class="form-control" name="phone" [(ngModel)]="form.phone" maxlength="50" />
              </div>

              <div class="col-md-3">
                <label class="form-label">Email</label>
                <input class="form-control" type="email" name="email" [(ngModel)]="form.email" maxlength="200" #email="ngModel" />
                <div class="invalid-feedback d-block" *ngIf="email.invalid && email.touched">Enter a valid email.</div>
              </div>
            </div>

            <div class="text-end mt-4">
              <button type="button" class="btn btn-secondary me-2" (click)="showForm.set(false)">Cancel</button>
              <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
            </div>
          </form>
        </div>
      </section>
    </div>
  `,
})
export class BranchesListComponent implements OnInit {
  private readonly svc = inject(BranchesService);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly rows = signal<BranchListItem[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly selectedBranch = signal<BranchListItem | null>(null);
  readonly assignedDepartments = signal<any[]>([]);
  readonly availableDepartments = signal<any[]>([]);
  readonly assignedTests = signal<any[]>([]);
  readonly availableTests = signal<any[]>([]);

  search = '';
  clinics: ClinicOption[] = [];
  clinicOptions: ClinicOption[] = [];
  laboratoryOptions: LaboratoryOption[] = [];
  form = this.emptyForm();
  selectedDepartmentId: number | null = null;
  selectedTestId: number | null = null;
  readonly Math = Math;

  ngOnInit(): void {
    this.loadClinics();
    this.loadLaboratories();
    this.load();
  }

  get filteredRows(): () => BranchListItem[] {
    return () => {
      const list = this.rows();
      const byStatus =
        this.statusFilter() === 'active'
          ? list.filter((x) => x.isActive)
          : this.statusFilter() === 'inactive'
            ? list.filter((x) => !x.isActive)
            : list;

      return byStatus;
    };
  }

  getClinicName(clinicId: number | null | undefined): string {
    if (!clinicId) return '—';
    return this.clinics.find((clinic) => clinic.id === clinicId)?.name ?? `Clinic #${clinicId}`;
  }

  getLaboratoryName(laboratoryId: number | null | undefined): string {
    if (!laboratoryId) return '—';
    return this.laboratoryOptions.find((lab) => lab.id === laboratoryId)?.name ?? `Laboratory #${laboratoryId}`;
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
    this.error.set('');
  }

  openEdit(row: BranchListItem): void {
    this.editingId.set(row.id);
    this.form = {
      ...this.emptyForm(),
      clinicId: row.clinicId ?? null,
      laboratoryId: row.laboratoryId ?? null,
      code: row.code,
      name: row.name,
      address: row.address ?? '',
      phone: row.phone ?? '',
      email: row.email ?? '',
      isActive: row.isActive,
    };
    this.showForm.set(true);
    this.error.set('');
  }

  save(isValid: boolean): void {
    if (!isValid) {
      this.error.set('Correct the highlighted validation errors.');
      return;
    }

    if (!this.form.clinicId) {
      this.error.set('Select a clinic.');
      return;
    }

    if (!this.form.laboratoryId) {
      this.error.set('Select a laboratory.');
      return;
    }

    const payload = {
      clinicId: this.form.clinicId,
      laboratoryId: this.form.laboratoryId,
      code: (this.form.code || '').trim(),
      name: (this.form.name || '').trim(),
      address: (this.form.address || '').trim() || null,
      phone: (this.form.phone || '').trim() || null,
      email: (this.form.email || '').trim() || null,
      isActive: !!this.form.isActive,
    };

    this.saving.set(true);
    this.error.set('');

    const request = this.editingId() ? this.svc.update(this.editingId()!, payload) : this.svc.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.extractMessage(err));
      },
    });
  }

  openDepartmentManager(row: BranchListItem): void {
    this.selectedBranch.set(row);
    this.selectedDepartmentId = null;
    this.selectedTestId = null;
    this.loadAssignedDepartments(row.id);
    this.loadAvailableDepartments(row.id);
    this.loadAssignedTests(row.id);
    this.loadAvailableTests(row.id);
    this.error.set('');
  }

  openTestManager(row: BranchListItem): void {
    this.selectedBranch.set(row);
    this.selectedDepartmentId = null;
    this.selectedTestId = null;
    this.loadAssignedDepartments(row.id);
    this.loadAvailableDepartments(row.id);
    this.loadAssignedTests(row.id);
    this.loadAvailableTests(row.id);
    this.error.set('');
  }

  closeDepartmentManager(): void {
    this.selectedBranch.set(null);
    this.assignedDepartments.set([]);
    this.availableDepartments.set([]);
    this.assignedTests.set([]);
    this.availableTests.set([]);
    this.selectedDepartmentId = null;
    this.selectedTestId = null;
  }

  loadAssignedDepartments(branchId: number): void {
    this.svc.getDepartments(branchId).subscribe({
      next: (items) => this.assignedDepartments.set(items ?? []),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  loadAvailableDepartments(branchId: number): void {
    this.svc.getAvailableDepartments(branchId).subscribe({
      next: (items) => this.availableDepartments.set(items ?? []),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  assignSelectedDepartment(): void {
    const branch = this.selectedBranch();
    if (!branch || this.selectedDepartmentId == null) return;

    this.svc.assignDepartment(branch.id, this.selectedDepartmentId).subscribe({
      next: () => {
        this.selectedDepartmentId = null;
        this.loadAssignedDepartments(branch.id);
        this.loadAvailableDepartments(branch.id);
      },
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  toggleAssignedDepartment(item: any): void {
    const branch = this.selectedBranch();
    if (!branch) return;

    this.svc.toggleDepartmentStatus(branch.id, item.departmentId, !item.isActive).subscribe({
      next: () => this.loadAssignedDepartments(branch.id),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  removeAssignedDepartment(item: any): void {
    const branch = this.selectedBranch();
    if (!branch) return;
    if (!confirm(`Remove ${item.departmentName} from ${branch.name}?`)) return;

    this.svc.removeDepartment(branch.id, item.departmentId).subscribe({
      next: () => {
        this.loadAssignedDepartments(branch.id);
        this.loadAvailableDepartments(branch.id);
      },
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  loadAssignedTests(branchId: number): void {
    this.svc.getBranchTests(branchId).subscribe({
      next: (items) => this.assignedTests.set(items ?? []),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  loadAvailableTests(branchId: number): void {
    this.svc.getAvailableTests(branchId).subscribe({
      next: (items) => this.availableTests.set(items ?? []),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  assignSelectedTest(): void {
    const branch = this.selectedBranch();
    if (!branch || this.selectedTestId == null) return;

    this.svc.assignTest(branch.id, this.selectedTestId).subscribe({
      next: () => {
        this.selectedTestId = null;
        this.loadAssignedTests(branch.id);
        this.loadAvailableTests(branch.id);
      },
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  toggleAssignedTest(item: any): void {
    const branch = this.selectedBranch();
    if (!branch) return;

    this.svc.toggleTestStatus(item.id, !item.isActive).subscribe({
      next: () => this.loadAssignedTests(branch.id),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  removeAssignedTest(item: any): void {
    const branch = this.selectedBranch();
    if (!branch) return;
    if (!confirm(`Remove ${item.testName} from ${branch.name}?`)) return;

    this.svc.removeTest(item.id).subscribe({
      next: () => {
        this.loadAssignedTests(branch.id);
        this.loadAvailableTests(branch.id);
      },
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  toggleStatus(row: BranchListItem): void {
    this.error.set('');
    this.svc.toggleStatus(row.id, !row.isActive).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.svc
      .list(this.search, undefined, this.page(), this.pageSize)
      .subscribe({
        next: (res) => {
          this.rows.set((res.items ?? []).map((row: BranchListItem) => ({ ...row, clinicName: this.getClinicName(row.clinicId ?? null) })));
          this.total.set(res.total ?? 0);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.rows.set([]);
          this.total.set(0);
          this.error.set(this.extractMessage(err));
        },
      });
  }

  loadClinics(): void {
    this.svc.getClinics().subscribe({
      next: (items) => {
        this.clinics = items ?? [];
        this.clinicOptions = items ?? [];
      },
      error: () => {
        this.clinics = [];
        this.clinicOptions = [];
      },
    });
  }

  loadLaboratories(): void {
    this.svc.getLaboratories().subscribe({
      next: (items) => {
        this.laboratoryOptions = items ?? [];
      },
      error: () => {
        this.laboratoryOptions = [];
      },
    });
  }

  goToPage(page: number): void {
    if (page < 1) return;
    const maxPage = Math.max(1, Math.ceil(this.total() / this.pageSize));
    if (page > maxPage) return;
    this.page.set(page);
    this.load();
  }

  emptyForm() {
    return {
      clinicId: null as number | null,
      laboratoryId: null as number | null,
      code: '',
      name: '',
      address: '',
      phone: '',
      email: '',
      isActive: true,
    };
  }

  extractMessage(err: any): string {
    const payload = err?.error;
    if (typeof payload === 'string') return payload;
    if (payload?.message) return payload.message;
    if (payload?.title) return payload.title;
    if (payload?.errors) {
      const values = Object.values(payload.errors as Record<string, unknown>);
      const flat = values.flatMap((item) => (Array.isArray(item) ? item : [item]));
      const first = flat.find((item) => typeof item === 'string');
      if (typeof first === 'string') return first;
    }
    return 'Request failed';
  }
}
