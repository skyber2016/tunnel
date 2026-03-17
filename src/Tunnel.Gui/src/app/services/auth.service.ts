import { Injectable, signal } from '@angular/core';
import { invoke } from '@tauri-apps/api/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly token = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  private retryTimer: ReturnType<typeof setInterval> | null = null;

  async loadToken(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const token = await invoke<string>('get_auth_token');
      this.token.set(token);
      this.error.set(null);
      this.stopRetry();
    } catch (e) {
      this.token.set(null);
      this.error.set(String(e));
      this.startRetry();
    } finally {
      this.loading.set(false);
    }
  }

  private startRetry(): void {
    if (this.retryTimer) return;
    this.retryTimer = setInterval(() => this.loadToken(), 5000);
  }

  private stopRetry(): void {
    if (this.retryTimer) {
      clearInterval(this.retryTimer);
      this.retryTimer = null;
    }
  }
}
