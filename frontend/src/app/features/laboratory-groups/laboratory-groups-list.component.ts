import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LaboratoryGroupsService, LaboratoryGroupLaboratoryItem, LaboratoryGroupListItem, LaboratoryListItem } from './laboratory-groups.service';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-laboratory-groups-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [DatePipe],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <div>
          <h2 class="mb-0">Laboratory Groups</h2>
          <small class="text-muted">Manage laboratory groups and their assigned laboratories</small>
        </div>
        <button class="btn btn-primary" (click)="openCreate()">+ Add Laboratory Group</button>
      </div>

      <div class="row g-3 align-items-center mb-3">
        <div class="col-md-7">
          <div class="input-group">
            <input
              class="form-control"
              [(ngModel)]="search"
              (keyup.enter)="load()"
              placeholder="Search by code or name"
            />
            <button class="btn btn-outline-secondary" (click)="load()">Search</button>
          </div>
        </div>
        <div class="col-md-5 text-md-end">
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
              <th>Description</th>
              <th>Laboratories</th>
              <th>Status</th>
              <th>Created</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of filteredRows()">
              <td>{{ row.code }}</td>
              <td>{{ row.name }}</td>
              <td>{{ row.description || '—' }}</td>
              <td>{{ row.laboratoryCount }}</td>
              <td>
                <span class="badge" [class.text-bg-success]="row.isActive" [class.text-bg-secondary]="!row.isActive">
                  {{ row.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td>{{ row.createdDate | date: 'yyyy-MM-dd' }}</td>
              <td class="text-end">
                <div class="btn-group btn-group-sm" role="group">
                  <button class="btn btn-outline-info" (click)="openManage(row)">Manage Laboratories</button>
                  <button class="btn btn-outline-primary" (click)="openEdit(row)">Edit</button>
                  <button class="btn btn-outline-secondary" (click)="toggleStatus(row)">
                    {{ row.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <ng-template #empty>
          <p class="text-muted py-3">No laboratory groups found.</p>
        </ng-template>
      </div>

      <div class="d-flex justify-content-between align-items-center mt-3" *ngIf="total() > pageSize">
        <small class="text-muted">Page {{ page() }} of {{ Math.ceil(total() / pageSize) }}</small>
        <div class="btn-group btn-group-sm">
          <button class="btn btn-outline-secondary" [disabled]="page() === 1" (click)="goToPage(page() - 1)">
            Previous
          </button>
          <button
            class="btn btn-outline-secondary"
            [disabled]="page() >= Math.ceil(total() / pageSize)"
            (click)="goToPage(page() + 1)"
          >
            Next
          </button>
        </div>
      </div>

      <section class="card mt-4" *ngIf="showForm()">
        <div class="card-header">{{ editingId() ? 'Edit' : 'Create' }} Laboratory Group</div>
        <div class="card-body">
          <form #groupForm="ngForm" (ngSubmit)="save(groupForm.valid ?? false)" novalidate>
            <div class="row g-3">
              <div class="col-md-3">
                <label class="form-label">Code *</label>
                <input
                  class="form-control"
                  name="code"
                  [(ngModel)]="form.code"
                  maxlength="100"
                  required
                  #code="ngModel"
                />
                <div class="invalid-feedback d-block" *ngIf="code.invalid && code.touched">Code is required.</div>
              </div>

              <div class="col-md-9">
                <label class="form-label">Name *</label>
                <input
                  class="form-control"
                  name="name"
                  [(ngModel)]="form.name"
                  maxlength="200"
                  required
                  #name="ngModel"
                />
                <div class="invalid-feedback d-block" *ngIf="name.invalid && name.touched">Name is required.</div>
              </div>

              <div class="col-12">
                <label class="form-label">Description</label>
                <textarea class="form-control" name="description" [(ngModel)]="form.description" rows="3" maxlength="2000"></textarea>
              </div>

              <div class="col-md-3">
                <div class="form-check mt-2">
                  <input id="lgIsActive" class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
                  <label class="form-check-label" for="lgIsActive">Active</label>
                </div>
              </div>
            </div>

            <div class="text-end mt-4">
              <button type="button" class="btn btn-secondary me-2" (click)="showForm.set(false)">Cancel</button>
              <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
            </div>
          </form>
        </div>
      </section>

      <section class="card mt-4" *ngIf="manageGroup">
        <div class="card-header d-flex justify-content-between align-items-center gap-2 flex-wrap">
          <span>Manage Laboratories for {{ manageGroup.name }}</span>
          <button class="btn btn-sm btn-outline-secondary" (click)="closeManage()">Close</button>
        </div>
        <div class="card-body">
              <div *ngIf="!manageGroup.isActive" class="alert alert-warning mb-3">
            This laboratory group is inactive. New laboratory assignments are blocked by backend validation.
          </div>

          <div class="row g-4">
            <div class="col-lg-6">
              <h5>Assigned Laboratories</h5>
              <div *ngIf="labsLoading()" class="text-muted">Loading assigned laboratories…</div>
              <div *ngIf="!labsLoading() && assignedLaboratories.length === 0" class="text-muted">No laboratories assigned.</div>
              <div class="list-group mt-3" *ngIf="assignedLaboratories.length">
                <div *ngFor="let lab of assignedLaboratories" class="list-group-item d-flex justify-content-between align-items-center gap-3">
                  <div>
                    <div class="fw-semibold">{{ lab.name }}</div>
                    <small class="text-muted">{{ lab.code }} • {{ lab.shortName || '—' }}</small>
                  </div>
                  <div class="d-flex align-items-center gap-2">
                    <span class="badge" [class.text-bg-success]="lab.isActive" [class.text-bg-secondary]="!lab.isActive">
                      {{ lab.isActive ? 'Active' : 'Inactive' }}
                    </span>
                    <button class="btn btn-sm btn-outline-danger" (click)="unassignLaboratory(lab.id)">Unassign</button>
                  </div>
                </div>
              </div>
            </div>

            <div class="col-lg-6">
              <h5>Available Laboratories</h5>
              <div *ngIf="!manageGroup.isActive" class="text-muted">Assignments are disabled while this group is inactive.</div>
              <div *ngIf="manageGroup.isActive && availableLaboratories.length === 0" class="text-muted">No available laboratories in this organization.</div>
              <div class="list-group mt-3" *ngIf="manageGroup.isActive && availableLaboratories.length">
                <div *ngFor="let lab of availableLaboratories" class="list-group-item d-flex justify-content-between align-items-center gap-3">
                  <div>
                    <div class="fw-semibold">{{ lab.name }}</div>
                    <small class="text-muted">{{ lab.code }} • {{ lab.shortName || '—' }}</small>
                  </div>
                  <button class="btn btn-sm btn-primary" (click)="assignLaboratory(lab.id)">Assign</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  `,
})
export class LaboratoryGroupsListComponent implements OnInit {
  private readonly svc = inject(LaboratoryGroupsService);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly rows = signal<LaboratoryGroupListItem[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly labsLoading = signal(false);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');

  search = '';
  form = this.emptyForm();
  manageGroup: LaboratoryGroupListItem | null = null;
  assignedLaboratories: LaboratoryGroupLaboratoryItem[] = [];
  availableLaboratories: LaboratoryListItem[] = [];
  readonly Math = Math;

  ngOnInit(): void {
    this.load();
  }

  get filteredRows(): () => LaboratoryGroupListItem[] {
    return () => {
      const q = this.rows();
      if (this.statusFilter() === 'active') return q.filter((x) => x.isActive);
      if (this.statusFilter() === 'inactive') return q.filter((x) => !x.isActive);
      return q;
    };
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
    this.error.set('');
  }

  openEdit(row: LaboratoryGroupListItem): void {
    this.editingId.set(row.id);
    this.form = { ...this.emptyForm(), code: row.code, name: row.name, description: row.description ?? '', isActive: row.isActive };
    this.showForm.set(true);
    this.error.set('');
  }

  save(isValid: boolean): void {
    if (!isValid) {
      this.error.set('Correct the highlighted validation errors.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    const payload = {
      code: (this.form.code || '').trim(),
      name: (this.form.name || '').trim(),
      description: (this.form.description || '').trim() || null,
      isActive: !!this.form.isActive,
    };

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

  toggleStatus(row: LaboratoryGroupListItem): void {
    this.error.set('');
    this.svc.toggleStatus(row.id, !row.isActive).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(this.extractMessage(err)),
    });
  }

  openManage(row: LaboratoryGroupListItem): void {
    this.manageGroup = row;
    this.loadManagement(row.id);
  }

  closeManage(): void {
    this.manageGroup = null;
    this.assignedLaboratories = [];
    this.availableLaboratories = [];
  }

  loadManagement(laboratoryGroupId: number): void {
    this.labsLoading.set(true);
    this.error.set('');

    this.svc.getLaboratories(laboratoryGroupId).subscribe({
      next: (assigned) => {
        this.assignedLaboratories = assigned ?? [];

        this.svc.getAvailableLaboratories().subscribe({
          next: (allLabs) => {
            const assignedIds = new Set(this.assignedLaboratories.map((x) => x.id));
            this.availableLaboratories = (allLabs ?? []).filter((lab) => !assignedIds.has(lab.id));
            this.labsLoading.set(false);
          },
          error: (err) => {
            this.labsLoading.set(false);
            this.availableLaboratories = [];
            this.error.set(this.extractMessage(err));
          },
        });
      },
      error: (err) => {
        this.labsLoading.set(false);
        this.assignedLaboratories = [];
        this.error.set(this.extractMessage(err));
      },
    });
  }

  assignLaboratory(laboratoryId: number): void {
    if (!this.manageGroup) return;

    this.svc.assignLaboratory(this.manageGroup.id, laboratoryId).subscribe({
      next: () => {
        this.loadManagement(this.manageGroup!.id);
        this.load();
      },
      error: (err) => {
        this.error.set(this.extractMessage(err));
      },
    });
  }

  unassignLaboratory(laboratoryId: number): void {
    if (!this.manageGroup) return;
    if (!confirm('Are you sure you want to unassign this laboratory from the group?')) return;

    this.svc.unassignLaboratory(this.manageGroup.id, laboratoryId).subscribe({
      next: () => {
        this.loadManagement(this.manageGroup!.id);
        this.load();
      },
      error: (err) => {
        this.error.set(this.extractMessage(err));
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.svc.list(this.search, this.page(), this.pageSize).subscribe({
      next: (res) => {
        this.rows.set(res.items ?? []);
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

  goToPage(page: number): void {
    if (page < 1) return;
    const maxPage = Math.max(1, Math.ceil(this.total() / this.pageSize));
    if (page > maxPage) return;
    this.page.set(page);
    this.load();
  }

  emptyForm() {
    return { code: '', name: '', description: '', isActive: true };
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
