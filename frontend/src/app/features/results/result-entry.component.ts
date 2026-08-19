import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AuthService } from '../../core/auth.service';

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

type AnimalInfo = {
  id: number;
  animalName: string;
  speciesId: number;
  speciesName?: string;
  animalId?: number;
  ageInDays?: number;
};

type Detail = {
  registrationItemId: number;
  itemBarcode: string;
  notes?: string | null;
  sampleStatus: string;
  status: string;
  technicianVerifiedBy?: string | null;
  technicianVerifiedAt?: string | null;
  pathologistAuthenticatedBy?: string | null;
  pathologistAuthenticatedAt?: string | null;
  digitalSignature?: string | null;
  verificationRemarks?: string | null;
  values: ValueRow[];
  animals: AnimalInfo[];
  ownerName?: string;
  ownerNameAr?: string;
  ownerMobile?: string;
  ownerEmail?: string;
  ownerCode?: string;
  ownerId?: number;
  clientId?: number;
  rerunReason?: string | null;
  rerunInitiatedBy?: string | null;
  rerunAt?: string | null;
};

@Component({
  standalone: true,
  selector: 'app-result-entry',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3" *ngIf="detail">
      <!-- HEADER: Registration info + status badge + Back -->
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h3 class="mb-1">Result Entry — {{ detail.itemBarcode }}</h3>
          <div class="small text-muted">
            Registration Barcode: {{ detail.itemBarcode }}
            <span class="badge ms-2"
              [ngClass]="{
                'bg-warning text-dark': detail.status === 'Pending',
                'bg-info text-dark': detail.status === 'InProgress',
                'bg-success': detail.status === 'Completed'
              }">
              {{ detail.status }}
            </span>
            <span *ngIf="detail.rerunReason" class="badge bg-orange text-white ms-2" style="background-color:#e67e22">
              Re-run
            </span>
          </div>
        </div>
        <a class="btn btn-outline-secondary btn-sm" routerLink="/results">Back</a>
      </div>

      <div class="alert alert-danger" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="alert alert-success" *ngIf="successMessage">{{ successMessage }}</div>

      <!-- OWNER DETAILS (read-only) -->
      <div class="card mb-3 border-primary">
        <div class="card-header bg-primary text-white py-2">
          <strong>Owner Details</strong>
        </div>
        <div class="card-body py-2">
          <div class="row g-2">
            <div class="col-md-3">
              <small class="text-muted d-block">Owner Name</small>
              <strong>{{ detail.ownerName || '—' }}</strong>
            </div>
            <div class="col-md-3">
              <small class="text-muted d-block">Name (Arabic)</small>
              <strong>{{ detail.ownerNameAr || '—' }}</strong>
            </div>
            <div class="col-md-2">
              <small class="text-muted d-block">Mobile</small>
              <span>{{ detail.ownerMobile || '—' }}</span>
            </div>
            <div class="col-md-2">
              <small class="text-muted d-block">Email</small>
              <span>{{ detail.ownerEmail || '—' }}</span>
            </div>
            <div class="col-md-2">
              <small class="text-muted d-block">Owner Code</small>
              <span>{{ detail.ownerCode || '—' }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ANIMAL SELECTION -->
      <div class="card mb-3" *ngIf="detail.animals && detail.animals.length > 0">
        <div class="card-header py-2">
          <strong>Animals ({{ detail.animals.length }})</strong>
        </div>
        <div class="card-body py-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              *ngFor="let animal of detail.animals; let i = index"
              class="btn btn-sm"
              [ngClass]="{
                'btn-primary': selectedAnimalIndex === i,
                'btn-outline-primary': selectedAnimalIndex !== i
              }"
              (click)="selectAnimal(i)">
              {{ animal.animalName }} ({{ animal.speciesName || 'Unknown' }})
              <span *ngIf="animal.ageInDays != null" class="ms-1 text-muted">
                • {{ animal.ageInDays }}d
              </span>
            </button>
          </div>
          <div *ngIf="selectedAnimal" class="mt-2 small text-muted">
            Selected: <strong>{{ selectedAnimal.animalName }}</strong>
            ({{ selectedAnimal.speciesName || 'Unknown' }})
            <span *ngIf="selectedAnimal.ageInDays != null"> • Age: {{ selectedAnimal.ageInDays }} days</span>
          </div>
        </div>
      </div>

      <!-- RE-RUN INFO -->
      <div class="alert alert-warning" *ngIf="detail.rerunReason">
        <strong>Re-run initiated</strong> by {{ detail.rerunInitiatedBy }}
        <span *ngIf="detail.rerunAt"> on {{ detail.rerunAt | date:'medium' }}</span>
        <br *ngIf="detail.rerunReason" />
        <span *ngIf="detail.rerunReason">Reason: {{ detail.rerunReason }}</span>
      </div>

      <!-- INTERPRETATION (moved from the old age card) -->
      <div class="card mb-3">
        <div class="card-body py-2">
          <div class="row g-2">
            <div class="col-md-12">
              <label class="form-label">Interpretation / Notes</label>
              <textarea class="form-control" rows="2" [(ngModel)]="detail.notes" name="notes"></textarea>
            </div>
          </div>
        </div>
      </div>

      <!-- RESULT ENTRY TABLE -->
      <div class="card mb-3">
        <div class="card-header py-2">
          <strong>Results</strong>
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-sm align-middle mb-0">
              <thead class="table-light">
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
      </div>

      <!-- VERIFICATION & AUTHENTICATION -->
      <div class="row g-3 mb-3">
        <div class="col-lg-6">
          <div class="card p-3 h-100">
            <h5>Technician Verification</h5>
            <div class="row g-2">
              <div class="col-md-8">
                <label class="form-label">Verified By</label>
                <input class="form-control" [value]="currentUserDisplay" readonly disabled />
              </div>
              <div class="col-md-4">
                <label class="form-label">Remarks</label>
                <input class="form-control" [(ngModel)]="remarks" name="remarks" />
              </div>
            </div>
            <div *ngIf="detail.technicianVerifiedBy" class="mt-2 small text-success">
              Verified by {{ detail.technicianVerifiedBy }}
              <span *ngIf="detail.technicianVerifiedAt"> on {{ detail.technicianVerifiedAt | date:'medium' }}</span>
            </div>
            <button
              *ngIf="!detail.technicianVerifiedBy"
              class="btn btn-outline-primary btn-sm mt-3"
              type="button"
              (click)="verify()">
              Technician Verify
            </button>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="card p-3 h-100">
            <h5>Pathologist Authentication</h5>
            <div class="row g-2">
              <div class="col-md-6">
                <label class="form-label">Authenticated By</label>
                <input class="form-control" [value]="currentUserDisplay" readonly disabled />
              </div>
              <div class="col-md-6">
                <label class="form-label">Digital Signature</label>
                <input class="form-control" [(ngModel)]="digitalSignature" name="digitalSignature" />
              </div>
            </div>
            <div *ngIf="detail.pathologistAuthenticatedBy" class="mt-2 small text-success">
              Authenticated by {{ detail.pathologistAuthenticatedBy }}
              <span *ngIf="detail.pathologistAuthenticatedAt"> on {{ detail.pathologistAuthenticatedAt | date:'medium' }}</span>
            </div>
            <button
              *ngIf="detail.technicianVerifiedBy && !detail.pathologistAuthenticatedBy"
              class="btn btn-outline-success btn-sm mt-3"
              type="button"
              [disabled]="!canAuthenticate"
              (click)="authenticate()">
              Authenticate (Completes Test)
            </button>
          </div>
        </div>
      </div>

      <!-- REPORT PREVIEW -->
      <div class="card mb-3" *ngIf="showPreview">
        <div class="card-header d-flex justify-content-between align-items-center py-2">
          <strong>Report Preview</strong>
          <button class="btn btn-sm btn-outline-secondary" (click)="showPreview = false">Close</button>
        </div>
        <div class="card-body p-0">
          <iframe *ngIf="reportPreviewUrl" [src]="reportPreviewUrl"
            style="width:100%;height:600px;border:none;"></iframe>
        </div>
      </div>

      <!-- WORKFLOW ACTIONS -->
      <div class="d-flex flex-wrap gap-2 mt-3">
        <button class="btn btn-primary" type="button" (click)="save()">Save</button>
        <button class="btn btn-outline-info" type="button" (click)="previewReport()">Report Preview</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('pdf')">Download PDF</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('png')">PNG</button>
        <button class="btn btn-outline-secondary" type="button" (click)="downloadReport('jpg')">JPG</button>
        <button class="btn btn-outline-success" type="button" (click)="sendReport('pdf')">WhatsApp PDF</button>
        <button class="btn btn-outline-success" type="button" (click)="sendReport('jpg')">WhatsApp JPG</button>
        <button
          *ngIf="detail.status === 'Completed' && canRerun"
          class="btn btn-outline-warning ms-auto"
          type="button"
          (click)="showRerunModal = true">
          Re-run / Move to Pending
        </button>
      </div>

      <!-- RE-RUN MODAL -->
      <div class="modal-backdrop" *ngIf="showRerunModal" (click)="showRerunModal = false"></div>
      <div class="modal d-block" *ngIf="showRerunModal" tabindex="-1">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Re-run Result</h5>
              <button type="button" class="btn-close" (click)="showRerunModal = false"></button>
            </div>
            <div class="modal-body">
              <p class="mb-2">Move this result back to the technician workflow for re-testing.</p>
              <p class="small text-muted mb-3">Previous authentication will be cleared. The test will be re-entered and re-authenticated.</p>
              <label class="form-label">Reason for Re-run <span class="text-danger">*</span></label>
              <textarea class="form-control" rows="3" [(ngModel)]="rerunReason" name="rerunReason"
                placeholder="Enter the reason for re-running this test..."></textarea>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="showRerunModal = false">Cancel</button>
              <button type="button" class="btn btn-warning" [disabled]="!rerunReason?.trim()" (click)="rerun()">
                Confirm Re-run
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      z-index: 1040;
    }
    .modal {
      z-index: 1050;
    }
  `]
})
export class ResultEntryComponent {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  readonly entryTypes = ResultEntryType;

  id!: number;
  detail?: Detail;
  verifiedBy = '';
  authenticatedBy = '';
  digitalSignature = '';
  remarks = '';
  errorMessage = '';
  successMessage = '';
  selectedAnimalIndex = 0;

  showPreview = false;
  reportPreviewUrl: SafeResourceUrl | null = null;

  showRerunModal = false;
  rerunReason = '';

  get currentUserDisplay(): string {
    return this.auth.getUsername() ?? '';
  }

  get selectedAnimal(): AnimalInfo | undefined {
    return this.detail?.animals?.[this.selectedAnimalIndex];
  }

  get canAuthenticate(): boolean {
    if (!this.detail) return false;
    const roles = this.auth.hasAnyRole(['Admin', 'Manager']);
    return roles;
  }

  get canRerun(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  ngOnInit() {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    this.reload();
  }

  selectAnimal(index: number) {
    this.selectedAnimalIndex = index;
  }

  reload() {
    this.http.get<Detail>(`/api/results/${this.id}`).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.selectedAnimalIndex = 0;
        this.digitalSignature = detail.digitalSignature ?? '';
        this.remarks = detail.verificationRemarks ?? '';
        this.successMessage = '';
        this.errorMessage = '';
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Failed to load result details.';
      }
    });
  }

  save() {
    if (!this.detail) return;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      registrationItemId: this.detail.registrationItemId,
      ageInDays: this.selectedAnimal?.ageInDays ?? null,
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
    this.http.post(`/api/results/${this.id}/verify`, { remarks: this.remarks }).subscribe({
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
        digitalSignature: this.digitalSignature,
        remarks: this.remarks,
      })
      .subscribe({
        next: () => {
          this.successMessage = 'Pathologist authentication saved. Test is now completed.';
          this.reload();
        },
        error: (err) => {
          this.errorMessage = err?.error ?? 'Authentication failed.';
        },
      });
  }

  rerun() {
    this.errorMessage = '';
    this.successMessage = '';
    this.http.post(`/api/results/${this.id}/rerun`, { reason: this.rerunReason }).subscribe({
      next: () => {
        this.successMessage = 'Result moved back to pending for re-run.';
        this.showRerunModal = false;
        this.rerunReason = '';
        this.reload();
      },
      error: (err) => {
        this.errorMessage = typeof err?.error === 'string' ? err.error : (err?.error?.detail ?? 'Re-run failed.');
      },
    });
  }

  previewReport() {
    this.showPreview = true;
    const url = `/api/results/${this.id}/report?format=pdf`;
    this.reportPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
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
