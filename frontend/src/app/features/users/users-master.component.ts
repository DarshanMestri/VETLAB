import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BranchAccessItem,
  CreateUserRequest,
  OrganizationOption,
  OrgBranch,
  RoleOption,
  UserListItem,
  UsersService,
} from './users.service';
import { UserAccessDialogComponent } from './user-access-dialog.component';

interface UserForm {
  userName: string;
  email: string;
  displayName: string;
  password: string;
  roleName: string;
  organizationId: number | null;
  isActive: boolean;
}

@Component({
  standalone: true,
  selector: 'app-users-master',
  imports: [CommonModule, FormsModule, UserAccessDialogComponent],
  template: `
    <div class="users-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Administration</p>
          <h1>User Creation Master</h1>
          <p class="subtitle">Create user accounts and manage access status.</p>
        </div>
        <button
          class="btn btn-outline-secondary"
          type="button"
          (click)="loadUsers()"
          [disabled]="loading()"
        >
          Refresh
        </button>
      </header>

      <div class="alert alert-danger" role="alert" *ngIf="error()">{{ error() }}</div>
      <div class="alert alert-warning" role="alert" *ngIf="warning()">{{ warning() }}</div>
      <div class="alert alert-success" role="status" *ngIf="message()">{{ message() }}</div>

      <div class="workspace">
        <section class="user-list" aria-labelledby="users-heading">
          <div class="section-toolbar">
            <div>
              <h2 id="users-heading">Users</h2>
              <span class="count">{{ filteredUsers().length }} records</span>
            </div>
            <label class="search-field">
              <span class="visually-hidden">Search users</span>
              <input
                class="form-control"
                type="search"
                placeholder="Search name, username, email or role"
                [ngModel]="search()"
                (ngModelChange)="search.set($event)"
              />
            </label>
          </div>

          <div class="table-responsive">
            <table class="table align-middle">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Role</th>
                  <th>Organization</th>
                  <th>Status</th>
                  <th class="text-end">Action</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let user of filteredUsers(); trackBy: trackUser">
                  <td>
                    <strong>{{ user.displayName || user.userName }}</strong>
                    <small>{{ user.userName }} · {{ user.email }}</small>
                  </td>
                  <td>
                    <span class="role-tag">{{ user.roles.join(', ') || 'Unassigned' }}</span>
                  </td>
                  <td>
                    <span>{{ organizationName(user.organizationId) }}</span>
                  </td>
                  <td>
                    <span class="status" [class.status-active]="user.isActive">
                      {{ user.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-end">
                    <button
                      class="btn btn-sm btn-outline-secondary me-1"
                      type="button"
                      [disabled]="updatingUserId() === user.id"
                      (click)="openAccess(user)"
                    >
                      Access
                      @if (accessCounts()[user.id]; as count) {
                        <span class="badge rounded-pill text-bg-success ms-1">{{ count }}</span>
                      }
                    </button>
                    <button
                      class="btn btn-sm btn-outline-primary me-1"
                      type="button"
                      [disabled]="updatingUserId() === user.id"
                      (click)="startEdit(user)"
                    >
                      Edit
                    </button>
                    <button
                      class="btn btn-sm"
                      [class.btn-outline-danger]="user.isActive"
                      [class.btn-outline-success]="!user.isActive"
                      type="button"
                      [disabled]="updatingUserId() === user.id"
                      (click)="toggleActive(user)"
                    >
                      {{ user.isActive ? 'Deactivate' : 'Activate' }}
                    </button>
                  </td>
                </tr>
                <tr *ngIf="!loading() && filteredUsers().length === 0">
                  <td colspan="5" class="empty-state">No users match the current search.</td>
                </tr>
                <tr *ngIf="loading()">
                  <td colspan="5" class="empty-state">Loading users…</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <aside class="create-panel" aria-labelledby="create-heading">
          <div class="panel-heading">
            <h2 id="create-heading">{{ isEditing() ? 'Edit User' : 'Create User' }}</h2>
            <p>{{
              isEditing()
                ? 'Update identity, role, and account status.'
                : 'Assign identity, role, and account status.'
            }}</p>
            <p class="panel-hint">Branch access is managed per user via the Access button.</p>
          </div>

          <form #userForm="ngForm" (ngSubmit)="onSubmit(userForm.valid)">
            <div class="field-grid">
              <label class="form-label full-width">
                Display name
                <input
                  class="form-control"
                  name="displayName"
                  [(ngModel)]="form.displayName"
                  required
                  maxlength="100"
                />
              </label>

              <label class="form-label">
                Username
                <input
                  class="form-control"
                  name="userName"
                  [(ngModel)]="form.userName"
                  required
                  maxlength="50"
                  autocomplete="off"
                />
              </label>

              <label class="form-label">
                Email
                <input
                  class="form-control"
                  name="email"
                  [(ngModel)]="form.email"
                  required
                  email
                  maxlength="150"
                  type="email"
                  autocomplete="off"
                />
              </label>

              <label class="form-label full-width">
                {{ isEditing() ? 'Reset password' : 'Temporary password' }}
                <input
                  class="form-control"
                  name="password"
                  [(ngModel)]="form.password"
                  [required]="!isEditing()"
                  minlength="6"
                  type="password"
                  autocomplete="new-password"
                />
                <span class="field-note">{{
                  isEditing()
                    ? 'Leave blank to keep the current password.'
                    : 'Must satisfy the configured account password policy.'
                }}</span>
              </label>

              <label class="form-label">
                Role
                <select class="form-select" name="roleName" [(ngModel)]="form.roleName" required>
                  <option value="" disabled>Select role</option>
                  <option *ngFor="let role of roles()" [value]="role.name">{{ role.name }}</option>
                </select>
              </label>

              <label class="form-label full-width">
                Home Organization <span class="text-danger">*</span>
                <select
                  class="form-select"
                  name="organizationId"
                  [(ngModel)]="form.organizationId"
                  required
                >
                  <option [ngValue]="null" disabled>Select organization</option>
                  @for(org of organizations(); track org.id) {
                    <option [ngValue]="org.id">{{org.name}}</option>
                  }
                </select>
                 <small class="form-text text-muted">
                   Default organization used at sign-in. Admins see every branch of it; other roles can be granted
                   branches in any organization via the Access dialog.
                 </small>
               </label>
             </div>

             @if (!isAdminRoleSelected()) {
               <div class="branch-access">
                 <div class="ba-head">
                   <strong>Branch Access</strong>
                   <span class="ba-count">{{ selectedBranchCount() }} branch{{ selectedBranchCount() === 1 ? '' : 'es' }} selected</span>
                 </div>
                 <small class="ba-hint">
                   Optional — grant branches in any organization now. Fine-tune Clinic/Laboratory flags later via
                   the Access button.
                 </small>

                 <input
                   class="form-control form-control-sm ba-search"
                   type="search"
                   placeholder="Filter organizations or branches…"
                   name="branchSearch"
                   [(ngModel)]="branchSearch"
                 />

                 <div class="ba-chips">
                   @for (org of visibleCatalogOrgs(); track org.id) {
                     <label class="ba-chip" [class.ba-chip-on]="isOrgChipOn(org.id)">
                       <input
                         type="checkbox"
                         [checked]="isOrgChipOn(org.id)"
                         (change)="toggleOrgChip(org.id, $any($event.target).checked)"
                       />
                       {{ org.name }}
                     </label>
                   } @empty {
                     <span class="text-muted small">No active branches available yet.</span>
                   }
                 </div>

                 @for (org of chosenOrgs(); track org.id) {
                   <div class="ba-group">
                     <label class="ba-group-head">
                       <input
                         type="checkbox"
                         [checked]="isOrgFullySelected(org.id)"
                         (change)="toggleAllOrgBranches(org.id, $any($event.target).checked)"
                       />
                       <strong>{{ org.name }}</strong>
                       <span class="ba-group-count">{{ selectedInOrg(org.id) }}/{{ catalogForOrg(org.id).length }}</span>
                     </label>
                     <div class="ba-branches">
                       @for (b of catalogForOrg(org.id); track b.id) {
                         <label class="ba-branch" [class.ba-branch-on]="selectedBranchIds.has(b.id)">
                           <input
                             type="checkbox"
                             [checked]="selectedBranchIds.has(b.id)"
                             (change)="toggleBranch(b.id, $any($event.target).checked)"
                           />
                           <span>
                             <strong>{{ b.name }}</strong>
                             <small>
                               {{ b.code ? b.code + ' · ' : '' }}
                               {{ b.laboratoryName || b.clinicName || 'No clinic/laboratory mapped' }}
                             </small>
                           </span>
                         </label>
                       }
                     </div>
                   </div>
                 }
               </div>
             } @else {
               <div class="alert alert-info mt-3 mb-0">
                 Administrator accounts implicitly hold every active branch of their home organization.
               </div>
             }

            <label class="active-control">
              <input type="checkbox" name="isActive" [(ngModel)]="form.isActive" />
              <span>
                <strong>Account active</strong>
                <small>User can sign in immediately after creation.</small>
              </span>
            </label>

            <div class="form-actions">
              <button
                class="btn btn-light"
                type="button"
                (click)="isEditing() ? cancelEdit() : resetForm()"
                [disabled]="saving()"
              >
                {{ isEditing() ? 'Cancel' : 'Clear' }}
              </button>
              <button
                class="btn btn-primary"
                type="submit"
                [disabled]="saving() || userForm.invalid"
              >
                {{
                  saving()
                    ? isEditing()
                      ? 'Saving…'
                      : 'Creating…'
                    : isEditing()
                      ? 'Save Changes'
                      : 'Create User'
                }}
              </button>
            </div>
          </form>
        </aside>
      </div>

      @if (accessUser(); as user) {
        <app-user-access-dialog
          [user]="user"
          (closed)="accessUser.set(null)"
          (saved)="onAccessSaved($event)"
        />
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .users-page {
        max-width: 1480px;
        margin: 0 auto;
        padding: 1rem 1.5rem 2rem;
      }
      .page-header {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1.25rem;
      }
      .eyebrow {
        margin: 0 0 0.25rem;
        color: #087f5b;
        font-size: 0.72rem;
        font-weight: 700;
        letter-spacing: 0;
        text-transform: uppercase;
      }
      h1 {
        margin: 0;
        color: #17212b;
        font-size: 1.75rem;
        font-weight: 700;
      }
      .subtitle {
        margin: 0.3rem 0 0;
        color: #637180;
      }
      .workspace {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(330px, 410px);
        gap: 1.25rem;
        align-items: start;
      }
      .user-list,
      .create-panel {
        border: 1px solid #dce2e7;
        border-radius: 6px;
        background: #fff;
        box-shadow: 0 2px 8px rgba(23, 33, 43, 0.05);
      }
      .section-toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 1rem 1.15rem;
        border-bottom: 1px solid #e8ecef;
      }
      h2 {
        margin: 0;
        color: #24313d;
        font-size: 1rem;
        font-weight: 700;
      }
      .count {
        color: #74808c;
        font-size: 0.78rem;
      }
      .search-field {
        width: min(330px, 50%);
      }
      .table {
        margin: 0;
      }
      .table th {
        padding: 0.7rem 1rem;
        color: #65717d;
        background: #f7f9fa;
        border-bottom-width: 1px;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0;
        white-space: nowrap;
      }
      .table td {
        padding: 0.8rem 1rem;
        color: #35424e;
        font-size: 0.86rem;
      }
      .table td strong,
      .table td small {
        display: block;
      }
      .table td small {
        margin-top: 0.18rem;
        color: #7a8793;
        font-size: 0.74rem;
      }
      .role-tag {
        display: inline-block;
        padding: 0.2rem 0.45rem;
        color: #365267;
        border: 1px solid #cbd7df;
        border-radius: 4px;
        background: #f5f8fa;
        font-size: 0.74rem;
      }
      .status {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        color: #8b3d3d;
        font-size: 0.78rem;
        font-weight: 600;
      }
      .status::before {
        width: 7px;
        height: 7px;
        content: '';
        border-radius: 50%;
        background: #cf5b5b;
      }
      .status-active {
        color: #176b4d;
      }
      .status-active::before {
        background: #2c9b70;
      }
      .empty-state {
        padding: 2.5rem !important;
        color: #77838e !important;
        text-align: center;
      }
      .panel-heading {
        padding: 1rem 1.15rem;
        border-bottom: 1px solid #e8ecef;
      }
      .panel-heading p {
        margin: 0.3rem 0 0;
        color: #74808c;
        font-size: 0.82rem;
      }
      form {
        padding: 1.15rem;
      }
      .field-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.9rem;
      }
      .full-width {
        grid-column: 1 / -1;
      }
      .form-label {
        margin: 0;
        color: #4c5965;
        font-size: 0.78rem;
        font-weight: 600;
      }
      .form-control,
      .form-select {
        margin-top: 0.35rem;
        min-height: 40px;
        font-size: 0.86rem;
      }
      .field-note {
        display: block;
        margin-top: 0.3rem;
        color: #7b8792;
        font-size: 0.7rem;
        font-weight: 400;
      }
      .active-control {
        display: flex;
        gap: 0.65rem;
        align-items: flex-start;
        margin: 1.1rem 0;
        padding: 0.8rem;
        border: 1px solid #dfe6e9;
        border-radius: 5px;
        background: #f8faf9;
        cursor: pointer;
      }
      .active-control input {
        width: 17px;
        height: 17px;
        margin-top: 0.1rem;
        accent-color: #087f5b;
      }
      .active-control strong,
      .active-control small {
        display: block;
      }
      .active-control strong {
        color: #34414c;
        font-size: 0.82rem;
      }
      .active-control small {
        margin-top: 0.18rem;
        color: #75818c;
        font-size: 0.72rem;
      }
      .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.65rem;
        padding-top: 1rem;
        border-top: 1px solid #e8ecef;
      }
      .btn-primary {
        border-color: #087f5b;
        background: #087f5b;
      }
      .btn-primary:hover,
      .btn-primary:focus {
        border-color: #066b4c;
        background: #066b4c;
      }
      @media (max-width: 1100px) {
        .workspace {
          grid-template-columns: 1fr;
        }
        .create-panel {
          order: -1;
        }
      }
      @media (max-width: 640px) {
        .users-page {
          padding: 0.75rem;
        }
        .page-header,
        .section-toolbar {
          align-items: stretch;
          flex-direction: column;
        }
        .search-field {
          width: 100%;
        }
        .field-grid {
          grid-template-columns: 1fr;
        }
        .full-width {
          grid-column: auto;
        }
      }
      .branch-access {
        margin-top: 0.9rem;
        padding: 0.85rem 1rem;
        border: 1px solid #dce2e7;
        border-radius: 6px;
        background: #f8fafb;
      }
      .ba-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
      }
      .ba-head strong {
        font-size: 0.88rem;
        color: #24313d;
      }
      .ba-count {
        font-size: 0.72rem;
        color: #087f5b;
        background: rgba(8, 127, 91, 0.1);
        padding: 0.15rem 0.55rem;
        border-radius: 999px;
        white-space: nowrap;
      }
      .ba-hint {
        display: block;
        margin: 0.2rem 0 0.6rem;
        font-size: 0.72rem;
        color: #74808c;
      }
      .ba-search {
        max-width: 320px;
      }
      .ba-chips {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        margin: 0.6rem 0;
      }
      .ba-chip {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.28rem 0.7rem;
        border: 1px solid #cfd7dd;
        border-radius: 999px;
        background: #fff;
        font-size: 0.78rem;
        color: #35424e;
        cursor: pointer;
        user-select: none;
      }
      .ba-chip:hover {
        border-color: #087f5b;
      }
      .ba-chip-on {
        border-color: #087f5b;
        background: rgba(8, 127, 91, 0.08);
        color: #066b4c;
        font-weight: 600;
      }
      .ba-group {
        border: 1px solid #e4e9ec;
        border-radius: 6px;
        background: #fff;
        overflow: hidden;
      }
      .ba-group + .ba-group,
      .ba-group + .ba-chips,
      .branch-access > .ba-group {
        margin-top: 0.55rem;
      }
      .ba-group-head {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.45rem 0.7rem;
        background: #eef2f4;
        cursor: pointer;
        user-select: none;
      }
      .ba-group-head strong {
        font-size: 0.76rem;
        text-transform: uppercase;
        letter-spacing: 0.03em;
        color: #51606d;
      }
      .ba-group-count {
        margin-left: auto;
        font-size: 0.72rem;
        color: #74808c;
      }
      .ba-branches {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 0.3rem 0.75rem;
        padding: 0.5rem 0.7rem 0.65rem;
      }
      .ba-branch {
        display: flex;
        align-items: flex-start;
        gap: 0.45rem;
        padding: 0.3rem 0.45rem;
        border-radius: 5px;
        cursor: pointer;
      }
      .ba-branch:hover {
        background: #f2f6f4;
      }
      .ba-branch-on {
        background: rgba(8, 127, 91, 0.07);
      }
      .ba-branch span strong {
        display: block;
        font-size: 0.8rem;
        color: #2a3641;
        line-height: 1.25;
      }
      .ba-branch span small {
        display: block;
        font-size: 0.68rem;
        color: #8592a0;
      }
    `,
  ],
})
export class UsersMasterComponent implements OnInit {
  private readonly usersService = inject(UsersService);

