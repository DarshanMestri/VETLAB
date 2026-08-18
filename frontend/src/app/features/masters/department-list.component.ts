import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DepartmentApi, Department } from './department.api';

@Component({
  standalone: true,
  selector: 'app-department-list',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mt-4">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h4>Departments</h4>
        <button class="btn btn-primary btn-sm" (click)="openCreate()">+ New</button>
      </div>
      <div class="input-group mb-3">
        <input
          class="form-control"
          placeholder="Search name/code"
          [(ngModel)]="search"
          (ngModelChange)="reload()"
        />
        <button class="btn btn-outline-secondary" (click)="clear()">Clear</button>
      </div>

      <table class="table table-sm table-striped">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Active</th>
            <th class="text-end"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let d of items()">
            <td>{{ d.code }}</td>
            <td>{{ d.name }}</td>
            <td>
              <span
                class="badge"
                [class.text-bg-success]="d.isActive"
                [class.text-bg-secondary]="!d.isActive"
                >{{ d.isActive ? 'Yes' : 'No' }}</span
              >
            </td>
            <td class="text-end">
              <button class="btn btn-sm btn-outline-secondary me-1" (click)="openEdit(d)">
                Edit
              </button>
              <button class="btn btn-sm btn-outline-warning" (click)="toggle(d)">
                {{ d.isActive ? 'Deactivate' : 'Activate' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="d-flex justify-content-between align-items-center">
        <small class="text-muted">Total: {{ total() }}</small>
        <div class="btn-group">
          <button
            class="btn btn-sm btn-outline-secondary"
            [disabled]="page() <= 1"
            (click)="prev()"
          >
            Prev
          </button>
          <button
            class="btn btn-sm btn-outline-secondary"
            [disabled]="page() * pageSize() >= total()"
            (click)="next()"
          >
            Next
          </button>
        </div>
      </div>

      <div *ngIf="showForm()" class="card mt-3">
        <div class="card-header">{{ editId() ? 'Edit Department' : 'Create Department' }}</div>
        <div class="card-body">
          <form (ngSubmit)="save()">
            <div class="row g-2">
              <div class="col-md-3">
                <label class="form-label">Code</label>
                <input class="form-control" [(ngModel)]="form.code" name="code" maxlength="50" />
              </div>
              <div class="col-md-7">
                <label class="form-label">Name</label>
                <input
                  class="form-control"
                  [(ngModel)]="form.name"
                  name="name"
                  required
                  maxlength="200"
                />
              </div>
              <div class="col-md-2 d-flex align-items-end">
                <div class="form-check form-switch">
                  <input
                    class="form-check-input"
                    type="checkbox"
                    [(ngModel)]="form.isActive"
                    name="isActive"
                  />
                  <label class="form-check-label">Active</label>
                </div>
              </div>
            </div>

            <div class="text-end mt-3">
              <button type="button" class="btn btn-secondary me-2" (click)="close()">Cancel</button>
              <button class="btn btn-primary">Save</button>
            </div>
            <div *ngIf="error()" class="alert alert-danger mt-3">{{ error() }}</div>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class DepartmentListComponent {
  private api = inject(DepartmentApi);

  search = '';
  page = signal(1);
  pageSize = signal(20);
  total = signal(0);
  items = signal<Department[]>([]);
  showForm = signal(false);
  editId = signal<number | null>(null);
  error = signal('');
  form: Omit<Department, 'id'> = { name: '', code: '', isActive: true };

  constructor() {
    this.reload();
  }

  reload() {
    this.api.list(this.search, this.page(), this.pageSize()).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.total.set(res.total);
      },
      error: (_) => this.error.set('Load failed'),
    });
  }
  next() {
    this.page.update((p) => p + 1);
    this.reload();
  }
  prev() {
    this.page.update((p) => Math.max(1, p - 1));
    this.reload();
  }
  clear() {
    this.search = '';
    this.page.set(1);
    this.reload();
  }

  openCreate() {
    this.editId.set(null);
    this.form = { name: '', code: '', isActive: true };
    this.error.set('');
    this.showForm.set(true);
  }
  openEdit(d: Department) {
    this.editId.set(d.id);
    this.form = { name: d.name, code: d.code ?? '', isActive: d.isActive };
    this.error.set('');
    this.showForm.set(true);
  }
  close() {
    this.showForm.set(false);
  }

  save() {
    const payload = { ...this.form, name: this.form.name.trim() };
    const obs = this.editId() ? this.api.update(this.editId()!, payload) : this.api.create(payload);
    obs.subscribe({
      next: (_) => {
        this.showForm.set(false);
        this.reload();
      },
      error: (err) => this.error.set(err.error ?? 'Save failed'),
    });
  }

  toggle(d: Department) {
    const newVal = !d.isActive;
    this.api.toggleStatus(d.id, newVal).subscribe({
      next: () => {
        d.isActive = newVal;
        this.reload();
      },
      error: (err) => this.error.set(err.error ?? 'Toggle failed'),
    });
  }
}
