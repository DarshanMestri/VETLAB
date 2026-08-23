import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/auth.service';

interface NavItem {
  label: string;
  route: string;
  roles?: string[];
  icon: string;
  step?: number;
}

interface NavGroup {
  key: string;
  label: string;
  icon: string;
  children: NavItem[];
}

const ICONS = {
  grid: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>`,
  dashboard: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>`,
  activity: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12h4l3-7 4 14 3-7h4"/></svg>`,
  users: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20c0-3.5 3-5.5 6.5-5.5s6.5 2 6.5 5.5"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M21 20c0-2.8-1.6-4.7-4-5.2"/></svg>`,
  briefcase: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/><path d="M3 12h18"/></svg>`,
  heart: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20s-7-4.5-9.3-8.6C1 8.5 3 5 6.5 5c2 0 3.4 1.2 4.2 2.3l1.3 1.7 1.3-1.7C14.1 6.2 15.5 5 17.5 5 21 5 23 8.5 21.3 11.4 19 15.5 12 20 12 20z"/></svg>`,
  paw: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="6" cy="9" r="1.8"/><circle cx="11" cy="6.5" r="1.8"/><circle cx="16" cy="9" r="1.8"/><circle cx="18.5" cy="14" r="1.8"/><path d="M6.5 18.5c1.5-2 3.5-3.5 5.5-3.5s4 1.5 5.5 3.5c1 1.3.8 3-.8 3.6-1.3.5-2.8.4-4.7.4s-3.4.1-4.7-.4c-1.6-.6-1.8-2.3-.8-3.6z"/></svg>`,
  clipboard: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v1"/><path d="M9 12h6M9 16h6"/></svg>`,
  flask: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M10 2h4"/><path d="M12 2v6l6.5 9.5A2 2 0 0 1 16.8 21H7.2a2 2 0 0 1-1.7-3.5L12 8"/><path d="M8.5 14h7"/></svg>`,
  fileCheck: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/><path d="M9 14l2 2 4-4"/></svg>`,
  testTube: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14.5 2v7.3l5.2 9.2A2 2 0 0 1 18 21H6a2 2 0 0 1-1.7-2.5l5.2-9.2V2"/><path d="M8 2h8"/><path d="M7 15h10"/></svg>`,
  layers: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2 2 7l10 5 10-5-10-5z"/><path d="M2 12l10 5 10-5"/><path d="M2 17l10 5 10-5"/></svg>`,
  package: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18l-2 14H5L3 6z"/><path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/></svg>`,
  tag: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2H4a2 2 0 0 0-2 2v8l10 10 10-10L12 2z"/><circle cx="7" cy="7" r="1.5"/></svg>`,
  ruler: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="9" width="20" height="6" rx="1.5" transform="rotate(-45 12 12)"/><path d="M7 14l2-2M11 18l2-2M9.5 12.5l1-1"/></svg>`,
  shield: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l7 3v6c0 4.5-3 8-7 9-4-1-7-4.5-7-9V6l7-3z"/></svg>`,
  layout: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>`,
  building: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 21V5a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v16"/><path d="M16 8h3a1 1 0 0 1 1 1v12"/><path d="M3 21h18"/><path d="M8 7h2M8 11h2M8 15h2"/></svg>`,
  branches: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="6" cy="6" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="12" cy="18" r="2.5"/><path d="M6 8.5v2a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2v-2"/></svg>`,
  mapPin: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 21s-7-5.1-7-11a7 7 0 1 1 14 0c0 5.9-7 11-7 11z"/><circle cx="12" cy="10" r="2.6"/></svg>`,
  userPlus: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20c0-3.5 3-5.5 6.5-5.5s6.5 2 6.5 5.5"/><path d="M18 8v6M15 11h6"/></svg>`,
  database: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/><path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6"/></svg>`,
  book: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>`,
  sliders: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 6h16M4 12h16M4 18h16"/><circle cx="9" cy="6" r="2"/><circle cx="15" cy="12" r="2"/><circle cx="8" cy="18" r="2"/></svg>`,
  card: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/><path d="M6 15h4"/></svg>`,
  box: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2 21 7v10l-9 5-9-5V7l9-5z"/><path d="M3 7l9 5 9-5M12 22V12"/></svg>`,
  message: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12a8 8 0 0 1-8 8H4l2-3a8 8 0 1 1 15-5z"/><path d="M8 10h8M8 14h5"/></svg>`,
  chevronLeft: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 6l-6 6 6 6"/></svg>`,
  chevronDown: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9l6 6 6-6"/></svg>`,
};

