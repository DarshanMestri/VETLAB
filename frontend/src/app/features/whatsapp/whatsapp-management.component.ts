import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

type TemplateRow = {
  id: number;
  code: string;
  name: string;
  messageType: number;
  languageCode: string;
  headerText?: string | null;
  bodyTemplate: string;
  footerText?: string | null;
  isActive: boolean;
};

type QueueRow = {
  id: number;
  messageType: number;
  status: number;
  toNumber: string;
  recipientName: string;
  templateName?: string | null;
  messageBody: string;
  attachmentFileName?: string | null;
  providerMessageId?: string | null;
  lastError?: string | null;
  retryCount: number;
  maxRetryCount: number;
  nextAttemptAt: string;
  lastAttemptAt?: string | null;
  sentAt?: string | null;
  deliveredAt?: string | null;
  externalReference?: string | null;
  createdAt: string;
};

type QueueActionResult = { queueId: number; status: number; message: string };

@Component({
  standalone: true,
  selector: 'app-whatsapp-management',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-1">WhatsApp Integration</h2>
          <p class="text-muted mb-0">Templates, queue, retries, and delivery status.</p>
        </div>
        <div class="d-flex gap-2">
          <button class="btn btn-outline-secondary btn-sm" type="button" (click)="activeTab = 'templates'">Templates</button>
          <button class="btn btn-outline-secondary btn-sm" type="button" (click)="activeTab = 'queue'">Queue</button>
        </div>
      </div>

      <div class="alert alert-success" *ngIf="message">{{ message }}</div>
      <div class="alert alert-danger" *ngIf="error">{{ error }}</div>

      <ng-container *ngIf="activeTab === 'templates'; else queueTab">
        <div class="card mb-3">
          <div class="card-header">{{ editingTemplateId ? 'Edit Template' : 'Create Template' }}</div>
          <div class="card-body">
            <div class="row g-3">
              <div class="col-md-3">
                <label class="form-label">Code</label>
                <input class="form-control" [(ngModel)]="templateForm.code" name="code" />
              </div>
              <div class="col-md-3">
                <label class="form-label">Name</label>
                <input class="form-control" [(ngModel)]="templateForm.name" name="name" />
              </div>
              <div class="col-md-3">
                <label class="form-label">Message Type</label>
                <select class="form-select" [(ngModel)]="templateForm.messageType" name="messageType">
                  <option [ngValue]="1">Report PDF</option>
                  <option [ngValue]="2">Report JPG</option>
                  <option [ngValue]="3">Invoice</option>
                  <option [ngValue]="4">Receipt</option>
                  <option [ngValue]="5">Appointment Reminder</option>
                  <option [ngValue]="6">Vaccination Reminder</option>
                </select>
              </div>
              <div class="col-md-3">
                <label class="form-label">Language</label>
                <input class="form-control" [(ngModel)]="templateForm.languageCode" name="languageCode" />
              </div>
              <div class="col-12">
                <label class="form-label">Header</label>
                <input class="form-control" [(ngModel)]="templateForm.headerText" name="headerText" />
              </div>
              <div class="col-12">
                <label class="form-label">Body Template</label>
                <textarea class="form-control" rows="4" [(ngModel)]="templateForm.bodyTemplate" name="bodyTemplate"></textarea>
                <small class="text-muted">Use placeholders like {{ '{{ownerName}}' }}, {{ '{{animalName}}' }}, {{ '{{registrationBarcode}}' }}.</small>
              </div>
              <div class="col-12">
                <label class="form-label">Footer</label>
                <input class="form-control" [(ngModel)]="templateForm.footerText" name="footerText" />
              </div>
              <div class="col-12 form-check ms-2">
                <input class="form-check-input" type="checkbox" [(ngModel)]="templateForm.isActive" name="isActive" id="templateActive" />
                <label class="form-check-label" for="templateActive">Active</label>
              </div>
            </div>
            <div class="mt-3 d-flex gap-2">
              <button class="btn btn-primary" type="button" (click)="saveTemplate()">{{ editingTemplateId ? 'Update' : 'Create' }}</button>
              <button class="btn btn-outline-secondary" type="button" (click)="resetTemplateForm()">Clear</button>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-header">Templates</div>
          <div class="card-body">
            <div class="row g-2 mb-3">
              <div class="col-md-6">
                <input class="form-control" [(ngModel)]="templateSearch" name="templateSearch" placeholder="Search templates" />
              </div>
              <div class="col-md-3">
                <button class="btn btn-outline-primary w-100" type="button" (click)="loadTemplates()">Search</button>
              </div>
            </div>
            <div class="table-responsive" *ngIf="templates.length; else noTemplates">
              <table class="table table-sm align-middle">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Language</th>
                    <th>Status</th>
                    <th class="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of templates">
                    <td>{{ item.code }}</td>
                    <td>{{ item.name }}</td>
                    <td>{{ messageTypeLabel(item.messageType) }}</td>
                    <td>{{ item.languageCode }}</td>
                    <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                    <td class="text-end">
                      <button class="btn btn-sm btn-outline-primary me-1" type="button" (click)="editTemplate(item)">Edit</button>
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="deleteTemplate(item.id)">Delete</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <ng-template #noTemplates><p class="text-muted mb-0">No templates found.</p></ng-template>
          </div>
        </div>
      </ng-container>

      <ng-template #queueTab>
        <div class="card mb-3">
          <div class="card-header">Queue Actions</div>
          <div class="card-body d-flex flex-wrap gap-2">
            <button class="btn btn-primary" type="button" (click)="processDue()">Process Due Queue</button>
            <button class="btn btn-outline-secondary" type="button" (click)="loadQueue()">Refresh Queue</button>
          </div>
        </div>

        <div class="card">
          <div class="card-header">WhatsApp Message Queue</div>
          <div class="card-body">
            <div class="row g-2 mb-3">
              <div class="col-md-4">
                <input class="form-control" [(ngModel)]="queueSearch" name="queueSearch" placeholder="Search queue" />
              </div>
              <div class="col-md-3">
                <select class="form-select" [(ngModel)]="queueStatus" name="queueStatus">
                  <option [ngValue]="null">All statuses</option>
                  <option [ngValue]="1">Queued</option>
                  <option [ngValue]="2">Processing</option>
                  <option [ngValue]="3">Sent</option>
                  <option [ngValue]="4">Delivered</option>
                  <option [ngValue]="5">Failed</option>
                </select>
              </div>
              <div class="col-md-3">
                <select class="form-select" [(ngModel)]="queueType" name="queueType">
                  <option [ngValue]="null">All message types</option>
                  <option [ngValue]="1">Report PDF</option>
                  <option [ngValue]="2">Report JPG</option>
                  <option [ngValue]="3">Invoice</option>
                  <option [ngValue]="4">Receipt</option>
                  <option [ngValue]="5">Appointment Reminder</option>
                  <option [ngValue]="6">Vaccination Reminder</option>
                </select>
              </div>
              <div class="col-md-2">
                <button class="btn btn-outline-primary w-100" type="button" (click)="loadQueue()">Search</button>
              </div>
            </div>

            <div class="table-responsive" *ngIf="queueRows.length; else noQueue">
              <table class="table table-sm align-middle">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Type</th>
                    <th>Recipient</th>
                    <th>Status</th>
                    <th>Retries</th>
                    <th>Next Attempt</th>
                    <th>Reference</th>
                    <th class="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of queueRows">
                    <td>{{ item.id }}</td>
                    <td>{{ messageTypeLabel(item.messageType) }}</td>
                    <td>
                      <div class="fw-semibold">{{ item.recipientName }}</div>
                      <small class="text-muted">{{ item.toNumber }}</small>
                    </td>
                    <td>
                      <div>{{ statusLabel(item.status) }}</div>
                      <small class="text-danger" *ngIf="item.lastError">{{ item.lastError }}</small>
                    </td>
                    <td>{{ item.retryCount }} / {{ item.maxRetryCount }}</td>
                    <td>{{ item.nextAttemptAt | date:'short' }}</td>
                    <td>{{ item.externalReference || '-' }}</td>
                    <td class="text-end">
                      <button class="btn btn-sm btn-outline-primary me-1" type="button" (click)="process(item.id)">Process</button>
                      <button class="btn btn-sm btn-outline-secondary" type="button" (click)="retry(item.id)">Retry</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <ng-template #noQueue><p class="text-muted mb-0">No queue items found.</p></ng-template>
          </div>
        </div>
      </ng-template>
    </div>
  `
})
export class WhatsAppManagementComponent {
  private readonly http = inject(HttpClient);

  activeTab: 'templates' | 'queue' = 'queue';
  templates: TemplateRow[] = [];
  queueRows: QueueRow[] = [];
  templateSearch = '';
  queueSearch = '';
  queueStatus: number | null = null;
  queueType: number | null = null;
  message = '';
  error = '';
  editingTemplateId: number | null = null;

  templateForm = this.emptyTemplateForm();

  ngOnInit(): void {
    this.loadTemplates();
    this.loadQueue();
  }

  loadTemplates(): void {
    this.http.get<Paged<TemplateRow>>('/api/whatsapp/templates', { params: { search: this.templateSearch || '' } }).subscribe({
      next: (response) => this.templates = response.items ?? [],
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  loadQueue(): void {
    const params: Record<string, string> = {};
    if (this.queueSearch.trim()) params['search'] = this.queueSearch.trim();
    if (this.queueStatus != null) params['status'] = String(this.queueStatus);
    if (this.queueType != null) params['type'] = String(this.queueType);

    this.http.get<Paged<QueueRow>>('/api/whatsapp/queue', { params }).subscribe({
      next: (response) => this.queueRows = response.items ?? [],
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  saveTemplate(): void {
    this.message = '';
    this.error = '';
    const request = this.editingTemplateId == null
      ? this.http.post<TemplateRow>('/api/whatsapp/templates', this.templateForm)
      : this.http.put<TemplateRow>(`/api/whatsapp/templates/${this.editingTemplateId}`, this.templateForm);

    request.subscribe({
      next: () => {
        this.message = `Template ${this.editingTemplateId == null ? 'created' : 'updated'} successfully.`;
        this.resetTemplateForm();
        this.loadTemplates();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  editTemplate(item: TemplateRow): void {
    this.editingTemplateId = item.id;
    this.templateForm = {
      code: item.code,
      name: item.name,
      messageType: item.messageType,
      languageCode: item.languageCode,
      headerText: item.headerText ?? '',
      bodyTemplate: item.bodyTemplate,
      footerText: item.footerText ?? '',
      isActive: item.isActive
    };
    this.activeTab = 'templates';
  }

  deleteTemplate(id: number): void {
    this.message = '';
    this.error = '';
    this.http.delete(`/api/whatsapp/templates/${id}`).subscribe({
      next: () => {
        this.message = 'Template deleted successfully.';
        this.loadTemplates();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  processDue(): void {
    this.message = '';
    this.error = '';
    this.http.post<QueueActionResult[]>('/api/whatsapp/queue/process-due', {}).subscribe({
      next: (response) => {
        this.message = response.length ? response.map((item) => `#${item.queueId}: ${item.message}`).join(' ') : 'No due queue items found.';
        this.loadQueue();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  process(id: number): void {
    this.http.post<QueueActionResult>(`/api/whatsapp/queue/${id}/process`, {}).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadQueue();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  retry(id: number): void {
    this.http.post<QueueActionResult>(`/api/whatsapp/queue/${id}/retry`, {}).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadQueue();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  resetTemplateForm(): void {
    this.editingTemplateId = null;
    this.templateForm = this.emptyTemplateForm();
  }

  messageTypeLabel(value: number): string {
    return {
      1: 'Report PDF',
      2: 'Report JPG',
      3: 'Invoice',
      4: 'Receipt',
      5: 'Appointment Reminder',
      6: 'Vaccination Reminder'
    }[value] ?? 'Unknown';
  }

  statusLabel(value: number): string {
    return {
      1: 'Queued',
      2: 'Processing',
      3: 'Sent',
      4: 'Delivered',
      5: 'Failed'
    }[value] ?? 'Unknown';
  }

  private emptyTemplateForm() {
    return {
      code: '',
      name: '',
      messageType: 1,
      languageCode: 'en',
      headerText: '',
      bodyTemplate: '',
      footerText: '',
      isActive: true
    };
  }

  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) return firstMessage;
    }

    return error?.error?.message ?? error?.error?.title ?? error?.error ?? 'WhatsApp request failed.';
  }
}