  readonly users = signal<UserListItem[]>([]);
  readonly roles = signal<RoleOption[]>([]);
  readonly organizations = signal<OrganizationOption[]>([]);
  readonly accessUser = signal<UserListItem | null>(null);
  readonly accessCounts = signal<Record<number, number>>({});
  readonly search = signal('');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly updatingUserId = signal<number | null>(null);
  readonly editingUser = signal<UserListItem | null>(null);
  readonly error = signal('');
  readonly message = signal('');
  readonly warning = signal('');

  readonly isEditing = computed(() => this.editingUser() !== null);

  form: UserForm = this.emptyForm();

  /** Active branches of ALL organizations, for inline access selection. */
  readonly branchCatalog = signal<OrgBranch[]>([]);
  branchSearch = '';
  readonly selectedBranchIds = new Set<number>();

  readonly filteredUsers = computed(() => {
    const query = this.search().trim().toLowerCase();
    if (!query) return this.users();
    return this.users().filter((user) =>
      [user.displayName, user.userName, user.email, ...user.roles].some((value) =>
        value?.toLowerCase().includes(query),
      ),
    );
  });

  ngOnInit(): void {
    this.loadUsers();
    this.usersService.listRoles().subscribe({
      next: (roles) => this.roles.set(roles),
      error: (error) => this.showError(error, 'Unable to load roles.'),
    });
    this.usersService.listOrganizations().subscribe({
      next: (orgs) => this.organizations.set(orgs),
      error: (error) => this.showError(error, 'Unable to load organizations.'),
    });
    this.usersService.listOrgBranches().subscribe({
      next: (branches) => this.branchCatalog.set(branches),
      error: () => this.branchCatalog.set([]),
    });
  }

