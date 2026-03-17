import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-error-screen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="error-screen">
      <div class="error-card">
        <div class="error-icon">⚠</div>
        <h1>Daemon Not Available</h1>
        <p class="error-desc">
          Cannot read auth token from <code>~/.tunnel/.auth</code>.<br>
          Make sure the SSH Tunnel Daemon is running.
        </p>
        <p class="error-detail">{{ auth.error() }}</p>
        <div class="retry-info">
          <span class="spinner"></span>
          Retrying automatically every 5 seconds...
        </div>
        <button class="btn-retry" (click)="auth.loadToken()">Retry Now</button>
      </div>
    </div>
  `,
  styles: [`
    .error-screen {
      display: flex; align-items: center; justify-content: center;
      height: 100vh; background: #0f172a; font-family: 'Segoe UI', system-ui, sans-serif;
    }
    .error-card {
      text-align: center; padding: 40px; max-width: 440px;
      background: #1e293b; border: 1px solid #334155; border-radius: 16px;
    }
    .error-icon { font-size: 3rem; margin-bottom: 12px; }
    h1 { color: #f1f5f9; font-size: 1.3rem; margin: 0 0 10px; }
    .error-desc { color: #94a3b8; font-size: 0.88rem; line-height: 1.6; margin: 0 0 10px; }
    .error-desc code { background: #0f172a; color: #38bdf8; padding: 2px 6px; border-radius: 4px; font-size: 0.82rem; }
    .error-detail { color: #64748b; font-size: 0.78rem; margin: 0 0 18px; word-break: break-all; }

    .retry-info {
      display: flex; align-items: center; justify-content: center; gap: 8px;
      color: #64748b; font-size: 0.8rem; margin-bottom: 16px;
    }
    .spinner {
      width: 14px; height: 14px; border: 2px solid #334155; border-top-color: #38bdf8;
      border-radius: 50%; animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .btn-retry {
      background: #2563eb; color: #fff; border: none; border-radius: 8px;
      padding: 10px 28px; font-size: 0.85rem; font-weight: 600; cursor: pointer;
      transition: background 0.15s;
    }
    .btn-retry:hover { background: #1d4ed8; }
  `]
})
export class ErrorScreenComponent {
  auth = inject(AuthService);
}
