import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';

interface LookupAnimal {
  id: number;
  code: string;
  name: string;
}

interface LookupOwner {
  id: number;
  code: string;
  name: string;
  animals: LookupAnimal[];
}

interface LookupDoctor {
  id: number;
  code: string;
  name: string;
}

interface LookupMedicine {
  id: number;
  code: string;
  name: string;
}

interface ClinicalLookups {
  owners: LookupOwner[];
  doctors: LookupDoctor[];
  medicines: LookupMedicine[];
}

interface AppointmentItem {
  id: number;
  ownerId: number;
  ownerName: string;
  animalId: number;
  animalName: string;
  doctorId?: number | null;
  doctorName?: string | null;
  type: number;
  status: number;
  scheduledAt: string;
  reason?: string | null;
  queueNumber?: number | null;
  queueStatus?: number | null;
  queuePriority?: number | null;
  hasEncounter: boolean;
}

interface QueueItem {
  id: number;
  appointmentId: number;
  queueNumber: number;
  queuedAt: string;
  status: number;
  priority?: number | null;
  animalName: string;
  ownerName: string;
  appointmentType: number;
  doctorId?: number | null;
  doctorName?: string | null;
}

interface PrescriptionFormItem {
  medicineId: number | null;
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string;
}

interface VaccinationFormItem {
  id?: number | null;
  vaccineName: string;
  administeredOn: string;
  dueOn: string;
  batchNumber: string;
  notes: string;
}

interface EncounterForm {
  doctorId: number | null;
  chiefComplaint: string;
  history: string;
  vitals: string;
  clinicalExamination: string;
  diagnosis: string;
  treatmentPlan: string;
  followUpAt: string;
  prescriptions: PrescriptionFormItem[];
  vaccinations: VaccinationFormItem[];
}

interface IntakeForm {
  ownerId: number | null;
  animalId: number | null;
  doctorId: number | null;
  type: number;
  scheduledAt: string;
  reason: string;
  priority: number | null;
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <div>
          <h2 class="mb-0">Clinical EMR</h2>
          <small class="text-muted">Appointments, walk-ins, emergency intake, queue, consultation, prescription, vaccination, and follow-up.</small>
        </div>
        <button *ngIf="canManage()" class="btn btn-primary" (click)="openIntake()">+ Appointment / Intake</button>
      </div>

      <div *ngIf="message()" class="alert alert-success">{{ message() }}</div>
      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

      <div class="row g-3 mb-3">
        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select class="form-select" [(ngModel)]="filters.status" name="statusFilter" (ngModelChange)="loadAppointments()">
            <option [ngValue]="null">All statuses</option>
            <option [ngValue]="1">Scheduled</option>
            <option [ngValue]="2">Checked In</option>
            <option [ngValue]="3">In Consultation</option>
            <option [ngValue]="4">Completed</option>
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">Type</label>
          <select class="form-select" [(ngModel)]="filters.type" name="typeFilter" (ngModelChange)="loadAppointments()">
            <option [ngValue]="null">All types</option>
            <option [ngValue]="1">Scheduled</option>
            <option [ngValue]="2">Walk-in</option>
            <option [ngValue]="3">Emergency</option>
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">From</label>
          <input type="date" class="form-control" [(ngModel)]="filters.from" name="fromFilter" (change)="loadAppointments()" />
        </div>
        <div class="col-md-3">
          <label class="form-label">To</label>
          <input type="date" class="form-control" [(ngModel)]="filters.to" name="toFilter" (change)="loadAppointments()" />
        </div>
      </div>

      <div class="row g-3">
        <div class="col-xl-7">
          <div class="card h-100">
            <div class="card-header">Appointments & Registrations</div>
            <div class="card-body p-0">
              <div class="table-responsive">
                <table class="table table-hover align-middle mb-0" *ngIf="appointments().length; else noAppointments">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Owner / Animal</th>
                      <th>Type</th>
                      <th>Status</th>
                      <th>Queue</th>
                      <th>Doctor</th>
                      <th class="text-end">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of appointments()">
                      <td>{{ item.scheduledAt | date:'short' }}</td>
                      <td>
                        <div class="fw-semibold">{{ item.ownerName }}</div>
                        <small class="text-muted">{{ item.animalName }}</small>
                      </td>
                      <td>{{ appointmentTypeLabel(item.type) }}</td>
                      <td>{{ appointmentStatusLabel(item.status) }}</td>
                      <td>{{ item.queueNumber ? ('#' + item.queueNumber) : '-' }}</td>
                      <td>{{ item.doctorName || '-' }}</td>
                      <td class="text-end">
                        <button *ngIf="canManage() && item.status === 1" class="btn btn-sm btn-outline-secondary me-1" (click)="checkIn(item)">Check In</button>
                        <button class="btn btn-sm btn-outline-primary me-1" (click)="openEmr(item)">{{ item.hasEncounter ? 'Open EMR' : 'Start EMR' }}</button>
                        <button class="btn btn-sm btn-outline-success" (click)="sendAppointmentReminder(item)">WhatsApp Reminder</button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <ng-template #noAppointments>
                <div class="p-3 text-muted">No appointments found.</div>
              </ng-template>
            </div>
          </div>
        </div>

