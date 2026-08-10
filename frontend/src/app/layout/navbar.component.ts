import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar navbar-expand-lg header-navbar px-3 py-2">
      <div class="container-fluid">
        <a class="navbar-brand d-flex align-items-center gap-2" routerLink="/dashboard">
          <span class="brand-mark rounded-circle d-flex align-items-center justify-content-center"
            >VL</span
          >
          <div>
            <div class="brand-title mb-0">Vet LIMS</div>
            <small class="text-white-50">Laboratory information system</small>
          </div>
        </a>

        <button
          class="navbar-toggler border-0"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#vetLimsNav"
          aria-controls="vetLimsNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" id="vetLimsNav">
          <ul class="navbar-nav ms-auto mb-2 mb-lg-0 align-items-lg-center gap-2 menu-links">
            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/species" routerLinkActive="active">Species</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/clients" routerLinkActive="active">Clients</a>
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/owners" routerLinkActive="active">Owners & Animals</a>
            </li>
            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/clinical" routerLinkActive="active">Clinical EMR</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/departments" routerLinkActive="active"
                >Departments</a
              >
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/units" routerLinkActive="active">Units</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin'])">
              <a class="nav-link" routerLink="/masters/breeds" routerLinkActive="active">Masters</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/tests" routerLinkActive="active">Tests</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/profiles" routerLinkActive="active">Profiles</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/packages" routerLinkActive="active">Packages</a>
            </li>

            <li class="nav-item" *ngIf="auth.hasAnyRole(['Admin', 'Manager'])">
              <a class="nav-link" routerLink="/rates" routerLinkActive="active">Rates</a>
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/registrations" routerLinkActive="active"
                >Registrations</a
              >
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/results" routerLinkActive="active">Results</a>
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/whatsapp" routerLinkActive="active">WhatsApp</a>
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/billing" routerLinkActive="active">Billing</a>
            </li>

            <li class="nav-item" *ngIf="auth.isAuthenticated()">
              <a class="nav-link" routerLink="/inventory-pharmacy" routerLinkActive="active">Inventory</a>
            </li>
          </ul>

          <div class="d-flex align-items-center gap-2 mt-3 mt-lg-0">
            <ng-container *ngIf="auth.isAuthenticated(); else loginBtn">
              <span class="user-chip rounded-pill px-3 py-1 text-white text-truncate">
                Hi, {{ (auth.username$ | async) || 'User' }}
              </span>
              <button class="btn btn-outline-light btn-sm btn-logout" (click)="logout()">
                Logout
              </button>
            </ng-container>
            <ng-template #loginBtn>
              <a class="btn btn-outline-light btn-sm" routerLink="/login">Login</a>
            </ng-template>
          </div>
        </div>
      </div>
    </nav>
  `,
  styles: [
    `
      .header-navbar {
        background: linear-gradient(135deg, #0f766e 0%, #1d4ed8 55%, #1e293b 100%);
        box-shadow: 0 0.8rem 1.8rem rgba(15, 23, 42, 0.22);
      }

      .navbar-brand {
        color: #fff;
      }

      .brand-mark {
        width: 44px;
        height: 44px;
        background: rgba(255, 255, 255, 0.2);
        color: #ffffff;
        font-weight: 700;
        font-size: 0.95rem;
      }

      .brand-title {
        font-size: 1rem;
        font-weight: 700;
      }

      .navbar-toggler {
        filter: invert(1);
      }

      .nav-link {
        color: rgba(255, 255, 255, 0.85);
        border-radius: 0.75rem;
        font-weight: 500;
        padding: 0.45rem 0.7rem;
        transition:
          color 0.15s ease,
          background-color 0.15s ease,
          box-shadow 0.15s ease;
      }

      .nav-link.active,
      .nav-link:hover {
        color: #ffffff;
        background-color: rgba(255, 255, 255, 0.16);
        box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.18);
      }

      .user-chip {
        background: rgba(255, 255, 255, 0.2);
        min-width: 120px;
      }

      .btn-logout {
        border-color: rgba(255, 255, 255, 0.7);
        color: #fff;
      }

      .btn-logout:hover {
        background: rgba(255, 255, 255, 0.12);
      }

      @media (max-width: 991.98px) {
        .navbar-collapse {
          background: rgba(15, 23, 42, 0.95);
          border-radius: 1rem;
          padding: 1rem;
        }
      }

      @media (min-width: 992px) {
        .menu-links {
          max-width: min(100%, 980px);
          flex-wrap: wrap;
          justify-content: flex-end;
        }
      }
    `,
  ],
})
export class NavbarComponent {
  auth = inject(AuthService);
  logout() {
    this.auth.logout();
  }
}
