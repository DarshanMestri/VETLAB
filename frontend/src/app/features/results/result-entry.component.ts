import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpResponse } from '@angular/common/http';

enum ResultEntryType {
  Numeric = 1,
  Text = 2,
  PositiveNegative = 3,
  Formula = 4,
  Image = 5,
  Pdf = 6,
}

type ValueRow = {
  testParameterId: number;
  parameterName: string;
  unitSymbol?: string;
  entryType: ResultEntryType;
  numericValue?: number | null;
  textValue?: string | null;
  positiveNegativeValue?: string | null;
  formulaExpression?: string | null;
  formulaResult?: string | null;
  imageUrl?: string | null;
  pdfAttachmentUrl?: string | null;
  rangeMinDays?: number | null;
  rangeMaxDays?: number | null;
  rangeLow?: number | null;
  rangeHigh?: number | null;
  isLow?: boolean | null;
  isHigh?: boolean | null;
  isOutOfRange?: boolean | null;
};

type Detail = {
  registrationItemId: number;
  itemBarcode: string;
  notes?: string | null;
  sampleStatus: string;
  technicianVerifiedBy?: string | null;
  technicianVerifiedAt?: string | null;
  pathologistAuthenticatedBy?: string | null;
  pathologistAuthenticatedAt?: string | null;
  digitalSignature?: string | null;
  verificationRemarks?: string | null;
  values: ValueRow[];
};

