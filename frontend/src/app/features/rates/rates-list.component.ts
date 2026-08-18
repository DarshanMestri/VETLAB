import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  RateService,
  RateClient,
  RateItem,
  ClientRate,
  BulkRateItem,
} from './rate.service';

interface RateRow {
  itemId: number;
  code?: string | null;
  name: string;
  price: number | null;
  priceInput: number | null;
  vatRate: number;
  isActive: boolean;
  dirty: boolean;
  rateId: number | null;
}

interface TabState {
  itemType: number;
  label: string;
  loading: boolean;
  loaded: boolean;
  search: string;
  rows: RateRow[];
}

const TABS: { itemType: number; label: string; load: (svc: RateService) => any }[] = [
  { itemType: 1, label: 'Tests', load: (s) => s.getTests() },
  { itemType: 2, label: 'Profiles', load: (s) => s.getProfiles() },
  { itemType: 3, label: 'Packages', load: (s) => s.getPackages() },
  { itemType: 4, label: 'Medicines', load: (s) => s.getMedicines() },
];

@Component({
  standalone: true,
  selector: 'app-rates-list',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 class="mb-0">Rate Master</h2>
          <small class="text-muted">Per-client pricing for tests, profiles, packages & medicines</small>
        </div>
      </div>

      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>
      <div *ngIf="success()" class="alert alert-success">{{ success() }}</div>

      <section class="card mb-3">
        <div class="card-body py-3">
          <label class="form-label fw-semibold mb-1" for="clientSelect">Client</label>
          <select
            id="clientSelect"
            class="form-select"
            [disabled]="loadingClients()"
            [(ngModel)]="selectedClientId"
            (ngModelChange)="onClientChange()"
          >
            <option [ngValue]="null" disabled>Select a client to manage pricing…</option>
            <option *ngFor="let c of clients()" [ngValue]="c.id">{{ c.name }}<span *ngIf="c.code"> ({{ c.code }})</span></option>
          </select>
          <p *ngIf="!clients().length && !loadingClients()" class="text-muted mt-2 mb-0">
            No clients found. Create a client in the Clients screen first.
          </p>
          <p *ngIf="selectedClient()" class="text-muted mt-2 mb-0">
            {{ pricedSummary() }}
          </p>
        </div>
      </section>

      <div *ngIf="selectedClient()">
        <ul class="nav nav-pills mb-3" role="tablist">
          <li class="nav-item" role="presentation" *ngFor="let tab of tabDefs">
            <button
              class="nav-link"
              [class.active]="activeItemType() === tab.itemType"
              (click)="switchTab(tab.itemType)"
              type="button"
            >
              {{ tab.label }}
              <span class="badge text-bg-secondary ms-1">{{ pricedCount(tab.itemType) }}</span>
            </button>
          </li>
        </ul>

        <section class="card">
          <div class="card-body">
            <div *ngIf="activeTab()?.loading" class="text-muted py-3">Loading…</div>

            <ng-container *ngIf="activeTab() && !activeTab()!.loading">
              <div class="d-flex flex-wrap justify-content-between gap-2 mb-3">
                <div class="input-group" style="max-width: 320px">
                  <input
                    class="form-control"
                    placeholder="Search code or name"
                    [(ngModel)]="activeTab()!.search"
                    (ngModelChange)="onSearch()"
                  />
                  <button class="btn btn-outline-secondary" (click)="clearSearch()">Clear</button>
                </div>
                <div class="d-flex align-items-center gap-3">
                  <div class="form-check form-switch mb-0">
                    <input
                      class="form-check-input"
                      type="checkbox"
                      role="switch"
                      id="showOnlyPriced"
                      [(ngModel)]="showOnlyPriced"
                    />
                    <label class="form-check-label small" for="showOnlyPriced">Show only priced items</label>
                  </div>
                  <span class="text-muted small" *ngIf="dirtyCount()">
                    {{ dirtyCount() }} unsaved change{{ dirtyCount() === 1 ? '' : 's' }}
                  </span>
                  <button
                    class="btn btn-primary"
                    [disabled]="saving() || !dirtyCount()"
                    (click)="saveActiveTab()"
                  >
                    {{ saving() ? 'Saving…' : 'Save Prices' }}
                  </button>
                </div>
              </div>

              <p class="text-muted small mb-2">
                Prices exclude VAT — VAT % (default 5) is applied on top and shown in the total column.
                Leave the price blank and Save to remove a price. Use the Active switch to keep a price but stop applying it.
              </p>

              <p class="text-muted small mb-2" *ngIf="showOnlyPriced && !filteredRows().length">
                No priced items yet. Turn off “Show only priced items” to add prices.
              </p>

              <div class="table-responsive" *ngIf="filteredRows().length; else emptyRows">
                <table class="table table-striped table-hover align-middle">
                  <thead>
                    <tr>
                      <th style="width: 100px">Code</th>
                      <th>Name</th>
                      <th style="width: 140px">Price (AED)</th>
                      <th style="width: 100px">VAT %</th>
                      <th style="width: 110px">VAT (AED)</th>
                      <th style="width: 120px">Total (AED)</th>
                      <th style="width: 90px">Active</th>
                      <th style="width: 90px" class="text-end"></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let row of filteredRows()" [class.table-warning]="row.dirty">
                      <td>{{ row.code || '—' }}</td>
                      <td>{{ row.name }}</td>
                      <td>
                        <input
                          type="number"
                          class="form-control form-control-sm"
                          style="max-width: 130px"
                          step="0.01"
                          min="0"
                          placeholder="e.g. 150.00"
                          [ngModel]="row.priceInput"
                          (ngModelChange)="onPriceChange(row, $event)"
                        />
                      </td>
                      <td>
                        <input
                          type="number"
                          class="form-control form-control-sm"
                          style="max-width: 80px"
                          step="0.5"
                          min="0"
                          max="100"
                          [ngModel]="row.vatRate"
                          (ngModelChange)="onVatChange(row, $event)"
                        />
                      </td>
                      <td>{{ vatAmount(row) | currency:'AED' }}</td>
                      <td class="fw-semibold">{{ inclTotal(row) | currency:'AED' }}</td>
                      <td>
                        <div class="form-check form-switch">
                          <input
                            class="form-check-input"
                            type="checkbox"
                            role="switch"
                            [disabled]="!canToggleActive(row)"
                            [(ngModel)]="row.isActive"
                            (ngModelChange)="markDirty(row)"
                          />
                        </div>
                      </td>
                      <td class="text-end">
                        <span *ngIf="row.rateId" class="badge text-bg-success">Set</span>
                        <span *ngIf="isNewPrice(row)" class="badge text-bg-info">New</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <ng-template #emptyRows>
                <p class="text-muted py-3">No {{ activeTab()?.label?.toLowerCase() }} match your search.</p>
              </ng-template>
            </ng-container>
          </div>
        </section>
      </div>
    </div>
  `,
})
export class RatesListComponent implements OnInit {
  private svc = inject(RateService);

  readonly tabDefs = TABS;

  clients = signal<RateClient[]>([]);
  loadingClients = signal(false);
  selectedClientId: number | null = null;

  activeItemType = signal<number>(1);
  saving = signal(false);
  error = signal('');
  success = signal('');
  showOnlyPriced = true;

  private tabs = new Map<number, TabState>();
  private loadedRates = new Map<number, ClientRate[]>();

  ngOnInit(): void {
    this.loadClients();
  }

  selectedClient(): RateClient | null {
    if (this.selectedClientId == null) return null;
    return this.clients().find((c) => c.id === this.selectedClientId) ?? null;
  }

  activeTab(): TabState | undefined {
    return this.tabs.get(this.activeItemType());
  }

  private loadClients(): void {
    this.loadingClients.set(true);
    this.svc.getClients().subscribe({
      next: (clients) => {
        this.clients.set(clients.filter((c) => c.isActive));
        this.loadingClients.set(false);
      },
      error: () => {
        this.clients.set([]);
        this.loadingClients.set(false);
        this.error.set('Failed to load clients.');
      },
    });
  }

  onClientChange(): void {
    this.error.set('');
    this.success.set('');
    this.tabs.clear();
    this.loadedRates.clear();
    if (this.selectedClientId == null) return;
    this.loadRatesForClient(this.selectedClientId);
    this.switchTab(this.activeItemType());
  }

  switchTab(itemType: number): void {
    this.activeItemType.set(itemType);
    const tab = this.ensureTab(itemType);
    if (!tab.loaded && !tab.loading) this.loadTab(tab);
  }

  private ensureTab(itemType: number): TabState {
    let tab = this.tabs.get(itemType);
    if (!tab) {
      const def = TABS.find((t) => t.itemType === itemType)!;
      tab = { itemType, label: def.label, loading: false, loaded: false, search: '', rows: [] };
      this.tabs.set(itemType, tab);
    }
    return tab;
  }

  private loadTab(tab: TabState): void {
    const def = TABS.find((t) => t.itemType === tab.itemType)!;
    tab.loading = true;
    this.error.set('');
    def.load(this.svc).subscribe({
      next: (items: RateItem[]) => {
        tab.loading = false;
        tab.loaded = true;
        tab.rows = items.map((it) => ({
          itemId: it.id,
          code: it.code,
          name: it.name,
          price: null,
          priceInput: null,
          vatRate: 5,
          isActive: true,
          dirty: false,
          rateId: null,
        }));
        this.applyRatesToTab(tab);
      },
      error: () => {
        tab.loading = false;
        this.error.set(`Failed to load ${tab.label.toLowerCase()}.`);
      },
    });
  }

  private loadRatesForClient(clientId: number): void {
    this.svc.listRates(clientId).subscribe({
      next: (rates) => {
        this.loadedRates.set(clientId, rates);
        for (const tab of this.tabs.values()) {
          if (tab.loaded) this.applyRatesToTab(tab);
        }
      },
      error: () => this.error.set('Failed to load existing prices.'),
    });
  }

  private applyRatesToTab(tab: TabState): void {
    const rates = this.loadedRates.get(this.selectedClientId ?? -1) ?? [];
    const byId = new Map<number, ClientRate>();
    for (const r of rates) if (r.itemType === tab.itemType) byId.set(r.itemId, r);

    for (const row of tab.rows) {
      const r = byId.get(row.itemId);
      row.rateId = r?.id ?? null;
      row.price = r?.rate ?? null;
      row.priceInput = r?.rate ?? null;
      row.vatRate = r?.vatRate ?? 5;
      row.isActive = r?.isActive ?? true;
      row.dirty = false;
    }
  }

  onPriceChange(row: RateRow, value: number | null): void {
    row.priceInput = typeof value === 'number' && !Number.isNaN(value) ? value : null;
    this.markDirty(row);
  }

  onVatChange(row: RateRow, value: number | null): void {
    row.vatRate = typeof value === 'number' && !Number.isNaN(value) ? value : 0;
    this.markDirty(row);
  }

  vatAmount(row: RateRow): number {
    const price = row.priceInput ?? 0;
    if (price <= 0) return 0;
    return Math.round(price * row.vatRate) / 100;
  }

  inclTotal(row: RateRow): number {
    return (row.priceInput ?? 0) + this.vatAmount(row);
  }

  markDirty(row: RateRow): void {
    row.dirty = true;
    this.success.set('');
  }

  onSearch(): void {}

  clearSearch(): void {
    const tab = this.activeTab();
    if (tab) tab.search = '';
  }

  filteredRows(): RateRow[] {
    const tab = this.activeTab();
    if (!tab) return [];
    let rows = tab.rows;
    if (this.showOnlyPriced) {
      rows = rows.filter((r) => r.rateId != null || (r.priceInput != null && r.priceInput > 0));
    }
    const term = tab.search.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter(
      (r) => (r.code ?? '').toLowerCase().includes(term) || r.name.toLowerCase().includes(term)
    );
  }

  canToggleActive(row: RateRow): boolean {
    return row.rateId != null || (row.priceInput != null && row.priceInput > 0);
  }

  isNewPrice(row: RateRow): boolean {
    return row.rateId == null && row.priceInput != null && row.priceInput > 0;
  }

  pricedCount(itemType: number): number {
    const tab = this.tabs.get(itemType);
    if (!tab) return 0;
    return tab.rows.filter((r) => r.rateId != null).length;
  }

  dirtyCount(): number {
    const tab = this.activeTab();
    if (!tab) return 0;
    return tab.rows.filter((r) => r.dirty).length;
  }

  pricedSummary(): string {
    const client = this.selectedClient();
    if (!client) return '';
    const total = [...this.tabs.values()].reduce((n, t) => n + this.pricedCount(t.itemType), 0);
    return `${total} price${total === 1 ? '' : 's'} set for ${client.name}.`;
  }

  saveActiveTab(): void {
    const tab = this.activeTab();
    const client = this.selectedClient();
    if (!tab || !client || !this.dirtyCount()) return;

    const items: BulkRateItem[] = tab.rows
      .filter((r) => r.dirty)
      .map((r) => {
        const value = r.priceInput;
        const hasValue = value != null && value > 0;
        return {
          itemType: tab.itemType,
          itemId: r.itemId,
          rate: hasValue ? value : null,
          vatRate: r.vatRate,
          isActive: r.isActive,
          currency: 'AED',
        };
      });

    this.saving.set(true);
    this.error.set('');
    this.success.set('');
    this.svc.bulkUpsert(client.id, items).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.loadRatesForClient(client.id);
        this.success.set(
          `${res.created} added, ${res.updated} updated, ${res.removed} removed.`
        );
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.title ?? err?.error ?? 'Failed to save prices.');
      },
    });
  }
}
