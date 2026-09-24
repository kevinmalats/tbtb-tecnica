namespace Tbtb.Domain;

public sealed class Country { public required string Code { get; set; } public required string Name { get; set; } }
public sealed class City { public int Id { get; set; } public required string CountryCode { get; set; } public required string Name { get; set; } public Country? Country { get; set; } }
public sealed class DocumentType { public required string Code { get; set; } public required string Name { get; set; } }
public sealed class Actor { public Guid Id { get; set; } public required string DisplayName { get; set; } public required string Role { get; set; } }

public sealed class Patient
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string DocumentCountryCode { get; set; }
    public required string DocumentTypeCode { get; set; }
    public required string DocumentNumber { get; set; }
    public required string NormalizedDocumentNumber { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public int CityId { get; set; }
    public DateOnly TreatmentStartDate { get; set; }
    public Guid AssignedManagerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public City? City { get; set; }
    public Actor? AssignedManager { get; set; }
}

public sealed class FollowUp
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ManagerId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Patient? Patient { get; set; }
}

public sealed class Contact
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ManagerId { get; set; }
    public int CityAtContactId { get; set; }
    public int CurrentRevision { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Patient? Patient { get; set; }
    public Actor? Manager { get; set; }
    public City? CityAtContact { get; set; }
    public List<ContactRevision> Revisions { get; set; } = [];
}

public sealed class ContactRevision
{
    public Guid ContactId { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public required string Channel { get; set; }
    public required string Result { get; set; }
    public string? CorrectionReason { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public Contact? Contact { get; set; }
    public Actor? RecordedByActor { get; set; }
}
