import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  LaboratoriesService,
  LaboratoryListItem,
} from './laboratories.service';
import { BranchesService, OrganizationOption } from '../branches/branches.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-laboratories-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="row g-3 align-items-center mb-3">
      <div class="col-md-6">
        <div class="input-group">
          <input
            class="form-control"
            [(ngModel)]="search"
            (keyup.enter)="load()"
            placeholder="Search by code or name"
          />
          <button class="btn btn-outline-secondary" type="button" (click)="load()">Search</button>
        </div>
      </div>
      <div class="col-md-6 text-md-end">
        <button class="btn btn-primary" type="button" (click)="openCreate()">+ Add Laboratory</button>
      </div>
    </div>

    <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>
    <div *ngIf="loading()" class="text-muted py-3">Loading…</div>

    <div class="table-responsive" *ngIf="!loading()">
      <table class="table table-striped table-hover align-middle" *ngIf="rows().length; else empty">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Organization</th>
            <th>Address</th>
            <th>Phone</th>
            <th>Email</th>
            <th>Status</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of rows()">
            <td>{{ row.code }}</td>
            <td>{{ row.name }}</td>
            <td>{{ row.organizationName || '—' }}</td>
            <td>{{ row.address || '—' }}</td>
            <td>{{ row.phone || '—' }}</td>
            <td>{{ row.email || '—' }}</td>
            <td>
              <span
                class="badge"
                [class.text-bg-success]="row.isActive"
                [class.text-bg-secondary]="!row.isActive"
              >
                {{ row.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td class="text-end">
              <div class="btn-group btn-group-sm" role="group">
                <button class="btn btn-outline-primary" type="button" (click)="openEdit(row)">Edit</button>
                <button
                  class="btn btn-outline-secondary"
                  type="button"
                  (click)="toggleStatus(row)"
                >
                  {{ row.isActive ? 'Deactivate' : 'Activate' }}
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
      <ng-template #empty><p class="text-muted py-3">No laboratories found.</p></ng-template>
    </div>

    <section class="card mt-4" *ngIf="showForm()">
      <div class="card-header">{{ editingId() ? 'Edit' : 'Create' }} Laboratory</div>
      <div class="card-body">
        <form #labForm="ngForm" (ngSubmit)="save(labForm.valid ?? false)" novalidate>
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Organization <span class="text-danger">*</span></label>
              <select
                class="form-select"
                name="organizationId"
                [(ngModel)]="form.organizationId"
                [disabled]="editingId() !== null"
                required
              >
                <option [ngValue]="null">Select Organization</option>
                @for (o of organizations; track o.id) {
                  <option [ngValue]="o.id">{{ o.name }}</option>
                }
              </select>
              @if (editingId() !== null) {
                <div class="form-text">Organization cannot be changed after creation.</div>
              }
            </div>
            <div class="col-md-2">
              <label class="form-label">Code *</label>
              <input class="form-control" name="code" [(ngModel)]="form.code" maxlength="100" required #code="ngModel" />
              <div class="invalid-feedback d-block" *ngIf="code.invalid && code.touched">Code is required.</div>
            </div>
            <div class="col-md-4">
              <label class="form-label">Name *</label>
              <input class="form-control" name="name" [(ngModel)]="form.name" maxlength="200" required #name="ngModel" />
              <div class="invalid-feedback d-block" *ngIf="name.invalid && name.touched">Name is required.</div>
            </div>
            <div class="col-md-2">
              <label class="form-label">Status</label>
              <div class="form-check mt-2">
                <input id="labIsActive" class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
                <label class="form-check-label" for="labIsActive">Active</label>
              </div>
            </div>

            <div class="col-md-6">
              <label class="form-label">Phone</label>
              <input class="form-control" name="phone" [(ngModel)]="form.phone" maxlength="50" />
            </div>
            <div class="col-md-6">
              <label class="form-label">WhatsApp</label>
              <input class="form-control" name="whatsAppNumber" [(ngModel)]="form.whatsAppNumber" maxlength="50" />
            </div>

            <div class="col-md-6">
              <label class="form-label">Email</label>
              <input class="form-control" type="email" name="email" [(ngModel)]="form.email" maxlength="200" #email="ngModel" email />
            </div>
            <div class="col-md-6">
              <label class="form-label">GST</label>
              <input class="form-control" name="gst" [(ngModel)]="form.gst" maxlength="50" />
            </div>

            <div class="col-md-6">
              <label class="form-label">Registration Number</label>
              <input class="form-control" name="registrationNumber" [(ngModel)]="form.registrationNumber" maxlength="100" />
            </div>
            <div class="col-md-6">
              <label class="form-label">Address</label>
              <input class="form-control" name="address" [(ngModel)]="form.address" maxlength="500" />
            </div>

            <div class="col-md-6">
              <label class="form-label">Report Header</label>
              <textarea class="form-control" name="reportHeader" [(ngModel)]="form.reportHeader" rows="2"></textarea>
            </div>
            <div class="col-md-6">
              <label class="form-label">Report Footer</label>
              <textarea class="form-control" name="reportFooter" [(ngModel)]="form.reportFooter" rows="2"></textarea>
            </div>
          </div>

          <div class="text-end mt-4">
            <button type="button" class="btn btn-secondary me-2" (click)="showForm.set(false)">Cancel</button>
            <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
          </div>
        </form>
      </div>
    </section>
  `,
})
export class LaboratoriesListComponent implements OnInit {
  private readonly svc = inject(LaboratoriesService);
  private readonly branchesSvc = inject(BranchesService);
  private readonly auth = inject(AuthService);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly rows = signal<LaboratoryListItem[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  search = '';
  form = this.emptyForm();
  organizations: OrganizationOption[] = [];
  private defaultOrganizationId: number | null = null;
  readonly Math = Math;

  ngOnInit(): void {
    this.loadOrganizations();
    this.load();
  }

  loadOrganizations(): void {
    this.branchesSvc.listOrganizations().subscribe({
      next: (items) => {
        this.organizations = items ?? [];
        const payloadOrgId = this.auth.getJwtPayload()?.organization_id;
        this.defaultOrganizationId =
          this.auth.getSelectedOrganizationId() ??
          (typeof payloadOrgId === 'number' ? payloadOrgId : null);
        if (!this.editingId()) {
          this.form = { ...this.form, organizationId: this.defaultOrganizationId };
        }
      },
      error: () => {
        this.organizations = [];
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    if (this.defaultOrganizationId != null) {
      this.form = { ...this.form, organizationId: this.defaultOrganizationId };
    }
    this.showForm.set(true);
    this.error.set('');
  }

  openEdit(row: LaboratoryListItem): void {
    this.editingId.set(row.id);
    this.form = {
      ...this.emptyForm(),
      organizationId: row.organizationId,
      code: row.code,
      name: row.name,
      logo: row.logo ?? '',
      address: row.address ?? '',
      phone: row.phone ?? '',
      email: row.email ?? '',
      gst: row.gst ?? '',
      registrationNumber: row.registrationNumber ?? '',
      whatsAppNumber: row.whatsAppNumber ?? '',
      reportHeader: row.reportHeader ?? '',
      reportFooter: row.reportFooter ?? '',
      digitalSignature: row.digitalSignature ?? '',
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

    const isCreate = this.editingId() === null;
    if (isCreate && !this.form.organizationId) {
      this.error.set('Select an organization for this laboratory.');
      return;
    }

    const payload: any = {
      code: (this.form.code || '').trim(),
      name: (this.form.name || '').trim(),
      logo: (this.form.logo || '').trim() || null,
      address: (this.form.address || '').trim() || null,
      phone: (this.form.phone || '').trim() || null,
      email: (this.form.email || '').trim() || null,
      gst: (this.form.gst || '').trim() || null,
      registrationNumber: (this.form.registrationNumber || '').trim() || null,
      whatsAppNumber: (this.form.whatsAppNumber || '').trim() || null,
      reportHeader: (this.form.reportHeader || '').trim() || null,
      reportFooter: (this.form.reportFooter || '').trim() || null,
      digitalSignature: (this.form.digitalSignature || '').trim() || null,
      isActive: !!this.form.isActive,
    };
    if (isCreate) payload.organizationId = this.form.organizationId;

    this.saving.set(true);
    this.error.set('');

    const request = this.editingId()
      ? this.svc.update(this.editingId()!, payload)
      : this.svc.create(payload);

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

  toggleStatus(row: LaboratoryListItem): void {
    this.error.set('');
    this.svc.toggleStatus(row.id, !row.isActive).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(this.extractMessage(err)),
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
    return {
      organizationId: null as number | null,
      code: '',
      name: '',
      logo: '',
      address: '',
      phone: '',
      email: '',
      gst: '',
      registrationNumber: '',
      whatsAppNumber: '',
      reportHeader: '',
      reportFooter: '',
      digitalSignature: '',
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
