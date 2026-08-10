import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };
type Lookup = { id: number; code?: string; name?: string };

type PurchaseRow = {
  id: number;
  purchaseNumber: string;
  purchaseDate: string;
  supplierId: number;
  supplierName: string;
  currency: string;
  totalAmount: number;
};

type PurchaseLine = {
  id: number;
  medicineId: number;
  medicineName: string;
  inventoryCategoryId?: number | null;
  inventoryCategoryName?: string | null;
  batchNumber: string;
  expiryDate?: string | null;
  quantity: number;
  unitCost: number;
  lowStockThreshold: number;
  returnedQuantity: number;
  consumedQuantity: number;
  availableQuantity: number;
  lineAmount: number;
};

type PurchaseDetail = {
  id: number;
  purchaseNumber: string;
  purchaseDate: string;
  supplierId: number;
  supplierName: string;
  currency: string;
  invoiceReference?: string | null;
  notes?: string | null;
  totalAmount: number;
  lines: PurchaseLine[];
};

type StockBatch = {
  purchaseLineId: number;
  purchaseNumber: string;
  purchaseDate: string;
  medicineName: string;
  batchNumber: string;
  expiryDate?: string | null;
  quantity: number;
  returnedQuantity: number;
  consumedQuantity: number;
  availableQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isExpired: boolean;
};

