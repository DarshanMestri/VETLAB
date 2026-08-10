import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

type PackageDto = { id: number; code: string; name: string; isActive: boolean };
type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

@Component({
  standalone: true,
  selector: 'app-packages-list',
  imports: [CommonModule],
  template: `
    <div class="container py-3">
      <h2>Packages</h2>
      <button class="btn btn-sm btn-primary me-2" (click)="reload()">Reload</button>
      <table class="table table-sm table-striped mt-3" *ngIf="rows.length">
        <thead>
          <tr>
            <th>ID</th>
            <th>Code</th>
            <th>Name</th>
            <th>Active</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows">
            <td>{{ r.id }}</td>
            <td>{{ r.code }}</td>
            <td>{{ r.name }}</td>
            <td>{{ r.isActive }}</td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!rows.length">No packages.</p>
    </div>
  `,
})
export class PackagesListComponent {
  private http = inject(HttpClient);
  rows: PackageDto[] = [];
  reload() {
    this.http
      .get<Paged<PackageDto>>(`/api/packages?page=1&pageSize=50`)
      .subscribe((res) => (this.rows = res.items ?? []));
  }
  ngOnInit() {
    this.reload();
  }
}
