import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';

type Row = {
  id: number;
  ownerName: string;
  clientId?: number;
  barcode: string;
  totalAmount: number;
  paidAmount: number;
  balance: number;
};
type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

@Component({
  standalone: true,
  selector: 'app-registrations-list',
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center">
        <h2>Registrations</h2>
        <a class="btn btn-primary btn-sm" [routerLink]="['/registrations', 'new']">+ New</a>
      </div>
      <table class="table table-sm table-striped mt-3" *ngIf="rows.length">
        <thead>
          <tr>
            <th>ID</th>
            <th>Owner</th>
            <th>Barcode</th>
            <th>Total</th>
            <th>Paid</th>
            <th>Balance</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows">
            <td>{{ r.id }}</td>
            <td>{{ r.ownerName }}</td>
            <td>{{ r.barcode }}</td>
            <td>{{ r.totalAmount }}</td>
            <td>{{ r.paidAmount }}</td>
            <td>{{ r.balance }}</td>
            <td class="text-end">
              <button class="btn btn-sm btn-outline-success me-1" type="button" (click)="sendInvoice(r.id)">WhatsApp Invoice</button>
              <button class="btn btn-sm btn-outline-success" type="button" (click)="sendReceipt(r.id)">WhatsApp Receipt</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!rows.length">No registrations.</p>
    </div>
  `,
})
export class RegistrationsListComponent {
  private http = inject(HttpClient);
  rows: Row[] = [];
  ngOnInit() {
    this.http
      .get<Paged<Row>>(`/api/registrations?page=1&pageSize=50`)
      .subscribe((res) => (this.rows = res.items ?? []));
  }

  sendInvoice(registrationId: number) {
    this.http.post<{ message: string }>(`/api/whatsapp/send/invoice/${registrationId}`, {}).subscribe({
      next: () => alert('Invoice queued for WhatsApp delivery.'),
      error: (err) => alert(err?.error ?? 'Unable to queue invoice.')
    });
  }

  sendReceipt(registrationId: number) {
    this.http.post<{ message: string }>(`/api/whatsapp/send/receipt/${registrationId}`, {}).subscribe({
      next: () => alert('Receipt queued for WhatsApp delivery.'),
      error: (err) => alert(err?.error ?? 'Unable to queue receipt.')
    });
  }
}
