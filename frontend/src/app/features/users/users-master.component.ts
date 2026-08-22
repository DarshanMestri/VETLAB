import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateUserRequest,
  OrganizationOption,
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
                Organization <span class="text-danger">*</span>
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
              </label>
            </div>

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
    `,
  ],
})
export class UsersMasterComponent implements OnInit {
  private readonly usersService = inject(UsersService);

  readonly users = signal<UserListItem[]>([]);
  readonly roles = signal<RoleOption[]>([]);
  readonly organizations = signal<OrganizationOption[]>([]);
  readonly accessUser = signal<UserListItem | null>(null);
  readonly search = signal('');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly updatingUserId = signal<number | null>(null);
  readonly editingUser = signal<UserListItem | null>(null);
  readonly error = signal('');
  readonly message = signal('');

  readonly isEditing = computed(() => this.editingUser() !== null);

  form: UserForm = this.emptyForm();

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
    this.usersService.create(request).subscribe({
      next: (user) => {
        this.users.update((users) =>
          [...users, user].sort((a, b) => a.userName.localeCompare(b.userName)),
        );
        this.message.set(`User ${user.userName} was created successfully.`);
        this.resetForm();
        this.saving.set(false);
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
    this.form = {
      userName: user.userName,
      email: user.email,
      displayName: user.displayName ?? '',
      password: '',
      roleName: user.roles[0] ?? '',
      organizationId: user.organizationId,
      isActive: user.isActive,
    };
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
          this.message.set(`User ${user.userName} was updated successfully.`);
          this.cancelEdit();
          this.saving.set(false);
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
      this.message.set(`Branch access for ${user.userName} was updated (${branchCount} branch${branchCount === 1 ? '' : 'es'}).`);
    }
  }

  resetForm(): void {
    this.form = this.emptyForm();
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
