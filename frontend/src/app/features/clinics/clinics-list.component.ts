import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClinicsService, ClinicListItem } from './clinics.service';
import { BranchesService, OrganizationOption } from '../branches/branches.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-clinics-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <h4>Clinics</h4>

      <!-- Toolbar -->
      <div class="row mb-3">
        <div class="col-md-4">
          <input type="text" class="form-control" placeholder="Search clinics..." [(ngModel)]="search" (keyup.enter)="load()">
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="load()">Search</button>
        </div>
        <div class="col-auto">
          <button class="btn btn-success" (click)="openCreate()">+ New Clinic</button>
        </div>
      </div>

      <!-- Table -->
      <table class="table table-bordered table-striped">
        <thead class="table-light">
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Organization</th>
            <th>Phone</th>
            <th>Email</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @for(item of items; track item.id) {
            <tr>
              <td>{{item.code}}</td>
              <td>{{item.name}}</td>
              <td>{{item.organizationName || '-'}}</td>
              <td>{{item.phone || '-'}}</td>
              <td>{{item.email || '-'}}</td>
              <td>
                <span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">
                  {{item.isActive ? 'Active' : 'Inactive'}}
                </span>
              </td>
              <td>
                <button class="btn btn-sm btn-outline-primary me-1" (click)="openEdit(item)">Edit</button>
                <button class="btn btn-sm btn-outline-warning" (click)="toggleStatus(item)">
                  {{item.isActive ? 'Deactivate' : 'Activate'}}
                </button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="7" class="text-center text-muted">No clinics found.</td></tr>
          }
        </tbody>
      </table>

      <!-- Pagination -->
      <nav>
        <ul class="pagination">
          <li class="page-item" [class.disabled]="page <= 1">
            <a class="page-link" (click)="goPage(page - 1)">Previous</a>
          </li>
          <li class="page-item disabled"><span class="page-link">Page {{page}} of {{totalPages}}</span></li>
          <li class="page-item" [class.disabled]="page >= totalPages">
            <a class="page-link" (click)="goPage(page + 1)">Next</a>
          </li>
        </ul>
      </nav>

      <!-- Create/Edit Modal -->
      @if (showForm) {
        <div class="modal show d-block" tabindex="-1" style="background:rgba(0,0,0,0.5)">
          <div class="modal-dialog">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title">{{editing ? 'Edit' : 'Create'}} Clinic</h5>
                <button type="button" class="btn-close" (click)="showForm = false"></button>
              </div>
              <div class="modal-body">
                @if (error) {
                  <div class="alert alert-danger py-2">{{error}}</div>
                }
                <div class="mb-3">
                  <label class="form-label">Organization <span class="text-danger">*</span></label>
                  <select class="form-select" [(ngModel)]="form.organizationId" name="organizationId" [disabled]="editing" required>
                    <option [ngValue]="null">Select Organization</option>
                    @for(o of organizations; track o.id) {
                      <option [ngValue]="o.id">{{o.name}}</option>
                    }
                  </select>
                  @if (editing) {
                    <div class="form-text">Organization cannot be changed after creation.</div>
                  }
                </div>
                <div class="mb-3">
                  <label class="form-label">Code <span class="text-danger">*</span></label>
                  <input class="form-control" [(ngModel)]="form.code" required>
                </div>
                <div class="mb-3">
                  <label class="form-label">Name <span class="text-danger">*</span></label>
                  <input class="form-control" [(ngModel)]="form.name" required>
                </div>
                <div class="mb-3">
                  <label class="form-label">Short Name</label>
                  <input class="form-control" [(ngModel)]="form.shortName">
                </div>
                <div class="mb-3">
                  <label class="form-label">Phone</label>
                  <input class="form-control" [(ngModel)]="form.phone">
                </div>
                <div class="mb-3">
                  <label class="form-label">Email</label>
                  <input class="form-control" [(ngModel)]="form.email" type="email">
                </div>
                <div class="mb-3">
                  <label class="form-label">Address</label>
                  <textarea class="form-control" [(ngModel)]="form.address" rows="2"></textarea>
                </div>
                <div class="mb-3 form-check">
                  <input class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" id="clinicActive">
                  <label class="form-check-label" for="clinicActive">Active</label>
                </div>
              </div>
              <div class="modal-footer">
                <button class="btn btn-secondary" (click)="showForm = false">Cancel</button>
                <button class="btn btn-primary" (click)="save()" [disabled]="!form.code || !form.name || (!editing && !form.organizationId)">{{editing ? 'Update' : 'Create'}}</button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class ClinicsListComponent implements OnInit {
  items: ClinicListItem[] = [];
  total = 0;
  page = 1;
  pageSize = 20;
  search = '';
  showForm = false;
  editing = false;
  editingId = 0;
  organizations: OrganizationOption[] = [];
  private defaultOrganizationId: number | null = null;
  error = '';
  form = this.emptyForm();

  constructor(
    private svc: ClinicsService,
    private branchesSvc: BranchesService,
    private auth: AuthService,
  ) {}

  ngOnInit() {
    this.loadOrganizations();
    this.load();
  }

  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  loadOrganizations() {
    this.branchesSvc.listOrganizations().subscribe({
      next: (items) => {
        this.organizations = items ?? [];
        const payloadOrgId = this.auth.getJwtPayload()?.organization_id;
        this.defaultOrganizationId =
          this.auth.getSelectedOrganizationId() ??
          (typeof payloadOrgId === 'number' ? payloadOrgId : null);
        if (!this.editing && !this.form.organizationId) {
          this.form = { ...this.form, organizationId: this.defaultOrganizationId };
        }
      },
      error: () => {
        this.organizations = [];
      },
    });
  }

  load() {
    this.svc.list({ search: this.search, page: this.page, pageSize: this.pageSize }).subscribe(r => {
      this.items = r.items ?? [];
      this.total = r.total ?? 0;
    });
  }

  goPage(p: number) { this.page = p; this.load(); }

  openCreate() {
    this.editing = false;
    this.editingId = 0;
    this.form = this.emptyForm();
    if (this.defaultOrganizationId != null) {
      this.form = { ...this.form, organizationId: this.defaultOrganizationId };
    }
    this.error = '';
    this.showForm = true;
  }

  openEdit(item: ClinicListItem) {
    this.editing = true;
    this.editingId = item.id;
    this.form = {
      organizationId: item.organizationId,
      code: item.code,
      name: item.name,
      shortName: item.shortName || '',
      phone: item.phone || '',
      email: item.email || '',
      address: item.address || '',
      isActive: item.isActive,
    };
    this.error = '';
    this.showForm = true;
  }

  save() {
    const isCreate = !this.editing;
    if (isCreate && !this.form.organizationId) {
      this.error = 'Select an organization for this clinic.';
      return;
    }
    const req: any = {
      code: (this.form.code || '').trim(),
      name: (this.form.name || '').trim(),
      shortName: this.form.shortName,
      phone: this.form.phone,
      email: this.form.email,
      address: this.form.address,
      isActive: this.form.isActive,
    };
    if (isCreate) req.organizationId = this.form.organizationId;

    const op = this.editing ? this.svc.update(this.editingId, req) : this.svc.create(req);
    op.subscribe({
      next: () => { this.showForm = false; this.load(); },
      error: (err) => { this.error = this.extractMessage(err); },
    });
  }

  toggleStatus(item: ClinicListItem) {
    this.svc.toggleStatus(item.id, !item.isActive).subscribe(() => this.load());
  }

  emptyForm() {
    return {
      organizationId: null as number | null,
      code: '',
      name: '',
      shortName: '',
      phone: '',
      email: '',
      address: '',
      isActive: true,
    };
  }

  extractMessage(err: any): string {
    const payload = err?.error;
    if (typeof payload === 'string') return payload;
    if (payload?.message) return payload.message;
    if (payload?.title) return payload.title;
    return 'Request failed';
  }
}
