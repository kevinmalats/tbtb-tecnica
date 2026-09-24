namespace Tbtb.Application.UseCases;

public sealed record ActorContext(Guid Id, string DisplayName, string Role);
public sealed record PatientCommand(string FullName, string DocumentCountryCode, string DocumentTypeCode, string DocumentNumber, string Phone, string? Email, int CityId, string TreatmentStartDate);
public sealed record FollowUpCommand(string PatientId, string ScheduledAt);
public sealed record ContactCommand(string PatientId, string OccurredAt, string Channel, string Result);
public sealed record CorrectionCommand(string OccurredAt, string Channel, string Result, string Reason, string ExpectedVersion);

public sealed record UseCaseResult(int Status, object? Value = null, string? Code = null, string? Title = null, IReadOnlyDictionary<string, string[]>? Errors = null, string? Location = null)
{
    public static UseCaseResult Ok(object value) => new(200, value);
    public static UseCaseResult Created(object value, string location) => new(201, value, Location: location);
    public static UseCaseResult Fail(int status, string code, string title) => new(status, Code: code, Title: title);
    public static UseCaseResult Invalid(Dictionary<string, string[]> errors) => new(400, Code: "VALIDATION_ERROR", Title: "La solicitud contiene datos invalidos.", Errors: errors);
}

public interface ITbtbUseCases
{
    Task<ActorContext?> FindActor(Guid id, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetCatalogs(ActorContext actor, CancellationToken cancellationToken = default);
    Task<UseCaseResult> CreatePatient(ActorContext actor, PatientCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetPatients(ActorContext actor, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetPatient(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
    Task<UseCaseResult> CreateFollowUp(ActorContext actor, FollowUpCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetFollowUps(ActorContext actor, string patientId, CancellationToken cancellationToken = default);
    Task<UseCaseResult> CreateContact(ActorContext actor, ContactCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetContacts(ActorContext actor, string? month, string? managerId, int? cityId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetContact(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
    Task<UseCaseResult> CorrectContact(ActorContext actor, Guid id, CorrectionCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetContactHistory(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsReady(CancellationToken cancellationToken = default);
}

public interface IActorRepository { Task<ActorContext?> FindActor(Guid id, CancellationToken cancellationToken = default); }
public interface ICatalogRepository { Task<UseCaseResult> GetCatalogs(ActorContext actor, CancellationToken cancellationToken = default); }
public interface IPatientRepository
{
    Task<UseCaseResult> CreatePatient(ActorContext actor, PatientCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetPatients(ActorContext actor, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetPatient(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
}
public interface IFollowUpRepository
{
    Task<UseCaseResult> CreateFollowUp(ActorContext actor, FollowUpCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetFollowUps(ActorContext actor, string patientId, CancellationToken cancellationToken = default);
}
public interface IContactRepository
{
    Task<UseCaseResult> CreateContact(ActorContext actor, ContactCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetContact(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
    Task<UseCaseResult> CorrectContact(ActorContext actor, Guid id, CorrectionCommand command, CancellationToken cancellationToken = default);
    Task<UseCaseResult> GetContactHistory(ActorContext actor, Guid id, CancellationToken cancellationToken = default);
}
public interface IMonthlyContactQuery { Task<UseCaseResult> GetContacts(ActorContext actor, string? month, string? managerId, int? cityId, int page, int pageSize, CancellationToken cancellationToken = default); }
public interface IHealthProbe { Task<bool> IsReady(CancellationToken cancellationToken = default); }

public sealed class TbtbUseCases(IActorRepository actors, ICatalogRepository catalogs, IPatientRepository patients, IFollowUpRepository followUps, IContactRepository contacts, IMonthlyContactQuery monthlyContacts, IHealthProbe health) : ITbtbUseCases
{
    public Task<ActorContext?> FindActor(Guid id, CancellationToken cancellationToken = default) => actors.FindActor(id, cancellationToken);
    public Task<UseCaseResult> GetCatalogs(ActorContext actor, CancellationToken cancellationToken = default) => catalogs.GetCatalogs(actor, cancellationToken);
    public Task<UseCaseResult> CreatePatient(ActorContext actor, PatientCommand command, CancellationToken cancellationToken = default) => patients.CreatePatient(actor, command, cancellationToken);
    public Task<UseCaseResult> GetPatients(ActorContext actor, int page, int pageSize, CancellationToken cancellationToken = default) => patients.GetPatients(actor, page, pageSize, cancellationToken);
    public Task<UseCaseResult> GetPatient(ActorContext actor, Guid id, CancellationToken cancellationToken = default) => patients.GetPatient(actor, id, cancellationToken);
    public Task<UseCaseResult> CreateFollowUp(ActorContext actor, FollowUpCommand command, CancellationToken cancellationToken = default) => followUps.CreateFollowUp(actor, command, cancellationToken);
    public Task<UseCaseResult> GetFollowUps(ActorContext actor, string patientId, CancellationToken cancellationToken = default) => followUps.GetFollowUps(actor, patientId, cancellationToken);
    public Task<UseCaseResult> CreateContact(ActorContext actor, ContactCommand command, CancellationToken cancellationToken = default) => contacts.CreateContact(actor, command, cancellationToken);
    public Task<UseCaseResult> GetContacts(ActorContext actor, string? month, string? managerId, int? cityId, int page, int pageSize, CancellationToken cancellationToken = default) => monthlyContacts.GetContacts(actor, month, managerId, cityId, page, pageSize, cancellationToken);
    public Task<UseCaseResult> GetContact(ActorContext actor, Guid id, CancellationToken cancellationToken = default) => contacts.GetContact(actor, id, cancellationToken);
    public Task<UseCaseResult> CorrectContact(ActorContext actor, Guid id, CorrectionCommand command, CancellationToken cancellationToken = default) => contacts.CorrectContact(actor, id, command, cancellationToken);
    public Task<UseCaseResult> GetContactHistory(ActorContext actor, Guid id, CancellationToken cancellationToken = default) => contacts.GetContactHistory(actor, id, cancellationToken);
    public Task<bool> IsReady(CancellationToken cancellationToken = default) => health.IsReady(cancellationToken);
}
