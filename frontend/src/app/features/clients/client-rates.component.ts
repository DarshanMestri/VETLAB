import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClientRateApi, ClientRate, RateItemType } from './client-rate.api';

@Component({
  standalone: true,
  selector: 'app-client-rates',
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div class="row g-2 mb-2">
        <div class="col-auto">
          <select
            class="form-select form-select-sm"
            [(ngModel)]="filterType"
            (ngModelChange)="load()"
          >
            <option [ngValue]="undefined">All Types</option>
            <option [ngValue]="RateItemType.Test">Test</option>
            <option [ngValue]="RateItemType.Profile">Profile</option>
            <option [ngValue]="RateItemType.Package">Package</option>
          </select>
        </div>
        <div class="col text-end">
          <button class="btn btn-sm btn-primary" (click)="openCreate()">+ Add Rate</button>
          <button class="btn btn-sm btn-secondary ms-2" (click)="close.emit()">Close</button>
        </div>
      </div>

      <table class="table table-sm">
        <thead>
          <tr>
            <th>Type</th>
            <th>Item ID</th>
            <th class="text-end" style="width:150px">Rate</th>
            <th style="width:160px"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of items()">
            <td>{{ typeName(r.itemType) }}</td>
            <td>{{ r.itemId }}</td>
            <td class="text-end">{{ r.rate | number : '1.2-2' }} {{ r.currency || '' }}</td>
            <td class="text-end">
              <button class="btn btn-sm btn-outline-secondary me-1" (click)="openEdit(r)">
                Edit
              </button>
              <button class="btn btn-sm btn-outline-danger" (click)="del(r)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div *ngIf="showForm()" class="card mt-3">
        <div class="card-body">
          <form (ngSubmit)="save()">
            <div class="row g-2">
              <div class="col-md-3">
                <label class="form-label">Type</label>
                <select class="form-select" [(ngModel)]="form.itemType" name="itemType" required>
                  <option [ngValue]="RateItemType.Test">Test</option>
                  <option [ngValue]="RateItemType.Profile">Profile</option>
                  <option [ngValue]="RateItemType.Package">Package</option>
                </select>
              </div>
              <div class="col-md-3">
                <label class="form-label">Item ID</label>
                <input
                  class="form-control"
                  type="number"
                  [(ngModel)]="form.itemId"
                  name="itemId"
                  required
                />
              </div>
              <div class="col-md-3">
                <label class="form-label">Rate</label>
                <input
                  class="form-control"
                  type="number"
                  step="0.01"
                  [(ngModel)]="form.rate"
                  name="rate"
                  required
                />
              </div>
              <div class="col-md-3">
                <label class="form-label">Currency</label>
                <input class="form-control" [(ngModel)]="form.currency" name="currency" />
              </div>
            </div>
            <div class="form-check form-switch mt-2">
              <input
                class="form-check-input"
                type="checkbox"
                [(ngModel)]="form.isActive"
                name="isActive"
              />
              <label class="form-check-label">Active</label>
            </div>

            <div class="text-end mt-3">
              <button class="btn btn-secondary me-2" type="button" (click)="closeForm()">
                Cancel
              </button>
              <button class="btn btn-primary">Save</button>
            </div>

            <div *ngIf="error()" class="alert alert-danger mt-3">{{ error() }}</div>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class ClientRatesComponent {
  @Input({ required: true }) clientId!: number;
  @Output() close = new EventEmitter<void>();

  RateItemType = RateItemType;
  private api = inject(ClientRateApi);

  items = signal<ClientRate[]>([]);
  filterType?: RateItemType;
  showForm = signal(false);
  editId: number | null = null;
  error = signal('');

  form: Omit<ClientRate, 'id' | 'clientId'> & { clientId?: number } = {
    itemId: 0,
    itemType: RateItemType.Test,
    rate: 0,
    currency: '',
    isActive: true,
  };

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.list(this.clientId, this.filterType).subscribe({
      next: (res) => this.items.set(res),
      error: (_) => this.error.set('Load failed'),
    });
  }

  typeName(t: RateItemType) {
    return RateItemType[t];
  }

  openCreate() {
    this.editId = null;
    this.form = { itemId: 0, itemType: RateItemType.Test, rate: 0, currency: '', isActive: true };
    this.showForm.set(true);
  }
  openEdit(r: ClientRate) {
    this.editId = r.id;
    this.form = {
      itemId: r.itemId,
      itemType: r.itemType,
      rate: r.rate,
      currency: r.currency || '',
      isActive: r.isActive,
    };
    this.showForm.set(true);
  }
  closeForm() {
    this.showForm.set(false);
  }

  save() {
    this.error.set('');
    if (this.editId) {
      this.api
        .update(this.editId, {
          itemId: this.form.itemId,
          itemType: this.form.itemType,
          rate: this.form.rate,
          currency: this.form.currency,
          isActive: this.form.isActive,
        })
        .subscribe({
          next: (_) => {
            this.showForm.set(false);
            this.load();
          },
          error: (err) => this.error.set(err.error ?? 'Save failed'),
        });
    } else {
      this.api
        .create({
          clientId: this.clientId,
          itemId: this.form.itemId,
          itemType: this.form.itemType,
          rate: this.form.rate,
          currency: this.form.currency,
          isActive: this.form.isActive,
        })
        .subscribe({
          next: (_) => {
            this.showForm.set(false);
            this.load();
          },
          error: (err) => this.error.set(err.error ?? 'Save failed'),
        });
    }
  }

  del(r: ClientRate) {
    if (!confirm('Delete this rate?')) return;
    this.api.delete(r.id).subscribe({ next: (_) => this.load() });
  }
}
