import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Contact } from '../features/contacts/domain/contact';
import { Patient } from '../features/patients/domain/patient';
import { Catalogs, ContactPage, ContactRevision, CorrectContact, CreateContact, CreateFollowUp, CreatePatient, FollowUp, Page, WorkspacePort } from '../application/workspace.port';

@Injectable({ providedIn: 'root' })
export class ApiService implements WorkspacePort {
  readonly actorId = signal('11111111-1111-4111-8111-111111111111');
  private readonly baseUrl = '/api/v1';

  constructor(private readonly http: HttpClient) {}

  setActor(id: string): void { this.actorId.set(id); }
  getCatalogs(): Promise<Catalogs> { return firstValueFrom(this.http.get<Catalogs>(`${this.baseUrl}/catalogs`, { headers: this.headers() })); }
  getPatients(): Promise<Page<Patient>> { return firstValueFrom(this.http.get<Page<Patient>>(`${this.baseUrl}/patients`, { headers: this.headers() })); }
  getPatient(id: string): Promise<Patient> { return firstValueFrom(this.http.get<Patient>(`${this.baseUrl}/patients/${id}`, { headers: this.headers() })); }
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
