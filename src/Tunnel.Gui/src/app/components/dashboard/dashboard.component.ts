import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TunnelService } from '../../services/tunnel.service';
import { TunnelStatus, Profile, ProfilesConfig, PortStatus } from '../../models/tunnel.model';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="dashboard">
      <!-- Header -->
      <header class="header">
        <div class="header-left">
          <h1 class="title">SSH Tunnel Manager</h1>
          <span class="badge" [class.connected]="status()?.isConnected" [class.disconnected]="!status()?.isConnected">
            {{ status()?.isConnected ? 'Connected' : 'Disconnected' }}
          </span>
        </div>
        <div class="header-actions">
          <button class="btn btn-outline" (click)="onReload()" [disabled]="actionLoading()">
            <span class="icon">↻</span> Reload
          </button>
          <button class="btn btn-danger" (click)="onClean()" [disabled]="actionLoading()">
            <span class="icon">🗑</span> Clean
          </button>
        </div>
      </header>

      <!-- Toast -->
      @if (toast()) {
        <div class="toast" [class.toast-success]="toastType() === 'success'" [class.toast-error]="toastType() === 'error'">
          {{ toast() }}
        </div>
      }

      <!-- Active Tunnel Info -->
      @if (status()?.isConnected) {
        <section class="active-tunnel card">
          <div class="card-header">
            <h2>Active Tunnel</h2>
            <div class="tunnel-actions">
              <button class="btn btn-warning" (click)="onReconnectProfile()" [disabled]="actionLoading()">Reconnect</button>
              <button class="btn btn-danger" (click)="onStop()" [disabled]="actionLoading()">Stop</button>
            </div>
          </div>
          <div class="tunnel-meta">
            <span class="meta-item"><strong>Profile:</strong> {{ status()?.activeProfile }}</span>
            <span class="meta-item"><strong>Jump Host:</strong> {{ status()?.jumpHost }}</span>
          </div>

          <!-- Port Table -->
          <table class="port-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Local</th>
                <th>Remote</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (port of status()?.ports; track port.name) {
                <tr>
                  <td class="port-name">{{ port.name }}</td>
                  <td><code>localhost:{{ port.localPort }}</code></td>
                  <td><code>{{ port.remoteHost }}:{{ port.remotePort }}</code></td>
                  <td>
                    <span class="status-dot" [class.open]="port.isStarted" [class.closed]="!port.isStarted"></span>
                    {{ port.isStarted ? 'Open' : 'Closed' }}
                  </td>
                  <td class="port-actions">
                    <button class="btn-sm btn-outline" (click)="onReconnectPort(port.name)" title="Reconnect">↻</button>
                    <button class="btn-sm btn-danger" (click)="onRemovePort(port.name)" title="Remove">✕</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>

          <!-- Add Port Form -->
          <details class="add-port-section">
            <summary class="btn btn-outline">+ Add Port</summary>
            <div class="add-port-form">
              <input type="text"    placeholder="Name"        [(ngModel)]="newPort.name" class="input">
              <input type="number"  placeholder="Local Port"  [(ngModel)]="newPort.local" class="input input-sm">
              <input type="number"  placeholder="Remote Port" [(ngModel)]="newPort.remote" class="input input-sm">
              <input type="text"    placeholder="Remote Host" [(ngModel)]="newPort.remoteHost" class="input">
              <button class="btn btn-primary" (click)="onAddPort()" [disabled]="actionLoading()">Add</button>
            </div>
          </details>
        </section>
      }

      <!-- Profile List -->
      <section class="profiles card">
        <div class="card-header">
          <h2>Profiles</h2>
        </div>
        @if (profilesLoading()) {
          <div class="loading-row">Loading profiles...</div>
        } @else if (!profiles()?.length) {
          <div class="empty-state">No profiles configured. Edit <code>~/.tunnel/profiles.json</code> to create one.</div>
        } @else {
          <div class="profile-grid">
            @for (p of profiles(); track p.name) {
              <div class="profile-card" [class.active]="p.name === status()?.activeProfile">
                <div class="profile-info">
                  <h3>{{ p.name }}</h3>
                  <p class="profile-host">{{ p.jumpHost.user }}&#64;{{ p.jumpHost.host }}:{{ p.jumpHost.port }}</p>
                  <p class="profile-ports">{{ p.ports.length }} port(s)</p>
                </div>
                <div class="profile-actions">
                  @if (p.name === status()?.activeProfile && status()?.isConnected) {
                    <button class="btn btn-danger btn-sm-text" (click)="onStop()" [disabled]="actionLoading()">Stop</button>
                  } @else {
                    <button class="btn btn-primary btn-sm-text" (click)="onStart(p)" [disabled]="actionLoading()">Start</button>
                  }
                  <button class="btn btn-danger btn-sm-text" (click)="onRemoveProfile(p.name)" [disabled]="actionLoading()">Delete</button>
                </div>
              </div>
            }
          </div>
        }
      </section>
    </div>
  `,
  styles: [`
    .dashboard { padding: 16px 20px; max-width: 900px; margin: 0 auto; font-family: 'Segoe UI', system-ui, sans-serif; }
    
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    .header-left { display: flex; align-items: center; gap: 12px; }
    .title { font-size: 1.25rem; font-weight: 700; margin: 0; color: #e2e8f0; }
    .header-actions { display: flex; gap: 8px; }

    .badge {
      font-size: 0.7rem; padding: 3px 10px; border-radius: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;
    }
    .badge.connected { background: #065f46; color: #6ee7b7; }
    .badge.disconnected { background: #7f1d1d; color: #fca5a5; }

    .card {
      background: #1e293b; border: 1px solid #334155; border-radius: 10px;
      padding: 16px; margin-bottom: 16px;
    }
    .card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
    .card-header h2 { margin: 0; font-size: 1rem; font-weight: 600; color: #cbd5e1; }

    .tunnel-meta { display: flex; gap: 24px; margin-bottom: 14px; font-size: 0.85rem; color: #94a3b8; }
    .tunnel-actions { display: flex; gap: 6px; }

    .port-table { width: 100%; border-collapse: collapse; font-size: 0.82rem; }
    .port-table th { text-align: left; color: #64748b; font-weight: 600; padding: 6px 8px; border-bottom: 1px solid #334155; }
    .port-table td { padding: 8px; border-bottom: 1px solid #1e293b; color: #cbd5e1; }
    .port-table code { background: #0f172a; padding: 2px 6px; border-radius: 4px; font-size: 0.78rem; color: #38bdf8; }
    .port-name { font-weight: 600; color: #e2e8f0; }
    .port-actions { display: flex; gap: 4px; }

    .status-dot {
      display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 6px;
    }
    .status-dot.open { background: #22c55e; box-shadow: 0 0 6px #22c55e80; }
    .status-dot.closed { background: #ef4444; }

    .profile-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 10px; }
    .profile-card {
      background: #0f172a; border: 1px solid #334155; border-radius: 8px;
      padding: 14px; display: flex; justify-content: space-between; align-items: center;
      transition: border-color 0.2s;
    }
    .profile-card.active { border-color: #22c55e; box-shadow: 0 0 0 1px #22c55e40; }
    .profile-card h3 { margin: 0 0 4px; font-size: 0.9rem; color: #f1f5f9; }
    .profile-host { margin: 0; font-size: 0.78rem; color: #64748b; }
    .profile-ports { margin: 4px 0 0; font-size: 0.75rem; color: #475569; }
    .profile-actions { display: flex; flex-direction: column; gap: 4px; }

    /* Buttons */
    .btn {
      border: none; cursor: pointer; border-radius: 6px; padding: 6px 14px;
      font-size: 0.8rem; font-weight: 600; transition: all 0.15s; display: inline-flex; align-items: center; gap: 4px;
    }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-primary { background: #2563eb; color: #fff; }
    .btn-primary:hover:not(:disabled) { background: #1d4ed8; }
    .btn-warning { background: #d97706; color: #fff; }
    .btn-warning:hover:not(:disabled) { background: #b45309; }
    .btn-danger { background: #dc2626; color: #fff; }
    .btn-danger:hover:not(:disabled) { background: #b91c1c; }
    .btn-outline { background: transparent; border: 1px solid #475569; color: #94a3b8; }
    .btn-outline:hover:not(:disabled) { border-color: #64748b; color: #cbd5e1; }
    .btn-sm-text { font-size: 0.72rem; padding: 4px 10px; }

    .btn-sm { border: none; cursor: pointer; border-radius: 4px; padding: 3px 8px; font-size: 0.75rem; font-weight: 600; }
    .btn-sm.btn-outline { background: transparent; border: 1px solid #475569; color: #94a3b8; }
    .btn-sm.btn-danger { background: #7f1d1d; color: #fca5a5; }

    /* Add port form */
    .add-port-section { margin-top: 12px; }
    .add-port-section summary { cursor: pointer; list-style: none; }
    .add-port-section summary::-webkit-details-marker { display: none; }
    .add-port-form { display: flex; gap: 8px; margin-top: 10px; align-items: center; flex-wrap: wrap; }
    .input {
      background: #0f172a; border: 1px solid #334155; color: #e2e8f0; border-radius: 6px;
      padding: 6px 10px; font-size: 0.8rem; outline: none;
    }
    .input:focus { border-color: #2563eb; }
    .input-sm { width: 90px; }

    /* States */
    .loading-row, .empty-state { padding: 20px; text-align: center; color: #64748b; font-size: 0.85rem; }
    .empty-state code { background: #0f172a; padding: 2px 6px; border-radius: 4px; color: #38bdf8; }

    /* Toast */
    .toast {
      padding: 10px 16px; border-radius: 8px; margin-bottom: 14px; font-size: 0.82rem; font-weight: 500; animation: fadeIn 0.2s;
    }
    .toast-success { background: #065f4680; color: #6ee7b7; border: 1px solid #065f46; }
    .toast-error { background: #7f1d1d80; color: #fca5a5; border: 1px solid #7f1d1d; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  private tunnelService = inject(TunnelService);
  private pollTimer: ReturnType<typeof setInterval> | null = null;

  status = signal<TunnelStatus | null>(null);
  profiles = signal<Profile[]>([]);
  profilesLoading = signal(true);
  actionLoading = signal(false);
  toast = signal<string | null>(null);
  toastType = signal<'success' | 'error'>('success');

  newPort = { name: '', local: 0, remote: 0, remoteHost: '127.0.0.1' };

  ngOnInit(): void {
    this.refresh();
    this.pollTimer = setInterval(() => this.refreshStatus(), 3000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  private refresh(): void {
    this.refreshStatus();
    this.refreshProfiles();
  }

  private refreshStatus(): void {
    this.tunnelService.getStatus().subscribe({
      next: res => { if (res.success) this.status.set(res.data!); },
    });
  }

  private refreshProfiles(): void {
    this.profilesLoading.set(true);
    this.tunnelService.getProfiles().subscribe({
      next: res => {
        if (res.success) this.profiles.set(res.data!.profiles);
        this.profilesLoading.set(false);
      },
      error: () => this.profilesLoading.set(false),
    });
  }

  private showToast(msg: string, type: 'success' | 'error'): void {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(null), 4000);
  }

  private doAction(obs: ReturnType<typeof this.tunnelService.reload>): void {
    this.actionLoading.set(true);
    obs.subscribe({
      next: res => {
        this.showToast(res.message, res.success ? 'success' : 'error');
        this.actionLoading.set(false);
        this.refresh();
      },
      error: err => {
        this.showToast(String(err.message || err), 'error');
        this.actionLoading.set(false);
      },
    });
  }

  onStart(p: Profile): void { this.doAction(this.tunnelService.startProfile(p)); }
  onStop(): void { this.doAction(this.tunnelService.stopTunnel()); }
  onReload(): void { this.doAction(this.tunnelService.reload()); }
  onClean(): void {
    if (!confirm('Are you sure you want to delete ALL profiles?')) return;
    this.doAction(this.tunnelService.clean());
  }
  onReconnectProfile(): void {
    const name = this.status()?.activeProfile;
    if (name) this.doAction(this.tunnelService.reconnect({ profileName: name }));
  }
  onReconnectPort(name: string): void {
    this.doAction(this.tunnelService.reconnect({ name }));
  }
  onRemovePort(name: string): void {
    this.doAction(this.tunnelService.removePort(name));
  }
  onRemoveProfile(name: string): void {
    if (!confirm(`Delete profile "${name}"?`)) return;
    this.doAction(this.tunnelService.removeProfile(name));
  }
  onAddPort(): void {
    if (!this.newPort.name || !this.newPort.local || !this.newPort.remote) return;
    this.doAction(this.tunnelService.addPort(this.newPort));
    this.newPort = { name: '', local: 0, remote: 0, remoteHost: '127.0.0.1' };
  }
}