@Component({
  standalone: true,
  selector: 'app-inventory-pharmacy-management',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-1">Inventory & Pharmacy</h2>
          <p class="text-muted mb-0">Purchase, returns, stock batches, expiry, consumption, and low-stock alerts.</p>
        </div>
      </div>

      <div class="alert alert-success" *ngIf="message">{{ message }}</div>
      <div class="alert alert-danger" *ngIf="error">{{ error }}</div>

      <div class="card mb-3">
        <div class="card-body d-flex gap-2">
          <button class="btn btn-outline-primary btn-sm" (click)="tab='purchases'">Purchases</button>
          <button class="btn btn-outline-primary btn-sm" (click)="tab='stock'">Stock</button>
          <button class="btn btn-outline-primary btn-sm" (click)="loadLowStock()">Low Stock Alerts</button>
        </div>
      </div>

      <ng-container *ngIf="tab==='purchases'; else stockTab">
        <div class="card mb-3">
          <div class="card-header">Create Purchase</div>
          <div class="card-body">
            <div class="row g-2 mb-2">
              <div class="col-md-3">
                <label class="form-label">Supplier</label>
                <select class="form-select" [(ngModel)]="purchaseForm.supplierId" name="supplierId">
                  <option [ngValue]="null">Select supplier</option>
                  <option *ngFor="let s of suppliers" [ngValue]="s.id">{{ s.name }}{{ s.code ? ' • ' + s.code : '' }}</option>
                </select>
              </div>
              <div class="col-md-2">
                <label class="form-label">Currency</label>
                <input class="form-control" [(ngModel)]="purchaseForm.currency" name="currency" />
              </div>
              <div class="col-md-3">
                <label class="form-label">Invoice Ref</label>
                <input class="form-control" [(ngModel)]="purchaseForm.invoiceReference" name="invoiceReference" />
              </div>
              <div class="col-md-4">
                <label class="form-label">Notes</label>
                <input class="form-control" [(ngModel)]="purchaseForm.notes" name="purchaseNotes" />
              </div>
            </div>
            <div class="d-flex justify-content-between align-items-center mb-2">
              <h6 class="mb-0">Purchase Lines</h6>
              <button class="btn btn-outline-secondary btn-sm" (click)="addPurchaseLine()">Add Line</button>
            </div>
            <div class="border rounded p-2 mb-2" *ngFor="let l of purchaseForm.lines; let i = index">
              <div class="row g-2">
                <div class="col-md-3">
                  <select class="form-select form-select-sm" [(ngModel)]="l.medicineId" [name]="'medicine'+i">
                    <option [ngValue]="null">Medicine</option>
                    <option *ngFor="let m of medicines" [ngValue]="m.id">{{ m.name }}{{ m.code ? ' • ' + m.code : '' }}</option>
                  </select>
                </div>
                <div class="col-md-2">
                  <select class="form-select form-select-sm" [(ngModel)]="l.inventoryCategoryId" [name]="'category'+i">
                    <option [ngValue]="null">Category</option>
                    <option *ngFor="let c of categories" [ngValue]="c.id">{{ c.name }}</option>
                  </select>
                </div>
                <div class="col-md-2">
                  <input class="form-control form-control-sm" [(ngModel)]="l.batchNumber" [name]="'batch'+i" placeholder="Batch" />
                </div>
                <div class="col-md-2">
                  <input class="form-control form-control-sm" type="date" [(ngModel)]="l.expiryDate" [name]="'expiry'+i" />
                </div>
                <div class="col-md-1">
                  <input class="form-control form-control-sm" type="number" min="0.001" step="0.001" [(ngModel)]="l.quantity" [name]="'qty'+i" placeholder="Qty" />
                </div>
                <div class="col-md-1">
                  <input class="form-control form-control-sm" type="number" min="0" step="0.01" [(ngModel)]="l.unitCost" [name]="'cost'+i" placeholder="Cost" />
                </div>
                <div class="col-md-1">
                  <input class="form-control form-control-sm" type="number" min="0" step="0.001" [(ngModel)]="l.lowStockThreshold" [name]="'threshold'+i" placeholder="Low" />
                </div>
              </div>
            </div>
            <button class="btn btn-primary btn-sm" (click)="createPurchase()">Save Purchase</button>
          </div>
        </div>

        <div class="card">
          <div class="card-header">Purchases</div>
          <div class="card-body p-0">
            <table class="table table-sm mb-0" *ngIf="purchases.length">
              <thead><tr><th>No</th><th>Date</th><th>Supplier</th><th>Total</th><th></th></tr></thead>
              <tbody>
                <tr *ngFor="let p of purchases">
                  <td>{{ p.purchaseNumber }}</td>
                  <td>{{ p.purchaseDate | date:'shortDate' }}</td>
                  <td>{{ p.supplierName }}</td>
                  <td>{{ p.totalAmount | number:'1.2-2' }} {{ p.currency }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-primary" (click)="openPurchase(p.id)">Details</button></td>
                </tr>
              </tbody>
            </table>
            <p class="p-3 mb-0 text-muted" *ngIf="!purchases.length">No purchases.</p>
          </div>
        </div>
      </ng-container>

      <ng-template #stockTab>
        <div class="card mb-3">
          <div class="card-body row g-2">
            <div class="col-md-4"><input class="form-control" [(ngModel)]="stockSearch" name="stockSearch" placeholder="Search medicine/batch/purchase" /></div>
            <div class="col-md-2"><input class="form-control" type="number" min="0" [(ngModel)]="expiryWithinDays" name="expiryWithinDays" placeholder="Expiry days" /></div>
            <div class="col-md-2 form-check ms-2 mt-2"><input class="form-check-input" type="checkbox" [(ngModel)]="lowStockOnly" name="lowStockOnly" id="lowStockOnly" /><label class="form-check-label" for="lowStockOnly">Low stock only</label></div>
            <div class="col-md-2"><button class="btn btn-outline-primary w-100" (click)="loadStock()">Search</button></div>
          </div>
        </div>
        <div class="card">
          <div class="card-header">Stock Batches</div>
          <div class="card-body p-0">
            <table class="table table-sm mb-0" *ngIf="stockRows.length">
              <thead><tr><th>Medicine</th><th>Batch</th><th>Expiry</th><th>Available</th><th>Low Threshold</th><th></th></tr></thead>
              <tbody>
                <tr *ngFor="let s of stockRows">
                  <td>{{ s.medicineName }}</td>
                  <td>{{ s.batchNumber }}</td>
                  <td [class.text-danger]="s.isExpired">{{ s.expiryDate || '-' }}</td>
                  <td [class.text-danger]="s.isLowStock">{{ s.availableQuantity }}</td>
                  <td>{{ s.lowStockThreshold }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-secondary" (click)="consume(s.purchaseLineId)">Consume</button></td>
                </tr>
              </tbody>
            </table>
            <p class="p-3 mb-0 text-muted" *ngIf="!stockRows.length">No stock records.</p>
          </div>
        </div>
      </ng-template>

      <div class="modal d-block" tabindex="-1" *ngIf="selectedPurchase">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Purchase {{ selectedPurchase.purchaseNumber }}</h5>
              <button type="button" class="btn-close" (click)="selectedPurchase=null"></button>
            </div>
            <div class="modal-body">
              <p class="mb-2"><strong>Total:</strong> {{ selectedPurchase.totalAmount | number:'1.2-2' }} {{ selectedPurchase.currency }}</p>
              <table class="table table-sm">
                <thead><tr><th>Medicine</th><th>Batch</th><th>Qty</th><th>Available</th><th>Return</th></tr></thead>
                <tbody>
                  <tr *ngFor="let line of selectedPurchase.lines">
                    <td>{{ line.medicineName }}</td>
                    <td>{{ line.batchNumber }}</td>
                    <td>{{ line.quantity }}</td>
                    <td>{{ line.availableQuantity }}</td>
                    <td><button class="btn btn-sm btn-outline-warning" (click)="returnLine(line.id)">Return</button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InventoryPharmacyManagementComponent {
  private readonly http = inject(HttpClient);

  tab: 'purchases' | 'stock' = 'purchases';
  message = '';
  error = '';

  suppliers: Lookup[] = [];
  medicines: Lookup[] = [];
  categories: Lookup[] = [];

  purchases: PurchaseRow[] = [];
  selectedPurchase: PurchaseDetail | null = null;
  stockRows: StockBatch[] = [];

  stockSearch = '';
  lowStockOnly = false;
  expiryWithinDays: number | null = null;

  purchaseForm = this.emptyPurchaseForm();

  ngOnInit(): void {
    this.loadLookups();
    this.loadPurchases();
    this.loadStock();
  }

  loadLookups(): void {
    this.http.get<{ suppliers: Lookup[]; medicines: Lookup[]; inventoryCategories: Lookup[] }>('/api/inventorypharmacy/lookups').subscribe({
      next: (res) => {
        this.suppliers = res.suppliers ?? [];
        this.medicines = res.medicines ?? [];
        this.categories = res.inventoryCategories ?? [];
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  loadPurchases(): void {
    this.http.get<Paged<PurchaseRow>>('/api/inventorypharmacy/purchases?page=1&pageSize=50').subscribe({
      next: (res) => this.purchases = res.items ?? [],
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  loadStock(): void {
    const params: Record<string, string> = { page: '1', pageSize: '100' };
    if (this.stockSearch.trim()) params['search'] = this.stockSearch.trim();
    if (this.lowStockOnly) params['lowStockOnly'] = 'true';
    if (this.expiryWithinDays != null) params['expiryWithinDays'] = String(this.expiryWithinDays);
    this.http.get<Paged<StockBatch>>('/api/inventorypharmacy/stock', { params }).subscribe({
      next: (res) => this.stockRows = res.items ?? [],
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  loadLowStock(): void {
    this.tab = 'stock';
    this.lowStockOnly = true;
    this.loadStock();
  }

  addPurchaseLine(): void {
    this.purchaseForm.lines.push({ medicineId: null, inventoryCategoryId: null, batchNumber: '', expiryDate: null, quantity: 1, unitCost: 0, lowStockThreshold: 0 });
  }

  createPurchase(): void {
    this.error = '';
    if (!this.purchaseForm.supplierId) {
      this.error = 'Supplier is required.';
      return;
    }
    if (!this.purchaseForm.lines.length) {
      this.error = 'At least one purchase line is required.';
      return;
    }
    const payload = {
      purchaseDate: new Date().toISOString(),
      supplierId: this.purchaseForm.supplierId,
      currency: this.purchaseForm.currency,
      invoiceReference: this.purchaseForm.invoiceReference,
      notes: this.purchaseForm.notes,
      lines: this.purchaseForm.lines.map((l: any) => ({
        medicineId: l.medicineId,
        inventoryCategoryId: l.inventoryCategoryId,
        batchNumber: l.batchNumber,
        expiryDate: l.expiryDate || null,
        quantity: l.quantity,
        unitCost: l.unitCost,
        lowStockThreshold: l.lowStockThreshold
      }))
    };
    this.http.post<PurchaseDetail>('/api/inventorypharmacy/purchases', payload).subscribe({
      next: () => {
        this.message = 'Purchase created.';
        this.purchaseForm = this.emptyPurchaseForm();
        this.loadPurchases();
        this.loadStock();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  openPurchase(id: number): void {
    this.http.get<PurchaseDetail>(`/api/inventorypharmacy/purchases/${id}`).subscribe({
      next: (res) => this.selectedPurchase = res,
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  returnLine(purchaseLineId: number): void {
    if (!this.selectedPurchase) return;
    const quantityText = prompt('Return quantity?');
    const quantity = Number(quantityText);
    if (!quantity || quantity <= 0) return;

    this.http.post(`/api/inventorypharmacy/purchases/${this.selectedPurchase.id}/returns`, {
      returnDate: new Date().toISOString(),
      notes: 'UI return',
      lines: [{ purchaseLineId, quantity, reason: 'Return' }]
    }).subscribe({
      next: () => {
        this.message = 'Purchase return recorded.';
        this.openPurchase(this.selectedPurchase!.id);
        this.loadStock();
        this.loadPurchases();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  consume(purchaseLineId: number): void {
    const quantityText = prompt('Consumption quantity?');
    const quantity = Number(quantityText);
    if (!quantity || quantity <= 0) return;

    this.http.post(`/api/inventorypharmacy/stock/${purchaseLineId}/consume`, {
      quantity,
      consumedAt: new Date().toISOString(),
      notes: 'UI consumption'
    }).subscribe({
      next: () => {
        this.message = 'Stock consumption saved.';
        this.loadStock();
      },
      error: (err) => this.error = this.errorMessage(err)
    });
  }

  private emptyPurchaseForm() {
    return {
      supplierId: null as number | null,
      currency: 'AED',
      invoiceReference: '',
      notes: '',
      lines: [{ medicineId: null as number | null, inventoryCategoryId: null as number | null, batchNumber: '', expiryDate: null as string | null, quantity: 1, unitCost: 0, lowStockThreshold: 0 }]
    };
  }

  private errorMessage(error: any): string {
    const validationErrors = error?.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstKey = Object.keys(validationErrors)[0];
      const firstMessage = firstKey ? validationErrors[firstKey]?.[0] : null;
      if (typeof firstMessage === 'string' && firstMessage.length > 0) return firstMessage;
    }

    return error?.error?.message ?? error?.error?.title ?? error?.error ?? 'Inventory request failed.';
  }
}
