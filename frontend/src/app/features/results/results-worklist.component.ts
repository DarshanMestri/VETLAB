import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';

type WorkItem = {
  registrationItemId: number;
  registrationId: number;
  ownerName: string;
  ownerNameAr?: string;
  ownerMobile?: string;
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
  animalNames?: string;
  animalCount?: number;
  rerunReason?: string;
};

type WorkGroup = {
  registrationId: number;
  ownerName: string;
  ownerNameAr?: string;
  ownerMobile?: string;
  animalNames: string;
  rows: WorkItem[];
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
          <label class="form-label">Owner (name / mobile)</label>
          <div class="input-group input-group-sm">
            <input
              class="form-control"
              [(ngModel)]="ownerFilter"
              name="ownerFilter"
              (keyup.enter)="reload()"
              placeholder="Search owner by name or mobile…"
            />
            <button class="btn btn-outline-primary" type="button" (click)="reload()">Search</button>
            <button
              class="btn btn-outline-secondary"
              type="button"
              (click)="clearOwnerFilter()"
              [disabled]="!ownerFilter"
            >✕</button>
          </div>
        </div>
        <div class="col-md-2">
          <label class="form-label">Department ID</label>
          <input class="form-control form-control-sm" type="number" [(ngModel)]="departmentId" name="departmentId" (change)="reload()" />
        </div>
        <div class="col-md-2">
          <label class="form-label">Test ID</label>
          <input class="form-control form-control-sm" type="number" [(ngModel)]="testId" name="testId" (change)="reload()" />
        </div>
        <div class="col-md-2">
          <label class="form-label">Barcode</label>
          <input class="form-control form-control-sm" [(ngModel)]="barcode" name="barcode" (change)="reload()" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select class="form-select form-select-sm" [(ngModel)]="status" name="status" (change)="reload()">
            <option value="">Pending + InProgress</option>
            <option>Pending</option>
            <option>InProgress</option>
            <option>Completed</option>
          </select>
        </div>
      </div>

      <table class="table table-sm table-striped align-middle" *ngIf="rows.length">
        <thead>
          <tr>
            <th>Barcode</th>
            <th>Owner</th>
            <th>Test</th>
            <th>Species</th>
            <th>Animals</th>
            <th>Lab Status</th>
            <th>Validation</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody *ngFor="let group of groups">
          <tr class="table-primary">
            <td colspan="8">
              <strong>{{ group.ownerName }}</strong>
              <span *ngIf="group.ownerNameAr" class="ms-2" dir="rtl">{{ group.ownerNameAr }}</span>
              <small *ngIf="group.ownerMobile" class="text-muted ms-2">Mobile: {{ group.ownerMobile }}</small>
              <span *ngIf="group.animalNames" class="text-muted ms-2">Animals: {{ group.animalNames }}</span>
            </td>
          </tr>
          <tr *ngFor="let row of group.rows">
            <td>{{ row.itemBarcode }}</td>
            <td>{{ row.ownerName }}</td>
            <td>{{ row.testCode }} - {{ row.testName }}</td>
            <td>{{ row.speciesName || 'Any' }}</td>
            <td>
              <small>{{ row.animalNames || '-' }}</small>
            </td>
            <td>
              <div>
                {{ row.status }}
                <span *ngIf="row.rerunReason" class="badge ms-1" style="background-color:rgba(255,148,120,.18);color:#b85c43;font-weight:600">Re-run</span>
              </div>
              <small class="text-muted">Sample: {{ row.sampleStatus }}</small>
            </td>
            <td>
              <small>
                Tech: {{ row.technicianVerified ? 'Done' : 'Pending' }}<br />
                Path: {{ row.pathologistAuthenticated ? 'Done' : 'Pending' }}
              </small>
            </td>
            <td class="d-flex flex-wrap gap-1">
              <button
                class="btn btn-outline-secondary btn-sm"
                type="button"
                [disabled]="row.sampleStatus !== 'NotCollected'"
                title="Sample not yet collected"
                (click)="updateSample(row.registrationItemId, 'collect')"
              >Collect</button>
              <button
                class="btn btn-outline-secondary btn-sm"
                type="button"
                [disabled]="row.sampleStatus !== 'Collected'"
                title="Collect the sample first, or already received"
                (click)="updateSample(row.registrationItemId, 'receive')"
              >Receive</button>
              <button
                class="btn btn-outline-secondary btn-sm"
                type="button"
                [disabled]="row.sampleStatus !== 'Received'"
                title="Receive the sample first, or already processed"
                (click)="updateSample(row.registrationItemId, 'process')"
              >Process</button>
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
  ownerFilter = '';
  departmentId?: number;
  testId?: number;
  barcode = '';
  status = '';

  ngOnInit() {
    this.reload();
  }

  get groups(): WorkGroup[] {
    const map = new Map<number, WorkGroup>();
    for (const row of this.rows) {
      let group = map.get(row.registrationId);
      if (!group) {
        group = {
          registrationId: row.registrationId,
          ownerName: row.ownerName,
          ownerNameAr: row.ownerNameAr,
          ownerMobile: row.ownerMobile,
          animalNames: row.animalNames ?? '',
          rows: [],
        };
        map.set(row.registrationId, group);
      }
      group.rows.push(row);
    }
    return [...map.values()];
  }

  reload() {
    const params = new URLSearchParams();
    if (this.ownerFilter.trim()) params.set('owner', this.ownerFilter.trim());
    if (this.departmentId) params.set('departmentId', String(this.departmentId));
    if (this.testId) params.set('testId', String(this.testId));
    if (this.barcode.trim()) params.set('barcode', this.barcode.trim());
    if (this.status) params.set('status', this.status);

    this.http.get<WorkItem[]>(`/api/results/worklist?${params.toString()}`).subscribe((rows) => {
      this.rows = rows ?? [];
    });
  }

  clearOwnerFilter() {
    this.ownerFilter = '';
    this.reload();
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
