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

type AnimalResult = {
  registrationAnimalId: number;
  animalId: number;
  animalName: string;
  speciesName?: string;
  ageInDays?: number;
  values: ValueRow[];
  isVerified: boolean;
  isAuthenticated: boolean;
  technicianVerifiedBy?: string | null;
  technicianVerifiedAt?: string | null;
  pathologistAuthenticatedBy?: string | null;
  pathologistAuthenticatedAt?: string | null;
  digitalSignature?: string | null;
  verificationRemarks?: string | null;
  notes?: string | null;
};

type Detail = {
  registrationItemId: number;
  itemBarcode: string;
  sampleStatus: string;
  status: string;
  animalResults: AnimalResult[];
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
      <!-- HEADER: Test info + status badge + Back -->
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h3 class="mb-1">
            <span *ngIf="testName">{{ testName }}</span>
            <span *ngIf="!testName">Result Entry</span>
            <small class="text-muted ms-2">({{ detail.itemBarcode }})</small>
          </h3>
          <div class="small text-muted">
            <span class="badge ms-2"
              [ngClass]="{
                'bg-warning text-dark': detail.status === 'Pending',
                'bg-info text-dark': detail.status === 'InProgress',
                'bg-success': detail.status === 'Completed'
              }">
              {{ detail.status }}
            </span>
            <span *ngIf="detail.rerunReason" class="badge ms-2" style="background-color:rgba(255,148,120,.18);color:#b85c43;font-weight:600">
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

      <!-- ANIMAL TABS with per-animal status badges -->
      <div class="card mb-3" *ngIf="detail.animalResults && detail.animalResults.length > 0">
        <div class="card-header py-2 d-flex justify-content-between align-items-center">
          <strong>Animals ({{ detail.animalResults.length }})</strong>
          <div class="d-flex gap-2">
            <button class="btn btn-outline-primary btn-sm" (click)="saveAll()" [disabled]="savingAll">
              <span *ngIf="savingAll" class="spinner-border spinner-border-sm me-1"></span>
              Save All
            </button>
            <button class="btn btn-outline-info btn-sm" (click)="verifyAll()" [disabled]="verifyingAll">
              <span *ngIf="verifyingAll" class="spinner-border spinner-border-sm me-1"></span>
              Verify All
            </button>
            <button class="btn btn-outline-success btn-sm" (click)="authenticateAll()" [disabled]="authenticatingAll" *ngIf="canAuthenticate">
              <span *ngIf="authenticatingAll" class="spinner-border spinner-border-sm me-1"></span>
              Authenticate All
            </button>
          </div>
        </div>
        <div class="card-body py-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              *ngFor="let animal of detail.animalResults; let i = index"
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
              <span *ngIf="animal.isVerified" class="badge bg-success ms-1">Verified</span>
              <span *ngIf="animal.isAuthenticated" class="badge bg-info text-dark ms-1">Authenticated</span>
              <span *ngIf="!animal.isVerified && !animal.isAuthenticated" class="badge bg-secondary ms-1">Draft</span>
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

      <!-- INTERPRETATION -->
      <div class="card mb-3">
        <div class="card-body py-2">
          <div class="row g-2">
            <div class="col-md-12">
              <label class="form-label">Interpretation / Notes</label>
              <textarea class="form-control" rows="2" [(ngModel)]="selectedAnimalNotes" name="notes"></textarea>
            </div>
          </div>
        </div>
      </div>

      <!-- RESULT ENTRY TABLE - grouped by test name (single test for now) -->
      <div class="card mb-3">
        <div class="card-header py-2">
          <strong>Results for {{ selectedAnimal?.animalName || 'selected animal' }}</strong>
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
                <tr *ngFor="let value of selectedAnimal?.values">
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

      <!-- PER-ANIMAL VERIFICATION & AUTHENTICATION -->
      <div class="row g-3 mb-3" *ngIf="selectedAnimal">
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
            <div *ngIf="selectedAnimal.isVerified" class="mt-2 small text-success">
              Verified by {{ selectedAnimal.technicianVerifiedBy }}
              <span *ngIf="selectedAnimal.technicianVerifiedAt"> on {{ selectedAnimal.technicianVerifiedAt | date:'medium' }}</span>
            </div>
            <div class="d-flex gap-2 mt-3">
              <button
                *ngIf="!selectedAnimal.isVerified"
                class="btn btn-outline-primary btn-sm"
                type="button"
                (click)="verify(selectedAnimal.registrationAnimalId)"
                [disabled]="verifying">
                <span *ngIf="verifying" class="spinner-border spinner-border-sm me-1"></span>
                Technician Verify
              </button>
              <button
                *ngIf="!selectedAnimal.isVerified"
                class="btn btn-outline-secondary btn-sm"
                type="button"
                (click)="saveAndVerify(selectedAnimal.registrationAnimalId)">
                Save & Verify
              </button>
            </div>
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
            <div *ngIf="selectedAnimal.isAuthenticated" class="mt-2 small text-success">
              Authenticated by {{ selectedAnimal.pathologistAuthenticatedBy }}
              <span *ngIf="selectedAnimal.pathologistAuthenticatedAt"> on {{ selectedAnimal.pathologistAuthenticatedAt | date:'medium' }}</span>
            </div>
            <button
              *ngIf="selectedAnimal.isVerified && !selectedAnimal.isAuthenticated"
              class="btn btn-outline-success btn-sm mt-3"
              type="button"
              [disabled]="!canAuthenticate || authenticating"
              (click)="authenticate(selectedAnimal.registrationAnimalId)">
              <span *ngIf="authenticating" class="spinner-border spinner-border-sm me-1"></span>
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
        <button class="btn btn-primary" type="button" (click)="save(selectedAnimal?.registrationAnimalId)" [disabled]="saving">
          <span *ngIf="saving" class="spinner-border spinner-border-sm me-1"></span>
          Save {{ selectedAnimal ? selectedAnimal.animalName : '' }}
        </button>
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
              <p class="small text-muted mb-3">Previous authentication will be cleared for ALL animals. The test will be re-entered and re-authenticated.</p>
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
  testName?: string;
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

  saving = false;
  savingAll = false;
  verifying = false;
  verifyingAll = false;
  authenticating = false;
  authenticatingAll = false;

  get currentUserDisplay(): string {
    return this.auth.getUsername() ?? '';
  }

  get selectedAnimal(): AnimalResult | undefined {
    return this.detail?.animalResults?.[this.selectedAnimalIndex];
  }

  get selectedAnimalNotes(): string {
    return this.selectedAnimal?.notes ?? '';
  }

  set selectedAnimalNotes(value: string) {
    if (this.selectedAnimal) {
      this.selectedAnimal.notes = value;
    }
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
        this.digitalSignature = detail.animalResults[0]?.digitalSignature ?? '';
        this.remarks = detail.animalResults[0]?.verificationRemarks ?? '';
        this.successMessage = '';
        this.errorMessage = '';
        
        // Extract test name from first animal's first parameter or use a generic label
        // We don't have test name in the DTO currently, but we can get it from the worklist or test metadata
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Failed to load result details.';
      }
    });
  }

  save(animalId?: number) {
    if (!this.detail) return;
    const targetAnimalId = animalId ?? this.selectedAnimal?.registrationAnimalId;
    if (!targetAnimalId) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.saving = true;

    const animal = this.detail.animalResults.find(a => a.registrationAnimalId === targetAnimalId);
    if (!animal) return;

    const payload = {
      registrationItemId: this.detail.registrationItemId,
      registrationAnimalId: targetAnimalId,
      ageInDays: animal.ageInDays ?? null,
      notes: animal.notes ?? null,
      values: animal.values.map((value) => ({
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
        this.successMessage = `Results saved for ${animal.animalName}.`;
        this.saving = false;
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Save failed.';
        this.saving = false;
      },
    });
  }

  saveAll() {
    if (!this.detail) return;
    
    this.errorMessage = '';
    this.successMessage = '';
    this.savingAll = true;

    const animalsToSave = [...this.detail.animalResults];
    let savedCount = 0;

    const saveNext = (index: number) => {
      if (index >= animalsToSave.length) {
        this.successMessage = `Results saved for ${savedCount} animal(s).`;
        this.savingAll = false;
        this.reload();
        return;
      }

      const animal = animalsToSave[index];
      const payload = {
        registrationItemId: this.detail!.registrationItemId,
        registrationAnimalId: animal.registrationAnimalId,
        ageInDays: animal.ageInDays ?? null,
        notes: animal.notes ?? null,
        values: animal.values.map((value) => ({
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
          savedCount++;
          saveNext(index + 1);
        },
        error: (err) => {
          this.errorMessage = `Failed to save for ${animal.animalName}: ${err?.error ?? 'Save failed.'}`;
          this.savingAll = false;
        },
      });
    };

    saveNext(0);
  }

  saveAndVerify(animalId: number) {
    this.save(animalId);
    // Note: verify will be called after reload, or we could chain it
  }

  verify(animalId?: number) {
    const targetAnimalId = animalId ?? this.selectedAnimal?.registrationAnimalId;
    if (!targetAnimalId) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.verifying = true;

    this.http.post(`/api/results/${this.id}/verify`, { registrationAnimalId: targetAnimalId, remarks: this.remarks }).subscribe({
      next: () => {
        const animal = this.detail?.animalResults.find(a => a.registrationAnimalId === targetAnimalId);
        this.successMessage = `Technician verification saved for ${animal?.animalName || 'animal'}.`;
        this.verifying = false;
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Verification failed.';
        this.verifying = false;
      },
    });
  }

  verifyAll() {
    if (!this.detail) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.verifyingAll = true;

    this.http.post(`/api/results/${this.id}/verify-all`, {}).subscribe({
      next: () => {
        this.successMessage = 'All animals verified by technician.';
        this.verifyingAll = false;
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Bulk verification failed.';
        this.verifyingAll = false;
      },
    });
  }

  authenticate(animalId?: number) {
    const targetAnimalId = animalId ?? this.selectedAnimal?.registrationAnimalId;
    if (!targetAnimalId) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.authenticating = true;

    this.http
      .post(`/api/results/${this.id}/authenticate`, {
        registrationAnimalId: targetAnimalId,
        digitalSignature: this.digitalSignature,
        remarks: this.remarks,
      })
      .subscribe({
        next: () => {
          const animal = this.detail?.animalResults.find(a => a.registrationAnimalId === targetAnimalId);
          this.successMessage = `Pathologist authentication saved for ${animal?.animalName || 'animal'}. Test completed.`;
          this.authenticating = false;
          this.reload();
        },
        error: (err) => {
          this.errorMessage = err?.error ?? 'Authentication failed.';
          this.authenticating = false;
        },
      });
  }

  authenticateAll() {
    if (!this.detail) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.authenticatingAll = true;

    const animalsToAuth = this.detail.animalResults.filter(a => a.isVerified && !a.isAuthenticated);
    
    if (animalsToAuth.length === 0) {
      this.successMessage = 'All verified animals are already authenticated.';
      this.authenticatingAll = false;
      return;
    }

    let authedCount = 0;

    const authNext = (index: number) => {
      if (index >= animalsToAuth.length) {
        this.successMessage = `${authedCount} animal(s) authenticated. Test ${authedCount === this.detail?.animalResults.length ? 'completed' : 'partially authenticated'}.`;
        this.authenticatingAll = false;
        this.reload();
        return;
      }

      const animal = animalsToAuth[index];

      this.http
        .post(`/api/results/${this.id}/authenticate`, {
          registrationAnimalId: animal.registrationAnimalId,
          digitalSignature: this.digitalSignature,
          remarks: this.remarks,
        })
        .subscribe({
          next: () => {
            authedCount++;
            authNext(index + 1);
          },
          error: (err) => {
            this.errorMessage = `Failed to authenticate ${animal.animalName}: ${err?.error ?? 'Authentication failed.'}`;
            this.authenticatingAll = false;
          },
        });
    };

    authNext(0);
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