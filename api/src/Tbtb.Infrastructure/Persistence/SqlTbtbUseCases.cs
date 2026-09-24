using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tbtb.Application.Abstractions;
using Tbtb.Application.UseCases;
using Tbtb.Application.Validation;
using Tbtb.Domain;

namespace Tbtb.Infrastructure.Persistence;

public sealed class SqlTbtbUseCases(TbtbDbContext db, IClock clock) : IActorRepository, ICatalogRepository, IPatientRepository, IFollowUpRepository, IContactRepository, IMonthlyContactQuery, IHealthProbe
{
    public async Task<ActorContext?> FindActor(Guid id, CancellationToken ct = default) =>
        await db.Actors.AsNoTracking().Where(x => x.Id == id).Select(x => new ActorContext(x.Id, x.DisplayName, x.Role)).SingleOrDefaultAsync(ct);

    public async Task<UseCaseResult> GetCatalogs(ActorContext actor, CancellationToken ct = default)
    {
        var managers = db.Actors.AsNoTracking().Where(x => x.Role == "GESTOR");
        if (actor.Role == "GESTOR") managers = managers.Where(x => x.Id == actor.Id);
        return UseCaseResult.Ok(new
        {
            countries = await db.Countries.AsNoTracking().Select(x => new { x.Code, x.Name }).ToArrayAsync(ct),
            cities = await db.Cities.AsNoTracking().Select(x => new { x.Id, x.CountryCode, x.Name }).ToArrayAsync(ct),
            documentTypes = await db.DocumentTypes.AsNoTracking().Select(x => new { x.Code, x.Name }).ToArrayAsync(ct),
            channels = Options(ContactRules.Channels),
            results = Options(ContactRules.Results),
            managers = await managers.Select(x => new { x.Id, x.DisplayName }).ToArrayAsync(ct)
        });
    }

