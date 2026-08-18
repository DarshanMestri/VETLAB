// src/app/features/departments/departments-list.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SimpleDialogComponent } from '../../shared/simple-dialog.component';

type Row = { id: number; code: string; name: string; isActive: boolean };

@Component({
  standalone: true,
  selector: 'app-departments-list',
  imports: [CommonModule, FormsModule, SimpleDialogComponent],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center">
        <h2>Departments</h2>
        <div>
          <input
            class="form-control form-control-sm d-inline-block w-auto me-2"
            placeholder="Search..."
            [(ngModel)]="search"
            (keyup.enter)="load()"
          />
          <button class="btn btn-sm btn-outline-secondary me-2" (click)="load()">Search</button>
          <button class="btn btn-sm btn-primary" (click)="openCreate()">+ Add</button>
        </div>
      </div>

      <table class="table table-sm table-striped mt-3" *ngIf="rows.length">
        <thead>
          <tr>
            <th>ID</th>
            <th>Code</th>
            <th>Name</th>
            <th>Status</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows">
            <td>{{ r.id }}</td>
            <td>{{ r.code }}</td>
            <td>{{ r.name }}</td>
            <td>
              <span
                class="badge"
                [class.text-bg-success]="r.isActive"
                [class.text-bg-secondary]="!r.isActive"
              >
                {{ r.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td class="text-end">
              <button class="btn btn-sm btn-outline-primary me-1" (click)="openEdit(r)">
                Edit
              </button>
              <button class="btn btn-sm btn-outline-warning me-1" (click)="toggle(r)">
                {{ r.isActive ? 'Deactivate' : 'Activate' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="!rows.length" class="mt-3">No departments found.</p>
    </div>

    <app-simple-dialog
      *ngIf="dlgOpen"
      [title]="dlgTitle"
      [model]="dlgModel"
      (submit)="save($event)"
      (closed)="dlgOpen = false"
    >
      <div class="mb-2">
        <label class="form-label">Code</label>
        <input class="form-control" [(ngModel)]="dlgModel.code" name="code" required />
        <div *ngIf="dlgFieldErrors.code" class="text-danger mt-1 small">
          {{ dlgFieldErrors.code }}
        </div>
      </div>
      <div class="mb-2">
        <label class="form-label">Name</label>
        <input class="form-control" [(ngModel)]="dlgModel.name" name="name" required />
        <div *ngIf="dlgFieldErrors.name" class="text-danger mt-1 small">
          {{ dlgFieldErrors.name }}
        </div>
      </div>
      <div class="form-check">
        <input
          type="checkbox"
          class="form-check-input"
          [(ngModel)]="dlgModel.isActive"
          name="isActive"
          id="depActive"
        />
        <label for="depActive" class="form-check-label">Active</label>
      </div>
      <div *ngIf="dlgError" class="alert alert-danger mt-3">{{ dlgError }}</div>
    </app-simple-dialog>
  `,
})
export class DepartmentsListComponent {
  private http = inject(HttpClient);
  rows: Row[] = [];
  search = '';

  dlgOpen = false;
  dlgTitle = 'New Department';
  dlgModel: any = { code: '', name: '', isActive: true };
  dlgFieldErrors = { code: '', name: '' };
  dlgError = '';
  editingId: number | null = null;

  ngOnInit() {
    this.load();
  }

  load() {
    const p = this.search ? '?search=' + encodeURIComponent(this.search) : '';
    this.http.get<any>(`/api/departments${p}`).subscribe((res) => {
      this.rows = res?.items ?? [];
    });
  }

  openCreate() {
    this.dlgTitle = 'New Department';
    this.editingId = null;
    this.dlgModel = { code: '', name: '', isActive: true };
    this.dlgError = '';
    this.dlgFieldErrors = { code: '', name: '' };
    this.dlgOpen = true;
  }
  openEdit(r: Row) {
    this.dlgTitle = `Edit #${r.id}`;
    this.editingId = r.id;
    this.dlgModel = { code: r.code, name: r.name, isActive: r.isActive };
    this.dlgError = '';
    this.dlgFieldErrors = { code: '', name: '' };
    this.dlgOpen = true;
  }

  save(model: any) {
    const payload = {
      code: model.code?.trim() ?? '',
      name: model.name?.trim() ?? '',
      isActive: model.isActive ?? true,
    };

    this.dlgFieldErrors = { code: '', name: '' };
    this.dlgError = '';

    if (!payload.code) {
      this.dlgFieldErrors.code = 'Code is required.';
    }
    if (!payload.name) {
      this.dlgFieldErrors.name = 'Name is required.';
    }
    if (!payload.code || !payload.name) {
      return;
    }

    if (this.editingId == null) {
      this.http.post('/api/departments', payload).subscribe({
        next: () => {
          this.dlgOpen = false;
          this.load();
        },
        error: (e) => this.handleSaveError(e, 'Create failed'),
      });
    } else {
      this.http.put(`/api/departments/${this.editingId}`, payload).subscribe({
        next: () => {
          this.dlgOpen = false;
          this.load();
        },
        error: (e) => this.handleSaveError(e, 'Update failed'),
      });
    }
  }

  private handleSaveError(error: any, fallback: string) {
    this.dlgFieldErrors = { code: '', name: '' };
    this.dlgError = '';

    const message =
      typeof error?.error === 'string'
        ? error.error
        : (error?.error?.title ?? error?.error?.detail ?? fallback);

    if (error?.status === 409) {
      this.dlgError = 'Duplicate department not allowed.';
      return;
    }

    if (typeof message === 'string') {
      const normalized = message.toLowerCase();
      if (normalized.includes('code already exists')) {
        this.dlgFieldErrors.code = 'Department code already exists.';
        return;
      }
      if (normalized.includes('name already exists')) {
        this.dlgFieldErrors.name = 'Department name already exists.';
        return;
      }
      if (
        normalized.includes('duplicate key value violates unique constraint') ||
        normalized.includes('duplicate key value') ||
        normalized.includes('unique constraint') ||
        normalized.includes('duplicate department')
      ) {
        this.dlgError = 'Duplicate department not allowed.';
        return;
      }
    }

    this.dlgError = message || fallback;
  }

  toggle(r: Row) {
    const newVal = !r.isActive;
    this.http
      .patch(`/api/departments/${r.id}/status`, newVal, {
        headers: { 'Content-Type': 'application/json' },
      })
      .subscribe({
        next: () => (r.isActive = newVal),
        error: (e) => alert(e?.error ?? 'Toggle failed'),
      });
  }
}
