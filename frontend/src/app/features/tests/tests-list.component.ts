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
  sampleTypeId?: number | null;
  valueType: number;
  isActive: boolean;
};
type Dept = { id: number; name: string };
type Unit = { id: number; name: string; symbol: string };
type SampleType = { id: number; name: string };
type ParamRow = {
  id: number;
  testId: number;
  name: string;
  unitId: number | null;
  unitSymbol?: string | null;
  sortOrder: number;
};
type RangeRow = {
  id: number;
  testParameterId: number;
  speciesId: number;
  ageFromDays: number | null;
  ageToDays: number | null;
  low: number | null;
  high: number | null;
  note: string | null;
  isActive: boolean;
};
type Species = { id: number; name: string };

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
            <th>Sample Type</th>
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
            <td>{{ sampleTypeName(r.sampleTypeId) }}</td>
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
        <label class="form-label">Sample Type</label>
        <select class="form-select" [(ngModel)]="dlgModel.sampleTypeId" name="sampleTypeId">
          <option [ngValue]="null">(none)</option>
          <option *ngFor="let st of sampleTypes" [ngValue]="st.id">{{ st.name }}</option>
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
      [saving]="paramSaving"
      okText="Save Param"
      (submit)="saveParam()"
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
          <button type="button" class="btn btn-sm btn-outline-secondary" (click)="reloadParams()">Reload</button>
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
              <td>{{ p.id }}</td>
              <td>{{ p.name }}</td>
              <td>{{ unitById(p.unitId)?.symbol || '-' }}</td>
              <td>{{ p.sortOrder }}</td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-info me-1" (click)="openRanges(p)">
                  Ranges
                </button>
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

    <!-- Reference Range manager -->
    <app-simple-dialog
      *ngIf="rangeOpen"
      [title]="rangeTitle"
      [model]="rangeModel"
      [saving]="rangeSaving"
      okText="Save Range"
      (submit)="saveRange()"
      (closed)="closeRanges()"
    >
      <div class="mb-2">
        <label class="form-label">Species</label>
        <select class="form-select" [(ngModel)]="rangeModel.speciesId" name="speciesId" required>
          <option [ngValue]="null" disabled>Select species...</option>
          <option *ngFor="let s of speciesList" [ngValue]="s.id">{{ s.name }}</option>
        </select>
      </div>
      <div class="row mb-2">
        <div class="col">
          <label class="form-label">Age From (days)</label>
          <input class="form-control" type="number" [(ngModel)]="rangeModel.ageFromDays" name="ageFromDays" />
        </div>
        <div class="col">
          <label class="form-label">Age To (days)</label>
          <input class="form-control" type="number" [(ngModel)]="rangeModel.ageToDays" name="ageToDays" />
        </div>
      </div>
      <div class="row mb-2">
        <div class="col">
          <label class="form-label">Low</label>
          <input class="form-control" type="number" step="any" [(ngModel)]="rangeModel.low" name="low" />
        </div>
        <div class="col">
          <label class="form-label">High</label>
          <input class="form-control" type="number" step="any" [(ngModel)]="rangeModel.high" name="high" />
        </div>
      </div>
      <div class="mb-2">
        <label class="form-label">Note</label>
        <input class="form-control" [(ngModel)]="rangeModel.note" name="rangeNote" />
      </div>
      <div class="form-check mb-2">
        <input type="checkbox" class="form-check-input" [(ngModel)]="rangeModel.isActive" name="rangeActive" id="rangeActiveChk" />
        <label for="rangeActiveChk" class="form-check-label">Active</label>
      </div>

      <div class="border rounded p-2 mt-3">
        <div class="d-flex justify-content-between align-items-center">
          <strong>Existing Ranges</strong>
          <button type="button" class="btn btn-sm btn-outline-secondary" (click)="reloadRanges()">Reload</button>
        </div>
        <table class="table table-sm mt-2" *ngIf="ranges.length">
          <thead>
            <tr>
              <th>Species</th>
              <th>Age Range</th>
              <th>Low</th>
              <th>High</th>
              <th>Note</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of ranges">
              <td>{{ speciesName(r.speciesId) }}</td>
              <td>{{ r.ageFromDays ?? '-' }} — {{ r.ageToDays ?? '-' }} days</td>
              <td>{{ r.low ?? '-' }}</td>
              <td>{{ r.high ?? '-' }}</td>
              <td>{{ r.note || '-' }}</td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-primary me-1" (click)="editRange(r)">Edit</button>
                <button class="btn btn-sm btn-outline-danger" (click)="deleteRange(r)">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
        <p *ngIf="!ranges.length" class="mb-0">No reference ranges yet.</p>
      </div>
    </app-simple-dialog>
  `,
})
export class TestsListComponent {
  private http = inject(HttpClient);

  rows: TestRow[] = [];
  depts: Dept[] = [];
  units: Unit[] = [];
  sampleTypes: SampleType[] = [];
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
    sampleTypeId: null,
    valueType: 1,
    isActive: true,
  };
  dlgFieldErrors = { departmentId: '', code: '', name: '', displayName: '' };
  editingId: number | null = null;

  // Params dialog
  paramOpen = false;
  paramTitle = 'Parameters';
  paramModel: any = { name: '', unitId: null, sortOrder: 1 };
  paramSaving = false;
  currentTestId: number | null = null;
  params: ParamRow[] = [];

  // Ranges dialog
  rangeOpen = false;
  rangeTitle = 'Reference Ranges';
  rangeModel: any = { speciesId: null, ageFromDays: null, ageToDays: null, low: null, high: null, note: '', isActive: true };
  rangeSaving = false;
  editingRangeId: number | null = null;
  rangeParamId: number | null = null;
  ranges: RangeRow[] = [];

  // Lookup data
  speciesList: Species[] = [];

  ngOnInit() {
    this.load();
    this.loadDepts();
    this.loadUnits();
    this.loadSampleTypes();
    this.loadSpecies();
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
  loadSpecies() {
    this.http
      .get<any>('/api/species?page=1&pageSize=999')
      .subscribe((res) => (this.speciesList = res?.items ?? []));
  }
  loadSampleTypes() {
    this.http
      .get<any>('/api/mastercatalog/sampletypes?page=1&pageSize=999')
      .subscribe((res) => (this.sampleTypes = res?.items ?? []));
  }
  deptName(id: number) {
    return this.depts.find((d) => d.id === id)?.name || id;
  }
  sampleTypeName(id?: number | null) {
    return id ? this.sampleTypes.find((s) => s.id === id)?.name || '-' : '-';
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
      sampleTypeId: null,
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
      sampleTypeId: r.sampleTypeId ?? null,
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
      sampleTypeId: model.sampleTypeId == null ? null : Number(model.sampleTypeId),
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
    this.paramSaving = false;
  }

  reloadParams() {
    if (!this.currentTestId) return;
    this.http
      .get<ParamRow[]>(`/api/tests/${this.currentTestId}/parameters`)
      .subscribe((res) => (this.params = (res ?? []).filter((p) => p.name).sort((a, b) => a.sortOrder - b.sortOrder)));
  }

  saveParam() {
    if (!this.currentTestId || this.paramSaving) return;
    const m = this.paramModel;
    const name = m.name?.trim() ?? '';
    if (!name) {
      alert('Parameter name is required.');
      return;
    }
    const dup = this.params.some(
      (p) => p.name.toLowerCase() === name.toLowerCase() && p.id !== m.id
    );
    if (dup) {
      alert('A parameter with this name already exists for this test.');
      return;
    }
    const payload = {
      testId: this.currentTestId,
      name,
      unitId: m.unitId ?? null,
      sortOrder: Number(m.sortOrder) || 0,
      isActive: m.isActive ?? true,
    };
    this.paramSaving = true;
    if (m.id) {
      this.http
        .put(`/api/tests/${this.currentTestId}/parameters/${m.id}`, payload)
        .subscribe({
          next: () => {
            this.paramModel = { name: '', unitId: null, sortOrder: (this.params.at(-1)?.sortOrder ?? 0) + 1 };
            this.paramSaving = false;
            this.reloadParams();
          },
          error: (e) => {
            this.paramSaving = false;
            alert(e?.error?.title ?? e?.error ?? 'Update failed');
          },
        });
    } else {
      this.http.post(`/api/tests/${this.currentTestId}/parameters`, payload).subscribe({
        next: () => {
          this.paramModel = { name: '', unitId: null, sortOrder: (this.params.at(-1)?.sortOrder ?? 0) + 1 };
          this.paramSaving = false;
          this.reloadParams();
        },
        error: (e) => {
          this.paramSaving = false;
          alert(e?.error?.title ?? e?.error ?? 'Create failed');
        },
      });
    }
  }

  editParam(p: ParamRow) {
    this.paramModel = { ...p };
  }

  deleteParam(p: ParamRow) {
    if (!this.currentTestId) return;
    if (!confirm(`Delete param "${p.name}"?`)) return;
    this.http.delete(`/api/tests/${this.currentTestId}/parameters/${p.id}`).subscribe({
      next: () => this.reloadParams(),
      error: (e) => alert(e?.error ?? 'Delete failed'),
    });
  }

  unitById(id: number | null | undefined) {
    return id == null ? null : this.units.find((u) => u.id === id) || null;
  }

  // ===== Reference Ranges manager =====
  openRanges(p: ParamRow) {
    this.rangeParamId = p.id;
    this.rangeTitle = `Ranges — ${p.name}`;
    this.rangeModel = { speciesId: null, ageFromDays: null, ageToDays: null, low: null, high: null, note: '', isActive: true };
    this.editingRangeId = null;
    this.paramOpen = false;
    this.rangeOpen = true;
    this.reloadRanges();
  }
  closeRanges() {
    this.rangeOpen = false;
    this.ranges = [];
    this.rangeParamId = null;
    this.editingRangeId = null;
    this.rangeSaving = false;
  }

  reloadRanges() {
    if (!this.rangeParamId) return;
    this.http
      .get<RangeRow[]>(`/api/tests/parameters/${this.rangeParamId}/ranges`)
      .subscribe((res) => (this.ranges = res ?? []));
  }

  saveRange() {
    if (!this.rangeParamId || this.rangeSaving) return;
    const m = this.rangeModel;
    if (!m.speciesId) {
      alert('Species is required.');
      return;
    }
    const payload = {
      testParameterId: this.rangeParamId,
      speciesId: Number(m.speciesId),
      ageFromDays: m.ageFromDays != null && m.ageFromDays !== '' ? Number(m.ageFromDays) : null,
      ageToDays: m.ageToDays != null && m.ageToDays !== '' ? Number(m.ageToDays) : null,
      low: m.low != null && m.low !== '' ? Number(m.low) : null,
      high: m.high != null && m.high !== '' ? Number(m.high) : null,
      note: m.note?.trim() || null,
      isActive: m.isActive ?? true,
    };
    this.rangeSaving = true;
    if (this.editingRangeId) {
      this.http
        .put(`/api/tests/parameters/${this.rangeParamId}/ranges/${this.editingRangeId}`, payload)
        .subscribe({
          next: () => {
            this.editingRangeId = null;
            this.rangeModel = { speciesId: null, ageFromDays: null, ageToDays: null, low: null, high: null, note: '', isActive: true };
            this.rangeSaving = false;
            this.reloadRanges();
          },
          error: (e) => {
            this.rangeSaving = false;
            alert(e?.error?.title ?? e?.error ?? 'Update failed');
          },
        });
    } else {
      this.http
        .post(`/api/tests/parameters/${this.rangeParamId}/ranges`, payload)
        .subscribe({
          next: () => {
            this.rangeModel = { speciesId: null, ageFromDays: null, ageToDays: null, low: null, high: null, note: '', isActive: true };
            this.rangeSaving = false;
            this.reloadRanges();
          },
          error: (e) => {
            this.rangeSaving = false;
            alert(e?.error?.title ?? e?.error ?? 'Create failed');
          },
        });
    }
  }

  editRange(r: RangeRow) {
    this.editingRangeId = r.id;
    this.rangeModel = {
      speciesId: r.speciesId,
      ageFromDays: r.ageFromDays,
      ageToDays: r.ageToDays,
      low: r.low,
      high: r.high,
      note: r.note || '',
      isActive: r.isActive,
    };
  }

  deleteRange(r: RangeRow) {
    if (!this.rangeParamId) return;
    if (!confirm(`Delete this reference range?`)) return;
    this.http.delete(`/api/tests/parameters/${this.rangeParamId}/ranges/${r.id}`).subscribe({
      next: () => this.reloadRanges(),
      error: (e) => alert(e?.error ?? 'Delete failed'),
    });
  }

  speciesName(id: number) {
    return this.speciesList.find((s) => s.id === id)?.name || id;
  }
}
