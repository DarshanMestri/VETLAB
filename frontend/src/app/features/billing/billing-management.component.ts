import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

type InvoiceStatus = 1 | 2 | 3;
type BillingModuleType = 1 | 2 | 3 | 4 | 5;
type PaymentMethod = 1 | 2 | 3;
type TransactionType = 1 | 2 | 3;

type Lookup = { id: number; ownerId?: number; code?: string; name?: string; ownerName?: string; barcode?: string; scheduledAt?: string };
type BillingLine = {
  moduleType: BillingModuleType;
  description: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
};
type BillingTransaction = {
  id: number;
  transactionType: TransactionType;
  method: PaymentMethod;
  amount: number;
  transactedAt: string;
  reference?: string | null;
  notes?: string | null;
};
type BillingInvoice = {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  status: InvoiceStatus;
  ownerName?: string | null;
  animalName?: string | null;
  currency: string;
  netAmount: number;
  paymentAmount: number;
  refundAmount: number;
  creditAmount: number;
  outstandingAmount: number;
  creditBalanceAmount: number;
};
type BillingInvoiceDetail = BillingInvoice & {
  ownerId?: number | null;
  animalId?: number | null;
  doctorId?: number | null;
  registrationId?: number | null;
  appointmentId?: number | null;
  discountAmount: number;
  notes?: string | null;
  lines: (BillingLine & { id: number; totalAmount: number })[];
  transactions: BillingTransaction[];
};

