import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import {
  LucideActivity, LucideCalendarClock, LucideChevronRight, LucideCircleAlert,
  LucideCircleCheck, LucideContactRound, LucideHeartPulse, LucideLayoutDashboard,
  LucideHistory, LucideMenu, LucidePhoneCall, LucidePlus, LucideRefreshCw, LucideSearch, LucideUsers
} from '@lucide/angular';
import { ApiService, Catalogs, ContactRevision, CreateContact, CreateFollowUp, CreatePatient, FollowUp } from './core/api.service';
import { Contact } from './features/contacts/domain/contact';
import { Patient } from './features/patients/domain/patient';

type View = 'dashboard' | 'patients' | 'agenda' | 'contacts';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideActivity, LucideCalendarClock, LucideChevronRight, LucideCircleAlert,
    LucideCircleCheck, LucideContactRound, LucideHeartPulse, LucideLayoutDashboard,
    LucideHistory, LucideMenu, LucidePhoneCall, LucidePlus, LucideRefreshCw, LucideSearch, LucideUsers
  ],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  readonly actors = [
    { id: '11111111-1111-4111-8111-111111111111', name: 'Gestor A', role: 'Gestor' },
    { id: '22222222-2222-4222-8222-222222222222', name: 'Gestor B', role: 'Gestor' },
    { id: '33333333-3333-4333-8333-333333333333', name: 'Coordinadora', role: 'Coordinacion' }
  ];
  readonly activeView = signal<View>('dashboard');
  readonly sidebarOpen = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly catalogs = signal<Catalogs | null>(null);
  readonly patients = signal<Patient[]>([]);
  readonly contacts = signal<Contact[]>([]);
  readonly followUps = signal<FollowUp[]>([]);
  readonly contactHistory = signal<ContactRevision[]>([]);
  readonly selectedContact = signal<Contact | null>(null);
  readonly managerFilter = signal('');
  readonly cityFilter = signal(0);
  readonly search = signal('');
  readonly selectedActor = signal(this.actors[0]);
  readonly month = signal(new Date().toISOString().slice(0, 7));
  readonly filteredPatients = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('es');
    if (!term) return this.patients();
    return this.patients().filter(patient =>
      `${patient.fullName} ${patient.documentNumber} ${patient.cityName}`.toLocaleLowerCase('es').includes(term));
  });
  readonly contactRate = computed(() => {
    const items = this.contacts();
    return items.length ? Math.round(items.filter(item => item.result === 'CONTACTADO').length * 100 / items.length) : 0;
  });

  patientForm: CreatePatient = this.emptyPatient();
  contactForm: CreateContact = this.emptyContact();
  followUpForm: CreateFollowUp = this.emptyFollowUp();
  correctionForm = { occurredAt: '', channel: '', result: '', reason: '' };

  constructor(readonly api: ApiService) {}

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const [catalogs, patients, contacts] = await Promise.all([
        this.api.getCatalogs(), this.api.getPatients(), this.api.getContacts(this.month())
      ]);
      this.catalogs.set(catalogs);
      this.patients.set(patients.items);
      this.contacts.set(contacts.items);
      this.applyCatalogDefaults(catalogs);
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally {
      this.loading.set(false);
    }
  }

  changeView(view: View): void { this.activeView.set(view); this.sidebarOpen.set(false); this.clearNotice(); }

  async changeActor(actorId: string): Promise<void> {
    const actor = this.actors.find(item => item.id === actorId) ?? this.actors[0];
    this.selectedActor.set(actor);
    this.api.setActor(actor.id);
    await this.load();
  }

  async submitPatient(form: NgForm): Promise<void> {
    if (form.invalid) return;
    this.saving.set(true);
    this.clearNotice();
    try {
      await this.api.createPatient({ ...this.patientForm, email: this.patientForm.email || null });
      this.success.set('Paciente registrado correctamente.');
      this.patientForm = this.emptyPatient();
      form.resetForm(this.patientForm);
      await this.loadPatients();
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally { this.saving.set(false); }
  }

  async submitContact(form: NgForm): Promise<void> {
    if (form.invalid) return;
    this.saving.set(true);
    this.clearNotice();
    try {
      const payload = { ...this.contactForm, occurredAt: new Date(this.contactForm.occurredAt).toISOString() };
      await this.api.createContact(payload);
      this.success.set('Contacto registrado correctamente.');
      this.contactForm = this.emptyContact();
      form.resetForm(this.contactForm);
      await this.loadContacts();
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally { this.saving.set(false); }
  }

  async changeMonth(value: string): Promise<void> { this.month.set(value); await this.loadContacts(); }
  async applyContactFilters(): Promise<void> { await this.loadContacts(); }
  async loadAgenda(patientId = this.followUpForm.patientId): Promise<void> {
    this.followUpForm.patientId = patientId;
    this.followUps.set(patientId ? await this.api.getFollowUps(patientId) : []);
  }
  async submitFollowUp(form: NgForm): Promise<void> {
    if (form.invalid) return; this.saving.set(true); this.clearNotice();
    try { await this.api.createFollowUp({ ...this.followUpForm, scheduledAt: new Date(this.followUpForm.scheduledAt).toISOString() }); this.success.set('Seguimiento agendado correctamente.'); await this.loadAgenda(); }
    catch (error) { this.error.set(this.api.message(error)); } finally { this.saving.set(false); }
  }
  async openContact(contact: Contact): Promise<void> {
    this.selectedContact.set(await this.api.getContact(contact.id));
    this.contactHistory.set(await this.api.getContactHistory(contact.id));
    const current = this.selectedContact()!;
    const date = new Date(current.occurredAt); date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    this.correctionForm = { occurredAt: date.toISOString().slice(0, 16), channel: current.channel, result: current.result, reason: '' };
  }
  closeContact(): void { this.selectedContact.set(null); this.contactHistory.set([]); }
  async submitCorrection(form: NgForm): Promise<void> {
    const contact = this.selectedContact(); if (!contact || form.invalid) return; this.saving.set(true); this.clearNotice();
    try { const updated = await this.api.correctContact(contact.id, { ...this.correctionForm, occurredAt: new Date(this.correctionForm.occurredAt).toISOString(), expectedVersion: contact.version }); this.success.set('Contacto corregido; el original permanece en el historial.'); await this.loadContacts(); await this.openContact(updated); }
    catch (error) { this.error.set(this.api.message(error)); if ((error as { status?: number }).status === 409) this.success.set('Recarga el contacto antes de intentar una nueva correccion.'); }
    finally { this.saving.set(false); }
  }
  patientName(id: string): string { return this.patients().find(item => item.id === id)?.fullName ?? 'Paciente'; }
  documentTypeName(code: string): string {
    return this.catalogs()?.documentTypes.find(item => item.code === code)?.name ?? {
      NATIONAL_ID: 'DNI',
      FOREIGN_ID: 'Documento extranjero',
      PASSPORT: 'Pasaporte'
    }[code] ?? code;
  }
  resultClass(result: string): string {
    return result === 'CONTACTADO' ? 'bg-emerald-50 text-emerald-700' : result === 'FALLIDO' ? 'bg-rose-50 text-rose-700' : 'bg-amber-50 text-amber-700';
  }
  initials(name: string): string { return name.split(' ').slice(0, 2).map(word => word[0]).join('').toUpperCase(); }
  formatDate(value: string): string { return new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium' }).format(new Date(`${value}T12:00:00`)); }
  formatDateTime(value: string): string { return new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }

  private async loadPatients(): Promise<void> { this.patients.set((await this.api.getPatients()).items); }
  private async loadContacts(): Promise<void> { this.contacts.set((await this.api.getContacts(this.month(), this.managerFilter(), this.cityFilter())).items); }
  private clearNotice(): void { this.error.set(''); this.success.set(''); }
  private applyCatalogDefaults(catalogs: Catalogs): void {
    this.patientForm.documentCountryCode ||= catalogs.countries[0]?.code ?? '';
    this.patientForm.documentTypeCode ||= catalogs.documentTypes[0]?.code ?? '';
    this.patientForm.cityId ||= catalogs.cities[0]?.id ?? 0;
    this.contactForm.patientId ||= this.patients()[0]?.id ?? '';
    this.contactForm.channel ||= catalogs.channels[0]?.code ?? '';
    this.contactForm.result ||= catalogs.results[0]?.code ?? '';
    this.followUpForm.patientId ||= this.patients()[0]?.id ?? '';
  }
  private emptyPatient(): CreatePatient {
    return { fullName: '', documentCountryCode: '', documentTypeCode: '', documentNumber: '', phone: '', email: null, cityId: 0, treatmentStartDate: new Date().toISOString().slice(0, 10) };
  }
  private emptyContact(): CreateContact {
    const local = new Date(Date.now() - new Date().getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
    return { patientId: '', occurredAt: local, channel: '', result: '' };
  }
  private emptyFollowUp(): CreateFollowUp {
    const date = new Date(Date.now() + 86_400_000); date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    return { patientId: '', scheduledAt: date.toISOString().slice(0, 16) };
  }
}
