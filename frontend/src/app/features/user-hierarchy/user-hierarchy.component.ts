import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  HierarchyBranch,
  HierarchyResponse,
  HierarchyUser,
  LoginHistoryItem,
  UserHierarchyService,
} from './user-hierarchy.service';

type LoginFilter = 'all' | 'success' | 'failed';

@Component({
  selector: 'app-user-hierarchy',
  standalone: true,
  imports: [CommonModule, DatePipe],
  providers: [DatePipe],
  template: `
    <div class="container py-3">
      <div class="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <div>
          <h2 class="mb-0">User Login &amp; Hierarchy</h2>
          <small class="text-muted">Organization and branch structure with login activity</small>
        </div>
        <button class="btn btn-outline-secondary" type="button" (click)="loadAll()" [disabled]="loading() || loadingLogins()">
          {{ loading() || loadingLogins() ? 'Refreshing…' : 'Refresh' }}
        </button>
      </div>

      <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

      <div class="row g-4">
        <div class="col-lg-7">
          <section class="card">
            <div class="card-header fw-semibold">Staff hierarchy</div>
            <div class="card-body">
              <div *ngIf="loading()" class="text-muted py-3">Loading…</div>
              <div *ngIf="!loading() && hierarchy() as tree">
                <div class="tree-root">
                  <div class="tree-row root">
                    <span class="tree-org-icon">
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 21V5a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v16"/><path d="M16 8h3a1 1 0 0 1 1 1v12"/><path d="M3 21h18"/><path d="M8 7h2M8 11h2M8 15h2"/></svg>
                    </span>
                    <div>
                      <div class="fw-semibold">{{ tree.organization.name }}</div>
                      <small class="text-muted">Org code: {{ tree.organization.code }}</small>
                    </div>
                    <span class="badge text-bg-light ms-auto">{{ totalUsers() }} users</span>
                  </div>

                  <div class="tree-level branches">
                    <div class="tree-branch" *ngFor="let branch of tree.branches">
                      <div class="tree-row branch">
                        <span class="tree-chevron">▾</span>
                        <span class="tree-branch-icon">
                          <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 21s-7-5.1-7-11a7 7 0 1 1 14 0c0 5.9-7 11-7 11z"/><circle cx="12" cy="10" r="2.6"/></svg>
                        </span>
                        <div>
                          <div class="fw-semibold">{{ branch.name }}</div>
                          <small class="text-muted">Branch: {{ branch.code }}</small>
                        </div>
                        <span class="badge text-bg-light ms-auto">{{ branch.users.length }} users</span>
                      </div>
                      <div class="tree-users" *ngIf="branch.users.length">
                        <div class="user-chip" *ngFor="let user of branch.users">
                          <span class="user-dot" [class.online]="user.isActive"></span>
                          <span class="user-name">{{ user.displayName || user.userName }}</span>
                          <small class="user-login">{{ user.userName }}</small>
                          <span class="badge ms-auto" [class]="roleBadge(user.roles[0])">{{ user.roles[0] || 'No role' }}</span>
                        </div>
                      </div>
                    </div>

                    <div class="tree-branch" *ngIf="tree.unassigned.length">
                      <div class="tree-row branch">
                        <span class="tree-chevron">▾</span>
                        <span class="tree-branch-icon">
                          <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20c0-3.5 3-5.5 6.5-5.5s6.5 2 6.5 5.5"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M21 20c0-2.8-1.6-4.7-4-5.2"/></svg>
                        </span>
                        <div>
                          <div class="fw-semibold">Unassigned users</div>
                          <small class="text-muted">No laboratory or branch</small>
                        </div>
                        <span class="badge text-bg-light ms-auto">{{ tree.unassigned.length }} users</span>
                      </div>
                      <div class="tree-users">
                        <div class="user-chip" *ngFor="let user of tree.unassigned">
                          <span class="user-dot" [class.online]="user.isActive"></span>
                          <span class="user-name">{{ user.displayName || user.userName }}</span>
                          <small class="user-login">{{ user.userName }}</small>
                          <span class="badge ms-auto" [class]="roleBadge(user.roles[0])">{{ user.roles[0] || 'No role' }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>

        <div class="col-lg-5">
          <section class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
              <span class="fw-semibold">Login activity</span>
              <div class="btn-group btn-group-sm" role="group" aria-label="Filter login activity">
                <button
                  class="btn btn-sm"
                  [class.btn-primary]="filter() === 'all'"
                  [class.btn-outline-secondary]="filter() !== 'all'"
                  type="button"
                  (click)="setFilter('all')"
                >
                  All
                </button>
                <button
                  class="btn btn-sm"
                  [class.btn-primary]="filter() === 'success'"
                  [class.btn-outline-secondary]="filter() !== 'success'"
                  type="button"
                  (click)="setFilter('success')"
                >
                  Successful
                </button>
                <button
                  class="btn btn-sm"
                  [class.btn-primary]="filter() === 'failed'"
                  [class.btn-outline-secondary]="filter() !== 'failed'"
                  type="button"
                  (click)="setFilter('failed')"
                >
                  Failed
                </button>
              </div>
            </div>
            <div class="card-body">
              <div *ngIf="loadingLogins()" class="text-muted py-3">Loading…</div>
              <div class="table-responsive" *ngIf="!loadingLogins()">
                <table class="table table-sm table-hover align-middle mb-0" *ngIf="logins().length; else noLogins">
                  <thead>
                    <tr>
                      <th>Time</th>
                      <th>User</th>
                      <th>Result</th>
                      <th>IP</th>
                      <th>Detail</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of logins()">
                      <td class="text-nowrap">{{ item.createdAt | date: 'MMM d, h:mm a' }}</td>
                      <td>
                        <div>{{ item.displayName || item.userName || 'Unknown' }}</div>
                        <small class="text-muted">{{ item.userName }}</small>
                      </td>
                      <td>
                        <span class="badge" [class.text-bg-success]="item.success" [class.text-bg-danger]="!item.success">
                          {{ item.success ? 'Success' : 'Failed' }}
                        </span>
                      </td>
                      <td><small>{{ item.ipAddress || '—' }}</small></td>
                      <td>
                        <small *ngIf="!item.success" class="text-danger">{{ item.failureReason || 'Failed' }}</small>
                        <small *ngIf="item.success" class="text-muted text-truncate d-inline-block" style="max-width: 140px;" [title]="item.userAgent || ''">{{ item.userAgent || '—' }}</small>
                      </td>
                    </tr>
                  </tbody>
                </table>
                <ng-template #noLogins><p class="text-muted py-3 mb-0">No login activity recorded yet.</p></ng-template>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .tree-root {
        border: 1px solid var(--color-border);
        border-radius: 0.5rem;
        padding: 0.75rem;
      }

      .tree-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.4rem 0.35rem;
        border-radius: 0.4rem;
      }

      .tree-row.root {
        background: var(--color-tint);
        border-left: 4px solid var(--color-primary-dark);
        margin-bottom: 0.35rem;
      }

      .tree-row.lab {
        background: rgba(102, 198, 216, 0.14);
        border-left: 4px solid var(--color-secondary);
        margin-bottom: 0.25rem;
      }

      .tree-row.branch {
        background: #f7fbfd;
        border-left: 4px solid #9fb4bf;
      }

      .tree-level.branches {
        margin-left: 1.15rem;
        padding-left: 0.65rem;
        border-left: 1px dashed #c3dbe4;
        margin-bottom: 0.35rem;
      }

      .tree-users {
        margin-left: 1.15rem;
        padding-left: 0.65rem;
        border-left: 1px dashed #c3dbe4;
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
        margin-bottom: 0.35rem;
      }

      .user-chip {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        padding: 0.35rem 0.55rem;
        border: 1px solid var(--color-border);
        border-radius: 0.45rem;
        background: #fff;
      }

      .user-dot {
        width: 8px;
        height: 8px;
        border-radius: 999px;
        background: #c3d2da;
      }

      .user-dot.online {
        background: var(--color-success);
        box-shadow: 0 0 0 2px rgba(61, 184, 154, 0.22);
      }

      .user-login {
        color: var(--color-secondary-text);
      }

      .tree-org-icon,
      .tree-lab-icon,
      .tree-branch-icon {
        font-size: 1rem;
      }
    `,
  ],
})
export class UserHierarchyComponent implements OnInit {
  private readonly svc = inject(UserHierarchyService);