  // ----- Inline branch access selection -------------------------------------

  isAdminRoleSelected(): boolean {
    return /^(super)?admin$/i.test(this.form.roleName);
  }

  /** Organizations that have at least one active branch, honoring the filter. */
  visibleCatalogOrgs(): OrganizationOption[] {
    const term = this.branchSearch.trim().toLowerCase();
    return this.organizations().filter((org) => {
      const branches = this.catalogForOrg(org.id);
      if (!branches.length) return false;
      if (!term) return true;
      return (
        org.name.toLowerCase().includes(term) ||
        branches.some((b) => `${b.name} ${b.code ?? ''}`.toLowerCase().includes(term))
      );
    });
  }

  catalogForOrg(orgId: number): OrgBranch[] {
    const term = this.branchSearch.trim().toLowerCase();
    const all = this.branchCatalog().filter((b) => b.organizationId === orgId);
    if (!term) return all;
    return all.filter((b) => `${b.name} ${b.code ?? ''}`.toLowerCase().includes(term));
  }

  chosenOrgs(): OrganizationOption[] {
    return this.organizations().filter((o) => this.isOrgChipOn(o.id));
  }

  private readonly chipOverride = new Set<number>();

  isOrgChipOn(orgId: number): boolean {
    return this.chipOverride.has(orgId) || this.selectedInOrg(orgId) > 0;
  }

