import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface SetupStep {
  step: number;
  title: string;
  route: string;
  routeLabel: string;
  points: string[];
}

interface MappingRow {
  entity: string;
  belongsTo: string;
  where: string;
}

@Component({
  selector: 'app-setup-guide',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-3">
      <div class="mb-4">
        <h2 class="mb-1">Setup Guide</h2>
        <small class="text-muted">
          How to create Organizations, Clinics, Laboratories and Branches — and how they map together.
        </small>
      </div>

      <div class="card mb-4">
        <div class="card-header fw-semibold">Hierarchy at a glance</div>
        <div class="card-body">
          <div class="d-flex flex-wrap align-items-center gap-2 hierarchy-flow">
            <span class="badge text-bg-primary p-3">Organization</span>
            <span class="arrow">&rarr;</span>
            <div class="d-flex flex-column gap-2">
              <span class="badge text-bg-success p-2">Clinic</span>
              <span class="badge text-bg-info p-2">Laboratory</span>
            </div>
            <span class="arrow">&rarr;</span>
            <span class="badge text-bg-warning text-dark p-3">Branch</span>
            <span class="arrow">&rarr;</span>
            <div class="d-flex flex-column gap-2">
              <span class="badge text-bg-secondary p-2">Departments</span>
              <span class="badge text-bg-secondary p-2">Tests</span>
              <span class="badge text-bg-dark p-2">User access</span>
            </div>
          </div>
          <p class="text-muted small mt-3 mb-0">
            A Branch is the mapping point: it links one Clinic and one Laboratory to an Organization.
            Users sign in through the branches they are granted access to.
          </p>
        </div>
      </div>

      <div class="row g-3 mb-4">
        @for(s of steps; track s.step) {
          <div class="col-md-6">
            <div class="card h-100">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-start mb-2">
                  <h5 class="mb-0">
                    <span class="badge text-bg-primary me-2">{{ s.step }}</span>{{ s.title }}
                  </h5>
                  <a class="btn btn-sm btn-outline-primary" [routerLink]="s.route">{{ s.routeLabel }}</a>
                </div>
                <ul class="text-muted mb-0 ps-3">
                  @for(p of s.points; track p) {
                    <li class="mt-1">{{ p }}</li>
                  }
                </ul>
              </div>
            </div>
          </div>
        }
      </div>

      <div class="card">
        <div class="card-header fw-semibold">Mapping reference</div>
        <div class="table-responsive">
          <table class="table table-striped align-middle mb-0">
            <thead>
              <tr>
                <th>Entity</th>
                <th>Belongs to</th>
                <th>Where you map it</th>
              </tr>
            </thead>
            <tbody>
              @for(r of mappings; track r.entity) {
                <tr>
                  <td class="fw-semibold">{{ r.entity }}</td>
                  <td>{{ r.belongsTo }}</td>
                  <td>{{ r.where }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: `
    .hierarchy-flow .arrow { font-size: 1.25rem; color: var(--color-secondary-text); font-weight: bold; }
    .hierarchy-flow .badge { min-width: 110px; }
  `,
})
export class SetupGuideComponent {
  readonly steps: SetupStep[] = [
    {
      step: 1,
      title: 'Create an Organization',
      route: '/organizations',
      routeLabel: 'Go to Organizations',
      points: [
        'The top-level tenant that owns everything else.',
        'Give it a unique Name and Code (e.g. "Al Manara Vet Lab" / MANARA).',
        'Create one per business entity; each can hold its own clinics, labs and branches.',
      ],
    },
    {
      step: 2,
      title: 'Create a Laboratory',
      route: '/laboratories',
      routeLabel: 'Go to Laboratories',
      points: [
        'Pick the Organization in the form — it defaults to your sign-in organization.',
        'Holds report settings: header, footer, signature, GST/registration numbers.',
      ],
    },
    {
      step: 3,
      title: 'Create a Clinic',
      route: '/clinics',
      routeLabel: 'Go to Clinics',
      points: [
        'Pick the Organization in the form — it defaults to your sign-in organization.',
        'Represents the practice/location that requests tests.',
        'Must be Active before it can host a branch.',
      ],
    },
    {
      step: 4,
      title: 'Create & map a Branch',
      route: '/branches',
      routeLabel: 'Go to Branches',
      points: [
        'Select the Organization first — Clinic and Laboratory dropdowns filter to it.',
        'A branch requires exactly one Clinic and one Laboratory from that organization.',
        'Branch Code must be unique within its organization.',
        'Editing lets you re-map the branch to another organization (dropdowns reload).',
      ],
    },
    {
      step: 5,
      title: 'Grant users branch access',
      route: '/users',
      routeLabel: 'Go to User Creation Master',
      points: [
        'New users cannot sign in until they are assigned a location.',
        'Assign the user to a branch (with clinic/laboratory access flags).',
        'Without an assignment, login shows "You do not have access to any active branch."',
      ],
    },
  ];

  readonly mappings: MappingRow[] = [
    { entity: 'Organization', belongsTo: 'Nothing — top level', where: 'Masters → Organizations' },
    { entity: 'Clinic', belongsTo: 'Organization (chosen at creation)', where: 'Created in Masters → Clinics' },
    { entity: 'Laboratory', belongsTo: 'Organization (chosen at creation)', where: 'Created in Masters → Laboratories' },
    { entity: 'Branch', belongsTo: 'Organization + Clinic + Laboratory', where: 'Mapped in Masters → Branches form' },
    { entity: 'Department', belongsTo: 'Branch', where: 'Branches list → Departments button' },
    { entity: 'Test', belongsTo: 'Branch', where: 'Branches list → Tests button' },
    { entity: 'User location', belongsTo: 'User + Branch', where: 'Administration → User Creation Master' },
  ];
}
