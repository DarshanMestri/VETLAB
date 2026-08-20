// src/app/auth/login.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, OrganizationOption, AuthBranchOption } from '../core/auth.service';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  private auth = inject(AuthService);
  usernameOrEmail = '';
  password = '';
  error = '';
  organizations: OrganizationOption[] = [];
  branches: AuthBranchOption[] = [];
  selectedOrganizationId: number | null = null;
  selectedBranchId: number | null = null;
  loadingOrganizations = false;
  loadingBranches = false;

  ngOnInit(): void {
    this.loadOrganizations();
  }

  onOrganizationChange(): void {
    this.selectedBranchId = null;
    this.branches = [];
    if (this.selectedOrganizationId) {
      this.loadBranches(this.selectedOrganizationId);
    }
  }

  submit() {
    this.error = '';
    if (!this.selectedOrganizationId) {
      this.error = 'Please select an organization.';
      return;
    }
    if (!this.selectedBranchId) {
      this.error = 'Please select a branch.';
      return;
    }
    this.auth
      .login(this.usernameOrEmail, this.password, this.selectedOrganizationId, this.selectedBranchId)
      .subscribe({
        next: () => {},
        error: (e) => (this.error = e?.error?.title || e?.error || 'Invalid username or password'),
      });
  }

  private loadOrganizations(): void {
    this.loadingOrganizations = true;
    this.auth.getOrganizations().subscribe({
      next: (items) => {
        this.organizations = items ?? [];
        if (!this.organizations.length) {
          this.error = 'No active organizations are configured. Contact administrator.';
          this.loadingOrganizations = false;
          return;
        }

        const storedOrgId = this.auth.getSelectedOrganizationId();
        const fallbackOrgId = this.organizations[0].id;
        this.selectedOrganizationId = this.organizations.some((x) => x.id === storedOrgId)
          ? storedOrgId
          : fallbackOrgId;

        if (this.selectedOrganizationId) {
          this.loadBranches(this.selectedOrganizationId);
        }

        this.loadingOrganizations = false;
      },
      error: (e) => {
        this.loadingOrganizations = false;
        this.error = e?.error?.title || e?.error || 'Unable to load organizations.';
      },
    });
  }

  private loadBranches(orgId: number): void {
    this.loadingBranches = true;
    this.auth.getBranches(orgId).subscribe({
      next: (items) => {
        this.branches = items ?? [];

        const storedBranchId = this.auth.getSelectedBranchId();
        this.selectedBranchId = this.branches.some((x) => x.id === storedBranchId)
          ? storedBranchId
          : this.branches.length === 1
            ? this.branches[0].id
            : null;

        this.loadingBranches = false;
      },
      error: (e) => {
        this.loadingBranches = false;
        this.branches = [];
      },
    });
  }
}