  toggleOrgChip(orgId: number, on: boolean): void {
    if (on) {
      this.chipOverride.add(orgId);
    } else {
      this.chipOverride.delete(orgId);
      for (const id of [...this.selectedBranchIds]) {
        if (this.branchCatalog().some((b) => b.id === id && b.organizationId === orgId)) {
          this.selectedBranchIds.delete(id);
        }
      }
    }
  }

  selectedInOrg(orgId: number): number {
    let count = 0;
    for (const b of this.branchCatalog()) {
      if (b.organizationId === orgId && this.selectedBranchIds.has(b.id)) count++;
    }
    return count;
  }

  isOrgFullySelected(orgId: number): boolean {
    const branches = this.branchCatalog().filter((b) => b.organizationId === orgId);
    return branches.length > 0 && branches.every((b) => this.selectedBranchIds.has(b.id));
  }

  toggleAllOrgBranches(orgId: number, on: boolean): void {
    for (const b of this.branchCatalog()) {
      if (b.organizationId !== orgId) continue;
      if (on) this.selectedBranchIds.add(b.id);
      else this.selectedBranchIds.delete(b.id);
    }
  }

  toggleBranch(branchId: number, on: boolean): void {
    if (on) this.selectedBranchIds.add(branchId);
    else this.selectedBranchIds.delete(branchId);
  }

