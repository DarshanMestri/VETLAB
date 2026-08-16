// src/app/app.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NavbarComponent } from './layout/navbar.component';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent],
  template: `
    <app-navbar *ngIf="showNavbar()"></app-navbar>
    <main
      [class.app-content]="showNavbar()"
      [class.container]="!isLoginRoute()"
      [class.py-3]="!isLoginRoute()"
    >
      <router-outlet></router-outlet>
    </main>
  `,
})
export class AppComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly currentUrl = signal(this.router.url);

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.currentUrl.set(event.urlAfterRedirects));
  }

  isLoginRoute(): boolean {
    return this.currentUrl().startsWith('/login');
  }

  showNavbar(): boolean {
    return this.auth.isAuthenticated() && !this.isLoginRoute();
  }
}
