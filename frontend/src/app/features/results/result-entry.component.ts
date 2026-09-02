import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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

type OrderedTest = {
  registrationItemId: number;
  itemType: number;
  testId?: number;
  itemName: string;
  itemCode?: string;
  status: string;
  isCurrent: boolean;
};

type TestEntry = {
  registrationItemId: number;
  testId?: number;
  testName: string;
  itemCode?: string;
  itemBarcode: string;
  status: string;
  isVerified: boolean;
  isAuthenticated: boolean;
  technicianVerifiedBy?: string | null;
  technicianVerifiedAt?: string | null;
  pathologistAuthenticatedBy?: string | null;
  pathologistAuthenticatedAt?: string | null;
  digitalSignature?: string | null;
  verificationRemarks?: string | null;
  notes?: string | null;
  values: ValueRow[];
  isDirty?: boolean;
};

type AnimalEntry = {
  registrationAnimalId: number;
  animalId: number;
  animalName: string;
  speciesName?: string;
  ageInDays?: number;
  tests: TestEntry[];
  isDirty?: boolean;
};

type Detail = {
  registrationId: number;
  registrationBarcode: string;
  ownerName?: string;
  ownerNameAr?: string;
  ownerMobile?: string;
  ownerEmail?: string;
  ownerCode?: string;
  animals: AnimalEntry[];
  orderedTests?: OrderedTest[];
};

type RerunTarget = {
  animal: AnimalEntry;
  test: TestEntry;
};

