import { InjectionToken } from '@angular/core';
import { Contact } from '../features/contacts/domain/contact';
import { Patient } from '../features/patients/domain/patient';

export interface Option { code: string; name: string; }
export interface City { id: number; countryCode: string; name: string; }
export interface Manager { id: string; displayName: string; }
export interface Catalogs { countries: Option[]; cities: City[]; documentTypes: Option[]; channels: Option[]; results: Option[]; managers: Manager[]; }
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number; }
export interface ContactPage extends Page<Contact> { month: string; timeZone: string; }
export interface CreatePatient { fullName: string; documentCountryCode: string; documentTypeCode: string; documentNumber: string; phone: string; email: string | null; cityId: number; treatmentStartDate: string; }
export interface CreateContact { patientId: string; occurredAt: string; channel: string; result: string; }
export interface FollowUp { id: string; patientId: string; managerId: string; scheduledAt: string; }
export interface CreateFollowUp { patientId: string; scheduledAt: string; }
export interface ContactRevision { contactId: string; revision: number; occurredAt: string; channel: string; result: string; reason: string | null; recordedAt: string; recordedBy: string; recordedByName: string; }
export interface CorrectContact { occurredAt: string; channel: string; result: string; reason: string; expectedVersion: string; }

export interface WorkspacePort {
  setActor(id: string): void;
  getCatalogs(): Promise<Catalogs>;
  getPatients(): Promise<Page<Patient>>;
  getPatient(id: string): Promise<Patient>;
  createPatient(value: CreatePatient): Promise<Patient>;
  getContacts(month: string, managerId?: string, cityId?: number): Promise<ContactPage>;
  createContact(value: CreateContact): Promise<Contact>;
  getContact(id: string): Promise<Contact>;
  getContactHistory(id: string): Promise<ContactRevision[]>;
  correctContact(id: string, value: CorrectContact): Promise<Contact>;
  getFollowUps(patientId: string): Promise<FollowUp[]>;
  createFollowUp(value: CreateFollowUp): Promise<FollowUp>;
  message(error: unknown): string;
}

export const WORKSPACE_PORT = new InjectionToken<WorkspacePort>('WORKSPACE_PORT');
