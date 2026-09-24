SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @RecordedAtUtc datetime2(3) = '2026-10-01T06:00:00.000';
DECLARE @Fixture TABLE (
    Id uniqueidentifier NOT NULL,
    PatientId uniqueidentifier NOT NULL,
    ManagerId uniqueidentifier NOT NULL,
    CityId int NOT NULL,
    OccurredAtUtc datetime2(3) NOT NULL
);

INSERT @Fixture VALUES
    ('cccccccc-cccc-4ccc-8ccc-000000000001', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, '2026-09-10T15:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000002', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3', '11111111-1111-4111-8111-111111111111', 2, '2026-09-11T15:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000003', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2', '22222222-2222-4222-8222-222222222222', 1, '2026-09-12T15:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000004', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4', '22222222-2222-4222-8222-222222222222', 2, '2026-09-13T15:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000005', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, '2026-09-01T04:59:59.999'),
    ('cccccccc-cccc-4ccc-8ccc-000000000006', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, '2026-10-01T05:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000007', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, '2026-09-01T05:00:00.000'),
    ('cccccccc-cccc-4ccc-8ccc-000000000008', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', '11111111-1111-4111-8111-111111111111', 1, '2026-10-01T04:59:59.999');

INSERT dbo.Contact (Id, PatientId, ManagerId, CityAtContactId, CurrentRevision, CreatedAtUtc, CreatedBy)
SELECT f.Id, f.PatientId, f.ManagerId, f.CityId, 1, @RecordedAtUtc, f.ManagerId
FROM @Fixture f
WHERE NOT EXISTS (SELECT 1 FROM dbo.Contact c WHERE c.Id = f.Id);

INSERT dbo.ContactRevision (ContactId, RevisionNumber, OccurredAtUtc, Channel, Result, CorrectionReason, RecordedAtUtc, RecordedBy)
SELECT f.Id, 1, f.OccurredAtUtc, 'LLAMADA', 'CONTACTADO', NULL, @RecordedAtUtc, f.ManagerId
FROM @Fixture f
WHERE NOT EXISTS (SELECT 1 FROM dbo.ContactRevision r WHERE r.ContactId = f.Id AND r.RevisionNumber = 1);

COMMIT;
