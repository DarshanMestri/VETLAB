import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrganizationsService } from './organizations.service';

@Component({
  selector: 'app-organizations-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-0">Organizations</h2>
          <small class="text-muted">Manage organizations</small>
        </div>
        <div>
          <button class="btn btn-primary" (click)="openCreate()">+ Add Organization</button>
        </div>
      </div>

      <div class="input-group mb-3">
        <input class="form-control" [(ngModel)]="search" placeholder="Search by code or name" (keyup.enter)="load()" />
        <button class="btn btn-outline-secondary" (click)="load()">Search</button>
      </div>

      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>
      <div *ngIf="loading()" class="text-muted py-3">Loading…</div>

      <div class="table-responsive" *ngIf="!loading()">
        <table class="table table-striped table-hover align-middle" *ngIf="rows().length; else empty">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Legal Name</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of rows()">
              <td>{{ row.code }}</td>
              <td>{{ row.name }}</td>
              <td>{{ row.legalName || '-' }}</td>
              <td>{{ row.email || '-' }}</td>
              <td>{{ row.phone || '-' }}</td>
              <td>
                <span class="badge" [class.text-bg-success]="row.isActive" [class.text-bg-secondary]="!row.isActive">{{ row.isActive ? 'Active' : 'Inactive' }}</span>
              </td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-primary me-1" (click)="openEdit(row)">Edit</button>
                <button class="btn btn-sm btn-outline-danger" (click)="remove(row)">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
        <ng-template #empty><p class="text-muted py-3">No organizations found.</p></ng-template>
      </div>

      <section class="card mt-4" *ngIf="showForm()">
        <div class="card-header">{{ editingId() ? 'Edit' : 'Create' }} Organization</div>
        <div class="card-body">
          <form #orgForm="ngForm" (ngSubmit)="save(orgForm.valid ?? false)" novalidate>
            <div class="row g-3">
              <div class="col-md-4">
                <label class="form-label">Code *</label>
                <input class="form-control" name="code" [(ngModel)]="form.code" maxlength="100" required #code="ngModel" />
                <div class="invalid-feedback d-block" *ngIf="code.invalid && code.touched">Code is required.</div>
              </div>
              <div class="col-md-8">
                <label class="form-label">Name *</label>
                <input class="form-control" name="name" [(ngModel)]="form.name" maxlength="200" required #name="ngModel" />
                <div class="invalid-feedback d-block" *ngIf="name.invalid && name.touched">Name is required.</div>
              </div>

              <div class="col-md-6">
                <label class="form-label">Legal Name</label>
                <input class="form-control" name="legalName" [(ngModel)]="form.legalName" maxlength="200" />
              </div>

              <div class="col-md-6">
                <label class="form-label">Email</label>
                <input type="email" class="form-control" name="email" [(ngModel)]="form.email" />
              </div>

              <div class="col-md-4">
                <label class="form-label">Phone</label>
                <input class="form-control" name="phone" [(ngModel)]="form.phone" maxlength="30" />
              </div>

              <div class="col-12">
                <label class="form-label">Address</label>
                <textarea class="form-control" name="address" [(ngModel)]="form.address" rows="2"></textarea>
              </div>

              <div class="col-md-3">
                <div class="form-check mt-2">
                  <input id="isActive" class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
                  <label class="form-check-label" for="isActive">Active</label>
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
    </div>
  `,
})
export class OrganizationsListComponent implements OnInit {
  private svc = inject(OrganizationsService);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly rows = signal<any[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  search = '';
  form: any = this.emptyForm();

  ngOnInit(): void {
    this.load();
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
    this.error.set('');
  }

  openEdit(row: any): void {
    this.editingId.set(row.id);
    this.form = { ...this.emptyForm(), ...row };
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
      legalName: (this.form.legalName || null),
      email: (this.form.email || null),
      phone: (this.form.phone || null),
      address: (this.form.address || null),
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

  remove(row: any): void {
    if (!confirm(`Delete ${row.name}? This will hide it from master lists.`)) return;
    this.svc.delete(row.id).subscribe({ next: () => this.load(), error: (err) => (this.error.set(this.extractMessage(err))) });
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
        this.error.set(this.extractMessage(err));
      },
    });
  }

  goToPage(p: number) {
    if (p < 1) return;
    this.page.set(p);
    this.load();
  }

  emptyForm() {
    return { code: '', name: '', legalName: '', email: '', phone: '', address: '', isActive: true };
  }

  extractMessage(err: any): string {
    return err?.error?.message ?? err?.error ?? 'Request failed';
  }
}
