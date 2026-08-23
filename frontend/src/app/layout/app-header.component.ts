// src/app/layout/app-header.component.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService, BranchOption } from '../core/auth.service';

@Component({
  standalone: true,
  selector: 'app-header',
  imports: [CommonModule],
  template: `
    <header class="ctx-bar">
      <div class="ctx-left">
        <span class="org-chip" [title]="organizationName() || ''">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path d="M3 21h18" />
            <path d="M5 21V7l7-4 7 4v14" />
            <path d="M9 9h1M14 9h1M9 13h1M14 13h1M9 17h6" />
          </svg>
          {{ organizationName() || 'Organization' }}
        </span>

        <div class="branch-switcher" *ngIf="branches().length > 1">
          <button
            type="button"
            class="branch-btn"
            (click)="toggleMenu()"
            [class.open]="menuOpen()"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
              <path d="M12 2 2 7l10 5 10-5-10-5z" />
              <path d="M2 12l10 5 10-5" />
              <path d="M2 17l10 5 10-5" />
            </svg>
            <span class="branch-name">{{ currentBranchName() || 'Branch' }}</span>
            <svg class="caret" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
              <path d="M6 9l6 6 6-6" />
            </svg>
          </button>

          <div class="menu" *ngIf="menuOpen()">
            <div class="menu-title">Switch branch</div>
            <button
              type="button"
              class="menu-item"
              *ngFor="let b of branches()"
              (click)="switchTo(b.id)"
              [disabled]="b.id === currentBranchId() || switching()"
              [class.current]="b.id === currentBranchId()"
            >
              <span class="mi-info">
                <strong>{{ b.name }}</strong>
                <small>
                  {{ b.code ? b.code + ' · ' : '' }}
                  {{
                    b.hasLaboratoryAccess && b.hasClinicAccess
                      ? 'Clinic & Laboratory'
                      : b.hasLaboratoryAccess
                        ? 'Laboratory'
                        : 'Clinic'
                  }}
                </small>
              </span>
              <span class="mi-check" *ngIf="b.id === currentBranchId()">✓</span>
            </button>
          </div>
        </div>
      </div>

      <div class="ctx-right">
        <span class="user-chip">
          <span class="avatar">{{ initial }}</span>
          {{ username }}
        </span>
        <button type="button" class="logout-btn" (click)="logout()" title="Sign out">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <path d="M16 17l5-5-5-5" />
            <path d="M21 12H9" />
          </svg>
        </button>
      </div>
    </header>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .ctx-bar {
        position: sticky;
        top: 0;
        z-index: 1030;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.45rem 1rem;
        background: #fff;
        border-bottom: 1px solid var(--color-border);
        box-shadow: 0 1px 4px rgba(90, 160, 185, 0.06);
      }

      .ctx-left {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        min-width: 0;
      }

      .org-chip {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        font-size: 0.85rem;
        font-weight: 600;
        color: var(--color-main-text);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .org-chip svg {
        width: 16px;
        height: 16px;
        color: var(--color-primary-dark);
        flex-shrink: 0;
      }

      .branch-switcher {
        position: relative;
      }

      .branch-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        border: 1px solid var(--color-border);
        background: #f7fbfd;
        border-radius: 999px;
        padding: 0.28rem 0.75rem;
        font-size: 0.82rem;
        font-weight: 600;
        color: var(--color-main-text);
        cursor: pointer;
        transition:
          border-color 0.15s ease,
          background-color 0.15s ease;
      }

      .branch-btn:hover,
      .branch-btn.open {
        border-color: var(--color-primary);
        background: var(--color-tint);
      }

      .branch-btn svg {
        width: 15px;
        height: 15px;
        color: var(--color-primary-dark);
      }

      .caret {
        transition: transform 0.15s ease;
      }

      .branch-btn.open .caret {
        transform: rotate(180deg);
      }

      .menu {
        position: absolute;
        top: calc(100% + 6px);
        left: 0;
        min-width: 260px;
        background: #fff;
        border: 1px solid var(--color-border);
        border-radius: 12px;
        box-shadow: 0 12px 32px rgba(90, 160, 185, 0.18);
        padding: 0.35rem;
        z-index: 1050;
      }

      .menu-title {
        font-size: 0.7rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-secondary-text);
        padding: 0.35rem 0.55rem;
      }

      .menu-item {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        width: 100%;
        border: none;
        background: none;
        text-align: left;
        padding: 0.5rem 0.55rem;
        border-radius: 8px;
        cursor: pointer;
      }

      .menu-item:hover:not(:disabled):not(.current) {
        background: var(--color-tint);
      }

      .menu-item.current {
        background: var(--color-tint);
      }

      .menu-item:disabled {
        cursor: default;
        opacity: 0.75;
      }

      .mi-info {
        display: flex;
        flex-direction: column;
        gap: 1px;

        strong {
          font-size: 0.86rem;
          color: var(--color-main-text);
        }

        small {
          font-size: 0.74rem;
          color: var(--color-secondary-text);
        }
      }

      .mi-check {
        color: var(--color-success);
        font-weight: 700;
      }

      .ctx-right {
        display: flex;
        align-items: center;
        gap: 0.6rem;
      }

      .user-chip {
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        font-size: 0.83rem;
        font-weight: 600;
        color: var(--color-main-text);
        white-space: nowrap;
      }

      .avatar {
        display: grid;
        place-items: center;
        width: 26px;
        height: 26px;
        border-radius: 50%;
        background: linear-gradient(135deg, #5fafcb, #66c6d8);
        color: #fff;
        font-size: 0.72rem;
        font-weight: 700;
      }

      .logout-btn {
        display: grid;
        place-items: center;
        width: 30px;
        height: 30px;
        border: 1px solid rgba(233, 121, 121, 0.35);
        border-radius: 8px;
        background: #fff;
        color: #d36565;
        cursor: pointer;
        transition:
          background-color 0.15s ease,
          border-color 0.15s ease;
      }

      .logout-btn:hover {
        background: rgba(233, 121, 121, 0.12);
        border-color: rgba(233, 121, 121, 0.5);
      }

      .logout-btn svg {
        width: 15px;
        height: 15px;
      }
    `,
  ],
})
export class AppHeaderComponent {
  private auth = inject(AuthService);
  private http = inject(HttpClient);