  readonly hierarchy = signal<HierarchyResponse | null>(null);
  readonly logins = signal<LoginHistoryItem[]>([]);
  readonly loading = signal(false);
  readonly loadingLogins = signal(false);
  readonly error = signal('');
  readonly filter = signal<LoginFilter>('all');

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loadHierarchy();
    this.loadLogins();
  }

  setFilter(f: LoginFilter): void {
    this.filter.set(f);
    this.loadLogins();
  }

  totalUsers(): number {
    const tree = this.hierarchy();
    if (!tree) return 0;
    const branchCount = tree.branches.reduce(
      (sum, branch) => sum + branch.users.length,
      0,
    );
    return branchCount + tree.unassigned.length;
  }

  roleBadge(role: string | undefined): string {
    switch (role) {
      case 'Admin':
        return 'text-bg-danger';
      case 'Manager':
        return 'text-bg-primary';
      default:
        return 'text-bg-secondary';
    }
  }

  private loadHierarchy(): void {
    this.loading.set(true);
    this.error.set('');
    this.svc.getHierarchy().subscribe({
      next: (tree) => {
        this.hierarchy.set(tree);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.extractMessage(err));
      },
    });
  }

  private loadLogins(): void {
    this.loadingLogins.set(true);
    this.error.set('');
    const success =
      this.filter() === 'success' ? true : this.filter() === 'failed' ? false : null;
    this.svc.getLoginHistory(100, success).subscribe({
      next: (items) => {
        this.logins.set(items ?? []);
        this.loadingLogins.set(false);
      },
      error: (err) => {
        this.loadingLogins.set(false);
        this.error.set(this.extractMessage(err));
      },
    });
  }

  private extractMessage(err: any): string {
    const payload = err?.error;
    if (typeof payload === 'string') return payload;
    if (payload?.message) return payload.message;
    if (payload?.title) return payload.title;
    return 'Request failed';
  }
}
