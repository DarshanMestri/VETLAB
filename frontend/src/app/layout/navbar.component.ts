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
      { label: 'Species', route: '/species', roles: ['Admin', 'Manager'], icon: ICONS.paw },
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
      {
        label: 'Organizations',
        route: '/organizations',
        roles: ['Admin'],
        icon: ICONS.building,
        step: 1,
      },
      {
        label: 'Laboratory Groups',
        route: '/laboratory-groups',
        roles: ['Admin'],
        icon: ICONS.branches,
        step: 2,
      },
      {
        label: 'Laboratories',
        route: '/laboratories',
        roles: ['Admin'],
        icon: ICONS.flask,
        step: 3,
      },
      {
        label: 'Clinics',
        route: '/clinics',
        roles: ['Admin'],
        icon: ICONS.paw,
        step: 4,
      },
      { label: 'Branches', route: '/branches', roles: ['Admin'], icon: ICONS.mapPin, step: 5 },
      {
        label: 'Departments',
        route: '/departments',
        roles: ['Admin', 'Manager'],
        icon: ICONS.layout,
        step: 6,
      },
      { label: 'Units', route: '/units', roles: ['Admin', 'Manager'], icon: ICONS.ruler, step: 7 },
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
        <div class="user-label">
          Signed in as <strong>{{ (auth.username$ | async) || 'User' }}</strong>
        </div>
        <button class="logout-button" type="button" (click)="logout()">Logout</button>
      </div>
      <ng-template #loginBtn><a class="login-button" routerLink="/login">Login</a></ng-template>
    </aside>
  `,
  styles: [
    `
      /* Navy sidebar theme (matches login page) */
      .app-sidebar {
        position: fixed;
        inset: 0 auto 0 0;
        z-index: 1040;
        display: flex;
        flex-direction: column;
        width: 264px;
        padding: 1.1rem 0.9rem 0.9rem;
        color: #fff;
        background: linear-gradient(180deg, #0a1f44 0%, #0c2b5c 100%);
        box-shadow: 0 0 2.5rem rgba(2, 8, 23, 0.45);
        border-right: 1px solid rgba(147, 197, 253, 0.12);
        transition: transform 0.28s ease;
      }

      .sidebar-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        padding: 0.25rem 0.4rem 1rem;
        border-bottom: 1px solid rgba(147, 197, 253, 0.14);
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        min-width: 0;
        color: #fff;
        text-decoration: none;
      }

      .brand-mark {
        display: grid;
        flex: 0 0 40px;
        width: 40px;
        height: 40px;
        place-items: center;
        color: #e0f2fe;
        border: 1px solid rgba(147, 197, 253, 0.4);
        border-radius: 11px;
        background: linear-gradient(135deg, #2563eb, #1e40af);
        box-shadow: 0 6px 14px -6px rgba(37, 99, 235, 0.8);

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
        }

        small {
          margin-top: 0.18rem;
          overflow: hidden;
          color: rgba(191, 219, 254, 0.6);
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
        border: 1px solid rgba(147, 197, 253, 0.25);
        border-radius: 8px;
        color: #bfdbfe;
        background: rgba(255, 255, 255, 0.06);
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
          color: #fff;
          border-color: rgba(147, 197, 253, 0.5);
          background: rgba(59, 130, 246, 0.25);
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
          background: rgba(147, 197, 253, 0.2);
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
        color: rgba(226, 240, 255, 0.78);
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
          color: #fff;
          background: rgba(59, 130, 246, 0.16);
        }

        &:focus-visible {
          outline: 2px solid #60a5fa;
          outline-offset: -2px;
        }
      }

      .menu-parent-icon {
        display: grid;
        flex: 0 0 20px;
        width: 20px;
        height: 20px;
        place-items: center;
        color: #93c5fd;

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
        color: rgba(191, 219, 254, 0.55);
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
        color: rgba(219, 234, 254, 0.7);
        border-left: 3px solid transparent;
        border-radius: 0.4rem;
        font-size: 0.87rem;
        text-decoration: none;
        transition:
          background-color 0.16s ease,
          color 0.16s ease,
          border-color 0.16s ease;

        &:hover {
          color: #fff;
          background: rgba(59, 130, 246, 0.14);
        }

        &.active {
          color: #fff;
          border-left-color: #60a5fa;
          background: rgba(37, 99, 235, 0.32);
        }
      }

      .child-icon {
        display: grid;
        flex: 0 0 16px;
        place-items: center;
        color: rgba(147, 197, 253, 0.6);

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
        color: #dbeafe;
        border: 1px solid rgba(147, 197, 253, 0.45);
        border-radius: 999px;
        background: rgba(37, 99, 235, 0.4);
        font-size: 0.62rem;
        font-weight: 700;
        line-height: 1;
      }

      .nav-link.active .child-step {
        color: #fff;
        border-color: #60a5fa;
        background: rgba(37, 99, 235, 0.7);
      }

      .nav-link.active .child-icon {
        color: #bfdbfe;
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
        border-top: 1px solid rgba(147, 197, 253, 0.14);
      }

      .user-label {
        overflow: hidden;
        margin-bottom: 0.65rem;
        color: rgba(191, 219, 254, 0.55);
        font-size: 0.74rem;
        text-overflow: ellipsis;
        white-space: nowrap;

        strong {
          color: #fff;
        }
      }

      .logout-button,
      .login-button {
        display: block;
        width: 100%;
        padding: 0.5rem 0.7rem;
        color: #bfdbfe;
        border: 1px solid rgba(147, 197, 253, 0.35);
        border-radius: 0.45rem;
        background: transparent;
        text-align: center;
        text-decoration: none;
        transition:
          background-color 0.2s ease,
          color 0.2s ease;
      }

      .logout-button:hover,
      .login-button:hover {
        color: #fff;
        background: rgba(37, 99, 235, 0.35);
      }

      /* Desktop: hamburger shows only when collapsed (body.sidebar-collapsed rules live in styles.css) */
      .sidebar-toggle {
        display: none;
      }

      /* Mobile: overlay drawer */
      @media (max-width: 991.98px) {
        .app-sidebar {
          transform: translateX(-100%);
          box-shadow: 0 0 3rem rgba(2, 8, 23, 0.5);
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
          z-index: 1030;
          display: block;
          width: 42px;
          height: 38px;
          border: 0;
          border-radius: 0.45rem;
          color: #fff;
          background: #0a1f44;
          box-shadow: 0 8px 20px -8px rgba(2, 8, 23, 0.55);
          cursor: pointer;

          span {
            display: block;
            width: 19px;
            height: 2px;
            margin: 4px auto;
            background: #e0f2fe;
          }
        }

        .sidebar-backdrop {
          position: fixed;
          inset: 0;
          z-index: 1035;
          display: block;
          background: rgba(2, 8, 23, 0.55);
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
