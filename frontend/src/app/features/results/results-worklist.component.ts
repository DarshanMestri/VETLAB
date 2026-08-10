import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';

type WorkItem = {
  registrationItemId: number;
  registrationId: number;
  ownerName: string;
  itemBarcode: string;
  testCode: string;
  testName: string;
  speciesId?: number;
  speciesName?: string;
  status: string;
  sampleTypeId?: number | null;
  sampleStatus: string;
  technicianVerified: boolean;
  pathologistAuthenticated: boolean;
};

@Component({
  standalone: true,
  selector: 'app-results-worklist',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="mb-0">Result Worklist</h2>
        <button class="btn btn-primary btn-sm" type="button" (click)="reload()">Refresh</button>
      </div>

      <div class="row g-2 mb-3">
        <div class="col-md-3">
          <label class="form-label">Department ID</label>
          <input class="form-control" type="number" [(ngModel)]="departmentId" name="departmentId" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Test ID</label>
          <input class="form-control" type="number" [(ngModel)]="testId" name="testId" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Barcode</label>
          <input class="form-control" [(ngModel)]="barcode" name="barcode" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select class="form-select" [(ngModel)]="status" name="status">
            <option value="">Pending + InProgress</option>
            <option>Pending</option>
            <option>InProgress</option>
            <option>Completed</option>
          </select>
        </div>
      </div>

      <table class="table table-sm table-striped" *ngIf="rows.length">
        <thead>
          <tr>
            <th>Barcode</th>
            <th>Owner</th>
            <th>Test</th>
            <th>Species</th>
            <th>Lab Status</th>
            <th>Validation</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of rows">
            <td>{{ row.itemBarcode }}</td>
            <td>{{ row.ownerName }}</td>
            <td>{{ row.testCode }} - {{ row.testName }}</td>
            <td>{{ row.speciesName || '-' }}</td>
            <td>
              <div>{{ row.status }}</div>
              <small class="text-muted">Sample: {{ row.sampleStatus }}</small>
            </td>
            <td>
              <small>
                Tech: {{ row.technicianVerified ? 'Done' : 'Pending' }}<br />
                Path: {{ row.pathologistAuthenticated ? 'Done' : 'Pending' }}
              </small>
            </td>
            <td class="d-flex flex-wrap gap-1">
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="updateSample(row.registrationItemId, 'collect')">Collect</button>
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="updateSample(row.registrationItemId, 'receive')">Receive</button>
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="updateSample(row.registrationItemId, 'process')">Process</button>
              <a class="btn btn-outline-primary btn-sm" [routerLink]="['/results', row.registrationItemId]">Enter</a>
              <button class="btn btn-outline-dark btn-sm" type="button" (click)="downloadReport(row.registrationItemId, 'pdf')">PDF</button>
              <button class="btn btn-outline-dark btn-sm" type="button" (click)="downloadReport(row.registrationItemId, 'png')">PNG</button>
              <button class="btn btn-outline-dark btn-sm" type="button" (click)="downloadReport(row.registrationItemId, 'jpg')">JPG</button>
              <button class="btn btn-outline-success btn-sm" type="button" (click)="queueReport(row.registrationItemId, 'pdf')">WA PDF</button>
              <button class="btn btn-outline-success btn-sm" type="button" (click)="queueReport(row.registrationItemId, 'jpg')">WA JPG</button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="!rows.length" class="mt-3">No items found.</p>
    </div>
  `,
})
export class ResultsWorklistComponent {
  private http = inject(HttpClient);

  rows: WorkItem[] = [];
  departmentId?: number;
  testId?: number;
  barcode = '';
  status = '';

  ngOnInit() {
    this.reload();
  }

  reload() {
    const params = new URLSearchParams();
    if (this.departmentId) params.set('departmentId', String(this.departmentId));
    if (this.testId) params.set('testId', String(this.testId));
    if (this.barcode.trim()) params.set('barcode', this.barcode.trim());
    if (this.status) params.set('status', this.status);

    this.http.get<WorkItem[]>(`/api/results/worklist?${params.toString()}`).subscribe((rows) => {
      this.rows = rows ?? [];
    });
  }

  updateSample(registrationItemId: number, action: 'collect' | 'receive' | 'process') {
    this.http.post(`/api/registrations/items/${registrationItemId}/${action}`, { notes: null }).subscribe({
      next: () => this.reload(),
      error: (err) => alert(err?.error ?? `Unable to ${action} sample.`),
    });
  }

  downloadReport(registrationItemId: number, format: 'pdf' | 'png' | 'jpg') {
    this.http
      .get(`/api/results/${registrationItemId}/report?format=${format}`, {
        observe: 'response',
        responseType: 'blob',
      })
      .subscribe({
        next: (response) => this.saveFile(response, format),
        error: (err) => alert(err?.error ?? `Unable to download ${format.toUpperCase()} report.`),
      });
  }

  queueReport(registrationItemId: number, format: 'pdf' | 'jpg') {
    this.http.post(`/api/whatsapp/send/report/${registrationItemId}`, { format }).subscribe({
      next: () => alert(`WhatsApp ${format.toUpperCase()} report queued.`),
      error: (err) => alert(err?.error ?? `Unable to queue WhatsApp ${format.toUpperCase()} report.`),
    });
  }

  private saveFile(response: HttpResponse<Blob>, fallbackExtension: string) {
    const disposition = response.headers.get('content-disposition') ?? '';
    const fileNameMatch = disposition.match(/filename=\"?([^\";]+)\"?/i);
    const fileName = fileNameMatch?.[1] ?? `result-report.${fallbackExtension}`;
    const blob = response.body;

    if (!blob) {
      alert('Downloaded report was empty.');
      return;
    }

    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
