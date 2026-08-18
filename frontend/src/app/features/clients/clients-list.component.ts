import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SimpleDialogComponent } from '../../shared/simple-dialog.component';

type Row = { id: number; code: string; name: string; isActive: boolean };

@Component({
  standalone: true,
  selector: 'app-clients-list',
  imports: [CommonModule, FormsModule, SimpleDialogComponent],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center">
        <h2>Clients</h2>
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

      <p *ngIf="!rows.length" class="mt-3">No clients found.</p>
    </div>

    <!-- Add/Edit dialog -->
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
      </div>
      <div class="mb-2">
        <label class="form-label">Name</label>
        <input class="form-control" [(ngModel)]="dlgModel.name" name="name" required />
      </div>
      <div class="form-check">
        <input
          type="checkbox"
          class="form-check-input"
          [(ngModel)]="dlgModel.isActive"
          name="isActive"
          id="activeChk"
        />
        <label for="activeChk" class="form-check-label">Active</label>
      </div>
    </app-simple-dialog>
  `,
})
export class ClientsListComponent {
  private http = inject(HttpClient);
  rows: Row[] = [];
  search = '';

  dlgOpen = false;
  dlgTitle = 'New Client';
  dlgModel: any = { code: '', name: '', isActive: true };
  editingId: number | null = null;

  ngOnInit() {
    this.load();
  }

  load() {
    const p = this.search ? '?search=' + encodeURIComponent(this.search) : '';
    this.http.get<Row[]>(`/api/clients${p}`).subscribe((res) => (this.rows = res ?? []));
  }

  openCreate() {
    this.dlgTitle = 'New Client';
    this.editingId = null;
    this.dlgModel = { code: '', name: '', isActive: true };
    this.dlgOpen = true;
  }

  openEdit(r: Row) {
    this.dlgTitle = `Edit #${r.id}`;
    this.editingId = r.id;
    this.dlgModel = { code: r.code, name: r.name, isActive: r.isActive };
    this.dlgOpen = true;
  }

  private extractError(err: any, fallback: string): string {
    const body = err?.error;
    if (typeof body === 'string') return body || fallback;
    if (body?.message) return body.message;
    if (body?.errors) {
      const msgs = Object.values(body.errors).flat().filter(Boolean);
      if (msgs.length) return msgs.join('\n');
    }
    if (body?.title) return body.title;
    return fallback;
  }

  save(model: any) {
    if (this.editingId == null) {
      this.http.post<Row>('/api/clients', model).subscribe({
        next: (_) => {
          this.dlgOpen = false;
          this.load();
        },
        error: (err) => alert(this.extractError(err, 'Create failed')),
      });
    } else {
      this.http.put(`/api/clients/${this.editingId}`, model).subscribe({
        next: (_) => {
          this.dlgOpen = false;
          this.load();
        },
        error: (err) => alert(this.extractError(err, 'Update failed')),
      });
    }
  }

  toggle(r: Row) {
    const newVal = !r.isActive;
    this.http
      .patch(`/api/clients/${r.id}/status`, newVal, {
        headers: { 'Content-Type': 'application/json' },
      })
      .subscribe({
        next: (_) => {
          r.isActive = newVal;
        },
        error: (err) => alert(this.extractError(err, 'Toggle failed')),
      });
  }
}
