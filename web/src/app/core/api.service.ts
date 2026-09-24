import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Contact } from '../features/contacts/domain/contact';
import { Patient } from '../features/patients/domain/patient';

export interface Option { code: string; name: string; }
export interface City { id: number; countryCode: string; name: string; }
export interface Manager { id: string; displayName: string; }
export interface Catalogs {
  countries: Option[];
  cities: City[];
  documentTypes: Option[];
  channels: Option[];
  results: Option[];
  managers: Manager[];
}
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number; }
export interface ContactPage extends Page<Contact> { month: string; timeZone: string; }
export interface CreatePatient {
  fullName: string;
  documentCountryCode: string;
  documentTypeCode: string;
  documentNumber: string;
  phone: string;
  email: string | null;
  cityId: number;
  treatmentStartDate: string;
}
export interface CreateContact { patientId: string; occurredAt: string; channel: string; result: string; }
export interface FollowUp { id: string; patientId: string; managerId: string; scheduledAt: string; }
export interface CreateFollowUp { patientId: string; scheduledAt: string; }
export interface ContactRevision { contactId: string; revision: number; occurredAt: string; channel: string; result: string; reason: string | null; recordedAt: string; recordedBy: string; recordedByName: string; }
export interface CorrectContact { occurredAt: string; channel: string; result: string; reason: string; expectedVersion: string; }

@Injectable({ providedIn: 'root' })
export class ApiService {
  readonly actorId = signal('11111111-1111-4111-8111-111111111111');
  private readonly baseUrl = '/api/v1';

  constructor(private readonly http: HttpClient) {}

  setActor(id: string): void { this.actorId.set(id); }
  getCatalogs(): Promise<Catalogs> { return firstValueFrom(this.http.get<Catalogs>(`${this.baseUrl}/catalogs`, { headers: this.headers() })); }
  getPatients(): Promise<Page<Patient>> { return firstValueFrom(this.http.get<Page<Patient>>(`${this.baseUrl}/patients`, { headers: this.headers() })); }
  createPatient(value: CreatePatient): Promise<Patient> { return firstValueFrom(this.http.post<Patient>(`${this.baseUrl}/patients`, value, { headers: this.headers() })); }
  getContacts(month: string, managerId = '', cityId = 0): Promise<ContactPage> {
    let params = new HttpParams().set('month', month);
    if (managerId) params = params.set('managerId', managerId);
    if (cityId) params = params.set('cityId', cityId);
    return firstValueFrom(this.http.get<ContactPage>(`${this.baseUrl}/contacts`, { headers: this.headers(), params }));
  }
  createContact(value: CreateContact): Promise<Contact> { return firstValueFrom(this.http.post<Contact>(`${this.baseUrl}/contacts`, value, { headers: this.headers() })); }
  getContact(id: string): Promise<Contact> { return firstValueFrom(this.http.get<Contact>(`${this.baseUrl}/contacts/${id}`, { headers: this.headers() })); }
  getContactHistory(id: string): Promise<ContactRevision[]> { return firstValueFrom(this.http.get<ContactRevision[]>(`${this.baseUrl}/contacts/${id}/history`, { headers: this.headers() })); }
  correctContact(id: string, value: CorrectContact): Promise<Contact> { return firstValueFrom(this.http.post<Contact>(`${this.baseUrl}/contacts/${id}/corrections`, value, { headers: this.headers() })); }
  getFollowUps(patientId: string): Promise<FollowUp[]> { return firstValueFrom(this.http.get<FollowUp[]>(`${this.baseUrl}/follow-ups`, { headers: this.headers(), params: new HttpParams().set('patientId', patientId) })); }
  createFollowUp(value: CreateFollowUp): Promise<FollowUp> { return firstValueFrom(this.http.post<FollowUp>(`${this.baseUrl}/follow-ups`, value, { headers: this.headers() })); }

  message(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.title ?? 'No fue posible completar la solicitud.';
    return 'Ocurrio un error inesperado.';
  }

  private headers(): HttpHeaders { return new HttpHeaders({ 'X-Demo-Actor-Id': this.actorId() }); }
}
