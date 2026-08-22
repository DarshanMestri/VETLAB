import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { roleGuard } from './core/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  {
    path: 'login',
    loadComponent: () => import('./auth/login.component').then((m) => m.LoginComponent),
  },

  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/dashboard.component').then((m) => m.DashboardComponent),
    canActivate: [authGuard],
  },
  {
    path: 'clinical',
    loadComponent: () =>
      import('./features/clinical/clinical-workspace.component').then(
        (m) => m.ClinicalWorkspaceComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'species',
    loadComponent: () =>
      import('./features/masters/master-list.component').then((m) => m.MasterListComponent),
    data: { type: 'species' },
    canActivate: [authGuard],
  },

  {
    path: 'clients',
    loadComponent: () =>
      import('./features/clients/clients-list.component').then((m) => m.ClientsListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'owners',
    loadComponent: () =>
      import('./features/owners/owner-management.component').then(
        (m) => m.OwnerManagementComponent,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'owners/:ownerId',
    loadComponent: () =>
      import('./features/owners/owner-management.component').then(
        (m) => m.OwnerManagementComponent,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'owners/:ownerId/animals/:animalId',
    loadComponent: () =>
      import('./features/owners/owner-management.component').then(
        (m) => m.OwnerManagementComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'departments',
    loadComponent: () =>
      import('./features/masters/master-list.component').then((m) => m.MasterListComponent),
    data: { type: 'departments' },
    canActivate: [authGuard],
  },

  {
    path: 'organizations',
    loadComponent: () =>
      import('./features/organizations/organizations-list.component').then(
        (m) => m.OrganizationsListComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'branches',
    loadComponent: () =>
      import('./features/branches/branches-list.component').then((m) => m.BranchesListComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'clinics',
    loadComponent: () =>
      import('./features/clinics/clinics-list.component').then((m) => m.ClinicsListComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'units',
    loadComponent: () =>
      import('./features/masters/master-list.component').then((m) => m.MasterListComponent),
    data: { type: 'units' },
    canActivate: [authGuard],
  },

  // Step 5: Existing catalog tables share one reusable management screen.
  {
    path: 'masters/:type',
    loadComponent: () =>
      import('./features/masters/master-list.component').then((m) => m.MasterListComponent),
    canActivate: [authGuard],
  },

  {
    path: 'tests',
    loadComponent: () =>
      import('./features/tests/tests-list.component').then((m) => m.TestsListComponent),
    canActivate: [authGuard],
  },

  {
    path: 'profiles',
    loadComponent: () =>
      import('./features/profiles/profiles-list.component').then((m) => m.ProfilesListComponent),
    canActivate: [authGuard],
  },

  {
    path: 'packages',
    loadComponent: () =>
      import('./features/packages/packages-list.component').then((m) => m.PackagesListComponent),
    canActivate: [authGuard],
  },

  {
    path: 'rates',
    loadComponent: () =>
      import('./features/rates/rates-list.component').then((m) => m.RatesListComponent),
    canActivate: [authGuard],
  },

  {
    path: 'registrations',
    loadComponent: () =>
      import('./features/registrations/registrations-list.component').then(
        (m) => m.RegistrationsListComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'registrations/new',
    loadComponent: () =>
      import('./features/registrations/registration-form.component').then(
        (m) => m.RegistrationFormComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'registrations/:id',
    loadComponent: () =>
      import('./features/registrations/registration-form.component').then(
        (m) => m.RegistrationFormComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'results',
    loadComponent: () =>
      import('./features/results/results-worklist.component').then(
        (m) => m.ResultsWorklistComponent,
      ),
    canActivate: [authGuard],
  },

  {
    path: 'results/:id',
    loadComponent: () =>
      import('./features/results/result-entry.component').then((m) => m.ResultEntryComponent),
    canActivate: [authGuard],
  },

  {
    path: 'users',
    loadComponent: () =>
      import('./features/users/users-master.component').then((m) => m.UsersMasterComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'setup-guide',
    loadComponent: () =>
      import('./features/admin-guide/setup-guide.component').then((m) => m.SetupGuideComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'laboratory-master',
    loadComponent: () =>
      import('./features/laboratory-master/laboratory-master.component').then(
        (m) => m.LaboratoryMasterComponent,
      ),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'laboratories',
    loadComponent: () =>
      import('./features/laboratories/laboratories-list.component').then(
        (m) => m.LaboratoriesListComponent,
      ),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'user-hierarchy',
    loadComponent: () =>
      import('./features/user-hierarchy/user-hierarchy.component').then(
        (m) => m.UserHierarchyComponent,
      ),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },

  {
    path: 'whatsapp',
    loadComponent: () =>
      import('./features/whatsapp/whatsapp-management.component').then(
        (m) => m.WhatsAppManagementComponent,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'billing',
    loadComponent: () =>
      import('./features/billing/billing-management.component').then(
        (m) => m.BillingManagementComponent,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'inventory-pharmacy',
    loadComponent: () =>
      import('./features/inventory-pharmacy/inventory-pharmacy-management.component').then(
        (m) => m.InventoryPharmacyManagementComponent,
      ),
    canActivate: [authGuard],
  },

  { path: '**', redirectTo: 'login' },
];
