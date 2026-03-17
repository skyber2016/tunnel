export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
}

export interface PortMapping {
  name: string;
  local: number;
  remote: number;
  remoteHost: string;
}

export interface JumpHostConfig {
  host: string;
  user: string;
  port: number;
  keyPath: string;
}

export interface Profile {
  name: string;
  jumpHost: JumpHostConfig;
  ports: PortMapping[];
}

export interface ProfilesConfig {
  profiles: Profile[];
  activeProfile?: string;
}

export interface PortStatus {
  name: string;
  profile: string;
  localPort: number;
  remotePort: number;
  remoteHost: string;
  isStarted: boolean;
}

export interface TunnelStatus {
  isConnected: boolean;
  activeProfile: string;
  jumpHost: string;
  ports: PortStatus[];
}

export interface AddPortRequest {
  name: string;
  local: number;
  remote: number;
  remoteHost: string;
}

export interface ReconnectRequest {
  profileName?: string;
  name?: string;
}