    public async Task<UseCaseResult> CreatePatient(ActorContext actor, PatientCommand r, CancellationToken ct = default)
    {
        if (actor.Role != "GESTOR") return Forbidden();
        var errors = PatientErrors(r);
        if (!await db.Countries.AnyAsync(x => x.Code == r.DocumentCountryCode, ct)) errors["documentCountryCode"] = ["El pais no existe."];
        if (!await db.DocumentTypes.AnyAsync(x => x.Code == r.DocumentTypeCode, ct)) errors["documentTypeCode"] = ["El tipo no existe."];
        var city = await db.Cities.FindAsync([r.CityId], ct); if (city is null) errors["cityId"] = ["La ciudad no existe."];
        if (errors.Count > 0) return UseCaseResult.Invalid(errors);
        var normalized = r.DocumentNumber.Trim().ToUpperInvariant();
        if (await db.Patients.AnyAsync(x => x.DocumentCountryCode == r.DocumentCountryCode && x.DocumentTypeCode == r.DocumentTypeCode && x.NormalizedDocumentNumber == normalized, ct)) return DuplicatePatient();
        var patient = new Patient { Id = Guid.NewGuid(), FullName = r.FullName.Trim(), DocumentCountryCode = r.DocumentCountryCode, DocumentTypeCode = r.DocumentTypeCode, DocumentNumber = r.DocumentNumber.Trim(), NormalizedDocumentNumber = normalized, Phone = r.Phone.Trim(), Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim(), CityId = r.CityId, TreatmentStartDate = DateOnly.ParseExact(r.TreatmentStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), AssignedManagerId = actor.Id, CreatedAtUtc = clock.UtcNow, CreatedBy = actor.Id };
        db.Patients.Add(patient);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return DuplicatePatient(); }
        return UseCaseResult.Created(PatientDto(patient, city!.Name), $"/api/v1/patients/{patient.Id}");
    }

    public async Task<UseCaseResult> GetPatients(ActorContext actor, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return UseCaseResult.Invalid(new() { ["page"] = ["Paginacion invalida."] });
        var query = db.Patients.AsNoTracking().AsQueryable(); if (actor.Role == "GESTOR") query = query.Where(x => x.AssignedManagerId == actor.Id);
        var total = await query.CountAsync(ct); var rows = await query.Include(x => x.City).OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return UseCaseResult.Ok(new { items = rows.Select(x => PatientDto(x, x.City!.Name)), total, page, pageSize });
    }

    public async Task<UseCaseResult> GetPatient(ActorContext actor, Guid id, CancellationToken ct = default)
    {
        var patient = await db.Patients.Include(x => x.City).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (actor.Role == "COORDINADORA" || x.AssignedManagerId == actor.Id), ct);
        return patient is null ? NotFound() : UseCaseResult.Ok(PatientDto(patient, patient.City!.Name));
    }

    public async Task<UseCaseResult> CreateFollowUp(ActorContext actor, FollowUpCommand r, CancellationToken ct = default)
    {
        if (actor.Role != "GESTOR") return Forbidden();
        var errors = new Dictionary<string, string[]>(); if (!Guid.TryParse(r.PatientId, out var patientId)) errors["patientId"] = ["El paciente no es valido."];
        if (!ContactRules.TryInstant(r.ScheduledAt, out var at) || at.UtcDateTime <= clock.UtcNow) errors["scheduledAt"] = ["La fecha debe incluir offset y ser futura."];
        if (errors.Count > 0) return UseCaseResult.Invalid(errors);
        if (!await db.Patients.AnyAsync(x => x.Id == patientId && x.AssignedManagerId == actor.Id, ct)) return NotFound();
        var followUp = new FollowUp { Id = Guid.NewGuid(), PatientId = patientId, ManagerId = actor.Id, ScheduledAtUtc = at.UtcDateTime, CreatedAtUtc = clock.UtcNow, CreatedBy = actor.Id };
        db.FollowUps.Add(followUp); await db.SaveChangesAsync(ct); return UseCaseResult.Created(FollowUpDto(followUp), $"/api/v1/follow-ups/{followUp.Id}");
    }

    public async Task<UseCaseResult> GetFollowUps(ActorContext actor, string patientId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(patientId, out var id)) return UseCaseResult.Invalid(new() { ["patientId"] = ["Paciente invalido."] });
        if (!await db.Patients.AnyAsync(x => x.Id == id && (actor.Role == "COORDINADORA" || x.AssignedManagerId == actor.Id), ct)) return NotFound();
        var rows = await db.FollowUps.AsNoTracking().Where(x => x.PatientId == id).OrderBy(x => x.ScheduledAtUtc).ToArrayAsync(ct); return UseCaseResult.Ok(rows.Select(FollowUpDto));
    }

    public async Task<UseCaseResult> CreateContact(ActorContext actor, ContactCommand r, CancellationToken ct = default)
    {
        if (actor.Role != "GESTOR") return Forbidden(); var errors = ContactErrors(r.PatientId, r.OccurredAt, r.Channel, r.Result);
        if (errors.Count > 0) return UseCaseResult.Invalid(errors); var patientId = Guid.Parse(r.PatientId); ContactRules.TryInstant(r.OccurredAt, out var at);
        var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == patientId && x.AssignedManagerId == actor.Id, ct); if (patient is null) return NotFound();
        var contact = new Contact { Id = Guid.NewGuid(), PatientId = patient.Id, ManagerId = actor.Id, CityAtContactId = patient.CityId, CurrentRevision = 1, CreatedAtUtc = clock.UtcNow, CreatedBy = actor.Id };
        contact.Revisions.Add(new ContactRevision { ContactId = contact.Id, RevisionNumber = 1, OccurredAtUtc = at.UtcDateTime, Channel = r.Channel, Result = r.Result, RecordedAtUtc = clock.UtcNow, RecordedBy = actor.Id });
        db.Add(contact); await db.SaveChangesAsync(ct); return UseCaseResult.Created(await ContactDto(contact.Id, ct), $"/api/v1/contacts/{contact.Id}");
    }

    public async Task<UseCaseResult> GetContacts(ActorContext actor, string? month, string? managerId, int? cityId, int page, int pageSize, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>(); var selectedMonth = month ?? TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.UtcNow, "SA Pacific Standard Time").ToString("yyyy-MM");
        var localStart = DateTime.MinValue; if (!Regex.IsMatch(selectedMonth, @"^\d{4}-(0[1-9]|1[0-2])$") || !DateTime.TryParseExact(selectedMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out localStart) || localStart.Year is < 1 or > 9998) errors["month"] = ["Mes invalido."];
        if (page < 1) errors["page"] = ["La pagina debe iniciar en 1."]; if (pageSize is < 1 or > 100) errors["pageSize"] = ["El tamano de pagina debe estar entre 1 y 100."];
        Guid? manager = null; if (managerId is not null) { if (!Guid.TryParse(managerId, out var parsed) || !await db.Actors.AnyAsync(x => x.Id == parsed && x.Role == "GESTOR", ct)) errors["managerId"] = ["El gestor no existe."]; else manager = parsed; }
        if (cityId.HasValue && !await db.Cities.AnyAsync(x => x.Id == cityId, ct)) errors["cityId"] = ["La ciudad no existe."]; if (errors.Count > 0) return UseCaseResult.Invalid(errors);
        if (actor.Role == "GESTOR" && manager.HasValue && manager != actor.Id) return Forbidden();
        var start = localStart.AddHours(5); var end = localStart.AddMonths(1).AddHours(5);
        var query = db.Contacts.AsNoTracking().Include(x => x.Patient).Include(x => x.Manager).Include(x => x.CityAtContact).Include(x => x.Revisions).Where(x => x.Revisions.Any(r => r.RevisionNumber == x.CurrentRevision && r.OccurredAtUtc >= start && r.OccurredAtUtc < end));
        var effective = actor.Role == "GESTOR" ? actor.Id : manager; if (effective.HasValue) query = query.Where(x => x.ManagerId == effective); if (cityId.HasValue) query = query.Where(x => x.CityAtContactId == cityId);
        var total = await query.CountAsync(ct); var rows = await query.ToArrayAsync(ct); var items = rows.OrderByDescending(x => x.Revisions.Single(r => r.RevisionNumber == x.CurrentRevision).OccurredAtUtc).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(ContactDto);
        return UseCaseResult.Ok(new { items, total, page, pageSize, month = selectedMonth, timeZone = "America/Bogota" });
    }

    public async Task<UseCaseResult> GetContact(ActorContext actor, Guid id, CancellationToken ct = default) { var contact = await ContactQuery(id, actor, ct); return contact is null ? NotFound() : UseCaseResult.Ok(ContactDto(contact)); }

    public async Task<UseCaseResult> CorrectContact(ActorContext actor, Guid id, CorrectionCommand r, CancellationToken ct = default)
    {
        if (actor.Role != "GESTOR") return Forbidden(); var contact = await db.Contacts.Include(x => x.Patient).Include(x => x.Manager).Include(x => x.CityAtContact).Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Id == id && x.ManagerId == actor.Id, ct); if (contact is null) return NotFound();
        var errors = new Dictionary<string, string[]>(); if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length is < 10 or > 500) errors["reason"] = ["El motivo debe tener entre 10 y 500 caracteres."];
        if (!ContactRules.TryInstant(r.OccurredAt, out var at) || at.UtcDateTime > clock.UtcNow) errors["occurredAt"] = ["La fecha debe incluir offset y no puede ser futura."]; if (!ContactRules.IsChannel(r.Channel)) errors["channel"] = ["El canal no es valido."]; if (!ContactRules.IsResult(r.Result)) errors["result"] = ["El resultado no es valido."]; if (!ContactRules.TryVersion(r.ExpectedVersion, out var version)) errors["expectedVersion"] = ["La version esperada no es valida."]; if (errors.Count > 0) return UseCaseResult.Invalid(errors);
        db.Entry(contact).Property(x => x.RowVersion).OriginalValue = version; var current = contact.Revisions.Single(x => x.RevisionNumber == contact.CurrentRevision); if (!contact.RowVersion.SequenceEqual(version)) return Conflict(); if (current.OccurredAtUtc == at.UtcDateTime && current.Channel == r.Channel && current.Result == r.Result) return UseCaseResult.Fail(400, "NO_CHANGES", "La correccion no modifica el contacto.");
        contact.CurrentRevision++; contact.Revisions.Add(new ContactRevision { ContactId = id, RevisionNumber = contact.CurrentRevision, OccurredAtUtc = at.UtcDateTime, Channel = r.Channel, Result = r.Result, CorrectionReason = r.Reason.Trim(), RecordedAtUtc = clock.UtcNow, RecordedBy = actor.Id });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return UseCaseResult.Ok(ContactDto(contact));
    }

    public async Task<UseCaseResult> GetContactHistory(ActorContext actor, Guid id, CancellationToken ct = default)
    {
        if (!await db.Contacts.AnyAsync(x => x.Id == id && (actor.Role == "COORDINADORA" || x.ManagerId == actor.Id), ct)) return NotFound();
        var rows = await db.ContactRevisions.Include(x => x.RecordedByActor).AsNoTracking().Where(x => x.ContactId == id).OrderBy(x => x.RevisionNumber).ToArrayAsync(ct);
        return UseCaseResult.Ok(rows.Select(x => new { contactId = id, revision = x.RevisionNumber, occurredAt = Iso(x.OccurredAtUtc), x.Channel, x.Result, reason = x.CorrectionReason, recordedAt = Iso(x.RecordedAtUtc), x.RecordedBy, recordedByName = x.RecordedByActor!.DisplayName }));
    }

    public async Task<bool> IsReady(CancellationToken ct = default) { try { return await db.Database.CanConnectAsync(ct) && await db.Actors.AsNoTracking().AnyAsync(ct); } catch { return false; } }

    private Dictionary<string, string[]> ContactErrors(string patientId, string occurredAt, string channel, string result) { var errors = new Dictionary<string, string[]>(); if (!Guid.TryParse(patientId, out _)) errors["patientId"] = ["El paciente no es valido."]; if (!ContactRules.IsChannel(channel)) errors["channel"] = ["El canal no es valido."]; if (!ContactRules.IsResult(result)) errors["result"] = ["El resultado no es valido."]; if (!ContactRules.TryInstant(occurredAt, out var at) || at.UtcDateTime > clock.UtcNow) errors["occurredAt"] = ["La fecha debe incluir offset y no puede ser futura."]; return errors; }
    private Dictionary<string, string[]> PatientErrors(PatientCommand r) { var errors = new Dictionary<string, string[]>(); if (string.IsNullOrWhiteSpace(r.FullName) || r.FullName.Trim().Length > 150) errors["fullName"] = ["El nombre es obligatorio y no debe superar 150 caracteres."]; if (string.IsNullOrWhiteSpace(r.DocumentNumber) || r.DocumentNumber.Trim().Length > 40) errors["documentNumber"] = ["El documento es obligatorio y no debe superar 40 caracteres."]; var phone = r.Phone?.Trim() ?? ""; if (phone.Length is < 7 or > 30 || !Regex.IsMatch(phone, @"^(?=(?:\D*\d){7})[0-9 +()\-]+$")) errors["phone"] = ["El telefono debe tener entre 7 y 30 caracteres y al menos siete digitos."]; var email = r.Email?.Trim(); if (!string.IsNullOrEmpty(email) && (email.Length > 254 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))) errors["email"] = ["El correo no es valido."]; if (!DateOnly.TryParseExact(r.TreatmentStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)) errors["treatmentStartDate"] = ["La fecha no es valida."]; else if (start > DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.UtcNow, "SA Pacific Standard Time"))) errors["treatmentStartDate"] = ["La fecha de inicio no puede ser futura."]; return errors; }
    private async Task<Contact?> ContactQuery(Guid id, ActorContext actor, CancellationToken ct) => await db.Contacts.AsNoTracking().Include(x => x.Patient).Include(x => x.Manager).Include(x => x.CityAtContact).Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Id == id && (actor.Role == "COORDINADORA" || x.ManagerId == actor.Id), ct);
    private async Task<object> ContactDto(Guid id, CancellationToken ct) => ContactDto(await db.Contacts.AsNoTracking().Include(x => x.Patient).Include(x => x.Manager).Include(x => x.CityAtContact).Include(x => x.Revisions).SingleAsync(x => x.Id == id, ct));
    private static object PatientDto(Patient x, string city) => new { x.Id, x.FullName, x.DocumentCountryCode, x.DocumentTypeCode, x.DocumentNumber, x.Phone, x.Email, x.CityId, cityName = city, treatmentStartDate = x.TreatmentStartDate.ToString("yyyy-MM-dd"), x.AssignedManagerId };
    private static object FollowUpDto(FollowUp x) => new { x.Id, x.PatientId, x.ManagerId, scheduledAt = Iso(x.ScheduledAtUtc) };
    private static object ContactDto(Contact c) { var r = c.Revisions.Single(x => x.RevisionNumber == c.CurrentRevision); return new { c.Id, c.PatientId, patientName = c.Patient!.FullName, c.ManagerId, managerName = c.Manager!.DisplayName, cityId = c.CityAtContactId, cityName = c.CityAtContact!.Name, occurredAt = Iso(r.OccurredAtUtc), r.Channel, r.Result, revision = c.CurrentRevision, version = Convert.ToBase64String(c.RowVersion) }; }
    private static object[] Options(IEnumerable<string> values) => values.Select(x => (object)new { code = x, name = x.Replace("_", " ") }).ToArray();
    private static string Iso(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
    private static UseCaseResult Forbidden() => UseCaseResult.Fail(403, "FORBIDDEN", "Sin permiso."); private static UseCaseResult NotFound() => UseCaseResult.Fail(404, "RESOURCE_NOT_FOUND", "No encontrado."); private static UseCaseResult DuplicatePatient() => UseCaseResult.Fail(409, "PATIENT_ALREADY_EXISTS", "El paciente ya existe."); private static UseCaseResult Conflict() => UseCaseResult.Fail(409, "CONTACT_VERSION_CONFLICT", "Version desactualizada.");
}
