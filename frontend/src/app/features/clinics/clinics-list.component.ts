import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClinicsService, ClinicListItem } from './clinics.service';

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
            <tr><td colspan="6" class="text-center text-muted">No clinics found.</td></tr>
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
                <button class="btn btn-primary" (click)="save()" [disabled]="!form.code || !form.name">{{editing ? 'Update' : 'Create'}}</button>
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
  form = { code: '', name: '', shortName: '', phone: '', email: '', address: '', isActive: true };

  constructor(private svc: ClinicsService) {}

  ngOnInit() { this.load(); }

  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  load() {
    this.svc.list({ search: this.search, page: this.page, pageSize: this.pageSize }).subscribe(r => {
      this.items = r.items;
      this.total = r.total;
    });
  }

  goPage(p: number) { this.page = p; this.load(); }

  openCreate() {
    this.editing = false;
    this.form = { code: '', name: '', shortName: '', phone: '', email: '', address: '', isActive: true };
    this.showForm = true;
  }

  openEdit(item: ClinicListItem) {
    this.editing = true;
    this.editingId = item.id;
    this.form = { code: item.code, name: item.name, shortName: item.shortName || '', phone: item.phone || '', email: item.email || '', address: item.address || '', isActive: item.isActive };
    this.showForm = true;
  }

  save() {
    const req = { ...this.form };
    const op = this.editing ? this.svc.update(this.editingId, req) : this.svc.create(req);
    op.subscribe(() => { this.showForm = false; this.load(); });
  }

  toggleStatus(item: ClinicListItem) {
    this.svc.toggleStatus(item.id, !item.isActive).subscribe(() => this.load());
  }
}