  selectedBranchCount(): number {
    return this.selectedBranchIds.size;
  }

  resetBranchSelection(): void {
    this.selectedBranchIds.clear();
    this.chipOverride.clear();
    this.branchSearch = '';
  }

  private buildAccessPayload(): BranchAccessItem[] {
    const payload: BranchAccessItem[] = [];
    for (const org of this.organizations()) {
      for (const branch of this.branchCatalog()) {
        if (branch.organizationId !== org.id || !this.selectedBranchIds.has(branch.id)) continue;
        payload.push({
          branchId: branch.id,
          isActive: true,
          hasClinicAccess: true,
          hasLaboratoryAccess: true,
          isDefault: false,
        });
      }
    }
    // First picked branch becomes the sign-in default.
    if (payload.length > 0) payload[0].isDefault = true;
    return payload;
  }

  loadUsers(): void {
    this.loading.set(true);
    this.error.set('');
    this.usersService.list().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.showError(error, 'Unable to load users.');
      },
    });
  }

  createUser(valid: boolean | null): void {
    if (!valid || !this.form.organizationId) return;
    this.saving.set(true);
    this.error.set('');
    this.message.set('');
    const request: CreateUserRequest = {
      userName: this.form.userName,
      email: this.form.email,
      displayName: this.form.displayName,
      password: this.form.password,
      roleName: this.form.roleName,
      organizationId: this.form.organizationId,
      isActive: this.form.isActive,
    };
    const accessPayload = this.isAdminRoleSelected() ? [] : this.buildAccessPayload();
    this.usersService.create(request).subscribe({
      next: (user) => {
        this.users.update((users) =>
          [...users, user].sort((a, b) => a.userName.localeCompare(b.userName)),
        );
        if (!accessPayload.length) {
          this.message.set(`User ${user.userName} was created successfully.`);
          this.resetForm();
          this.saving.set(false);
          return;
        }
        this.usersService.saveLocations(user.id, accessPayload).subscribe({
          next: () => {
            this.accessCounts.update((counts) => ({ ...counts, [user.id]: accessPayload.length }));
            this.message.set(
              `User ${user.userName} was created with access to ${accessPayload.length} branch${accessPayload.length === 1 ? '' : 'es'}.`,
            );
            this.resetForm();
            this.saving.set(false);
          },
          error: () => {
            // The account exists; only the grants failed. Surface a retry path.
            this.warning.set(
              `User ${user.userName} was created, but saving branch access failed — use the Access button to retry.`,
            );
            this.resetForm();
            this.saving.set(false);
          },
        });
      },
      error: (error) => {
        this.saving.set(false);
        this.showError(error, 'Unable to create user.');
      },
    });
  }

  startEdit(user: UserListItem): void {
    this.editingUser.set(user);
    this.error.set('');
    this.message.set('');
    this.warning.set('');
    this.resetBranchSelection();
    this.form = {
      userName: user.userName,
      email: user.email,
      displayName: user.displayName ?? '',
      password: '',
      roleName: user.roles[0] ?? '',
      organizationId: user.organizationId,
      isActive: user.isActive,
    };
    if (this.isAdminRoleSelected()) return;
    // Prefill inline selection from the user's saved branch grants.
    this.usersService.getLocations(user.id).subscribe({
      next: (rows) => {
        for (const row of rows) {
          if (!row.isActive) continue;
          this.selectedBranchIds.add(row.branchId);
        }
      },
      error: () => {},
    });
  }

  cancelEdit(): void {
    this.editingUser.set(null);
    this.error.set('');
    this.message.set('');
    this.resetForm();
  }

  onSubmit(valid: boolean | null): void {
    if (this.isEditing()) {
      this.updateUser(valid);
    } else {
      this.createUser(valid);
    }
  }

  updateUser(valid: boolean | null): void {
    if (!valid || !this.form.organizationId) return;
    const editing = this.editingUser();
    if (!editing) return;
    this.saving.set(true);
    this.error.set('');
    this.message.set('');
    const accessPayload = this.isAdminRoleSelected() ? [] : this.buildAccessPayload();
    this.usersService
      .update(editing.id, {
        userName: this.form.userName,
        email: this.form.email,
        displayName: this.form.displayName,
        roleName: this.form.roleName,
        organizationId: this.form.organizationId,
        isActive: this.form.isActive,
        newPassword: this.form.password || undefined,
      })
      .subscribe({
        next: (user) => {
          this.users.update((users) =>
            users
              .map((item) => (item.id === user.id ? user : item))
              .sort((a, b) => a.userName.localeCompare(b.userName)),
          );
          if (!accessPayload.length) {
            this.message.set(`User ${user.userName} was updated successfully.`);
            this.cancelEdit();
            this.saving.set(false);
            return;
          }
          this.usersService.saveLocations(user.id, accessPayload).subscribe({
            next: () => {
              this.accessCounts.update((counts) => ({
                ...counts,
                [user.id]: accessPayload.length,
              }));
              this.message.set(
                `User ${user.userName} was updated with access to ${accessPayload.length} branch${accessPayload.length === 1 ? '' : 'es'}.`,
              );
              this.cancelEdit();
              this.saving.set(false);
            },
            error: () => {
              this.warning.set(
                `User details were saved, but updating branch access failed — use the Access button to retry.`,
              );
              this.cancelEdit();
              this.saving.set(false);
            },
          });
        },
        error: (error) => {
          this.saving.set(false);
          this.showError(error, 'Unable to update user.');
        },
      });
  }

  toggleActive(user: UserListItem): void {
    this.updatingUserId.set(user.id);
    this.error.set('');
    this.message.set('');
    this.usersService.toggleActive(user.id, !user.isActive).subscribe({
      next: () => {
        this.users.update((users) =>
          users.map((item) => (item.id === user.id ? { ...item, isActive: !item.isActive } : item)),
        );
        this.message.set(`${user.userName} is now ${user.isActive ? 'inactive' : 'active'}.`);
        this.updatingUserId.set(null);
      },
      error: (error) => {
        this.updatingUserId.set(null);
        this.showError(error, 'Unable to update account status.');
      },
    });
  }

  openAccess(user: UserListItem): void {
    this.accessUser.set(user);
  }

  onAccessSaved(branchCount: number): void {
    const user = this.accessUser();
    this.accessUser.set(null);
    if (user) {
      this.accessCounts.update((counts) => ({ ...counts, [user.id]: branchCount }));
      this.message.set(`Branch access for ${user.userName} was updated (${branchCount} branch${branchCount === 1 ? '' : 'es'}).`);
    }
  }

  resetForm(): void {
    this.form = this.emptyForm();
    this.resetBranchSelection();
    this.warning.set('');
  }

  organizationName(organizationId: number | null): string {
    return this.organizations().find((o) => o.id === organizationId)?.name ?? 'Not assigned';
  }

  trackUser(_index: number, user: UserListItem): number {
    return user.id;
  }

  private emptyForm(): UserForm {
    return {
      userName: '',
      email: '',
      displayName: '',
      password: '',
      roleName: '',
      organizationId: null,
      isActive: true,
    };
  }

  private showError(error: any, fallback: string): void {
    const err = error?.error;
    this.error.set(
      (typeof err === 'string' && err.trim()) ||
        err?.message ||
        err?.detail ||
        err?.title ||
        fallback,
    );
  }
}