        <div class="col-xl-5">
          <div class="card h-100">
            <div class="card-header">Queue Management</div>
            <div class="card-body">
              <div *ngFor="let item of queue()" class="border rounded p-2 mb-2">
                <div class="d-flex justify-content-between align-items-start gap-2">
                  <div>
                    <div class="fw-semibold">#{{ item.queueNumber }} · {{ item.ownerName }}</div>
                    <div>{{ item.animalName }}</div>
                    <small class="text-muted">{{ appointmentTypeLabel(item.appointmentType) }} · {{ queueStatusLabel(item.status) }} · {{ item.doctorName || 'Unassigned doctor' }}</small>
                  </div>
                  <button *ngIf="canManage()" class="btn btn-sm btn-outline-primary" (click)="call(item)">Consult</button>
                </div>
              </div>
              <p class="text-muted mb-0" *ngIf="!queue().length">No patients waiting in queue.</p>
            </div>
          </div>
        </div>
      </div>

      <section class="card mt-4" *ngIf="showIntake()">
        <div class="card-header">Appointment / Walk-in / Emergency Registration</div>
        <div class="card-body">
          <form (ngSubmit)="createAppointment()" novalidate>
            <div class="row g-3">
              <div class="col-md-4">
                <label class="form-label">Registration type *</label>
                <select class="form-select" [(ngModel)]="intake.type" name="type">
                  <option [ngValue]="1">Scheduled Appointment</option>
                  <option [ngValue]="2">Walk-in Registration</option>
                  <option [ngValue]="3">Emergency Registration</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Owner *</label>
                <select class="form-select" [(ngModel)]="intake.ownerId" name="ownerId" (ngModelChange)="onOwnerChange()">
                  <option [ngValue]="null">Select owner</option>
                  <option *ngFor="let owner of lookups().owners" [ngValue]="owner.id">{{ owner.name }} ({{ owner.code }})</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Animal *</label>
                <select class="form-select" [(ngModel)]="intake.animalId" name="animalId">
                  <option [ngValue]="null">Select animal</option>
                  <option *ngFor="let animal of selectedOwnerAnimals()" [ngValue]="animal.id">{{ animal.name }} ({{ animal.code }})</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Doctor</label>
                <select class="form-select" [(ngModel)]="intake.doctorId" name="doctorId">
                  <option [ngValue]="null">Unassigned</option>
                  <option *ngFor="let doctor of lookups().doctors" [ngValue]="doctor.id">{{ doctor.name }}</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Date & time *</label>
                <input type="datetime-local" class="form-control" [(ngModel)]="intake.scheduledAt" name="scheduledAt" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Queue priority</label>
                <input type="number" class="form-control" min="1" max="999" [(ngModel)]="intake.priority" name="priority" [disabled]="intake.type === 1" />
              </div>
              <div class="col-12">
                <label class="form-label">Reason</label>
                <textarea class="form-control" rows="2" [(ngModel)]="intake.reason" name="reason"></textarea>
              </div>
            </div>
            <div class="text-end mt-3">
              <button type="button" class="btn btn-secondary me-2" (click)="closeIntake()">Cancel</button>
              <button class="btn btn-primary" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save Intake' }}</button>
            </div>
          </form>
        </div>
      </section>

