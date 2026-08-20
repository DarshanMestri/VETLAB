import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LaboratoriesListComponent } from '../laboratories/laboratories-list.component';
import { BranchesListComponent } from '../branches/branches-list.component';
import { DepartmentsListComponent } from '../departments/departments-list.component';
import { LaboratoryGroupsListComponent } from '../laboratory-groups/laboratory-groups-list.component';
import { UnitsListComponent } from '../units/units-list.component';

interface MasterTab {
  key: string;
  label: string;
  hint: string;
}

@Component({
  selector: 'app-laboratory-master',
  standalone: true,
  imports: [
    CommonModule,
    LaboratoriesListComponent,
    BranchesListComponent,
    DepartmentsListComponent,
    LaboratoryGroupsListComponent,
    UnitsListComponent,
  ],
  template: `
    <div class="container py-3">
      <div class="mb-3">
        <h2 class="mb-0">Laboratory Master</h2>
        <small class="text-muted">Manage all laboratory master data in one place</small>
      </div>

      <div class="d-flex flex-wrap gap-2 mb-3" role="tablist" aria-label="Laboratory master sections">
        <button
          *ngFor="let tab of tabs"
          class="btn btn-sm"
          [class.btn-primary]="activeTab === tab.key"
          [class.btn-outline-secondary]="activeTab !== tab.key"
          type="button"
          role="tab"
          [attr.aria-selected]="activeTab === tab.key"
          (click)="setTab(tab.key)"
        >
          {{ tab.label }}
        </button>
      </div>

      <div class="tab-pane" role="tabpanel">
        <ng-container *ngIf="activeTab === 'laboratories'">
          <app-laboratories-list></app-laboratories-list>
        </ng-container>
        <ng-container *ngIf="activeTab === 'locations'">
          <app-branches-list></app-branches-list>
        </ng-container>
        <ng-container *ngIf="activeTab === 'departments'">
          <app-departments-list></app-departments-list>
        </ng-container>
        <ng-container *ngIf="activeTab === 'laboratory-groups'">
          <app-laboratory-groups-list></app-laboratory-groups-list>
        </ng-container>
        <ng-container *ngIf="activeTab === 'units'">
          <app-units-list></app-units-list>
        </ng-container>
      </div>
    </div>
  `,
})
export class LaboratoryMasterComponent {
  readonly tabs: MasterTab[] = [
    { key: 'laboratories', label: 'Laboratories', hint: 'Laboratories (clinics) managed by this organization' },
    { key: 'locations', label: 'Branches', hint: 'Branch locations under each laboratory' },
    { key: 'departments', label: 'Departments', hint: 'Department master used across laboratories' },
    { key: 'laboratory-groups', label: 'Laboratory Groups', hint: 'Groups that cluster laboratories' },
    { key: 'units', label: 'Units', hint: 'Units used by test results' },
  ];

  activeTab = 'laboratories';

  setTab(key: string): void {
    this.activeTab = key;
  }
}
