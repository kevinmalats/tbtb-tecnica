SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @ReferenceUtc datetime2(3) = CONVERT(datetime2(3), '$(DemoReferenceUtc)', 127);

DECLARE @Demo TABLE (
    Id uniqueidentifier NOT NULL,
    PatientId uniqueidentifier NOT NULL,
    ManagerId uniqueidentifier NOT NULL,
    CityId int NOT NULL,
    OccurredAtUtc datetime2(3) NOT NULL,
    Channel varchar(16) NOT NULL,
    Result varchar(20) NOT NULL
);

INSERT @Demo VALUES
    ('dddddddd-dddd-4ddd-8ddd-000000000001', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, DATEADD(day, -2, @ReferenceUtc), 'LLAMADA', 'CONTACTADO'),
    ('dddddddd-dddd-4ddd-8ddd-000000000002', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3', '11111111-1111-4111-8111-111111111111', 2, DATEADD(month, -1, @ReferenceUtc), 'WHATSAPP', 'SIN_RESPUESTA');

INSERT dbo.Contact (Id, PatientId, ManagerId, CityAtContactId, CurrentRevision, CreatedAtUtc, CreatedBy)
SELECT d.Id, d.PatientId, d.ManagerId, d.CityId, 1, @ReferenceUtc, d.ManagerId
FROM @Demo d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Contact c WHERE c.Id = d.Id);

INSERT dbo.ContactRevision (ContactId, RevisionNumber, OccurredAtUtc, Channel, Result, CorrectionReason, RecordedAtUtc, RecordedBy)
SELECT d.Id, 1, d.OccurredAtUtc, d.Channel, d.Result, NULL, @ReferenceUtc, d.ManagerId
FROM @Demo d
WHERE NOT EXISTS (SELECT 1 FROM dbo.ContactRevision r WHERE r.ContactId = d.Id AND r.RevisionNumber = 1);

COMMIT;