const NAV_GROUPS: NavGroup[] = [
  {
    key: 'workspace',
    label: 'Workspace',
    icon: ICONS.grid,
    children: [
      { label: 'Dashboard', route: '/dashboard', icon: ICONS.dashboard },
      { label: 'Clinical EMR', route: '/clinical', icon: ICONS.activity },
    ],
  },
  {
    key: 'patient',
    label: 'Patient Management',
    icon: ICONS.users,
    children: [
      { label: 'Clients', route: '/clients', roles: ['Admin', 'Manager'], icon: ICONS.briefcase },
      { label: 'Owners & Animals', route: '/owners', icon: ICONS.heart },
      { label: 'Registrations', route: '/registrations', icon: ICONS.clipboard },
    ],
  },
  {
    key: 'laboratory',
    label: 'Laboratory',
    icon: ICONS.flask,
    children: [
      { label: 'Results', route: '/results', icon: ICONS.fileCheck },
      { label: 'Tests', route: '/tests', roles: ['Admin', 'Manager'], icon: ICONS.testTube },
      { label: 'Profiles', route: '/profiles', roles: ['Admin', 'Manager'], icon: ICONS.layers },
      { label: 'Packages', route: '/packages', roles: ['Admin', 'Manager'], icon: ICONS.package },
      { label: 'Rates', route: '/rates', roles: ['Admin', 'Manager'], icon: ICONS.tag },
    ],
  },
  {
    key: 'masters',
    label: 'Masters',
    icon: ICONS.database,
    children: [
      // Setup order: Organization → Branches → Laboratory & Clinic → Departments.
      // Units are not a setup step; they are managed via Administration → Masters
      // and the Laboratory Master screen.
      {
        label: 'Organizations',
        route: '/organizations',
        roles: ['Admin'],
        icon: ICONS.building,
        step: 1,
      },
      { label: 'Branches', route: '/branches', roles: ['Admin'], icon: ICONS.mapPin, step: 2 },
      {
        label: 'Laboratories',
        route: '/laboratories',
        roles: ['Admin'],
        icon: ICONS.flask,
        step: 3,
      },
      { label: 'Clinics', route: '/clinics', roles: ['Admin'], icon: ICONS.paw, step: 4 },
      {
        label: 'Departments',
        route: '/departments',
        roles: ['Admin', 'Manager'],
        icon: ICONS.layout,
        step: 5,
      },
    ],
  },
  {
    key: 'admin',
    label: 'Administration',
    icon: ICONS.shield,
    children: [
      {
        label: 'Laboratory Master',
        route: '/laboratory-master',
        roles: ['Admin'],
        icon: ICONS.database,
      },
      {
        label: 'User Creation Master',
        route: '/users',
        roles: ['Admin'],
        icon: ICONS.userPlus,
      },
      {
        label: 'User Login & Hierarchy',
        route: '/user-hierarchy',
        roles: ['Admin'],
        icon: ICONS.activity,
      },
      { label: 'Masters', route: '/masters/breeds', roles: ['Admin'], icon: ICONS.tag },
      {
        label: 'Setup Guide',
        route: '/setup-guide',
        roles: ['Admin'],
        icon: ICONS.book,
      },
    ],
  },
  {
    key: 'operations',
    label: 'Operations',
    icon: ICONS.sliders,
    children: [
      { label: 'Billing', route: '/billing', icon: ICONS.card },
      { label: 'Inventory', route: '/inventory-pharmacy', icon: ICONS.box },
      { label: 'WhatsApp', route: '/whatsapp', icon: ICONS.message },
    ],
  },
];