      <section class="card mt-4" *ngIf="selected() as appointment">
        <div class="card-header d-flex justify-content-between align-items-center">
          <div>
            <div class="fw-semibold">Clinical EMR — {{ appointment.animalName }}</div>
            <small>{{ appointment.ownerName }} · {{ appointmentTypeLabel(appointment.type) }} · {{ appointmentStatusLabel(appointment.status) }}</small>
          </div>
          <span class="badge text-bg-light" *ngIf="appointment.queueNumber">Queue #{{ appointment.queueNumber }}</span>
        </div>
        <div class="card-body">
          <form (ngSubmit)="saveEmr()" novalidate>
            <div class="row g-3">
              <div class="col-md-4">
                <label class="form-label">Doctor</label>
                <select class="form-select" [(ngModel)]="emr.doctorId" name="emrDoctorId">
                  <option [ngValue]="null">Unassigned</option>
                  <option *ngFor="let doctor of lookups().doctors" [ngValue]="doctor.id">{{ doctor.name }}</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Follow up</label>
                <input type="datetime-local" class="form-control" [(ngModel)]="emr.followUpAt" name="followUpAt" />
              </div>
              <div class="col-md-4 d-flex align-items-end">
                <button *ngIf="canManage() && appointment.status !== 3 && appointment.status !== 4" type="button" class="btn btn-outline-secondary w-100" (click)="checkIn(appointment)">Check In / Queue</button>
              </div>
              <div class="col-12">
                <label class="form-label">Chief Complaint</label>
                <textarea class="form-control" rows="2" [(ngModel)]="emr.chiefComplaint" name="chiefComplaint"></textarea>
              </div>
              <div class="col-12">
                <label class="form-label">History</label>
                <textarea class="form-control" rows="3" [(ngModel)]="emr.history" name="history"></textarea>
              </div>
              <div class="col-md-4">
                <label class="form-label">Vitals</label>
                <textarea class="form-control" rows="4" [(ngModel)]="emr.vitals" name="vitals"></textarea>
              </div>
              <div class="col-md-8">
                <label class="form-label">Clinical Examination</label>
                <textarea class="form-control" rows="4" [(ngModel)]="emr.clinicalExamination" name="clinicalExamination"></textarea>
              </div>
              <div class="col-md-6">
                <label class="form-label">Diagnosis</label>
                <textarea class="form-control" rows="3" [(ngModel)]="emr.diagnosis" name="diagnosis"></textarea>
              </div>
              <div class="col-md-6">
                <label class="form-label">Treatment Plan</label>
                <textarea class="form-control" rows="3" [(ngModel)]="emr.treatmentPlan" name="treatmentPlan"></textarea>
              </div>
            </div>

            <div class="d-flex justify-content-between align-items-center mt-4 mb-2">
              <h5 class="mb-0">Prescription</h5>
              <button *ngIf="canManage()" type="button" class="btn btn-sm btn-outline-primary" (click)="addPrescription()">+ Medicine</button>
            </div>
            <div class="border rounded p-3 mb-2" *ngFor="let item of emr.prescriptions; let i = index">
              <div class="row g-2">
                <div class="col-md-4">
                  <label class="form-label">Medicine</label>
                  <select class="form-select" [(ngModel)]="item.medicineId" [name]="'medicineId' + i" (ngModelChange)="onMedicineChange(i)">
                    <option [ngValue]="null">Select medicine</option>
                    <option *ngFor="let medicine of lookups().medicines" [ngValue]="medicine.id">{{ medicine.name }}</option>
                  </select>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Medicine name *</label>
                  <input class="form-control" [(ngModel)]="item.medicineName" [name]="'medicineName' + i" />
                </div>
                <div class="col-md-4">
                  <label class="form-label">Dosage</label>
                  <input class="form-control" [(ngModel)]="item.dosage" [name]="'dosage' + i" />
                </div>
                <div class="col-md-4">
                  <label class="form-label">Frequency</label>
                  <input class="form-control" [(ngModel)]="item.frequency" [name]="'frequency' + i" />
                </div>
                <div class="col-md-4">
                  <label class="form-label">Duration</label>
                  <input class="form-control" [(ngModel)]="item.duration" [name]="'duration' + i" />
                </div>
                <div class="col-md-4 d-flex align-items-end">
                  <button *ngIf="canManage()" type="button" class="btn btn-outline-danger w-100" (click)="removePrescription(i)">Remove</button>
                </div>
                <div class="col-12">
                  <label class="form-label">Instructions</label>
                  <textarea class="form-control" rows="2" [(ngModel)]="item.instructions" [name]="'instructions' + i"></textarea>
                </div>
              </div>
            </div>
            <p class="text-muted" *ngIf="!emr.prescriptions.length">No prescription items added.</p>

