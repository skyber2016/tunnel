import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiResponse,
  TunnelStatus,
  ProfilesConfig,
  Profile,
  AddPortRequest,
  ReconnectRequest,
} from '../models/tunnel.model';

const BASE = 'http://localhost:6385/api';

@Injectable({ providedIn: 'root' })
export class TunnelService {
  private http = inject(HttpClient);

  getStatus(): Observable<ApiResponse<TunnelStatus>> {
    return this.http.get<ApiResponse<TunnelStatus>>(`${BASE}/status`);
  }

  getProfiles(): Observable<ApiResponse<ProfilesConfig>> {
    return this.http.get<ApiResponse<ProfilesConfig>>(`${BASE}/profiles`);
  }

  startProfile(profile: Profile): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/start`, profile);
  }

  stopTunnel(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/stop`, {});
  }

  reload(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/reload`, {});
  }

  reconnect(req: ReconnectRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/reconnect`, req);
  }

  addPort(req: AddPortRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/ports/add`, req);
  }

  removePort(name: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/remove/port`, { name });
  }

  removeProfile(profileName: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/remove/profile`, { profileName });
  }

  clean(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${BASE}/clean`, {});
  }
}
