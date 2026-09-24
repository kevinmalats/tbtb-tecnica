export type ContactChannel = 'LLAMADA' | 'WHATSAPP' | 'CORREO';
export type ContactResult = 'CONTACTADO' | 'SIN_RESPUESTA' | 'FALLIDO';

export interface Contact {
  id: string;
  patientId: string;
  patientName: string;
  managerId: string;
  managerName: string;
  cityId: number;
  cityName: string;
  occurredAt: string;
  channel: ContactChannel;
  result: ContactResult;
  revision: number;
  version: string;
}
