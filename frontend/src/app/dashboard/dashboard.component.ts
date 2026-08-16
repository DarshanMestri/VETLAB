import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

type Kpi = { label: string; value: number; unit?: string | null };
type DashboardSection = { dashboard: string; kpis: Kpi[] };
type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };
type RevenueRow = {
  invoiceId: number;
  invoiceNumber: string;
  invoiceDate: string;
  ownerName?: string | null;
  netAmount: number;
  paymentAmount: number;
  outstandingAmount: number;
  currency: string;
};
type RevenueResponse = Paged<RevenueRow> & {
  summary: { netAmount: number; paymentAmount: number; outstandingAmount: number };
};
type PendingRow = {
  registrationItemId: number;
  registrationId: number;
  registrationBarcode: string;
  ownerName: string;
  testName?: string | null;
  status: string;
  sampleStatus: string;
  createdAt: string;
};
type TestStat = { testName: string; orderedCount: number; completedCount: number };
type SpeciesStat = { speciesName: string; registrationCount: number; testCount: number };
type InventoryRow = {
  purchaseLineId: number;
  medicineName: string;
  batchNumber: string;
  expiryDate?: string | null;
  quantity: number;
  availableQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isExpired: boolean;
};
type AuditRow = {
  id: number;
  tableName: string;
  recordId: string;
  action: string;
  changedBy?: string | null;
  changedAt: string;
};

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="dashboard-page container py-4">
      <section class="dashboard-hero rounded-4 p-4 p-md-5 mb-4">
        <div class="d-flex flex-column flex-lg-row align-items-start justify-content-between gap-3">
          <div>
            <span class="badge bg-primary bg-opacity-15 text-primary rounded-pill mb-3"
              >Overview</span
            >
            <h1 class="display-6 fw-semibold mb-2">Vet LIMS Dashboard</h1>
            <p class="text-muted mb-0">
              Role dashboards and analytical reports for hospital + laboratory operations.
            </p>
          </div>
          <div class="d-flex gap-2 flex-wrap">
            <div class="dashboard-chip rounded-4 px-3 py-2">
              <div class="small text-muted">Role</div>
              <div class="h5 mb-0">{{ activeDashboardName }}</div>
            </div>
            <div class="dashboard-chip rounded-4 px-3 py-2">
              <div class="small text-muted">Report Type</div>
              <div class="h6 mb-0 text-capitalize">{{ activeReport }}</div>
            </div>
            <div class="dashboard-chip rounded-4 px-3 py-2">
              <div class="small text-muted">Pending Reports</div>
              <div class="h5 mb-0">{{ pendingTotal }}</div>
            </div>
          </div>
        </div>
      </section>

      <section class="dashboard-section mb-4">
        <div class="card">
          <div class="card-body">
            <div class="d-flex flex-wrap gap-2 mb-3">
              <button
                class="btn btn-outline-primary btn-sm"
                (click)="loadDashboard('admin')"
                [class.active]="activeDashboard === 'admin'"
              >
                Admin Dashboard
              </button>
              <button
                class="btn btn-outline-primary btn-sm"
                (click)="loadDashboard('doctor')"
                [class.active]="activeDashboard === 'doctor'"
              >
                Doctor Dashboard
              </button>
              <button
                class="btn btn-outline-primary btn-sm"
                (click)="loadDashboard('laboratory')"
                [class.active]="activeDashboard === 'laboratory'"
              >
                Laboratory Dashboard
              </button>
              <button
                class="btn btn-outline-primary btn-sm"
                (click)="loadDashboard('reception')"
                [class.active]="activeDashboard === 'reception'"
              >
                Reception Dashboard
              </button>
            </div>
            <div class="row g-3" *ngIf="dashboardSection">
              <div class="col-12 col-sm-6 col-lg-3" *ngFor="let kpi of dashboardSection.kpis">
                <div class="card h-100 border-0 shadow-sm">
                  <div class="card-body">
                    <div class="small text-muted">{{ kpi.label }}</div>
                    <div class="h4 mb-0">
                      {{ kpi.value | number: '1.0-2' }}
                      <small class="text-muted">{{ kpi.unit || '' }}</small>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="dashboard-section">
        <div
          class="d-flex flex-column flex-md-row align-items-start justify-content-between gap-3 mb-3"
        >
          <div>
            <h2 class="h4 mb-1">Reports</h2>
            <p class="text-muted mb-0">
              Revenue, pending, test/species statistics, inventory, and audit logs.
            </p>
          </div>
          <div class="d-flex gap-2 flex-wrap">
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('revenue')">
              Revenue
            </button>
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('pending')">
              Pending Reports
            </button>
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('tests')">
              Test Statistics
            </button>
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('species')">
              Species Statistics
            </button>
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('inventory')">
              Inventory Reports
            </button>
            <button class="btn btn-outline-secondary btn-sm" (click)="setReport('audit')">
              Audit Reports
            </button>
          </div>
        </div>

        <div class="card mb-3">
          <div class="card-body">
            <div
              class="row g-2 mb-3"
              *ngIf="
                activeReport === 'revenue' ||
                activeReport === 'pending' ||
                activeReport === 'inventory' ||
                activeReport === 'audit'
              "
            >
              <div class="col-md-4">
                <input
                  class="form-control"
                  [(ngModel)]="search"
                  name="search"
                  placeholder="Search reports"
                />
              </div>
              <div class="col-md-3" *ngIf="activeReport === 'revenue' || activeReport === 'audit'">
                <input class="form-control" type="date" [(ngModel)]="fromDate" name="fromDate" />
              </div>
              <div class="col-md-3" *ngIf="activeReport === 'revenue' || activeReport === 'audit'">
                <input class="form-control" type="date" [(ngModel)]="toDate" name="toDate" />
              </div>
              <div class="col-md-2" *ngIf="activeReport === 'inventory'">
                <div class="form-check mt-2">
                  <input
                    class="form-check-input"
                    type="checkbox"
                    [(ngModel)]="lowStockOnly"
                    name="lowStockOnly"
                    id="lowStockOnly"
                  />
                  <label class="form-check-label" for="lowStockOnly">Low stock</label>
                </div>
              </div>
              <div class="col-md-2">
                <button class="btn btn-outline-primary w-100" (click)="loadReport(1)">
                  Search
                </button>
              </div>
            </div>

            <ng-container [ngSwitch]="activeReport">
              <div *ngSwitchCase="'revenue'">
                <div class="d-flex gap-3 mb-2">
                  <div><strong>Net:</strong> {{ revenueSummary.netAmount | number: '1.2-2' }}</div>
                  <div>
                    <strong>Paid:</strong> {{ revenueSummary.paymentAmount | number: '1.2-2' }}
                  </div>
                  <div>
                    <strong>Outstanding:</strong>
                    {{ revenueSummary.outstandingAmount | number: '1.2-2' }}
                  </div>
                </div>
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Invoice</th>
                        <th>Date</th>
                        <th>Owner</th>
                        <th>Net</th>
                        <th>Paid</th>
                        <th>Outstanding</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of revenueRows">
                        <td>{{ row.invoiceNumber }}</td>
                        <td>{{ row.invoiceDate | date: 'shortDate' }}</td>
                        <td>{{ row.ownerName || '-' }}</td>
                        <td>{{ row.netAmount | number: '1.2-2' }}</td>
                        <td>{{ row.paymentAmount | number: '1.2-2' }}</td>
                        <td>{{ row.outstandingAmount | number: '1.2-2' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div *ngSwitchCase="'pending'">
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Barcode</th>
                        <th>Owner</th>
                        <th>Test</th>
                        <th>Status</th>
                        <th>Sample</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of pendingRows">
                        <td>{{ row.registrationBarcode }}</td>
                        <td>{{ row.ownerName }}</td>
                        <td>{{ row.testName || '-' }}</td>
                        <td>{{ row.status }}</td>
                        <td>{{ row.sampleStatus }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div *ngSwitchCase="'tests'">
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Test</th>
                        <th>Ordered</th>
                        <th>Completed</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of testStats">
                        <td>{{ row.testName }}</td>
                        <td>{{ row.orderedCount }}</td>
                        <td>{{ row.completedCount }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div *ngSwitchCase="'species'">
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Species</th>
                        <th>Registrations</th>
                        <th>Tests</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of speciesStats">
                        <td>{{ row.speciesName }}</td>
                        <td>{{ row.registrationCount }}</td>
                        <td>{{ row.testCount }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div *ngSwitchCase="'inventory'">
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Medicine</th>
                        <th>Batch</th>
                        <th>Expiry</th>
                        <th>Available</th>
                        <th>Low Threshold</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of inventoryRows">
                        <td>{{ row.medicineName }}</td>
                        <td>{{ row.batchNumber }}</td>
                        <td [class.text-danger]="row.isExpired">{{ row.expiryDate || '-' }}</td>
                        <td [class.text-danger]="row.isLowStock">{{ row.availableQuantity }}</td>
                        <td>{{ row.lowStockThreshold }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div *ngSwitchCase="'audit'">
                <div class="table-responsive">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>At</th>
                        <th>User</th>
                        <th>Table</th>
                        <th>Action</th>
                        <th>Record</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let row of auditRows">
                        <td>{{ row.changedAt | date: 'short' }}</td>
                        <td>{{ row.changedBy || '-' }}</td>
                        <td>{{ row.tableName }}</td>
                        <td>{{ row.action }}</td>
                        <td>{{ row.recordId }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </ng-container>

            <div
              class="d-flex justify-content-between align-items-center mt-2"
              *ngIf="showPagination"
            >
              <small class="text-muted">Total: {{ reportTotal }}</small>
              <div class="d-flex gap-2">
                <button
                  class="btn btn-outline-secondary btn-sm"
                  [disabled]="reportPage <= 1"
                  (click)="loadReport(reportPage - 1)"
                >
                  Prev
                </button>
                <span class="small align-self-center">Page {{ reportPage }}</span>
                <button
                  class="btn btn-outline-secondary btn-sm"
                  [disabled]="reportPage * reportPageSize >= reportTotal"
                  (click)="loadReport(reportPage + 1)"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="row g-3 mt-2">
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <a class="dashboard-card card h-100 text-decoration-none" routerLink="/clients"
              ><div class="card-body">
                <div class="card-icon bg-primary bg-opacity-10 text-primary mb-3">👥</div>
                <h5 class="card-title mb-1">Client Master</h5>
                <p class="card-text text-muted">Create and manage clients.</p>
              </div></a
            >
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <a class="dashboard-card card h-100 text-decoration-none" routerLink="/results"
              ><div class="card-body">
                <div class="card-icon bg-info bg-opacity-10 text-info mb-3">✅</div>
                <h5 class="card-title mb-1">Result Entry</h5>
                <p class="card-text text-muted">Enter and validate results.</p>
              </div></a
            >
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <a class="dashboard-card card h-100 text-decoration-none" routerLink="/billing"
              ><div class="card-body">
                <div class="card-icon bg-warning bg-opacity-10 text-warning mb-3">🧾</div>
                <h5 class="card-title mb-1">Billing System</h5>
                <p class="card-text text-muted">Invoices and receipts.</p>
              </div></a
            >
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <a
              class="dashboard-card card h-100 text-decoration-none"
              routerLink="/inventory-pharmacy"
              ><div class="card-body">
                <div class="card-icon bg-success bg-opacity-10 text-success mb-3">💊</div>
                <h5 class="card-title mb-1">Inventory & Pharmacy</h5>
                <p class="card-text text-muted">Stock, expiry, low-stock alerts.</p>
              </div></a
            >
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .dashboard-page {
        max-width: 1180px;
      }

      .dashboard-hero {
        background: var(--color-card-background);
        border: 1px solid var(--color-border);
        box-shadow: 0 0.75rem 1.5rem rgba(15, 118, 110, 0.08);
      }

      .dashboard-chip {
        background: var(--color-card-background);
        border: 1px solid var(--color-border);
        min-width: 120px;
      }

      .dashboard-card {
        border: 1px solid var(--color-border);
        transition:
          transform 0.2s ease,
          box-shadow 0.2s ease,
          border-color 0.2s ease;
      }

      .dashboard-card:hover {
        transform: translateY(-6px);
        box-shadow: 0 1rem 1.6rem rgba(15, 23, 42, 0.12);
        border-color: var(--color-primary);
      }

      .dashboard-card .card-body {
        min-height: 150px;
        display: flex;
        flex-direction: column;
        justify-content: center;
      }

      .card-icon {
        width: 48px;
        height: 48px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        border-radius: 14px;
        font-size: 1.15rem;
      }

      @media (max-width: 575.98px) {
        .dashboard-hero {
          padding: 1.5rem 1rem;
        }
      }
    `,
  ],
})
export class DashboardComponent {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  dashboardSection: DashboardSection | null = null;
  activeDashboard: 'admin' | 'doctor' | 'laboratory' | 'reception' = 'admin';
  activeDashboardName = 'Admin';
  activeReport: 'revenue' | 'pending' | 'tests' | 'species' | 'inventory' | 'audit' = 'revenue';
  search = '';
  fromDate = '';
  toDate = '';
  lowStockOnly = false;

  revenueRows: RevenueRow[] = [];
  revenueSummary = { netAmount: 0, paymentAmount: 0, outstandingAmount: 0 };
  pendingRows: PendingRow[] = [];
  testStats: TestStat[] = [];
  speciesStats: SpeciesStat[] = [];
  inventoryRows: InventoryRow[] = [];
  auditRows: AuditRow[] = [];

  reportPage = 1;
  reportPageSize = 20;
  reportTotal = 0;
  pendingTotal = 0;

  get showPagination(): boolean {
    return (
      this.activeReport === 'revenue' ||
      this.activeReport === 'pending' ||
      this.activeReport === 'inventory' ||
      this.activeReport === 'audit'
    );
  }

  ngOnInit() {
    if (this.auth.hasRole('Doctor')) this.activeDashboard = 'doctor';
    else if (this.auth.hasRole('Laboratory')) this.activeDashboard = 'laboratory';
    else if (this.auth.hasRole('Reception')) this.activeDashboard = 'reception';
    else this.activeDashboard = 'admin';

    this.loadDashboard(this.activeDashboard);
    this.loadReport(1);
  }

  loadDashboard(type: 'admin' | 'doctor' | 'laboratory' | 'reception') {
    this.activeDashboard = type;
    this.activeDashboardName = type.charAt(0).toUpperCase() + type.slice(1);
    this.http.get<DashboardSection>(`/api/dashboardreports/dashboard/${type}`).subscribe({
      next: (res) => (this.dashboardSection = res),
    });
  }

  setReport(type: 'revenue' | 'pending' | 'tests' | 'species' | 'inventory' | 'audit') {
    this.activeReport = type;
    this.search = '';
    this.loadReport(1);
  }

  loadReport(page: number) {
    this.reportPage = page;
    const params: Record<string, string> = {
      page: String(page),
      pageSize: String(this.reportPageSize),
    };
    if (this.search.trim()) params['search'] = this.search.trim();
    if (this.fromDate) params['from'] = new Date(this.fromDate).toISOString();
    if (this.toDate) params['to'] = new Date(this.toDate).toISOString();

    if (this.activeReport === 'revenue') {
      this.http
        .get<RevenueResponse>('/api/dashboardreports/reports/revenue', { params })
        .subscribe((res) => {
          this.revenueRows = res.items ?? [];
          this.revenueSummary = res.summary ?? this.revenueSummary;
          this.reportTotal = res.total ?? 0;
        });
      return;
    }
    if (this.activeReport === 'pending') {
      this.http
        .get<Paged<PendingRow>>('/api/dashboardreports/reports/pending', { params })
        .subscribe((res) => {
          this.pendingRows = res.items ?? [];
          this.pendingTotal = res.total ?? 0;
          this.reportTotal = res.total ?? 0;
        });
      return;
    }
    if (this.activeReport === 'tests') {
      this.http
        .get<TestStat[]>('/api/dashboardreports/reports/test-statistics')
        .subscribe((res) => (this.testStats = res ?? []));
      this.reportTotal = 0;
      return;
    }
    if (this.activeReport === 'species') {
      this.http
        .get<SpeciesStat[]>('/api/dashboardreports/reports/species-statistics')
        .subscribe((res) => (this.speciesStats = res ?? []));
      this.reportTotal = 0;
      return;
    }
    if (this.activeReport === 'inventory') {
      if (this.lowStockOnly) params['lowStockOnly'] = 'true';
      this.http
        .get<Paged<InventoryRow>>('/api/dashboardreports/reports/inventory', { params })
        .subscribe((res) => {
          this.inventoryRows = res.items ?? [];
          this.reportTotal = res.total ?? 0;
        });
      return;
    }
    this.http
      .get<Paged<AuditRow>>('/api/dashboardreports/reports/audit', { params })
      .subscribe((res) => {
        this.auditRows = res.items ?? [];
        this.reportTotal = res.total ?? 0;
      });
  }
}
