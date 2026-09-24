import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  LucideActivity, LucideCalendarClock, LucideChevronRight, LucideCircleAlert,
  LucideCircleCheck, LucideContactRound, LucideHeartPulse, LucideLayoutDashboard,
  LucideHistory, LucideMenu, LucidePhoneCall, LucidePlus, LucideRefreshCw, LucideSearch, LucideUsers
} from '@lucide/angular';
import { Catalogs, ContactRevision, CreateContact, CreateFollowUp, CreatePatient, FollowUp, WorkspaceFacade } from './presentation/workspace.facade';
import { Contact } from './features/contacts/domain/contact';
import { Patient } from './features/patients/domain/patient';

type View = 'dashboard' | 'patients' | 'agenda' | 'contacts';

@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LucideActivity, LucideCalendarClock, LucideChevronRight, LucideCircleAlert,
    LucideCircleCheck, LucideContactRound, LucideHeartPulse, LucideLayoutDashboard,
    LucideHistory, LucideMenu, LucidePhoneCall, LucidePlus, LucideRefreshCw, LucideSearch, LucideUsers
  ],
  templateUrl: './app.component.html'
})
export class WorkspaceComponent implements OnInit {
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
  readonly conflict = signal(false);
  readonly catalogs = signal<Catalogs | null>(null);
  readonly patients = signal<Patient[]>([]);
  readonly contacts = signal<Contact[]>([]);
  readonly selectedPatient = signal<Patient | null>(null);
  readonly followUps = signal<FollowUp[]>([]);
  readonly upcomingCount = signal(0);
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
  readonly contactedCount = computed(() => this.contacts().filter(item => item.result === 'CONTACTADO').length);

  readonly patientForm;
  readonly contactForm;
  readonly followUpForm;
  readonly correctionForm;

