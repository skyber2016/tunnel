import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { AuthService } from './services/auth.service';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ErrorScreenComponent } from './components/error-screen/error-screen.component';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DashboardComponent, ErrorScreenComponent],
  template: `
    @if (auth.loading()) {
      <div class="splash">
        <div class="splash-spinner"></div>
        <p>Connecting to Daemon...</p>
      </div>
    } @else if (auth.error()) {
      <app-error-screen />
    } @else {
      <app-dashboard />
    }
  `,
  styles: [`
    .splash {
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      height: 100vh; background: #0f172a; color: #94a3b8; font-family: 'Segoe UI', system-ui, sans-serif;
    }
    .splash p { font-size: 0.9rem; margin-top: 16px; }
    .splash-spinner {
      width: 32px; height: 32px; border: 3px solid #334155; border-top-color: #2563eb;
      border-radius: 50%; animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class AppComponent implements OnInit {
  auth = inject(AuthService);

  ngOnInit(): void {
    this.auth.loadToken();
  }
}
