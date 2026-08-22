// src/app/auth/login.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, BranchOption, LoginStartResponse } from '../core/auth.service';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  private auth = inject(AuthService);

  step: 'credentials' | 'select-branch' = 'credentials';
  usernameOrEmail = '';
  password = '';
  error = '';
  submitting = false;
  selectingBranchId: number | null = null;

  branches: BranchOption[] = [];
  organizationName = '';

  submit(): void {
    this.error = '';
    if (!this.usernameOrEmail.trim() || !this.password) {
      this.error = 'Please enter your username and password.';
      return;
    }
    this.submitting = true;
    this.auth.login(this.usernameOrEmail.trim(), this.password).subscribe({
      next: (res: LoginStartResponse) => {
        this.submitting = false;
        const branches = res.branches ?? [];
        if (!res.requiresBranchSelection && branches.length === 1) {
          this.chooseBranch(branches[0].id);
          return;
        }
        if (!branches.length) {
          this.error = 'Your account has no branch access. Contact your administrator.';
          return;
        }
        this.branches = branches;
        this.step = 'select-branch';
      },
      error: (e) => {
        this.submitting = false;
        this.error = this.extractError(e, 'Invalid username or password');
      },
    });
  }

  chooseBranch(branchId: number): void {
    if (this.selectingBranchId) return;
    this.error = '';
    this.selectingBranchId = branchId;
    this.auth.selectBranch(branchId).subscribe({
      next: () => {},
      error: (e) => {
        this.selectingBranchId = null;
        this.error = this.extractError(e, 'Unable to select branch. Please sign in again.');
        this.backToCredentials();
      },
    });
  }

  private extractError(e: any, fallback: string): string {
    const err = e?.error;
    if (typeof err === 'string' && err.trim()) return err;
    if (err?.message) return err.message;
    if (err?.title) return err.title;
    if (err?.detail) return err.detail;
    if (typeof e?.message === 'string' && e.message) return e.message;
    return fallback;
  }

  backToCredentials(): void {
    this.step = 'credentials';
    this.branches = [];
    this.selectingBranchId = null;
    this.password = '';
  }
}