  readonly organizationName = signal<string>('');
  readonly branches = signal<BranchOption[]>([]);
  readonly menuOpen = signal(false);
  readonly switching = signal(false);

  constructor() {
    this.organizationName.set(this.auth.getOrganizationName() ?? '');
    this.loadBranches();
  }

  get username(): string | null {
    return this.auth.getUsername();
  }

  get initial(): string {
    return (this.username ?? '?').charAt(0).toUpperCase();
  }

  currentBranchId(): number | null {
    return this.auth.getSelectedBranchId();
  }

  currentBranchName(): string {
    const id = this.currentBranchId();
    return this.branches().find((b) => b.id === id)?.name ?? this.auth.getBranchName() ?? '';
  }

  toggleMenu(): void {
    this.menuOpen.update((v) => !v);
    if (this.menuOpen() && !this.branches().length) this.loadBranches(true);
  }

  switchTo(branchId: number): void {
    if (this.switching()) return;
    this.switching.set(true);
    this.auth.selectBranch(branchId).subscribe({
      next: () => window.location.reload(),
      error: () => this.switching.set(false),
    });
  }

  logout(): void {
    this.auth.logout();
  }

  private loadBranches(force = false): void {
    if (!force && this.branches().length) return;
    this.http.get<BranchOption[]>('/api/auth/my-branches').subscribe((items) => {
      this.branches.set(items ?? []);
      if ((items ?? []).length <= 1) this.menuOpen.set(false);
    });
  }
}