@Component({
  standalone: true,
  selector: 'app-result-entry',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3" *ngIf="detail">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h3 class="mb-1">Result Entry — {{ detail.itemBarcode }}</h3>
          <div class="small text-muted">
            Sample: {{ detail.sampleStatus }} • Technician:
            {{ detail.technicianVerifiedBy || 'Pending' }} • Pathologist:
            {{ detail.pathologistAuthenticatedBy || 'Pending' }}
          </div>
        </div>
        <a class="btn btn-outline-secondary btn-sm" routerLink="/results">Back</a>
      </div>

      <div class="alert alert-danger" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="alert alert-success" *ngIf="successMessage">{{ successMessage }}</div>

      <div class="card p-3 my-3">
        <div class="row g-2">
          <div class="col-md-3">
            <label class="form-label">Age (days)</label>
            <input type="number" class="form-control" [(ngModel)]="ageDays" name="ageDays" />
          </div>
          <div class="col-md-9">
            <label class="form-label">Interpretation</label>
            <textarea class="form-control" rows="2" [(ngModel)]="detail.notes" name="notes"></textarea>
          </div>
        </div>
      </div>

      <div class="card p-3">
        <div class="table-responsive">
          <table class="table table-sm align-middle">
            <thead>
              <tr>
                <th>Parameter</th>
                <th style="width: 20%">Type</th>
                <th style="width: 35%">Result</th>
                <th>Range</th>
                <th>Flag</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let value of detail.values">
                <td>
                  <div>{{ value.parameterName }}</div>
                  <small class="text-muted">{{ value.unitSymbol || 'No unit' }}</small>
                </td>
                <td>
                  <select class="form-select form-select-sm" [(ngModel)]="value.entryType" [name]="'entryType' + value.testParameterId">
                    <option [ngValue]="entryTypes.Numeric">Numeric</option>
                    <option [ngValue]="entryTypes.Text">Text</option>
                    <option [ngValue]="entryTypes.PositiveNegative">Positive / Negative</option>
                    <option [ngValue]="entryTypes.Formula">Formula</option>
                    <option [ngValue]="entryTypes.Image">Image URL</option>
                    <option [ngValue]="entryTypes.Pdf">PDF URL</option>
                  </select>
                </td>
                <td>
                  <ng-container [ngSwitch]="value.entryType">
                    <input *ngSwitchCase="entryTypes.Numeric" type="number" class="form-control form-control-sm" [(ngModel)]="value.numericValue" [name]="'numeric' + value.testParameterId" />
                    <input *ngSwitchCase="entryTypes.Text" type="text" class="form-control form-control-sm" [(ngModel)]="value.textValue" [name]="'text' + value.testParameterId" />
                    <select *ngSwitchCase="entryTypes.PositiveNegative" class="form-select form-select-sm" [(ngModel)]="value.positiveNegativeValue" [name]="'pn' + value.testParameterId">
                      <option [ngValue]="null">Select</option>
                      <option value="Positive">Positive</option>
                      <option value="Negative">Negative</option>
                    </select>
                    <div *ngSwitchCase="entryTypes.Formula" class="d-grid gap-1">
                      <input type="text" class="form-control form-control-sm" [(ngModel)]="value.formulaExpression" [name]="'formulaExpression' + value.testParameterId" placeholder="Formula expression" />
                      <input type="number" class="form-control form-control-sm" [(ngModel)]="value.numericValue" [name]="'formulaNumeric' + value.testParameterId" placeholder="Numeric result" />
                      <input type="text" class="form-control form-control-sm" [(ngModel)]="value.formulaResult" [name]="'formulaResult' + value.testParameterId" placeholder="Display result" />
                    </div>
                    <input *ngSwitchCase="entryTypes.Image" type="url" class="form-control form-control-sm" [(ngModel)]="value.imageUrl" [name]="'image' + value.testParameterId" placeholder="https://..." />
                    <input *ngSwitchCase="entryTypes.Pdf" type="url" class="form-control form-control-sm" [(ngModel)]="value.pdfAttachmentUrl" [name]="'pdf' + value.testParameterId" placeholder="https://..." />
                  </ng-container>
                </td>
                <td>
                  <span *ngIf="value.rangeLow != null || value.rangeHigh != null">
                    {{ value.rangeLow ?? '' }} - {{ value.rangeHigh ?? '' }}
                    <small class="text-muted">({{ value.rangeMinDays ?? '-' }}–{{ value.rangeMaxDays ?? '-' }} d)</small>
                  </span>
                  <span *ngIf="value.rangeLow == null && value.rangeHigh == null">—</span>
                </td>
                <td>
                  <span class="text-danger" *ngIf="value.isLow">LOW</span>
                  <span class="text-danger" *ngIf="value.isHigh">HIGH</span>
                  <span class="text-danger" *ngIf="value.isOutOfRange && !value.isLow && !value.isHigh">OUT</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="row g-3 mt-3">
        <div class="col-lg-6">
          <div class="card p-3 h-100">
            <h5>Verification</h5>
            <div class="row g-2">
              <div class="col-md-6">
                <label class="form-label">Technician</label>
                <input class="form-control" [(ngModel)]="verifiedBy" name="verifiedBy" />
              </div>
              <div class="col-md-6">
                <label class="form-label">Remarks</label>
                <input class="form-control" [(ngModel)]="remarks" name="remarks" />
              </div>
            </div>
            <button class="btn btn-outline-primary btn-sm mt-3" type="button" (click)="verify()">Technician Verify</button>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="card p-3 h-100">
            <h5>Authentication</h5>
            <div class="row g-2">
              <div class="col-md-6">
                <label class="form-label">Pathologist</label>
                <input class="form-control" [(ngModel)]="authenticatedBy" name="authenticatedBy" />
              </div>
              <div class="col-md-6">
                <label class="form-label">Digital Signature</label>
                <input class="form-control" [(ngModel)]="digitalSignature" name="digitalSignature" />
              </div>
            </div>
            <button class="btn btn-outline-success btn-sm mt-3" type="button" (click)="authenticate()">Authenticate</button>
          </div>
        </div>
      </div>

      <div class="d-flex gap-2 mt-3">
        <button class="btn btn-primary" type="button" (click)="save()">Save</button>
        <button class="btn btn-success" type="button" (click)="complete()">Mark Complete</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('pdf')">PDF</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('png')">PNG</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('jpg')">JPG</button>
        <button class="btn btn-outline-success" type="button" (click)="sendReport('pdf')">WhatsApp PDF</button>
        <button class="btn btn-outline-success" type="button" (click)="sendReport('jpg')">WhatsApp JPG</button>
      </div>
    </div>
  `,
})
export class ResultEntryComponent {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  readonly entryTypes = ResultEntryType;

  id!: number;
  detail?: Detail;
  ageDays: number | null = null;
  verifiedBy = '';
  authenticatedBy = '';
  digitalSignature = '';
  remarks = '';
  errorMessage = '';
  successMessage = '';

  ngOnInit() {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    this.reload();
  }

  reload() {
    this.http.get<Detail>(`/api/results/${this.id}`).subscribe((detail) => {
      this.detail = detail;
      this.verifiedBy = detail.technicianVerifiedBy ?? '';
      this.authenticatedBy = detail.pathologistAuthenticatedBy ?? '';
      this.digitalSignature = detail.digitalSignature ?? '';
      this.remarks = detail.verificationRemarks ?? '';
    });
  }

  save() {
    if (!this.detail) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      registrationItemId: this.detail.registrationItemId,
      ageInDays: this.ageDays,
      notes: this.detail.notes ?? null,
      values: this.detail.values.map((value) => ({
        testParameterId: value.testParameterId,
        entryType: value.entryType,
        numericValue: value.numericValue ?? null,
        textValue: value.textValue ?? null,
        positiveNegativeValue: value.positiveNegativeValue ?? null,
        formulaExpression: value.formulaExpression ?? null,
        formulaResult: value.formulaResult ?? null,
        imageUrl: value.imageUrl ?? null,
        pdfAttachmentUrl: value.pdfAttachmentUrl ?? null,
      })),
    };

    this.http.post(`/api/results/${this.id}/enter`, payload).subscribe({
      next: () => {
        this.successMessage = 'Results saved.';
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Save failed.';
      },
    });
  }

  verify() {
    this.errorMessage = '';
    this.successMessage = '';
    this.http.post(`/api/results/${this.id}/verify`, { verifiedBy: this.verifiedBy, remarks: this.remarks }).subscribe({
      next: () => {
        this.successMessage = 'Technician verification saved.';
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Verification failed.';
      },
    });
  }

  authenticate() {
    this.errorMessage = '';
    this.successMessage = '';
    this.http
      .post(`/api/results/${this.id}/authenticate`, {
        authenticatedBy: this.authenticatedBy,
        digitalSignature: this.digitalSignature,
        remarks: this.remarks,
      })
      .subscribe({
        next: () => {
          this.successMessage = 'Pathologist authentication saved.';
          this.reload();
        },
        error: (err) => {
          this.errorMessage = err?.error ?? 'Authentication failed.';
        },
      });
  }

  complete() {
    this.errorMessage = '';
    this.successMessage = '';
    this.http.post(`/api/results/${this.id}/complete`, {}).subscribe({
      next: () => {
        this.successMessage = 'Result marked complete.';
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Complete failed.';
      },
    });
  }

  downloadReport(format: 'pdf' | 'png' | 'jpg') {
    this.errorMessage = '';
    this.successMessage = '';

    this.http
      .get(`/api/results/${this.id}/report?format=${format}`, {
        observe: 'response',
        responseType: 'blob',
      })
      .subscribe({
        next: (response) => this.saveFile(response, format),
        error: (err) => {
          this.errorMessage = err?.error ?? `Unable to download ${format.toUpperCase()} report.`;
        },
      });
  }

  sendReport(format: 'pdf' | 'jpg') {
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post<{ message: string }>(`/api/whatsapp/send/report/${this.id}`, { format }).subscribe({
      next: () => {
        this.successMessage = `WhatsApp ${format.toUpperCase()} report queued successfully.`;
      },
      error: (err) => {
        this.errorMessage = err?.error ?? `Unable to queue WhatsApp ${format.toUpperCase()} report.`;
      }
    });
  }

  private saveFile(response: HttpResponse<Blob>, fallbackExtension: string) {
    const disposition = response.headers.get('content-disposition') ?? '';
    const fileNameMatch = disposition.match(/filename=\"?([^\";]+)\"?/i);
    const fileName = fileNameMatch?.[1] ?? `result-report.${fallbackExtension}`;
    const blob = response.body;

    if (!blob) {
      this.errorMessage = 'Downloaded report was empty.';
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