            <div class="d-flex justify-content-between align-items-center mt-4 mb-2">
              <h5 class="mb-0">Vaccination</h5>
              <button *ngIf="canManage()" type="button" class="btn btn-sm btn-outline-primary" (click)="addVaccination()">+ Vaccination</button>
            </div>
            <div class="border rounded p-3 mb-2" *ngFor="let item of emr.vaccinations; let i = index">
              <div class="row g-2">
                <div class="col-md-4">
                  <label class="form-label">Vaccine name *</label>
                  <input class="form-control" [(ngModel)]="item.vaccineName" [name]="'vaccineName' + i" />
                </div>
                <div class="col-md-3">
                  <label class="form-label">Administered on *</label>
                  <input type="date" class="form-control" [(ngModel)]="item.administeredOn" [name]="'administeredOn' + i" />
                </div>
                <div class="col-md-3">
                  <label class="form-label">Due on</label>
                  <input type="date" class="form-control" [(ngModel)]="item.dueOn" [name]="'dueOn' + i" />
                </div>
                <div class="col-md-2 d-flex align-items-end">
                  <button *ngIf="canManage()" type="button" class="btn btn-outline-danger w-100" (click)="removeVaccination(i)">Remove</button>
                </div>
                <div class="col-md-2 d-flex align-items-end" *ngIf="item.id">
                  <button type="button" class="btn btn-outline-success w-100" (click)="sendVaccinationReminder(item)">Reminder</button>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Batch number</label>
                  <input class="form-control" [(ngModel)]="item.batchNumber" [name]="'batchNumber' + i" />
                </div>
                <div class="col-md-8">
                  <label class="form-label">Notes</label>
                  <input class="form-control" [(ngModel)]="item.notes" [name]="'vaccineNotes' + i" />
                </div>
              </div>
            </div>
            <p class="text-muted" *ngIf="!emr.vaccinations.length">No vaccination items added.</p>