@Component({
  standalone: true,
  selector: 'app-billing-management',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-1">Billing System</h2>
          <p class="text-muted mb-0">Consultation, laboratory, pharmacy, surgery, and hospitalization billing.</p>
        </div>
        <button class="btn btn-primary btn-sm" type="button" (click)="startCreate()">+ New Invoice</button>
      </div>

      <div class="alert alert-success" *ngIf="message">{{ message }}</div>
      <div class="alert alert-danger" *ngIf="error">{{ error }}</div>

      <div class="card mb-3">
        <div class="card-body">
          <div class="row g-2">
            <div class="col-md-4">
              <input class="form-control" [(ngModel)]="search" name="search" placeholder="Search invoice / owner / animal" />
            </div>
            <div class="col-md-3">
              <select class="form-select" [(ngModel)]="statusFilter" name="statusFilter">
                <option [ngValue]="null">All statuses</option>
                <option [ngValue]="1">Draft</option>
                <option [ngValue]="2">Finalized</option>
                <option [ngValue]="3">Cancelled</option>
              </select>
            </div>
            <div class="col-md-3">
              <select class="form-select" [(ngModel)]="moduleFilter" name="moduleFilter">
                <option [ngValue]="null">All modules</option>
                <option [ngValue]="1">Consultation</option>
                <option [ngValue]="2">Laboratory</option>
                <option [ngValue]="3">Pharmacy</option>
                <option [ngValue]="4">Surgery</option>
                <option [ngValue]="5">Hospitalization</option>
              </select>
            </div>
            <div class="col-md-2 d-grid">
              <button class="btn btn-outline-primary" type="button" (click)="loadInvoices(1)">Search</button>
            </div>
          </div>
        </div>
      </div>

      <div class="card mb-3">
        <div class="card-header">Invoices</div>
        <div class="card-body p-0">
          <div class="table-responsive" *ngIf="invoices.length; else noInvoices">
            <table class="table table-sm mb-0 align-middle">
              <thead>
                <tr>
                  <th>Invoice</th>
                  <th>Date</th>
                  <th>Owner / Animal</th>
                  <th>Status</th>
                  <th>Net</th>
                  <th>Outstanding</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let row of invoices">
                  <td>{{ row.invoiceNumber }}</td>
                  <td>{{ row.invoiceDate | date:'shortDate' }}</td>
                  <td>
                    <div>{{ row.ownerName || '-' }}</div>
                    <small class="text-muted">{{ row.animalName || '' }}</small>
                  </td>
                  <td>{{ statusLabel(row.status) }}</td>
                  <td>{{ row.netAmount | number:'1.2-2' }} {{ row.currency }}</td>
                  <td>{{ row.outstandingAmount | number:'1.2-2' }} {{ row.currency }}</td>
                  <td class="text-end">
                    <button class="btn btn-sm btn-outline-primary me-1" type="button" (click)="edit(row.id)">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" type="button" (click)="remove(row.id)">Delete</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <ng-template #noInvoices><p class="p-3 mb-0 text-muted">No invoices found.</p></ng-template>
        </div>
        <div class="card-footer d-flex justify-content-between align-items-center">
          <small class="text-muted">Total: {{ total }}</small>
          <div class="d-flex gap-2">
            <button class="btn btn-outline-secondary btn-sm" [disabled]="page <= 1" (click)="loadInvoices(page - 1)">Prev</button>
            <span class="small align-self-center">Page {{ page }}</span>
            <button class="btn btn-outline-secondary btn-sm" [disabled]="(page * pageSize) >= total" (click)="loadInvoices(page + 1)">Next</button>
          </div>
        </div>
      </div>

      <div class="card" *ngIf="showEditor">
        <div class="card-header">{{ editingId ? 'Edit Invoice' : 'Create Invoice' }}</div>
        <div class="card-body">
          <div class="row g-2 mb-3">
            <div class="col-md-3">
              <label class="form-label">Date</label>
              <input class="form-control" type="datetime-local" [(ngModel)]="form.invoiceDate" name="invoiceDate" />
            </div>
            <div class="col-md-3">
              <label class="form-label">Status</label>
              <select class="form-select" [(ngModel)]="form.status" name="status">
                <option [ngValue]="1">Draft</option>
                <option [ngValue]="2">Finalized</option>
                <option [ngValue]="3">Cancelled</option>
              </select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Currency</label>
              <input class="form-control" [(ngModel)]="form.currency" name="currency" />
            </div>
            <div class="col-md-2">
              <label class="form-label">Invoice Discount</label>
              <input class="form-control" type="number" min="0" step="0.01" [(ngModel)]="form.discountAmount" name="discountAmount" />
            </div>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-md-4">
              <label class="form-label">Owner</label>
              <select class="form-select" [(ngModel)]="form.ownerId" (ngModelChange)="onOwnerChanged()" name="ownerId">
                <option [ngValue]="null">Select owner</option>
                <option *ngFor="let owner of owners" [ngValue]="owner.id">{{ owner.name }}{{ owner.code ? ' • ' + owner.code : '' }}</option>
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Animal</label>
              <select class="form-select" [(ngModel)]="form.animalId" name="animalId">
                <option [ngValue]="null">Select animal</option>
                <option *ngFor="let animal of filteredAnimals()" [ngValue]="animal.id">{{ animal.name }}{{ animal.code ? ' • ' + animal.code : '' }}</option>
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Doctor</label>
              <select class="form-select" [(ngModel)]="form.doctorId" name="doctorId">
                <option [ngValue]="null">Select doctor</option>
                <option *ngFor="let doctor of doctors" [ngValue]="doctor.id">{{ doctor.name }}{{ doctor.code ? ' • ' + doctor.code : '' }}</option>
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Registration</label>
              <select class="form-select" [(ngModel)]="form.registrationId" name="registrationId">
                <option [ngValue]="null">Select registration</option>
                <option *ngFor="let registration of registrations" [ngValue]="registration.id">{{ registration.barcode }}{{ registration.ownerName ? ' • ' + registration.ownerName : '' }}</option>
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Appointment</label>
              <select class="form-select" [(ngModel)]="form.appointmentId" name="appointmentId">
                <option [ngValue]="null">Select appointment</option>
                <option *ngFor="let appointment of appointments" [ngValue]="appointment.id">#{{ appointment.id }} • {{ appointment.scheduledAt | date:'short' }}</option>
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Notes</label>
              <input class="form-control" [(ngModel)]="form.notes" name="notes" />
            </div>
          </div>

          <div class="d-flex justify-content-between align-items-center mb-2">
            <h6 class="mb-0">Billing Lines</h6>
            <button class="btn btn-outline-primary btn-sm" type="button" (click)="addLine()">Add Line</button>
          </div>
          <div class="border rounded p-2 mb-2" *ngFor="let line of form.lines; let i = index">
            <div class="row g-2">
              <div class="col-md-2">
                <label class="form-label">Module</label>
                <select class="form-select form-select-sm" [(ngModel)]="line.moduleType" [name]="'module'+i">
                  <option [ngValue]="1">Consultation</option>
                  <option [ngValue]="2">Laboratory</option>
                  <option [ngValue]="3">Pharmacy</option>
                  <option [ngValue]="4">Surgery</option>
                  <option [ngValue]="5">Hospitalization</option>
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label">Description</label>
                <input class="form-control form-control-sm" [(ngModel)]="line.description" [name]="'description'+i" />
              </div>
              <div class="col-md-1">
                <label class="form-label">Qty</label>
                <input class="form-control form-control-sm" type="number" min="0.001" step="0.001" [(ngModel)]="line.quantity" [name]="'qty'+i" />
              </div>
              <div class="col-md-2">
                <label class="form-label">Unit Price</label>
                <input class="form-control form-control-sm" type="number" min="0" step="0.01" [(ngModel)]="line.unitPrice" [name]="'price'+i" />
              </div>
              <div class="col-md-2">
                <label class="form-label">Discount</label>
                <input class="form-control form-control-sm" type="number" min="0" step="0.01" [(ngModel)]="line.discountAmount" [name]="'lineDiscount'+i" />
              </div>
              <div class="col-md-1 d-grid">
                <label class="form-label">&nbsp;</label>
                <button class="btn btn-outline-danger btn-sm" type="button" (click)="removeLine(i)">X</button>
              </div>
            </div>
          </div>

          <div class="d-flex gap-2 mt-3">
            <button class="btn btn-primary" type="button" (click)="save()">Save Invoice</button>
            <button class="btn btn-outline-secondary" type="button" (click)="resetEditor()">Clear</button>
          </div>

          <div class="mt-4" *ngIf="editingId && transactions.length">
            <h6>Transactions (Receipt / Payment / Refund / Credit)</h6>
            <table class="table table-sm">
              <thead>
                <tr>
                  <th>Type</th>
                  <th>Method</th>
                  <th>Amount</th>
                  <th>Date</th>
                  <th>Reference</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let tx of transactions">
                  <td>{{ transactionTypeLabel(tx.transactionType) }}</td>
                  <td>{{ paymentMethodLabel(tx.method) }}</td>
                  <td>{{ tx.amount | number:'1.2-2' }}</td>
                  <td>{{ tx.transactedAt | date:'short' }}</td>
                  <td>{{ tx.reference || '-' }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="mt-3" *ngIf="editingId">
            <h6>Add Transaction</h6>
            <div class="row g-2">
              <div class="col-md-3">
                <select class="form-select" [(ngModel)]="transactionForm.transactionType" name="transactionType">
                  <option [ngValue]="1">Payment</option>
                  <option [ngValue]="2">Refund</option>
                  <option [ngValue]="3">Credit</option>
                </select>
              </div>
              <div class="col-md-2">
                <select class="form-select" [(ngModel)]="transactionForm.method" name="transactionMethod">
                  <option [ngValue]="1">Cash</option>
                  <option [ngValue]="2">Card</option>
                  <option [ngValue]="3">Cheque</option>
                </select>
              </div>
              <div class="col-md-2">
                <input class="form-control" type="number" min="0.01" step="0.01" [(ngModel)]="transactionForm.amount" name="transactionAmount" placeholder="Amount" />
              </div>
              <div class="col-md-2">
                <input class="form-control" [(ngModel)]="transactionForm.reference" name="transactionRef" placeholder="Reference" />
              </div>
              <div class="col-md-3 d-grid">
                <button class="btn btn-outline-success" type="button" (click)="addTransaction()">Add</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class BillingManagementComponent {
  private readonly http = inject(HttpClient);

  invoices: BillingInvoice[] = [];
  total = 0;
  page = 1;
  pageSize = 20;
  search = '';
  statusFilter: InvoiceStatus | null = null;
  moduleFilter: BillingModuleType | null = null;

  showEditor = false;
  editingId: number | null = null;
  message = '';
  error = '';

  owners: Lookup[] = [];
  animals: Lookup[] = [];
  doctors: Lookup[] = [];
  registrations: Lookup[] = [];
  appointments: Lookup[] = [];
  transactions: BillingTransaction[] = [];

  form = this.emptyForm();
  transactionForm = this.emptyTransactionForm();

  ngOnInit(): void {
    this.loadLookups();
    this.loadInvoices(1);
  }

  loadInvoices(page: number): void {
    this.page = page;
    const params: Record<string, string> = { page: String(this.page), pageSize: String(this.pageSize) };
    if (this.search.trim()) params['search'] = this.search.trim();
    if (this.statusFilter != null) params['status'] = String(this.statusFilter);
    if (this.moduleFilter != null) params['moduleType'] = String(this.moduleFilter);
    this.http.get<Paged<BillingInvoice>>('/api/billing/invoices', { params }).subscribe({
      next: (response) => {
        this.invoices = response.items ?? [];
        this.total = response.total ?? 0;
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  startCreate(): void {
    this.resetEditor();
    this.showEditor = true;
  }

  edit(id: number): void {
    this.http.get<BillingInvoiceDetail>(`/api/billing/invoices/${id}`).subscribe({
      next: (invoice) => {
        this.editingId = invoice.id;
        this.form = {
          invoiceDate: this.toLocalInput(invoice.invoiceDate),
          status: invoice.status,
          ownerId: invoice.ownerId ?? null,
          animalId: invoice.animalId ?? null,
          doctorId: invoice.doctorId ?? null,
          registrationId: invoice.registrationId ?? null,
          appointmentId: invoice.appointmentId ?? null,
          currency: invoice.currency,
          discountAmount: invoice.discountAmount,
          notes: invoice.notes ?? '',
          lines: (invoice.lines ?? []).map((line) => ({
            moduleType: line.moduleType,
            description: line.description,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            discountAmount: line.discountAmount
          }))
        };
        this.transactions = invoice.transactions ?? [];
        this.showEditor = true;
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  save(): void {
    this.message = '';
    this.error = '';

    if (!this.form.currency.trim()) {
      this.error = 'Currency is required.';
      return;
    }
    if (!this.form.lines.length) {
      this.error = 'At least one billing line is required.';
      return;
    }
    for (const line of this.form.lines) {
      if (!line.description.trim() || line.quantity <= 0 || line.unitPrice < 0 || line.discountAmount < 0) {
        this.error = 'Each line needs description, positive quantity, and non-negative prices.';
        return;
      }
    }

    const payload = {
      ...this.form,
      invoiceDate: this.form.invoiceDate ? new Date(this.form.invoiceDate).toISOString() : null
    };
    const request = this.editingId == null
      ? this.http.post<BillingInvoiceDetail>('/api/billing/invoices', payload)
      : this.http.put<BillingInvoiceDetail>(`/api/billing/invoices/${this.editingId}`, payload);

    request.subscribe({
      next: (invoice) => {
        this.message = `Invoice ${this.editingId == null ? 'created' : 'updated'} successfully.`;
        this.editingId = invoice.id;
        this.transactions = invoice.transactions ?? [];
        this.loadInvoices(this.page);
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  remove(id: number): void {
    this.http.delete(`/api/billing/invoices/${id}`).subscribe({
      next: () => {
        this.message = 'Invoice deleted.';
        this.loadInvoices(this.page);
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  addLine(): void {
    this.form.lines.push({ moduleType: 1, description: '', quantity: 1, unitPrice: 0, discountAmount: 0 });
  }

  removeLine(index: number): void {
    this.form.lines.splice(index, 1);
    if (!this.form.lines.length) this.addLine();
  }

  addTransaction(): void {
    if (!this.editingId) return;
    if (this.transactionForm.amount <= 0) {
      this.error = 'Transaction amount must be greater than zero.';
      return;
    }

    this.http.post(`/api/billing/invoices/${this.editingId}/transactions`, this.transactionForm).subscribe({
      next: () => {
        this.message = 'Transaction added.';
        this.transactionForm = this.emptyTransactionForm();
        this.edit(this.editingId!);
        this.loadInvoices(this.page);
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  filteredAnimals(): Lookup[] {
    if (!this.form.ownerId) return this.animals;
    return this.animals.filter((animal) => animal.ownerId === this.form.ownerId);
  }

  onOwnerChanged(): void {
    this.form.animalId = null;
  }

  resetEditor(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.transactions = [];
    this.transactionForm = this.emptyTransactionForm();
  }

  statusLabel(value: number): string {
    return { 1: 'Draft', 2: 'Finalized', 3: 'Cancelled' }[value] ?? 'Unknown';
  }

  transactionTypeLabel(value: number): string {
    return { 1: 'Payment', 2: 'Refund', 3: 'Credit' }[value] ?? 'Unknown';
  }

  paymentMethodLabel(value: number): string {
    return { 1: 'Cash', 2: 'Card', 3: 'Cheque' }[value] ?? 'Unknown';
  }

  private loadLookups(): void {
    this.http.get<{ owners: Lookup[]; animals: Lookup[]; doctors: Lookup[]; registrations: Lookup[]; appointments: Lookup[] }>('/api/billing/lookups').subscribe({
      next: (res) => {
        this.owners = res.owners ?? [];
        this.animals = res.animals ?? [];
        this.doctors = res.doctors ?? [];
        this.registrations = res.registrations ?? [];
        this.appointments = res.appointments ?? [];
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  private emptyForm() {
    return {
      invoiceDate: this.toLocalInput(new Date().toISOString()),
      status: 1 as InvoiceStatus,
      ownerId: null as number | null,
      animalId: null as number | null,
      doctorId: null as number | null,
      registrationId: null as number | null,
      appointmentId: null as number | null,
      currency: 'AED',
      discountAmount: 0,
      notes: '',
      lines: [{ moduleType: 1 as BillingModuleType, description: '', quantity: 1, unitPrice: 0, discountAmount: 0 }]
    };
  }

  private emptyTransactionForm() {
    return {
      transactionType: 1 as TransactionType,
      method: 1 as PaymentMethod,
      amount: 0,
      transactedAt: null as string | null,
      reference: '',
      notes: ''
    };
  }

  private toLocalInput(isoDate: string): string {
    const d = new Date(isoDate);
    const tzOffsetMs = d.getTimezoneOffset() * 60000;
    return new Date(d.getTime() - tzOffsetMs).toISOString().slice(0, 16);
  }

  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) return firstMessage;
    }

    return error?.error?.message ?? error?.error?.title ?? error?.error ?? 'Billing request failed.';
  }
}
