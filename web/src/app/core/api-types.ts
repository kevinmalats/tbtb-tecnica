export type Role = 'GESTOR' | 'COORDINADORA';

export interface DemoActor {
  id: string;
  displayName: string;
  role: Role;
}

export interface Problem {
  type: string;
  title: string;
  status: number;
  code: string;
  traceId: string;
  errors?: Record<string, string[]>;
}
