import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

type Row = {
  id: number;
  clientId?: number;
  itemType: number;
  itemId: number;
  rate: number;
  currency?: string;
  isActive?: boolean;
};

@Component({
  standalone: true,
  selector: 'app-rates-list',
  imports: [CommonModule],
  template: `
    <div class="container py-3">
      <h2>Rates</h2>
      <button class="btn btn-sm btn-primary" (click)="load()">Reload</button>
      <table class="table table-sm table-striped mt-3" *ngIf="rows.length">
        <thead>
          <tr>
            <th>ID</th>
            <th>Client</th>
            <th>Type</th>
            <th>Item</th>
            <th>Rate</th>
            <th>Curr</th>
            <th>Active</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows">
            <td>{{ r.id }}</td>
            <td>{{ r.clientId || '-' }}</td>
            <td>{{ r.itemType }}</td>
            <td>{{ r.itemId }}</td>
            <td>{{ r.rate }}</td>
            <td>{{ r.currency }}</td>
            <td>{{ r.isActive }}</td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!rows.length">No rates.</p>
    </div>
  `,
})
export class RatesListComponent {
  private http = inject(HttpClient);
  rows: Row[] = [];
  load() {
    this.http.get<Row[]>('/api/rates').subscribe((res) => (this.rows = res ?? []));
  }
  ngOnInit() {
    this.load();
  }
}