  constructor(readonly api: WorkspaceFacade, private readonly router: Router, private readonly formBuilder: FormBuilder) {
    this.patientForm = this.formBuilder.nonNullable.group({
      fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
      documentCountryCode: ['', Validators.required],
      documentTypeCode: ['', Validators.required],
      documentNumber: ['', [Validators.required, Validators.maxLength(50)]],
      phone: ['', [Validators.required, Validators.maxLength(30)]],
      email: ['', [Validators.email, Validators.maxLength(254)]],
      cityId: [0, [Validators.required, Validators.min(1)]],
      treatmentStartDate: [new Date().toISOString().slice(0, 10), Validators.required]
    });
    this.contactForm = this.formBuilder.nonNullable.group({
      patientId: ['', Validators.required],
      occurredAt: [this.localDateTime(), Validators.required],
      channel: ['', Validators.required],
      result: ['', Validators.required]
    });
    this.followUpForm = this.formBuilder.nonNullable.group({
      patientId: ['', Validators.required],
      scheduledAt: [this.localDateTime(86_400_000), Validators.required]
    });
    this.correctionForm = this.formBuilder.nonNullable.group({
      occurredAt: ['', Validators.required],
      channel: ['', Validators.required],
      result: ['', Validators.required],
      reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]]
    });
  }

  async ngOnInit(): Promise<void> {
    const segment = this.router.url.split('/')[1];
    if (segment === 'patients' || segment === 'agenda' || segment === 'contacts') this.activeView.set(segment);
    await this.load();
    const contactId = this.router.url.split('/')[2];
    if (segment === 'contacts' && contactId) {
      const contact = this.contacts().find(item => item.id === contactId) ?? await this.api.getContact(contactId);
      await this.openContact(contact, false);
    }
    if (segment === 'patients' && contactId && contactId !== 'new') {
      const patient = this.patients().find(item => item.id === contactId) ?? await this.api.getPatient(contactId);
      this.openPatient(patient, false);
    }
  }

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
      const agendas = await Promise.all(patients.items.map(patient => this.api.getFollowUps(patient.id)));
      this.upcomingCount.set(agendas.flat().filter(item => new Date(item.scheduledAt).getTime() > Date.now()).length);
      this.applyCatalogDefaults(catalogs);
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally {
      this.loading.set(false);
    }
  }

  changeView(view: View): void {
    this.activeView.set(view); this.sidebarOpen.set(false); this.clearNotice();
    void this.router.navigateByUrl(view === 'dashboard' ? '/' : `/${view}`);
  }

  async changeActor(actorId: string): Promise<void> {
    const actor = this.actors.find(item => item.id === actorId) ?? this.actors[0];
    this.selectedActor.set(actor);
    this.api.setActor(actor.id);
    await this.load();
  }

  async submitPatient(): Promise<void> {
    if (this.patientForm.invalid) { this.patientForm.markAllAsTouched(); return; }
    this.saving.set(true);
    this.clearNotice();
    try {
      const value = this.patientForm.getRawValue();
      await this.api.createPatient({ ...value, email: value.email || null });
      this.success.set('Paciente registrado correctamente.');
      this.patientForm.reset({ fullName: '', documentCountryCode: '', documentTypeCode: '', documentNumber: '', phone: '', email: '', cityId: 0, treatmentStartDate: new Date().toISOString().slice(0, 10) });
      await this.loadPatients();
      const catalogs = this.catalogs(); if (catalogs) this.applyCatalogDefaults(catalogs);
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally { this.saving.set(false); }
  }

  async submitContact(): Promise<void> {
    if (this.contactForm.invalid) { this.contactForm.markAllAsTouched(); return; }
    this.saving.set(true);
    this.clearNotice();
    try {
      const value = this.contactForm.getRawValue();
      const payload: CreateContact = { ...value, occurredAt: new Date(value.occurredAt).toISOString() };
      await this.api.createContact(payload);
      this.success.set('Contacto registrado correctamente.');
      this.contactForm.reset({ patientId: this.patients()[0]?.id ?? '', occurredAt: this.localDateTime(), channel: this.catalogs()?.channels[0]?.code ?? '', result: this.catalogs()?.results[0]?.code ?? '' });
      await this.loadContacts();
    } catch (error: unknown) {
      this.error.set(this.api.message(error));
    } finally { this.saving.set(false); }
  }

  async changeMonth(value: string): Promise<void> { this.month.set(value); await this.loadContacts(); }
  async applyContactFilters(): Promise<void> { await this.loadContacts(); }
  async loadAgenda(patientId = this.followUpForm.controls.patientId.value): Promise<void> {
    this.followUpForm.controls.patientId.setValue(patientId);
    this.followUps.set(patientId ? await this.api.getFollowUps(patientId) : []);
  }
  async submitFollowUp(): Promise<void> {
    if (this.followUpForm.invalid) { this.followUpForm.markAllAsTouched(); return; } this.saving.set(true); this.clearNotice();
    try { const value = this.followUpForm.getRawValue(); await this.api.createFollowUp({ ...value, scheduledAt: new Date(value.scheduledAt).toISOString() }); this.success.set('Seguimiento agendado correctamente.'); await this.loadAgenda(); }
    catch (error) { this.error.set(this.api.message(error)); } finally { this.saving.set(false); }
  }
  async openContact(contact: Contact, updateUrl = true): Promise<void> {
    if (updateUrl) window.history.pushState({}, '', `/contacts/${contact.id}`);
    this.selectedContact.set(await this.api.getContact(contact.id));
    this.contactHistory.set(await this.api.getContactHistory(contact.id));
    const current = this.selectedContact()!;
    const date = new Date(current.occurredAt); date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    this.correctionForm.setValue({ occurredAt: date.toISOString().slice(0, 16), channel: current.channel, result: current.result, reason: '' });
  }
  closeContact(): void { this.selectedContact.set(null); this.contactHistory.set([]); window.history.pushState({}, '', '/contacts'); }
  async submitCorrection(): Promise<void> {
    const contact = this.selectedContact(); if (!contact || this.correctionForm.invalid) { this.correctionForm.markAllAsTouched(); return; } this.saving.set(true); this.clearNotice();
    try { const value = this.correctionForm.getRawValue(); const updated = await this.api.correctContact(contact.id, { ...value, occurredAt: new Date(value.occurredAt).toISOString(), expectedVersion: contact.version }); this.success.set('Contacto corregido; el original permanece en el historial.'); await this.loadContacts(); await this.openContact(updated); }
    catch (error) { this.error.set(this.api.message(error)); if ((error as { status?: number }).status === 409) this.conflict.set(true); }
    finally { this.saving.set(false); }
  }
  async reloadSelectedContact(): Promise<void> {
    const id = this.selectedContact()?.id;
    if (!id) return;
    const current = await this.api.getContact(id);
    await this.openContact(current, false);
    this.conflict.set(false);
    this.error.set('');
  }
  patientName(id: string): string { return this.patients().find(item => item.id === id)?.fullName ?? 'Paciente'; }
  openPatient(patient: Patient, updateUrl = true): void {
    this.selectedPatient.set(patient);
    if (updateUrl) window.history.pushState({}, '', `/patients/${patient.id}`);
  }
  closePatient(): void { this.selectedPatient.set(null); window.history.pushState({}, '', '/patients'); }
  schedulePatient(patient: Patient): void { this.followUpForm.controls.patientId.setValue(patient.id); this.closePatient(); this.changeView('agenda'); void this.loadAgenda(patient.id); }
  contactPatient(patient: Patient): void { this.contactForm.controls.patientId.setValue(patient.id); this.closePatient(); this.changeView('contacts'); }
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
  private clearNotice(): void { this.error.set(''); this.success.set(''); this.conflict.set(false); }
  private applyCatalogDefaults(catalogs: Catalogs): void {
    if (!this.patientForm.controls.documentCountryCode.value) this.patientForm.controls.documentCountryCode.setValue(catalogs.countries[0]?.code ?? '');
    if (!this.patientForm.controls.documentTypeCode.value) this.patientForm.controls.documentTypeCode.setValue(catalogs.documentTypes[0]?.code ?? '');
    if (!this.patientForm.controls.cityId.value) this.patientForm.controls.cityId.setValue(catalogs.cities[0]?.id ?? 0);
    if (!this.contactForm.controls.patientId.value) this.contactForm.controls.patientId.setValue(this.patients()[0]?.id ?? '');
    if (!this.contactForm.controls.channel.value) this.contactForm.controls.channel.setValue(catalogs.channels[0]?.code ?? '');
    if (!this.contactForm.controls.result.value) this.contactForm.controls.result.setValue(catalogs.results[0]?.code ?? '');
    if (!this.followUpForm.controls.patientId.value) this.followUpForm.controls.patientId.setValue(this.patients()[0]?.id ?? '');
  }
  private localDateTime(offsetMs = 0): string {
    const date = new Date(Date.now() + offsetMs); date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    return date.toISOString().slice(0, 16);
  }
}
