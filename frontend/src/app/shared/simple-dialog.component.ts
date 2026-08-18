// src/app/shared/simple-dialog.component.ts
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  standalone: true,
  selector: 'app-simple-dialog',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop fade show" (click)="close()"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <form (ngSubmit)="submit.emit(model)">
            <div class="modal-header">
              <h5 class="modal-title">{{ title }}</h5>
              <button type="button" class="btn-close" (click)="close()"></button>
            </div>
            <div class="modal-body">
              <ng-content></ng-content>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="close()">
                Cancel
              </button>
              <button type="submit" class="btn btn-primary" [disabled]="saving">{{ saving ? 'Saving…' : (okText || 'Save') }}</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class SimpleDialogComponent<T = any> {
  @Input() title = 'Edit';
  @Input() okText = 'Save';
  @Input() model!: T;
  @Input() saving = false;
  @Output() submit = new EventEmitter<T>();
  @Output() closed = new EventEmitter<void>();
  close() {
    this.closed.emit();
  }
}
