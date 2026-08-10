// src/app/features/tests/tests-list.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SimpleDialogComponent } from '../../shared/simple-dialog.component';

type TestRow = {
  id: number;
  code: string;
  name: string;
  displayName?: string | null;
  departmentId: number;
  unitId?: number | null;
  valueType: number;
  isActive: boolean;
};
type Dept = { id: number; name: string };
type Unit = { id: number; name: string; symbol: string };
type ParamRow = {
  testParameterId: number;
  name: string;
  unitId: number | null;
  unitSymbol?: string | null;
  sortOrder: number;
};

@Component({
  standalone: true,
  selector: 'app-tests-list',
  imports: [CommonModule, FormsModule, SimpleDialogComponent],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center">
        <h2>Tests</h2>
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
            <th>Display Name</th>
            <th>Dept</th>
            <th>Unit</th>
            <th>Type</th>
            <th>Status</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows">
            <td>{{ r.id }}</td>
            <td>{{ r.code }}</td>
            <td>{{ r.name }}</td>
            <td>{{ r.displayName || '-' }}</td>
            <td>{{ deptName(r.departmentId) }}</td>
            <td>{{ unitById(r.unitId)?.symbol || '-' }}</td>
            <td>{{ valueTypeLabel(r.valueType) }}</td>
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
              <button class="btn btn-sm btn-outline-danger me-1" (click)="remove(r)">Delete</button>
              <button class="btn btn-sm btn-outline-success" (click)="openParams(r)">
                Parameters
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="!rows.length" class="mt-3">No tests found.</p>
    </div>

    <!-- Add/Edit test -->
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
      <div class="mb-2">
        <label class="form-label">Display Name</label>
        <input
          class="form-control"
          [(ngModel)]="dlgModel.displayName"
          name="displayName"
          required
        />
        <div *ngIf="dlgFieldErrors.displayName" class="text-danger mt-1 small">
          {{ dlgFieldErrors.displayName }}
        </div>
      </div>
      <div class="mb-2">
        <label class="form-label">Department</label>
        <select
          class="form-select"
          [(ngModel)]="dlgModel.departmentId"
          name="departmentId"
          required
        >
          <option [ngValue]="0" disabled>Select...</option>
          <option *ngFor="let d of depts" [ngValue]="d.id">{{ d.name }}</option>
        </select>
        <div *ngIf="dlgFieldErrors.departmentId" class="text-danger mt-1 small">
          {{ dlgFieldErrors.departmentId }}
        </div>
      </div>
      <div class="mb-2">
        <label class="form-label">Unit</label>
        <select class="form-select" [(ngModel)]="dlgModel.unitId" name="unitId">
          <option [ngValue]="null" disabled>Select...</option>
          <option *ngFor="let u of units" [ngValue]="u.id">{{ u.name }} ({{ u.symbol }})</option>
        </select>
      </div>
      <div class="mb-2">
        <label class="form-label">Value Type</label>
        <select class="form-select" [(ngModel)]="dlgModel.valueType" name="valueType" required>
          <option [ngValue]="1">Text</option>
          <option [ngValue]="2">Numeric</option>
          <option [ngValue]="3">Datetime</option>
        </select>
      </div>
      <div class="form-check">
        <input
          type="checkbox"
          class="form-check-input"
          [(ngModel)]="dlgModel.isActive"
          name="isActive"
          id="testActive"
        />
        <label for="testActive" class="form-check-label">Active</label>
      </div>
    </app-simple-dialog>

    <!-- Parameter manager -->
    <app-simple-dialog
      *ngIf="paramOpen"
      [title]="paramTitle"
      [model]="paramModel"
      okText="Save Param"
      (submit)="saveParam($event)"
      (closed)="closeParams()"
    >
      <div class="mb-2">
        <label class="form-label">Name</label>
        <input class="form-control" [(ngModel)]="paramModel.name" name="pname" required />
      </div>
      <div class="mb-2">
        <label class="form-label">Unit</label>
        <select class="form-select" [(ngModel)]="paramModel.unitId" name="unitId">
          <option [ngValue]="null">(none)</option>
          <option *ngFor="let u of units" [ngValue]="u.id">{{ u.name }} ({{ u.symbol }})</option>
        </select>
      </div>
      <div class="mb-2">
        <label class="form-label">Sort Order</label>
        <input
          class="form-control"
          type="number"
          [(ngModel)]="paramModel.sortOrder"
          name="sortOrder"
          required
        />
      </div>

      <div class="border rounded p-2 mt-3">
        <div class="d-flex justify-content-between align-items-center">
          <strong>Existing Parameters</strong>
          <button class="btn btn-sm btn-outline-secondary" (click)="reloadParams()">Reload</button>
        </div>
        <table class="table table-sm mt-2" *ngIf="params.length">
          <thead>
            <tr>
              <th>ID</th>
              <th>Name</th>
              <th>Unit</th>
              <th>Sort</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of params">
              <td>{{ p.testParameterId }}</td>
              <td>{{ p.name }}</td>
              <td>{{ unitById(p.unitId)?.symbol || '-' }}</td>
              <td>{{ p.sortOrder }}</td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-primary me-1" (click)="editParam(p)">
                  Edit
                </button>
                <button class="btn btn-sm btn-outline-danger" (click)="deleteParam(p)">
                  Delete
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p *ngIf="!params.length" class="mb-0">No parameters yet.</p>
      </div>
    </app-simple-dialog>
  `,
})
export class TestsListComponent {
  private http = inject(HttpClient);

  rows: TestRow[] = [];
  depts: Dept[] = [];
  units: Unit[] = [];
  search = '';

  // Test dialog
  dlgOpen = false;
  dlgTitle = 'New Test';
  dlgModel: any = {
    code: '',
    name: '',
    displayName: '',
    departmentId: 0,
    unitId: null,
    valueType: 1,
    isActive: true,
  };
  dlgFieldErrors = { departmentId: '', code: '', name: '', displayName: '' };
  editingId: number | null = null;

  // Params dialog
  paramOpen = false;
  paramTitle = 'Parameters';
  paramModel: any = { name: '', unitId: null, sortOrder: 1 };
  currentTestId: number | null = null;
  params: ParamRow[] = [];

  ngOnInit() {
    this.load();
    this.loadDepts();
    this.loadUnits();
  }

  // ===== Tests list =====
  load() {
    const p = this.search ? '?search=' + encodeURIComponent(this.search) : '';
    this.http.get<any>(`/api/tests${p}`).subscribe((res) => {
      this.rows = res?.items ?? [];
    });
  }
  loadDepts() {
    this.http.get<any>('/api/departments?page=1&pageSize=999').subscribe((res) => {
      this.depts = res?.items ?? [];
    });
  }
  loadUnits() {
    this.http
      .get<any>('/api/units?page=1&pageSize=999')
      .subscribe((res) => (this.units = res?.items ?? []));
  }
  deptName(id: number) {
    return this.depts.find((d) => d.id === id)?.name || id;
  }

  valueTypeLabel(value: number) {
    return value === 2 ? 'Numeric' : value === 3 ? 'Datetime' : 'Text';
  }

  openCreate() {
    this.dlgTitle = 'New Test';
    this.editingId = null;
    this.dlgModel = {
      code: '',
      name: '',
      displayName: '',
      departmentId: 0,
      unitId: null,
      valueType: 1,
      isActive: true,
    };
    this.dlgFieldErrors = { departmentId: '', code: '', name: '', displayName: '' };
    this.dlgOpen = true;
  }

  openEdit(r: TestRow) {
    this.dlgTitle = `Edit #${r.id}`;
    this.editingId = r.id;
    this.dlgModel = {
      code: r.code,
      name: r.name,
      displayName: r.displayName,
      departmentId: r.departmentId,
      unitId: r.unitId,
      valueType: r.valueType,
      isActive: r.isActive,
    };
    this.dlgFieldErrors = { departmentId: '', code: '', name: '', displayName: '' };
    this.dlgOpen = true;
  }

  save(model: any) {
    const departmentId = Number(model.departmentId ?? 0);
    const unitId = model.unitId == null ? null : Number(model.unitId);
    const valueType = Number(model.valueType) || 1;

    const payload = {
      name: model.name?.trim() ?? '',
      displayName: model.displayName?.trim() ?? '',
      code: model.code?.trim() ?? null,
      departmentId,
      unitId,
      valueType,
      isActive: model.isActive ?? true,
    };

    this.dlgFieldErrors = { departmentId: '', code: '', name: '', displayName: '' };

    if (!payload.code) {
      this.dlgFieldErrors.code = 'Code is required.';
    }
    if (!payload.name) {
      this.dlgFieldErrors.name = 'Name is required.';
    }
    if (!payload.displayName) {
      this.dlgFieldErrors.displayName = 'Display Name is required.';
    }
    if (!Number.isInteger(departmentId) || departmentId <= 0) {
      this.dlgFieldErrors.departmentId = 'Please select a valid department.';
    }
    if (
      this.dlgFieldErrors.code ||
      this.dlgFieldErrors.name ||
      this.dlgFieldErrors.displayName ||
      this.dlgFieldErrors.departmentId
    ) {
      return;
    }

    if (this.editingId == null) {
      this.http.post('/api/tests', payload).subscribe({
        next: () => {
          this.dlgOpen = false;
          this.load();
        },
        error: (e) => this.handleSaveError(e),
      });
    } else {
      this.http.put(`/api/tests/${this.editingId}`, payload).subscribe({
        next: () => {
          this.dlgOpen = false;
          this.load();
        },
        error: (e) => this.handleSaveError(e),
      });
    }
  }

  private handleSaveError(error: any) {
    const message =
      typeof error?.error === 'string'
        ? error.error
        : (error?.error?.title ?? error?.error?.detail ?? 'Save failed');
    const normalized = typeof message === 'string' ? message.toLowerCase() : '';

    if (error?.status === 409 || normalized.includes('code must be unique')) {
      this.dlgFieldErrors.code = 'Test code must be unique.';
      return;
    }

    alert(message);
  }

  toggle(r: TestRow) {
    const newVal = !r.isActive;
    this.http
      .patch(`/api/tests/${r.id}/status`, newVal, {
        headers: { 'Content-Type': 'application/json' },
      })
      .subscribe({
        next: () => (r.isActive = newVal),
        error: (e) => alert(e?.error ?? 'Toggle failed'),
      });
  }

  remove(r: TestRow) {
    if (!confirm(`Delete "${r.name}"?`)) return;
    this.http
      .delete(`/api/tests/${r.id}`)
      .subscribe({ next: () => this.load(), error: (e) => alert(e?.error ?? 'Delete failed') });
  }

  // ===== Parameters manager =====
  openParams(r: TestRow) {
    this.currentTestId = r.id;
    this.paramTitle = `Parameters — ${r.code} / ${r.name}`;
    this.paramModel = {
      name: '',
      unitId: null,
      sortOrder: (this.params.at(-1)?.sortOrder ?? 0) + 1,
    };
    this.paramOpen = true;
    this.reloadParams();
  }
  closeParams() {
    this.paramOpen = false;
    this.params = [];
    this.currentTestId = null;
  }

  reloadParams() {
    if (!this.currentTestId) return;
    this.http
      .get<ParamRow[]>(`/api/tests/${this.currentTestId}/parameters`)
      .subscribe((res) => (this.params = (res ?? []).sort((a, b) => a.sortOrder - b.sortOrder)));
  }

  saveParam(model: any) {
    if (!this.currentTestId) return;
    // If model has 'testParameterId' => update; else create
    if (model.testParameterId) {
      this.http
        .put(`/api/tests/${this.currentTestId}/parameters/${model.testParameterId}`, model)
        .subscribe({
          next: () => {
            this.paramModel = { name: '', unitId: null, sortOrder: 1 };
            this.reloadParams();
          },
          error: (e) => alert(e?.error ?? 'Update failed'),
        });
    } else {
      this.http.post(`/api/tests/${this.currentTestId}/parameters`, model).subscribe({
        next: () => {
          this.paramModel = { name: '', unitId: null, sortOrder: 1 };
          this.reloadParams();
        },
        error: (e) => alert(e?.error ?? 'Create failed'),
      });
    }
  }

  editParam(p: ParamRow) {
    // clone param into form model
    this.paramModel = { ...p };
  }

  deleteParam(p: ParamRow) {
    if (!this.currentTestId) return;
    if (!confirm(`Delete param "${p.name}"?`)) return;
    this.http.delete(`/api/tests/${this.currentTestId}/parameters/${p.testParameterId}`).subscribe({
      next: () => this.reloadParams(),
      error: (e) => alert(e?.error ?? 'Delete failed'),
    });
  }

  unitById(id: number | null | undefined) {
    return id == null ? null : this.units.find((u) => u.id === id) || null;
  }
}
