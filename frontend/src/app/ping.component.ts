import { Component } from '@angular/core';

@Component({
  selector: 'app-ping',
  standalone: true,
  // No imports required; pure inline text
  template: `<div style="padding:2rem;font-size:1.25rem">✅ Minimal render works</div>`,
})
export class PingComponent {}
