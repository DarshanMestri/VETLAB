// src/app/auth/login.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, LabClinicOption } from '../core/auth.service';

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
  locations: LabClinicOption[] = [];
  selectedClinicId: number | null = null;
  selectedBranchId: number | null = null;
  loadingLocations = false;

  ngOnInit(): void {
    this.loadLocations();
  }

  get selectedClinic(): LabClinicOption | undefined {
    return this.locations.find((clinic) => clinic.clinicId === this.selectedClinicId);
  }

  get requiresBranchSelection(): boolean {
    return (this.selectedClinic?.branches.length ?? 0) > 0;
  }

  onClinicChange(): void {
    const branches = this.selectedClinic?.branches ?? [];
    if (!branches.some((branch) => branch.branchId === this.selectedBranchId)) {
      this.selectedBranchId = branches.length === 1 ? branches[0].branchId : null;
    }
  }

  submit() {
    this.error = '';
    if (!this.selectedClinicId) {
      this.error = 'Please select a lab location.';
      return;
    }
    if (this.requiresBranchSelection && !this.selectedBranchId) {
      this.error = 'Please select a branch for this lab location.';
      return;
    }
    this.auth
      .login(this.usernameOrEmail, this.password, this.selectedClinicId, this.selectedBranchId)
      .subscribe({
        next: () => {},
        error: (e) => (this.error = e?.error?.title || e?.error || 'Invalid username or password'),
      });
  }

  private loadLocations(): void {
    this.loadingLocations = true;
    this.auth.getLabLocations().subscribe({
      next: (items) => {
        this.locations = items ?? [];
        if (!this.locations.length) {
          this.error = 'No active lab locations are configured. Contact administrator.';
          this.loadingLocations = false;
          return;
        }

        const storedClinicId = this.auth.getSelectedClinicId();
        const fallbackClinicId = this.locations[0].clinicId;
        this.selectedClinicId = this.locations.some((x) => x.clinicId === storedClinicId)
          ? storedClinicId
          : fallbackClinicId;

        const branches = this.selectedClinic?.branches ?? [];
        const storedBranchId = this.auth.getSelectedBranchId();
        this.selectedBranchId = branches.some((x) => x.branchId === storedBranchId)
          ? storedBranchId
          : branches.length === 1
            ? branches[0].branchId
            : null;

        this.loadingLocations = false;
      },
      error: (e) => {
        this.loadingLocations = false;
        this.error = e?.error?.title || e?.error || 'Unable to load lab locations.';
      },
    });
  }
}
