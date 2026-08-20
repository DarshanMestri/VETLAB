import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';

type Row = {
  id: number;
  ownerName: string;
  clientId?: number;
  barcode: string;
  createdAt: string;
  totalAmount: number;
  paidAmount: number;
  balance: number;
  discountAmount: number;
  paymentStatus: string;
};
type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

@Component({
  standalone: true,
  selector: 'app-registrations-list',
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center">
        <h2>Registrations</h2>
        <a class="btn btn-primary btn-sm" [routerLink]="['/registrations', 'new']">+ New</a>
      </div>

      <!-- Filters -->
      <div class="card mt-3">
        <div class="card-body py-2">
          <div class="row g-2 align-items-end">
            <div class="col-md-3">
              <label class="form-label small fw-semibold">Search</label>
              <input class="form-control form-control-sm" [(ngModel)]="search" name="search" placeholder="Owner, barcode..." (keyup.enter)="loadData()" />
            </div>
            <div class="col-md-2">
              <label class="form-label small fw-semibold">Date From</label>
              <input class="form-control form-control-sm" type="date" [(ngModel)]="dateFrom" name="dateFrom" />
            </div>
            <div class="col-md-2">
              <label class="form-label small fw-semibold">Date To</label>
              <input class="form-control form-control-sm" type="date" [(ngModel)]="dateTo" name="dateTo" />
            </div>
            <div class="col-auto">
              <button class="btn btn-sm btn-primary" (click)="loadData()">Search</button>
            </div>
            <div class="col-auto">
              <button class="btn btn-sm btn-outline-secondary" (click)="clearFilters()">Clear</button>
            </div>
          </div>
        </div>
      </div>

      <table class="table table-sm table-striped mt-3" *ngIf="rows.length">
        <thead>
          <tr>
            <th>ID</th>
            <th>Date</th>
            <th>Owner</th>
            <th>Barcode</th>
            <th>Total</th>
            <th>Discount</th>
            <th>Paid</th>
            <th>Balance</th>
            <th>Status</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows"
              [class.table-danger]="r.paymentStatus === 'Unpaid'"
              [class.table-success]="r.paymentStatus === 'Paid'"
              [class.table-secondary]="r.paymentStatus === 'Partially Paid'">
            <td>{{ r.id }}</td>
            <td>{{ r.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
            <td>{{ r.ownerName }}</td>
            <td>{{ r.barcode }}</td>
            <td>{{ r.totalAmount | currency:'AED' }}</td>
            <td>{{ r.discountAmount ? (r.discountAmount | currency:'AED') : '-' }}</td>
            <td>{{ r.paidAmount | currency:'AED' }}</td>
            <td>{{ r.balance | currency:'AED' }}</td>
            <td>
              <span class="badge"
                [class.bg-danger]="r.paymentStatus === 'Unpaid'"
                [class.bg-warning text-dark]="r.paymentStatus === 'Partially Paid'"
                [class.bg-success]="r.paymentStatus === 'Paid'">
                {{ r.paymentStatus || 'Unpaid' }}
              </span>
            </td>
            <td class="text-end">
              <a class="btn btn-sm btn-outline-primary me-1" [routerLink]="['/registrations', r.id]">Edit</a>
              <button class="btn btn-sm btn-outline-secondary me-1" type="button" (click)="reprintBarcode(r.id)">Reprint Barcode</button>
              <button class="btn btn-sm btn-outline-success me-1" type="button" (click)="sendInvoice(r.id)">WhatsApp Invoice</button>
              <button class="btn btn-sm btn-outline-success" type="button" (click)="sendReceipt(r.id)">WhatsApp Receipt</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!rows.length">No registrations.</p>

      <!-- Pagination -->
      <div class="d-flex justify-content-between align-items-center mt-2" *ngIf="total > 0">
        <small class="text-muted">Showing {{ page }} of {{ totalPages }} pages ({{ total }} total)</small>
        <div class="btn-group btn-group-sm">
          <button class="btn btn-outline-secondary" [disabled]="page <= 1" (click)="prevPage()">Previous</button>
          <button class="btn btn-outline-secondary" [disabled]="page * pageSize >= total" (click)="nextPage()">Next</button>
        </div>
      </div>
    </div>
  `,
})
export class RegistrationsListComponent {
  Math = Math;
  private http = inject(HttpClient);
  rows: Row[] = [];
  total = 0;
  page = 1;
  pageSize = 20;

  search = '';
  dateFrom = '';
  dateTo = '';

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    const params: any = { page: this.page, pageSize: this.pageSize };
    if (this.search?.trim()) params.search = this.search.trim();
    if (this.dateFrom) params.dateFrom = this.dateFrom;
    if (this.dateTo) params.dateTo = this.dateTo;

    this.http
      .get<Paged<Row>>('/api/registrations', { params })
      .subscribe((res) => {
        this.rows = res.items ?? [];
        this.total = res.total;
      });
  }

  clearFilters() {
    this.search = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.page = 1;
    this.loadData();
  }

  prevPage() {
    if (this.page > 1) { this.page--; this.loadData(); }
  }

  nextPage() {
    if (this.page * this.pageSize < this.total) { this.page++; this.loadData(); }
  }

  private extractError(err: any, fallback: string): string {
    if (typeof err?.error === 'string') return err.error;
    return err?.error?.title
      || (err?.error?.errors ? Object.values(err.error.errors).flat().join(' ') : null)
      || err?.error?.message
      || fallback;
  }

  sendInvoice(registrationId: number) {
    this.http.post<{ message: string }>(`/api/whatsapp/send/invoice/${registrationId}`, {}).subscribe({
      next: () => alert('Invoice queued for WhatsApp delivery.'),
      error: (err) => alert(this.extractError(err, 'Unable to queue invoice.'))
    });
  }

  sendReceipt(registrationId: number) {
    this.http.post<{ message: string }>(`/api/whatsapp/send/receipt/${registrationId}`, {}).subscribe({
      next: () => alert('Receipt queued for WhatsApp delivery.'),
      error: (err) => alert(this.extractError(err, 'Unable to queue receipt.'))
    });
  }

  reprintBarcode(registrationId: number) {
    this.http.get(`/api/registrations/${registrationId}/barcodes/pdf`, {
      params: { count: 2 },
      responseType: 'blob',
    }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => alert('Failed to generate barcode PDF.'),
    });
  }
}