            <div class="text-end mt-3">
              <button class="btn btn-primary" [disabled]="saving() || !canManage()">{{ saving() ? 'Saving…' : 'Complete Consultation' }}</button>
            </div>
          </form>
        </div>
      </section>
    </div>
  `
})
export class ClinicalWorkspaceComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly appointments = signal<AppointmentItem[]>([]);
  readonly queue = signal<QueueItem[]>([]);
  readonly lookups = signal<ClinicalLookups>({ owners: [], doctors: [], medicines: [] });
  readonly selected = signal<AppointmentItem | null>(null);
  readonly showIntake = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  readonly filters = {
    from: '',
    to: '',
    status: null as number | null,
    type: null as number | null
  };

  intake: IntakeForm = this.emptyIntake();
  emr: EncounterForm = this.emptyEncounter();

  ngOnInit(): void {
    this.loadLookups();
    this.load();
  }

  canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  load(): void {
    this.loadAppointments();
    this.loadQueue();
  }

  loadLookups(): void {
    this.http.get<ClinicalLookups>('/api/clinical/lookups').subscribe({
      next: (response) => this.lookups.set(response),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  loadAppointments(): void {
    const params: Record<string, string | number> = {};
    if (this.filters.from) params['from'] = new Date(`${this.filters.from}T00:00:00`).toISOString();
    if (this.filters.to) params['to'] = new Date(`${this.filters.to}T23:59:59`).toISOString();
    if (this.filters.status != null) params['status'] = this.filters.status;
    if (this.filters.type != null) params['type'] = this.filters.type;

    this.http.get<AppointmentItem[]>('/api/clinical/appointments', { params }).subscribe({
      next: (response) => this.appointments.set(response),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  loadQueue(): void {
    this.http.get<QueueItem[]>('/api/clinical/queue').subscribe({
      next: (response) => this.queue.set(response),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  openIntake(): void {
    this.intake = this.emptyIntake();
    this.showIntake.set(true);
    this.error.set('');
  }

  closeIntake(): void {
    this.showIntake.set(false);
    this.intake = this.emptyIntake();
  }

  onOwnerChange(): void {
    const animals = this.selectedOwnerAnimals();
    if (!animals.some((item) => item.id === this.intake.animalId)) {
      this.intake.animalId = null;
    }
  }

  selectedOwnerAnimals(): LookupAnimal[] {
    return this.lookups().owners.find((owner) => owner.id === this.intake.ownerId)?.animals ?? [];
  }

  createAppointment(): void {
    const validationMessage = this.validateIntake();
    if (validationMessage) {
      this.error.set(validationMessage);
      return;
    }

    const payload = {
      ownerId: this.intake.ownerId,
      animalId: this.intake.animalId,
      doctorId: this.intake.doctorId,
      type: this.intake.type,
      scheduledAt: new Date(this.intake.scheduledAt).toISOString(),
      reason: this.cleanString(this.intake.reason),
      priority: this.intake.type === 1 ? null : this.intake.priority
    };

    this.saving.set(true);
    this.error.set('');
    this.http.post<AppointmentItem>('/api/clinical/appointments', payload).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.showIntake.set(false);
        this.selected.set(response);
        this.message.set('Clinical intake saved successfully.');
        this.load();
        this.openEmr(response);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      }
    });
  }

  checkIn(item: AppointmentItem): void {
    this.http.post(`/api/clinical/appointments/${item.id}/check-in`, {}, { params: item.type === 3 ? { priority: 1 } : {} }).subscribe({
      next: () => {
        this.message.set('Patient added to queue.');
        this.load();
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  call(item: QueueItem): void {
    this.http.post(`/api/clinical/queue/${item.id}/call`, {}).subscribe({
      next: () => {
        this.message.set('Queue updated to in consultation.');
        this.load();
        const appointment = this.appointments().find((x) => x.id === item.appointmentId);
        if (appointment) this.openEmr(appointment);
      },
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  sendAppointmentReminder(item: AppointmentItem): void {
    this.message.set('');
    this.error.set('');
    this.http.post(`/api/whatsapp/send/appointment-reminder/${item.id}`, {}).subscribe({
      next: () => this.message.set('Appointment reminder queued for WhatsApp delivery.'),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  openEmr(item: AppointmentItem): void {
    this.selected.set(item);
    this.error.set('');
    this.message.set('');
    this.http.get<any>(`/api/clinical/appointments/${item.id}/emr`).subscribe({
      next: (response) => {
        this.emr = this.mapEncounter(response);
      },
      error: () => {
        this.emr = this.emptyEncounter();
        this.emr.doctorId = item.doctorId ?? null;
      }
    });
  }

  addPrescription(): void {
    this.emr.prescriptions = [...this.emr.prescriptions, this.emptyPrescription()];
  }

  removePrescription(index: number): void {
    this.emr.prescriptions = this.emr.prescriptions.filter((_, i) => i !== index);
  }

  onMedicineChange(index: number): void {
    const item = this.emr.prescriptions[index];
    const medicine = this.lookups().medicines.find((entry) => entry.id === item.medicineId);
    if (medicine && !item.medicineName.trim()) {
      item.medicineName = medicine.name;
    }
  }

  addVaccination(): void {
    this.emr.vaccinations = [...this.emr.vaccinations, this.emptyVaccination()];
  }

  removeVaccination(index: number): void {
    this.emr.vaccinations = this.emr.vaccinations.filter((_, i) => i !== index);
  }

  sendVaccinationReminder(item: VaccinationFormItem): void {
    if (!item.id) {
      this.error.set('Save the consultation before sending a vaccination reminder.');
      return;
    }

    this.message.set('');
    this.error.set('');
    this.http.post(`/api/whatsapp/send/vaccination-reminder/${item.id}`, {}).subscribe({
      next: () => this.message.set('Vaccination reminder queued for WhatsApp delivery.'),
      error: (err) => this.error.set(this.errorMessage(err))
    });
  }

  saveEmr(): void {
    const appointment = this.selected();
    if (!appointment) return;

    const validationMessage = this.validateEncounter();
    if (validationMessage) {
      this.error.set(validationMessage);
      return;
    }

    const payload = {
      doctorId: this.emr.doctorId,
      chiefComplaint: this.cleanString(this.emr.chiefComplaint),
      history: this.cleanString(this.emr.history),
      vitals: this.cleanString(this.emr.vitals),
      clinicalExamination: this.cleanString(this.emr.clinicalExamination),
      diagnosis: this.cleanString(this.emr.diagnosis),
      treatmentPlan: this.cleanString(this.emr.treatmentPlan),
      followUpAt: this.emr.followUpAt ? new Date(this.emr.followUpAt).toISOString() : null,
      prescriptions: this.emr.prescriptions.map((item) => ({
        medicineId: item.medicineId,
        medicineName: item.medicineName.trim(),
        dosage: this.cleanString(item.dosage),
        frequency: this.cleanString(item.frequency),
        duration: this.cleanString(item.duration),
        instructions: this.cleanString(item.instructions)
      })),
      vaccinations: this.emr.vaccinations.map((item) => ({
        vaccineName: item.vaccineName.trim(),
        administeredOn: item.administeredOn,
        dueOn: item.dueOn || null,
        batchNumber: this.cleanString(item.batchNumber),
        notes: this.cleanString(item.notes)
      }))
    };

    this.saving.set(true);
    this.error.set('');
    this.http.put(`/api/clinical/appointments/${appointment.id}/emr`, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.message.set('Clinical EMR saved successfully.');
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.errorMessage(err));
      }
    });
  }

  appointmentTypeLabel(value: number): string {
    return value === 1 ? 'Scheduled' : value === 2 ? 'Walk-in' : 'Emergency';
  }

  appointmentStatusLabel(value: number): string {
    switch (value) {
      case 1: return 'Scheduled';
      case 2: return 'Checked In';
      case 3: return 'In Consultation';
      case 4: return 'Completed';
      case 5: return 'Cancelled';
      default: return 'No Show';
    }
  }

  queueStatusLabel(value: number): string {
    switch (value) {
      case 1: return 'Waiting';
      case 2: return 'Called';
      case 3: return 'In Consultation';
      case 4: return 'Completed';
      default: return 'Cancelled';
    }
  }

  private validateIntake(): string | null {
    if (!this.intake.ownerId) return 'Owner is required.';
    if (!this.intake.animalId) return 'Animal is required.';
    if (!this.intake.scheduledAt) return 'Date and time are required.';
    if (this.intake.priority != null && (this.intake.priority < 1 || this.intake.priority > 999)) return 'Priority must be between 1 and 999.';
    return null;
  }

  private validateEncounter(): string | null {
    const invalidPrescription = this.emr.prescriptions.find((item) => item.medicineName.trim().length > 200);
    if (invalidPrescription) return 'Medicine name cannot exceed 200 characters.';
    const invalidVaccination = this.emr.vaccinations.find((item) => !item.vaccineName.trim() || !item.administeredOn);
    if (invalidVaccination) return 'Each vaccination needs a vaccine name and administered date.';
    return null;
  }

  private mapEncounter(response: any): EncounterForm {
    return {
      doctorId: response.doctorId ?? null,
      chiefComplaint: response.chiefComplaint ?? '',
      history: response.history ?? '',
      vitals: response.vitals ?? '',
      clinicalExamination: response.clinicalExamination ?? '',
      diagnosis: response.diagnosis ?? '',
      treatmentPlan: response.treatmentPlan ?? '',
      followUpAt: response.followUpAt ? this.toDateTimeLocal(response.followUpAt) : '',
      prescriptions: (response.prescriptions ?? []).map((item: any) => ({
        medicineId: item.medicineId ?? null,
        medicineName: item.medicineName ?? '',
        dosage: item.dosage ?? '',
        frequency: item.frequency ?? '',
        duration: item.duration ?? '',
        instructions: item.instructions ?? ''
      })),
      vaccinations: (response.vaccinations ?? []).map((item: any) => ({
        id: item.id ?? null,
        vaccineName: item.vaccineName ?? '',
        administeredOn: item.administeredOn ?? '',
        dueOn: item.dueOn ?? '',
        batchNumber: item.batchNumber ?? '',
        notes: item.notes ?? ''
      }))
    };
  }

  private emptyIntake(): IntakeForm {
    return {
      ownerId: null,
      animalId: null,
      doctorId: null,
      type: 2,
      scheduledAt: new Date().toISOString().slice(0, 16),
      reason: '',
      priority: null
    };
  }

  private emptyEncounter(): EncounterForm {
    return {
      doctorId: null,
      chiefComplaint: '',
      history: '',
      vitals: '',
      clinicalExamination: '',
      diagnosis: '',
      treatmentPlan: '',
      followUpAt: '',
      prescriptions: [],
      vaccinations: []
    };
  }

  private emptyPrescription(): PrescriptionFormItem {
    return { medicineId: null, medicineName: '', dosage: '', frequency: '', duration: '', instructions: '' };
  }

  private emptyVaccination(): VaccinationFormItem {
    return { id: null, vaccineName: '', administeredOn: new Date().toISOString().slice(0, 10), dueOn: '', batchNumber: '', notes: '' };
  }

  private cleanString(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private toDateTimeLocal(value: string): string {
    return new Date(value).toISOString().slice(0, 16);
  }

  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) return firstMessage;
    }

    return error?.error?.message ?? error?.error?.title ?? error?.error ?? 'Clinical request failed.';
  }
}