@Component({
  standalone: true,
  selector: 'app-result-entry',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-3" *ngIf="detail">
      <!-- HEADER: Test context + status badge + Back -->
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h3 class="mb-1">
            <span *ngIf="testName">{{ testName }}</span>
            <span *ngIf="!testName">Result Entry</span>
            <small class="text-muted ms-2">({{ detail.registrationBarcode }})</small>
          </h3>
          <div class="small text-muted">
            Ordered tests: <strong>{{ detail.animals[0]?.tests?.length ?? 0 }}</strong>
            • Animals: <strong>{{ detail.animals?.length ?? 0 }}</strong>
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

      <!-- ORDERED TESTS (all tests for this registration, switchable to their own pages) -->
      <div class="card mb-3" *ngIf="detail.orderedTests && detail.orderedTests.length > 1">
        <div class="card-header py-2">
          <strong>Ordered Tests</strong>
        </div>
        <div class="card-body py-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              *ngFor="let ot of detail.orderedTests"
              type="button"
              class="btn btn-sm"
              [disabled]="ot.isCurrent"
              [ngClass]="ot.isCurrent ? 'btn-primary' : 'btn-outline-primary'"
              (click)="switchTest(ot.registrationItemId)">
              {{ ot.itemName }}<span *ngIf="ot.itemCode" class="ms-1 small opacity-75">({{ ot.itemCode }})</span>
              <span class="badge ms-1" [ngClass]="testStatusClass(ot.status)">{{ ot.status }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- ANIMAL TABS on top -->
      <div class="card mb-3" *ngIf="detail.animals && detail.animals.length > 0">
        <div class="card-header py-2 d-flex justify-content-between align-items-center">
          <strong>Animals ({{ detail.animals.length }})</strong>
          <button *ngIf="showSaveAll" class="btn btn-outline-primary btn-sm" (click)="saveAll()" [disabled]="savingAll">
            <span *ngIf="savingAll" class="spinner-border spinner-border-sm me-1"></span>
            Save All ({{ selectedAnimal?.animalName }})
          </button>
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
              <span *ngIf="animal.ageInDays != null" class="ms-1 text-muted">• {{ animal.ageInDays }}d</span>
              <span *ngIf="animal.isDirty" class="badge bg-warning text-dark ms-1">Edits</span>
            </button>
          </div>
          <div *ngIf="selectedAnimal" class="mt-2 small text-muted">
            Selected: <strong>{{ selectedAnimal.animalName }}</strong>
            ({{ selectedAnimal.speciesName || 'Unknown' }})
            <span *ngIf="selectedAnimal.ageInDays != null"> • Age: {{ selectedAnimal.ageInDays }} days</span>
            • Tests: <strong>{{ selectedAnimal.tests?.length ?? 0 }}</strong>
          </div>
        </div>
      </div>

      <!-- PER-TEST CARDS for the selected animal -->
      <ng-container *ngIf="selectedAnimal">
        <div *ngFor="let test of selectedAnimal.tests; trackBy: trackByItem" class="card mb-3">
          <div class="card-header py-2 bg-primary text-white d-flex justify-content-between align-items-center">
            <strong>
              {{ test.testName }}
              <small *ngIf="test.itemBarcode" class="ms-2 opacity-75">({{ test.itemBarcode }})</small>
            </strong>
            <span class="badge" [ngClass]="testStatusClass(test.status)">{{ test.status }}</span>
          </div>

          <div class="card-body py-2">
            <div class="d-flex justify-content-between align-items-center">
              <strong>Results for {{ selectedAnimal.animalName }}</strong>
              <small class="text-muted">Test: {{ test.testName }}</small>
            </div>
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
                  <tr *ngFor="let value of test.values; trackBy: trackByValue">
                    <td>
                      <div>{{ value.parameterName }}</div>
                      <small class="text-muted">{{ value.unitSymbol || 'No unit' }}</small>
                    </td>
                    <td>
                      <select class="form-select form-select-sm" [(ngModel)]="value.entryType" [name]="'entryType' + test.registrationItemId + '_' + value.testParameterId" (ngModelChange)="markDirty()">
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
                        <input *ngSwitchCase="entryTypes.Numeric" type="number" class="form-control form-control-sm" [(ngModel)]="value.numericValue" [name]="'numeric' + test.registrationItemId + '_' + value.testParameterId" (ngModelChange)="markDirty()" />
                        <input *ngSwitchCase="entryTypes.Text" type="text" class="form-control form-control-sm" [(ngModel)]="value.textValue" [name]="'text' + test.registrationItemId + '_' + value.testParameterId" (ngModelChange)="markDirty()" />
                        <select *ngSwitchCase="entryTypes.PositiveNegative" class="form-select form-select-sm" [(ngModel)]="value.positiveNegativeValue" [name]="'pn' + test.registrationItemId + '_' + value.testParameterId" (ngModelChange)="markDirty()">
                          <option [ngValue]="null">Select</option>
                          <option value="Positive">Positive</option>
                          <option value="Negative">Negative</option>
                        </select>
                        <div *ngSwitchCase="entryTypes.Formula" class="d-grid gap-1">
                          <input type="text" class="form-control form-control-sm" [(ngModel)]="value.formulaExpression" [name]="'formulaExpression' + test.registrationItemId + '_' + value.testParameterId" placeholder="Formula expression" (ngModelChange)="markDirty()" />
                          <input type="number" class="form-control form-control-sm" [(ngModel)]="value.numericValue" [name]="'formulaNumeric' + test.registrationItemId + '_' + value.testParameterId" placeholder="Numeric result" (ngModelChange)="markDirty()" />
                          <input type="text" class="form-control form-control-sm" [(ngModel)]="value.formulaResult" [name]="'formulaResult' + test.registrationItemId + '_' + value.testParameterId" placeholder="Display result" (ngModelChange)="markDirty()" />
                        </div>
                        <input *ngSwitchCase="entryTypes.Image" type="url" class="form-control form-control-sm" [(ngModel)]="value.imageUrl" [name]="'image' + test.registrationItemId + '_' + value.testParameterId" placeholder="https://..." (ngModelChange)="markDirty()" />
                        <input *ngSwitchCase="entryTypes.Pdf" type="url" class="form-control form-control-sm" [(ngModel)]="value.pdfAttachmentUrl" [name]="'pdf' + test.registrationItemId + '_' + value.testParameterId" placeholder="https://..." (ngModelChange)="markDirty()" />
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

          <!-- Notes, remarks, signature -->
          <div class="card-body py-2">
            <div class="row g-2">
              <div class="col-md-12">
                <label class="form-label">Interpretation / Notes</label>
                <textarea class="form-control" rows="2" [(ngModel)]="test.notes" [name]="'notes' + test.registrationItemId" (ngModelChange)="markDirty()"></textarea>
              </div>
              <div class="col-md-4">
                <label class="form-label">Verification Remarks</label>
                <input class="form-control" [(ngModel)]="test.verificationRemarks" [name]="'remarks' + test.registrationItemId" (ngModelChange)="markDirty()" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Digital Signature</label>
                <input class="form-control" [(ngModel)]="test.digitalSignature" [name]="'signature' + test.registrationItemId" (ngModelChange)="markDirty()" />
              </div>
              <div class="col-md-4 d-flex align-items-end">
                <button class="btn btn-primary w-100" type="button" (click)="save(selectedAnimal, test)" [disabled]="saving">
                  <span *ngIf="saving" class="spinner-border spinner-border-sm me-1"></span>
                  Save {{ test.testName }}
                </button>
              </div>
            </div>
          </div>

          <!-- Verification & Authentication -->
          <div class="card-body py-2 border-top">
            <div class="row g-2 align-items-center">
              <div class="col-md-2"><small class="text-muted">Verified:</small></div>
              <div class="col-md-4">
                <span *ngIf="test.isVerified" class="text-success">by {{ test.technicianVerifiedBy }} ({{ test.technicianVerifiedAt | date:'medium' }})</span>
                <span *ngIf="!test.isVerified" class="text-muted">Not verified</span>
              </div>
              <div class="col-md-6">
                <div class="d-flex gap-2">
                  <button
                    *ngIf="!test.isVerified"
                    class="btn btn-outline-primary btn-sm"
                    type="button"
                    (click)="verify(selectedAnimal, test)"
                    [disabled]="verifying">
                    <span *ngIf="verifying" class="spinner-border spinner-border-sm me-1"></span>
                    Technician Verify
                  </button>
                  <button
                    *ngIf="!test.isVerified"
                    class="btn btn-outline-secondary btn-sm"
                    type="button"
                    (click)="saveAndVerify(selectedAnimal, test)">
                    Save & Verify
                  </button>
                </div>
              </div>
            </div>
            <div class="row g-2 align-items-center mt-1">
              <div class="col-md-2"><small class="text-muted">Authenticated:</small></div>
              <div class="col-md-4">
                <span *ngIf="test.isAuthenticated" class="text-success">by {{ test.pathologistAuthenticatedBy }} ({{ test.pathologistAuthenticatedAt | date:'medium' }})</span>
                <span *ngIf="!test.isAuthenticated" class="text-muted">Not authenticated</span>
              </div>
              <div class="col-md-6">
                <button
                  *ngIf="test.isVerified && !test.isAuthenticated"
                  class="btn btn-outline-success btn-sm"
                  type="button"
                  [disabled]="!canAuthenticate || authenticating"
                  (click)="authenticate(selectedAnimal, test)">
                  <span *ngIf="authenticating" class="spinner-border spinner-border-sm me-1"></span>
                  Authenticate (Completes Test)
                </button>
                <button
                  *ngIf="test.isAuthenticated && canRerun"
                  class="btn btn-outline-warning btn-sm ms-2"
                  type="button"
                  (click)="openRerun(selectedAnimal, test)">
                  Re-run
                </button>
              </div>
            </div>
          </div>

          <!-- Report actions (per test) -->
          <div class="card-body py-2 border-top">
            <div class="d-flex flex-wrap gap-2 align-items-center">
              <small class="text-muted me-2">Report:</small>
              <button class="btn btn-outline-info btn-sm" type="button" (click)="previewReport(test)">Preview</button>
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="downloadReport(test, 'pdf')">PDF</button>
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="downloadReport(test, 'png')">PNG</button>
              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="downloadReport(test, 'jpg')">JPG</button>
              <button class="btn btn-outline-success btn-sm" type="button" (click)="sendReport(test, 'pdf')">WhatsApp PDF</button>
              <button class="btn btn-outline-success btn-sm" type="button" (click)="sendReport(test, 'jpg')">WhatsApp JPG</button>
            </div>
          </div>
        </div>
      </ng-container>

      <div class="alert alert-info" *ngIf="selectedAnimal && !selectedAnimal?.tests?.length">
        No ordered tests available for {{ selectedAnimal.animalName }}.
      </div>

      <!-- REPORT PREVIEW -->
      <div class="card mb-3" *ngIf="showPreview">
        <div class="card-header d-flex justify-content-between align-items-center py-2">
          <strong>Report Preview{{ previewTestName ? ' — ' + previewTestName : '' }}</strong>
          <button class="btn btn-sm btn-outline-secondary" (click)="showPreview = false">Close</button>
        </div>
        <div class="card-body p-0">
          <iframe *ngIf="reportPreviewUrl" [src]="reportPreviewUrl"
            style="width:100%;height:600px;border:none;"></iframe>
        </div>
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
              <p class="mb-2">Re-run <strong>{{ rerunTarget?.test?.testName }}</strong> for <strong>{{ rerunTarget?.animal?.animalName || 'selected animal' }}</strong>. Its saved values will be kept; verification and authentication will be cleared so it can be re-tested and re-approved.</p>
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
  private router = inject(Router);
  private auth = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  readonly entryTypes = ResultEntryType;

  id!: number;
  detail?: Detail;
  testName?: string;
  errorMessage = '';
  successMessage = '';
  selectedAnimalIndex = 0;

  showPreview = false;
  reportPreviewUrl: SafeResourceUrl | null = null;
  previewTestName = '';

  showRerunModal = false;
  rerunTarget: RerunTarget | null = null;
  rerunReason = '';

  saving = false;
  savingAll = false;
  verifying = false;
  authenticating = false;

  get selectedAnimal(): AnimalEntry | undefined {
    return this.detail?.animals?.[this.selectedAnimalIndex];
  }

  get canAuthenticate(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  get canRerun(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  get showSaveAll(): boolean {
    return !!this.selectedAnimal?.isDirty;
  }

  get currentTestName(): string {
    return this.detail?.animals?.[0]?.tests?.[0]?.testName ?? this.testName ?? '';
  }

  markDirty() {
    if (this.selectedAnimal) {
      this.selectedAnimal.isDirty = true;
    }
  }

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const newId = Number(params.get('id'));
      if (newId !== this.id) {
        this.id = newId;
        this.reload();
      }
    });
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    this.reload();
  }

  switchTest(registrationItemId: number) {
    if (registrationItemId !== this.id) {
      this.router.navigate(['/results', registrationItemId]);
    }
  }

  trackByItem(_index: number, item: TestEntry) {
    return item.registrationItemId;
  }

  trackByValue(_index: number, item: ValueRow) {
    return item.testParameterId;
  }

  private testStatusClass(status: string): string {
    switch (status) {
      case 'Completed': return 'bg-success';
      case 'InProgress': return 'bg-info text-dark';
      case 'Pending': return 'bg-warning text-dark';
      default: return 'bg-secondary';
    }
  }

  selectAnimal(index: number) {
    this.selectedAnimalIndex = index;
  }

  reload() {
    this.http.get<Detail>(`/api/results/${this.id}`).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.selectedAnimalIndex = 0;
        this.successMessage = '';
        this.errorMessage = '';
        detail.animals?.forEach(a => a.isDirty = false);

        const current = detail.orderedTests?.find(t => t.isCurrent);
        this.testName = current?.itemName ?? this.testName ?? '';
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Failed to load result details.';
      }
    });
  }

  save(animal: AnimalEntry, test: TestEntry) {
    if (!animal || !test) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.saving = true;

    const payload = {
      registrationItemId: test.registrationItemId,
      registrationAnimalId: animal.registrationAnimalId,
      ageInDays: animal.ageInDays ?? null,
      notes: test.notes ?? null,
      values: test.values.map((value) => ({
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

    this.http.post(`/api/results/${test.registrationItemId}/enter`, payload).subscribe({
      next: () => {
        this.successMessage = `Results saved for ${animal.animalName} — ${test.testName}.`;
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
    const animal = this.selectedAnimal;
    if (!animal || !animal.tests?.length) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.savingAll = true;

    const testsToSave = [...animal.tests];
    let savedCount = 0;

    const saveNext = (index: number) => {
      if (index >= testsToSave.length) {
        this.successMessage = `Results saved for ${savedCount} test(s) of ${animal.animalName}.`;
        this.savingAll = false;
        this.reload();
        return;
      }

      const test = testsToSave[index];
      const payload = {
        registrationItemId: test.registrationItemId,
        registrationAnimalId: animal.registrationAnimalId,
        ageInDays: animal.ageInDays ?? null,
        notes: test.notes ?? null,
        values: test.values.map((value) => ({
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

      this.http.post(`/api/results/${test.registrationItemId}/enter`, payload).subscribe({
        next: () => {
          savedCount++;
          saveNext(index + 1);
        },
        error: (err) => {
          this.errorMessage = `Failed to save for ${animal.animalName} — ${test.testName}: ${err?.error ?? 'Save failed.'}`;
          this.savingAll = false;
        },
      });
    };

    saveNext(0);
  }

  saveAndVerify(animal: AnimalEntry, test: TestEntry) {
    this.save(animal, test);
  }

  verify(animal: AnimalEntry, test: TestEntry) {
    if (!animal || !test) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.verifying = true;

    this.http.post(`/api/results/${test.registrationItemId}/verify`, {
      registrationAnimalId: animal.registrationAnimalId,
      remarks: test.verificationRemarks ?? null,
    }).subscribe({
      next: () => {
        this.successMessage = `Technician verification saved for ${animal.animalName} — ${test.testName}.`;
        this.verifying = false;
        this.reload();
      },
      error: (err) => {
        this.errorMessage = err?.error ?? 'Verification failed.';
        this.verifying = false;
      },
    });
  }

  authenticate(animal: AnimalEntry, test: TestEntry) {
    if (!animal || !test) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.authenticating = true;

    this.http
      .post(`/api/results/${test.registrationItemId}/authenticate`, {
        registrationAnimalId: animal.registrationAnimalId,
        digitalSignature: test.digitalSignature ?? null,
        remarks: test.verificationRemarks ?? null,
      })
      .subscribe({
        next: () => {
          this.successMessage = `Pathologist authentication saved for ${animal.animalName} — ${test.testName}. Test completed.`;
          this.authenticating = false;
          this.reload();
        },
        error: (err) => {
          this.errorMessage = err?.error ?? 'Authentication failed.';
          this.authenticating = false;
        },
      });
  }

  openRerun(animal: AnimalEntry, test: TestEntry) {
    this.rerunTarget = { animal, test };
    this.rerunReason = '';
    this.showRerunModal = true;
  }

  rerun() {
    if (!this.rerunTarget) return;
    const { animal, test } = this.rerunTarget;

    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`/api/results/${test.registrationItemId}/rerun`, {
      reason: this.rerunReason,
      registrationAnimalId: animal.registrationAnimalId,
    }).subscribe({
      next: () => {
        this.successMessage = `${test.testName} for ${animal.animalName} moved back for re-run.`;
        this.showRerunModal = false;
        this.rerunTarget = null;
        this.rerunReason = '';
        this.reload();
      },
      error: (err) => {
        this.errorMessage = typeof err?.error === 'string' ? err.error : (err?.error?.detail ?? 'Re-run failed.');
      },
    });
  }

  previewReport(test: TestEntry) {
    this.showPreview = true;
    this.previewTestName = test.testName;
    const url = `/api/results/${test.registrationItemId}/report?format=pdf`;
    this.reportPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  downloadReport(test: TestEntry, format: 'pdf' | 'png' | 'jpg') {
    this.errorMessage = '';
    this.successMessage = '';

    this.http
      .get(`/api/results/${test.registrationItemId}/report?format=${format}`, {
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

  sendReport(test: TestEntry, format: 'pdf' | 'jpg') {
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post<{ message: string }>(`/api/whatsapp/send/report/${test.registrationItemId}`, { format }).subscribe({
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
