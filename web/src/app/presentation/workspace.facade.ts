import { Inject, Injectable } from '@angular/core';
import {
  Catalogs,
  ContactPage,
  ContactRevision,
  CorrectContact,
  CreateContact,
  CreateFollowUp,
  CreatePatient,
  FollowUp,
  Page,
  WorkspacePort,
  WORKSPACE_PORT
} from '../application/workspace.port';
import { Contact } from '../features/contacts/domain/contact';
import { Patient } from '../features/patients/domain/patient';

export type { Catalogs, ContactRevision, CreateContact, CreateFollowUp, CreatePatient, FollowUp };

@Injectable({ providedIn: 'root' })
export class WorkspaceFacade {
  constructor(@Inject(WORKSPACE_PORT) private readonly api: WorkspacePort) {}

  setActor(id: string): void { this.api.setActor(id); }
  getCatalogs(): Promise<Catalogs> { return this.api.getCatalogs(); }
  getPatients(): Promise<Page<Patient>> { return this.api.getPatients(); }
  getPatient(id: string): Promise<Patient> { return this.api.getPatient(id); }
  createPatient(value: CreatePatient): Promise<Patient> { return this.api.createPatient(value); }
  getContacts(month: string, managerId = '', cityId = 0): Promise<ContactPage> { return this.api.getContacts(month, managerId, cityId); }
  createContact(value: CreateContact): Promise<Contact> { return this.api.createContact(value); }
  getContact(id: string): Promise<Contact> { return this.api.getContact(id); }
  getContactHistory(id: string): Promise<ContactRevision[]> { return this.api.getContactHistory(id); }
  correctContact(id: string, value: CorrectContact): Promise<Contact> { return this.api.correctContact(id, value); }
  getFollowUps(patientId: string): Promise<FollowUp[]> { return this.api.getFollowUps(patientId); }
  createFollowUp(value: CreateFollowUp): Promise<FollowUp> { return this.api.createFollowUp(value); }
  message(error: unknown): string { return this.api.message(error); }
}