@Component({
  standalone: true,
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <button
      class="sidebar-toggle"
      type="button"
      (click)="openSidebar()"
      aria-label="Toggle menu"
      aria-expanded="false"
    >
      <span></span><span></span><span></span>
    </button>
    <div class="sidebar-backdrop" *ngIf="sidebarOpen()" (click)="closeSidebar()"></div>

    <aside
      class="app-sidebar"
      [class.is-open]="sidebarOpen()"
      [class.is-collapsed]="sidebarCollapsed()"
    >
      <div class="sidebar-header">
        <a class="brand" routerLink="/dashboard" (click)="closeSidebar()">
          <span class="brand-mark">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 3h6v6h6v6h-6v6H9v-6H3V9h6V3z" />
            </svg>
          </span>
          <span class="brand-text">
            <strong>Vet LIMS</strong>
            <small>Laboratory information system</small>
          </span>
        </a>
        <button
          class="sidebar-collapse"
          type="button"
          (click)="toggleCollapse()"
          aria-label="Hide sidebar"
          title="Hide sidebar"
        >
          <span [innerHTML]="trustSvg(ICONS.chevronLeft)"></span>
        </button>
        <button
          class="sidebar-close"
          type="button"
          (click)="closeSidebar()"
          aria-label="Close menu"
        >
          &times;
        </button>
      </div>

      <nav class="sidebar-nav" aria-label="Main navigation">
        <ng-container *ngFor="let group of visibleGroups()">
          <div class="menu-group">
            <button
              class="menu-parent"
              type="button"
              [attr.aria-expanded]="isGroupOpen(group.key)"
              (click)="toggleGroup(group.key)"
            >
              <span class="menu-parent-icon" [innerHTML]="trustSvg(group.icon)"></span>
              <span class="menu-parent-label">{{ group.label }}</span>
              <span
                class="menu-chevron"
                [class.rotated]="isGroupOpen(group.key)"
                [innerHTML]="trustSvg(ICONS.chevronDown)"
              ></span>
            </button>

            <div class="menu-children" [class.expanded]="isGroupOpen(group.key)">
              <a
                class="nav-link"
                *ngFor="let child of allowedChildren(group)"
                [routerLink]="child.route"
                routerLinkActive="active"
                (click)="closeSidebar()"
              >
                <span class="child-step" *ngIf="child.step">{{ child.step }}</span>
                <span class="child-icon" *ngIf="!child.step" [innerHTML]="trustSvg(child.icon)"></span>
                <span class="child-label">{{ child.label }}</span>
              </a>
            </div>
          </div>
        </ng-container>
      </nav>

      <div class="sidebar-footer" *ngIf="auth.isAuthenticated(); else loginBtn">
        <div class="user-card">
          <div class="uc-top">
            <span class="uc-avatar">{{ initial }}</span>
            <span class="uc-name" [title]="username">{{ username }}</span>
          </div>
          <div class="uc-meta" [title]="organizationName">
            <span class="uc-icon" [innerHTML]="trustSvg(ICONS.building)"></span>
            <span class="uc-text">{{ organizationName }}</span>
          </div>
          <div class="uc-meta" [title]="branchName">
            <span class="uc-icon" [innerHTML]="trustSvg(ICONS.mapPin)"></span>
            <span class="uc-text">{{ branchName }}</span>
          </div>
        </div>
        <button class="logout-button" type="button" (click)="logout()">Logout</button>
      </div>
      <ng-template #loginBtn><a class="login-button" routerLink="/login">Login</a></ng-template>
    </aside>
  `,
  styles: [
    `
      /* Light airy sidebar (matches login page) */
      .app-sidebar {
        position: fixed;
        inset: 0 auto 0 0;
        z-index: 1040;
        display: flex;
        flex-direction: column;
        width: 264px;
        padding: 1.1rem 0.9rem 0.9rem;
        color: var(--color-main-text);
        background: var(--color-sidebar);
        box-shadow: 0 0 2rem rgba(90, 160, 185, 0.09);
        border-right: 1px solid var(--color-border);
        transition: transform 0.28s ease;
      }

      .sidebar-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        padding: 0.25rem 0.4rem 1rem;
        border-bottom: 1px solid var(--color-border);
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        min-width: 0;
        color: var(--color-main-text);
        text-decoration: none;
      }

      .brand-mark {
        display: grid;
        flex: 0 0 40px;
        width: 40px;
        height: 40px;
        place-items: center;
        color: #fff;
        border-radius: 11px;
        background: linear-gradient(135deg, #5fafcb, #66c6d8);
        box-shadow: 0 6px 14px -6px rgba(95, 175, 203, 0.7);

        svg {
          width: 19px;
          height: 19px;
        }
      }

      .brand-text {
        display: flex;
        min-width: 0;
        flex-direction: column;
        line-height: 1.2;

        strong {
          font-size: 1.02rem;
          letter-spacing: 0.01em;
          color: var(--color-main-text);
        }

        small {
          margin-top: 0.18rem;
          overflow: hidden;
          color: var(--color-secondary-text);
          font-size: 0.66rem;
          text-overflow: ellipsis;
          white-space: nowrap;
        }
      }

      .sidebar-collapse,
      .sidebar-close {
        display: grid;
        place-items: center;
        width: 32px;
        height: 32px;
        padding: 0;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        color: var(--color-secondary-text);
        background: transparent;
        cursor: pointer;
        transition:
          background-color 0.2s ease,
          color 0.2s ease,
          border-color 0.2s ease;

        svg {
          width: 16px;
          height: 16px;
        }

        &:hover {
          color: var(--color-primary-dark);
          border-color: var(--color-light-cyan);
          background: var(--color-tint);
        }
      }

      .sidebar-close {
        display: none;
        font-size: 1.3rem;
        line-height: 1;
      }

      .sidebar-nav {
        flex: 1;
        overflow-y: auto;
        padding: 0.65rem 0;

        &::-webkit-scrollbar {
          width: 6px;
        }

        &::-webkit-scrollbar-thumb {
          border-radius: 999px;
          background: rgba(95, 175, 203, 0.25);
        }
      }

      /* Parent / child accordion */
      .menu-group {
        margin-bottom: 0.35rem;
      }

      .menu-parent {
        display: flex;
        align-items: center;
        gap: 0.65rem;
        width: 100%;
        padding: 0.62rem 0.7rem;
        border: none;
        border-left: 3px solid transparent;
        border-radius: 0.4rem;
        color: rgba(95, 104, 115, 0.85);
        background: transparent;
        font-size: 0.9rem;
        font-weight: 600;
        text-align: left;
        cursor: pointer;
        transition:
          background-color 0.18s ease,
          color 0.18s ease,
          border-color 0.18s ease;

        &:hover {
          color: var(--color-main-text);
          background: var(--color-tint);
        }

        &:focus-visible {
          outline: 2px solid var(--color-cyan);
          outline-offset: -2px;
        }
      }

      .menu-parent-icon {
        display: grid;
        flex: 0 0 20px;
        width: 20px;
        height: 20px;
        place-items: center;
        color: var(--color-primary-dark);

        svg {
          width: 19px;
          height: 19px;
        }
      }

      .menu-parent-label {
        flex: 1;
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .menu-chevron {
        display: grid;
        flex: 0 0 16px;
        place-items: center;
        color: rgba(138, 148, 158, 0.6);
        transition: transform 0.28s ease;

        svg {
          width: 15px;
          height: 15px;
        }
      }

      .menu-chevron.rotated {
        transform: rotate(180deg);
      }

      .menu-children {
        max-height: 0;
        overflow: hidden;
        opacity: 0;
        transition:
          max-height 0.32s ease,
          opacity 0.25s ease,
          padding 0.25s ease;
      }

      .menu-children.expanded {
        max-height: 640px;
        padding: 0.25rem 0 0.4rem;
        opacity: 1;
      }

      .nav-link {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        padding: 0.48rem 0.7rem 0.48rem 2.35rem;
        margin: 0 0 0.1rem;
        color: rgba(95, 104, 115, 0.8);
        border-left: 3px solid transparent;
        border-radius: 0.4rem;
        font-size: 0.87rem;
        text-decoration: none;
        transition:
          background-color 0.16s ease,
          color 0.16s ease,
          border-color 0.16s ease;

        &:hover {
          color: var(--color-main-text);
          background: #f2fafc;
        }

        &.active {
          color: #3d87a3;
          font-weight: 600;
          border-left-color: var(--color-active-menu);
          background: var(--color-tint);
        }
      }

      .child-icon {
        display: grid;
        flex: 0 0 16px;
        place-items: center;
        color: rgba(74, 159, 190, 0.75);

        svg {
          width: 16px;
          height: 16px;
        }
      }

      .child-step {
        display: grid;
        flex: 0 0 16px;
        width: 16px;
        height: 16px;
        place-items: center;
        color: var(--color-primary-dark);
        border: 1px solid rgba(95, 175, 203, 0.45);
        border-radius: 999px;
        background: rgba(95, 175, 203, 0.12);
        font-size: 0.62rem;
        font-weight: 700;
        line-height: 1;
      }

      .nav-link.active .child-step {
        color: #fff;
        border-color: var(--color-active-menu);
        background: var(--color-active-menu);
      }

      .nav-link.active .child-icon {
        color: var(--color-primary-dark);
      }

      .child-label {
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      /* Footer */
      .sidebar-footer {
        padding: 0.9rem 0.4rem 0;
        border-top: 1px solid var(--color-border);
      }

      .user-card {
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
        margin-bottom: 0.65rem;
        padding: 0.7rem 0.6rem;
        border: 1px solid var(--color-border);
        border-radius: 0.55rem;
        background: #f7fbfd;
      }

      .uc-top {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        min-width: 0;
      }

      .uc-avatar {
        display: grid;
        place-items: center;
        flex: 0 0 28px;
        width: 28px;
        height: 28px;
        color: #fff;
        border-radius: 50%;
        background: linear-gradient(135deg, #5fafcb, #66c6d8);
        font-size: 0.75rem;
        font-weight: 700;
      }

      .uc-name {
        min-width: 0;
        overflow: hidden;
        color: var(--color-main-text);
        font-size: 0.86rem;
        font-weight: 600;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .uc-meta {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        min-width: 0;
      }

      .uc-icon {
        display: grid;
        place-items: center;
        flex: 0 0 16px;
        width: 16px;
        height: 16px;
        color: var(--color-primary-dark);

        svg {
          width: 13px;
          height: 13px;
        }
      }

      .uc-text {
        min-width: 0;
        overflow: hidden;
        color: var(--color-secondary-text);
        font-size: 0.72rem;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .logout-button,
      .login-button {
        display: block;
        width: 100%;
        padding: 0.5rem 0.7rem;
        color: var(--color-primary-dark);
        border: 1px solid var(--color-border);
        border-radius: 0.45rem;
        background: transparent;
        text-align: center;
        text-decoration: none;
        transition:
          background-color 0.2s ease,
          color 0.2s ease,
          border-color 0.2s ease;
      }

      .logout-button:hover,
      .login-button:hover {
        color: #3d87a3;
        border-color: var(--color-light-cyan);
        background: var(--color-tint);
      }

      /* Desktop: hamburger shows only when collapsed (body.sidebar-collapsed rules live in styles.css) */
      .sidebar-toggle {
        display: none;
      }

      /* Mobile: overlay drawer */
      @media (max-width: 991.98px) {
        .app-sidebar {
          transform: translateX(-100%);
          box-shadow: 0 0 3rem rgba(70, 81, 92, 0.25);
        }

        .app-sidebar.is-open {
          transform: translateX(0);
        }

        .sidebar-close {
          display: grid;
        }

        .sidebar-collapse {
          display: none;
        }

        .sidebar-toggle {
          position: fixed;
          top: 0.85rem;
          left: 0.85rem;
          z-index: 1037;
          display: block;
          width: 42px;
          height: 38px;
          border: 1px solid var(--color-border);
          border-radius: 0.45rem;
          color: var(--color-primary-dark);
          background: var(--color-card-background);
          box-shadow: 0 8px 20px -10px rgba(90, 160, 185, 0.35);
          cursor: pointer;

          span {
            display: block;
            width: 19px;
            height: 2px;
            margin: 4px auto;
            border-radius: 2px;
            background: var(--color-primary-dark);
          }
        }

        .sidebar-backdrop {
          position: fixed;
          inset: 0;
          z-index: 1035;
          display: block;
          background: rgba(70, 81, 92, 0.45);
        }
      }
    `,
  ],
})
export class NavbarComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);

  readonly ICONS = ICONS;
  readonly groups = NAV_GROUPS;

  readonly sidebarOpen = signal(false);
  readonly sidebarCollapsed = signal(false);
  readonly openGroups = signal<Record<string, boolean>>({ workspace: true });

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.autoOpenGroupForActiveRoute());
  }

  visibleGroups(): NavGroup[] {
    return this.groups.filter((group) => this.allowedChildren(group).length > 0);
  }

  allowedChildren(group: NavGroup): NavItem[] {
    return group.children.filter(
      (child) => !child.roles || this.auth.hasAnyRole(child.roles),
    );
  }

  isGroupOpen(key: string): boolean {
    return !!this.openGroups()[key];
  }

  toggleGroup(key: string): void {
    this.openGroups.update((state) => ({ ...state, [key]: !state[key] }));
  }

  trustSvg(markup: string) {
    return this.sanitizer.bypassSecurityTrustHtml(markup);
  }

  toggleCollapse(): void {
    this.setCollapsed(!this.sidebarCollapsed());
  }

  openSidebar(): void {
    if (window.matchMedia('(min-width: 992px)').matches) {
      this.setCollapsed(false);
    } else {
      this.sidebarOpen.set(true);
    }
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
    this.closeSidebar();
  }

  get username(): string {
    return this.auth.getUsername() || 'Unknown user';
  }

  get organizationName(): string {
    return this.auth.getOrganizationName() || 'No organization';
  }

  get branchName(): string {
    return this.auth.getBranchName() || 'No branch';
  }

  get initial(): string {
    return this.username.charAt(0).toUpperCase();
  }

  private setCollapsed(collapsed: boolean): void {
    this.sidebarCollapsed.set(collapsed);
    if (typeof document !== 'undefined') {
      document.body.classList.toggle('sidebar-collapsed', collapsed);
    }
    if (collapsed) this.closeSidebar();
  }

  private autoOpenGroupForActiveRoute(): void {
    const url = this.router.url;
    const open: Record<string, boolean> = { ...this.openGroups() };
    for (const group of this.groups) {
      if (group.children.some((child) => url.startsWith(child.route))) {
        open[group.key] = true;
      }
    }
    this.openGroups.set(open);
  }
}
