// src/app/features/users/user-access-dialog.component.ts
import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import {
  BranchAccessItem,
  BranchAccessRow,
  OrgBranch,
  UserListItem,
  UsersService,
} from './users.service';

interface EditRow {
  branchId: number;
  organizationId: number;
  organizationName: string;
  name: string;
  code: string | null;
  laboratoryName: string;
  clinicName: string;
  included: boolean;
  clinic: boolean;
  lab: boolean;
  isDefault: boolean;
}

interface OrgGroup {
  id: number;
  name: string;
  rows: EditRow[];
}

@Component({
  standalone: true,
  selector: 'app-user-access-dialog',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="ua-backdrop" (click)="close()">
      <div class="ua-dialog" (click)="$event.stopPropagation()">
        <header class="ua-header">
          <div>
            <h3>Branch &amp; Organization Access</h3>
            <p>{{ user().displayName || user().userName }} · {{ user().roles.join(', ') }}</p>
          </div>
          <button type="button" class="ua-close" (click)="close()" aria-label="Close">&times;</button>
        </header>

        <div *ngIf="isAdmin()" class="alert alert-info ua-note">
          Administrator accounts implicitly hold every active branch of their home organization. Explicit grants are
          only used for non-admin roles.
        </div>

        <div *ngIf="error()" class="alert alert-danger ua-note">{{ error() }}</div>

        <div class="ua-toolbar">
          <input
            class="form-control form-control-sm"
            type="search"
            placeholder="Search organization or branch…"
            [ngModel]="search()"
            (ngModelChange)="search.set($event)"
          />
          <span class="ua-count">
            {{ grantedCount() }} of {{ rows().length }} branches granted
          </span>
        </div>

        <div class="ua-body">
          <ng-container *ngIf="!loading(); else loadingTpl">
            <div class="table-responsive">
              <table class="table table-sm align-middle">
                <thead>
                  <tr>
                    <th style="width:64px">Access</th>
                    <th>Branch</th>
                    <th class="text-center" style="width:70px">Clinic</th>
                    <th class="text-center" style="width:90px">Laboratory</th>
                    <th class="text-center" style="width:80px">Default</th>
                  </tr>
                </thead>
                <tbody>
                  @for (group of groups(); track group.id) {
                    <tr class="org-row">
                      <td class="text-center">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          title="Select all branches of this organization"
                          [disabled]="locked()"
                          [checked]="isOrgFullySelected(group)"
                          (change)="toggleOrg(group, $any($event.target).checked)"
                        />
                      </td>
                      <td colspan="4">
                        <strong>{{ group.name }}</strong>
                        <small>{{ selectedIn(group) }}/{{ group.rows.length }} granted</small>
                      </td>
                    </tr>
                    @for (row of group.rows; track row.branchId) {
                      <tr [class.row-disabled]="!row.included && !isAdmin()">
                        <td class="text-center">
                          <input
                            type="checkbox"
                            class="form-check-input"
                            [disabled]="locked()"
                            [(ngModel)]="row.included"
                            (ngModelChange)="onIncludedChange(row)"
                          />
                        </td>
                        <td>
                          <strong>{{ row.name }}</strong>
                          <small>
                            {{ row.code ? row.code + ' · ' : '' }}
                            {{ row.laboratoryName || row.clinicName || 'No clinic/laboratory mapped' }}
                          </small>
                        </td>
                        <td class="text-center">
                          <input
                            type="checkbox"
                            class="form-check-input"
                            [disabled]="locked() || !row.included"
                            [(ngModel)]="row.clinic"
                            (ngModelChange)="clearDefaultIfUngrouped(row)"
                          />
                        </td>
                        <td class="text-center">
                          <input
                            type="checkbox"
                            class="form-check-input"
                            [disabled]="locked() || !row.included"
                            [(ngModel)]="row.lab"
                            (ngModelChange)="clearDefaultIfUngrouped(row)"
                          />
                        </td>
                        <td class="text-center">
                          <input
                            type="radio"
                            class="form-check-input"
                            name="default-branch"
                            [disabled]="locked() || !row.included"
                            [ngModel]="defaultBranchId()"
                            [value]="row.branchId"
                            (ngModelChange)="setDefault(row.branchId)"
                          />
                        </td>
                      </tr>
                    }
                  }
                  @empty {
                    <tr>
                      <td colspan="5" class="text-center text-muted py-4">No active branches found.</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </ng-container>
          <ng-template #loadingTpl>
            <div class="text-center text-muted py-4">Loading…</div>
          </ng-template>
        </div>

        <footer class="ua-footer">
          <span class="hint">Payload is authoritative — unticking Access revokes that branch.</span>
          <div class="actions">
            <button type="button" class="btn btn-light" (click)="close()" [disabled]="saving()">Cancel</button>
            <button
              type="button"
              class="btn btn-primary"
              (click)="save()"
              [disabled]="locked() || saving() || loading()"
            >
              {{ saving() ? 'Saving…' : 'Save Access (' + grantedCount() + ')' }}
            </button>
          </div>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      .ua-backdrop {
        position: fixed;
        inset: 0;
        z-index: 2000;
        background: rgba(15, 23, 42, 0.45);
        display: grid;
        place-items: center;
        padding: 1rem;
      }
      .ua-dialog {
        width: min(760px, 100%);
        max-height: 85vh;
        display: flex;
        flex-direction: column;
        background: #fff;
        border-radius: 10px;
        box-shadow: 0 24px 64px rgba(15, 23, 42, 0.28);
        overflow: hidden;
      }
      .ua-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        padding: 1rem 1.15rem 0.75rem;
        border-bottom: 1px solid #e8ecef;
      }
      .ua-header h3 {
        margin: 0;
        font-size: 1.05rem;
        color: #17212b;
      }
      .ua-header p {
        margin: 0.2rem 0 0;
        font-size: 0.78rem;
        color: #74808c;
      }
      .ua-close {
        border: none;
        background: none;
        font-size: 1.5rem;
        line-height: 1;
        color: #64748b;
        cursor: pointer;
      }
      .ua-close:hover {
        color: #0f172a;
      }
      .ua-note {
        margin: 0.75rem 1.15rem 0;
        font-size: 0.8rem;
      }
      .ua-toolbar {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem 1.15rem 0;
      }
      .ua-count {
        white-space: nowrap;
        font-size: 0.75rem;
        color: #74808c;
      }
      .ua-body {
        overflow-y: auto;
        padding: 0.75rem 1.15rem;
        flex: 1;
      }
      table td strong {
        display: block;
        font-size: 0.86rem;
        color: #35424e;
      }
      table td small {
        display: block;
        font-size: 0.72rem;
        color: #7a8793;
      }
      tr.org-row td {
        background: #f4f6f8;
        position: sticky;
        top: 0;
      }
      tr.org-row td strong {
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.03em;
        color: #51606d;
      }
      tr.org-row td small {
        margin-left: 0.5rem;
        display: inline;
      }
      .row-disabled td {
        opacity: 0.55;
      }
      .ua-footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.8rem 1.15rem;
        border-top: 1px solid #e8ecef;
      }
      .hint {
        font-size: 0.7rem;
        color: #94a3b8;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
      }
      .btn-primary {
        border-color: #087f5b;
        background: #087f5b;
      }
      .btn-primary:hover {
        border-color: #066b4c;
        background: #066b4c;
      }
    `,
  ],
})
export class UserAccessDialogComponent {
  private readonly usersService = inject(UsersService);

  readonly user = input.required<UserListItem>();
  readonly closed = output<void>();
  readonly saved = output<number>();

  readonly rows = signal<EditRow[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly defaultBranchId = signal<number | null>(null);
  readonly search = signal('');

  constructor() {
    this.load();
  }

  isAdmin(): boolean {
    return this.user().roles.some((r) => /^(super)?admin$/i.test(r));
  }

  locked(): boolean {
    return this.isAdmin() || this.saving();
  }

  close(): void {
    if (!this.saving()) this.closed.emit();
  }

  /** Rows grouped by organization, honoring the search filter. */
  groups(): OrgGroup[] {
    const term = this.search().trim().toLowerCase();
    const visible = term
      ? this.rows().filter((r) =>
          `${r.organizationName} ${r.name} ${r.code ?? ''}`.toLowerCase().includes(term),
        )
      : this.rows();

    const map = new Map<number, OrgGroup>();
    for (const r of visible) {
      let g = map.get(r.organizationId);
      if (!g) {
        g = {
          id: r.organizationId,
          name: r.organizationName || `Organization #${r.organizationId}`,
          rows: [],
        };
        map.set(r.organizationId, g);
      }
      g.rows.push(r);
    }
    return [...map.values()].sort((a, b) => a.name.localeCompare(b.name));
  }

  grantedCount(): number {
    return this.rows().filter((r) => r.included).length;
  }

  selectedIn(group: OrgGroup): number {
    return group.rows.filter((r) => r.included).length;
  }

  isOrgFullySelected(group: OrgGroup): boolean {
    return group.rows.length > 0 && group.rows.every((r) => r.included);
  }

  toggleOrg(group: OrgGroup, checked: boolean): void {
    for (const row of [...group.rows]) {
      if (row.included !== checked) {
        row.included = checked;
        this.onIncludedChange(row);
      }
    }
  }

  onIncludedChange(row: EditRow): void {
    if (!row.included) {
      row.clinic = false;
      row.lab = false;
      if (this.defaultBranchId() === row.branchId) this.defaultBranchId.set(null);
    } else if (this.defaultBranchId() === null) {
      this.defaultBranchId.set(row.branchId);
    }
  }

  setDefault(branchId: number): void {
    this.defaultBranchId.set(branchId);
  }

  clearDefaultIfUngrouped(row: EditRow): void {
    if (!row.clinic && !row.lab && this.defaultBranchId() === row.branchId) {
      this.defaultBranchId.set(null);
    }
  }

  save(): void {
    const payload: BranchAccessItem[] = this.rows()
      .filter((r) => r.included)
      .map((r) => ({
        branchId: r.branchId,
        isActive: true,
        hasClinicAccess: r.clinic || r.lab,
        hasLaboratoryAccess: r.lab,
        isDefault: this.defaultBranchId() === r.branchId,
      }));

    this.saving.set(true);
    this.error.set('');
    this.usersService.saveLocations(this.user().id, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.emit(payload.length);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.extractError(err, 'Unable to save branch access.'));
      },
    });
  }

  private extractError(err: any, fallback: string): string {
    const body = err?.error;
    if (typeof body === 'string' && body.trim()) return body;
    if (body?.message) return body.message;
    if (body?.detail) return body.detail;
    if (body?.title) return body.title;
    return fallback;
  }

  private load(): void {
    forkJoin([this.usersService.listOrgBranches(), this.usersService.getLocations(this.user().id)]).subscribe({
      next: ([branches, locations]) => this.buildRows(branches, locations),
      error: () => {
        this.loading.set(false);
        this.error.set('Unable to load branch data.');
      },
    });
  }

  private buildRows(branches: OrgBranch[], locations: BranchAccessRow[]): void {
    const byBranch = new Map(locations.map((l) => [l.branchId, l]));
    const rows = branches.map<EditRow>((b) => {
      const loc = byBranch.get(b.id);
      const included = !!loc?.isActive;
      return {
        branchId: b.id,
        organizationId: b.organizationId,
        organizationName: b.organizationName ?? '',
        name: b.name,
        code: b.code,
        laboratoryName: b.laboratoryName || loc?.laboratoryName || '',
        clinicName: b.clinicName || loc?.clinicName || '',
        included,
        clinic: included && (loc?.hasClinicAccess ?? false),
        lab: included && (loc?.hasLaboratoryAccess ?? false),
        isDefault: !!loc?.isDefault,
      };
    });
    const def = rows.find((r) => r.isDefault);
    this.defaultBranchId.set(def?.branchId ?? null);
    this.rows.set(rows);
    this.loading.set(false);
  }
}
