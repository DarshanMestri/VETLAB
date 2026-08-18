import { Component, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SpeciesApi, Species } from './species.api';

@Component({
  selector: 'app-species-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mt-4">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h4>Species</h4>
        <button class="btn btn-primary btn-sm" (click)="openCreate()">+ New</button>
      </div>

      <div class="input-group mb-3">
        <input
          class="form-control"
          placeholder="Search (code/name)"
          [(ngModel)]="searchTerm"
          (ngModelChange)="reload()"
        />
        <button class="btn btn-outline-secondary" (click)="clearSearch()">Clear</button>
      </div>

      <table class="table table-striped table-sm">
        <thead>
          <tr>
            <th style="width:110px">Code</th>
            <th>Name</th>
            <th style="width:120px">Status</th>
            <th style="width:220px"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of items()">
            <td>{{ s.code }}</td>
            <td>{{ s.name }}</td>
            <td>
              <span
                class="badge"
                [class.text-bg-success]="s.isActive"
                [class.text-bg-secondary]="!s.isActive"
                >{{ s.isActive ? 'Active' : 'Inactive' }}</span
              >
            </td>
            <td class="text-end">
              <button class="btn btn-sm btn-outline-secondary me-1" (click)="openEdit(s)">
                Edit
              </button>
              <button class="btn btn-sm btn-outline-warning" (click)="toggle(s)">
                {{ s.isActive ? 'Deactivate' : 'Activate' }}
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
            (click)="next()"
            [disabled]="page() * pageSize() >= total()"
          >
            Next
          </button>
        </div>
      </div>

      <!-- Modal-ish inline form -->
      <div *ngIf="showForm()" class="card mt-4">
        <div class="card-header">{{ editId() ? 'Edit Species' : 'Create Species' }}</div>
        <div class="card-body">
          <form (ngSubmit)="save()">
            <div class="mb-2">
              <label class="form-label">Code</label>
              <input
                class="form-control"
                [(ngModel)]="form.code"
                name="code"
                required
                maxlength="20"
              />
            </div>
            <div class="mb-2">
              <label class="form-label">Name</label>
              <input
                class="form-control"
                [(ngModel)]="form.name"
                name="name"
                required
                maxlength="100"
              />
            </div>

            <div class="text-end">
              <button class="btn btn-secondary me-2" type="button" (click)="closeForm()">
                Cancel
              </button>
              <button class="btn btn-primary" type="submit">Save</button>
            </div>

            <div *ngIf="error()" class="alert alert-danger mt-3">{{ error() }}</div>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class SpeciesListComponent {
  private api = inject(SpeciesApi);

  // pagination & data
  page = signal(1);
  pageSize = signal(10);
  total = signal(0);
  items = signal<Species[]>([]);
  searchTerm = '';

  // form state
  showForm = signal(false);
  editId = signal<number | null>(null);
  form = { code: '', name: '' };
  error = signal('');

  constructor() {
    this.reload();
  }

  reload() {
    this.api.list(this.searchTerm, this.page(), this.pageSize()).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.total.set(res.total);
      },
      error: (err) => this.error.set(err.error?.title ?? 'Failed to load'),
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
  clearSearch() {
    this.searchTerm = '';
    this.page.set(1);
    this.reload();
  }

  openCreate() {
    this.editId.set(null);
    this.form = { code: '', name: '' };
    this.error.set('');
    this.showForm.set(true);
  }
  openEdit(s: Species) {
    this.editId.set(s.id);
    this.form = { code: s.code, name: s.name };
    this.error.set('');
    this.showForm.set(true);
  }
  closeForm() {
    this.showForm.set(false);
  }

  save() {
    const payload = { code: this.form.code.trim(), name: this.form.name.trim() };
    const obs = this.editId() ? this.api.update(this.editId()!, payload) : this.api.create(payload);

    obs.subscribe({
      next: (_) => {
        this.showForm.set(false);
        this.reload();
      },
      error: (err) => this.error.set(err.error ?? 'Save failed'),
    });
  }

  toggle(s: Species) {
    const newVal = !s.isActive;
    this.api.toggleStatus(s.id, newVal).subscribe({
      next: (_) => this.reload(),
      error: (err) => this.error.set(err.error ?? 'Toggle failed'),
    });
  }
}
