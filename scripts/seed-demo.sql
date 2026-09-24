MERGE dbo.Country AS target
USING (VALUES ('CO', N'Colombia'), ('PE', N'Peru'), ('EC', N'Ecuador')) AS source (Code, Name)
ON target.Code = source.Code
WHEN NOT MATCHED THEN INSERT (Code, Name) VALUES (source.Code, source.Name);

MERGE dbo.City AS target
USING (VALUES (1, 'CO', N'Bogota'), (2, 'PE', N'Lima')) AS source (Id, CountryCode, Name)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, CountryCode, Name) VALUES (source.Id, source.CountryCode, source.Name);

MERGE dbo.DocumentType AS target
USING (VALUES ('NATIONAL_ID', N'DNI'), ('FOREIGN_ID', N'Documento extranjero'), ('PASSPORT', N'Pasaporte')) AS source (Code, Name)
ON target.Code = source.Code
WHEN MATCHED AND target.Name <> source.Name THEN UPDATE SET Name = source.Name
WHEN NOT MATCHED THEN INSERT (Code, Name) VALUES (source.Code, source.Name);

MERGE dbo.Actor AS target
USING (VALUES
    ('11111111-1111-4111-8111-111111111111', N'Gestor A', 'GESTOR'),
    ('22222222-2222-4222-8222-222222222222', N'Gestor B', 'GESTOR'),
    ('33333333-3333-4333-8333-333333333333', N'Coordinadora', 'COORDINADORA')
) AS source (Id, DisplayName, Role)
ON target.Id = CONVERT(uniqueidentifier, source.Id)
WHEN NOT MATCHED THEN INSERT (Id, DisplayName, Role) VALUES (CONVERT(uniqueidentifier, source.Id), source.DisplayName, source.Role);

MERGE dbo.Patient AS target
USING (VALUES
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', N'Paciente de prueba A', N'001234', 1, '11111111-1111-4111-8111-111111111111'),
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2', N'Paciente de prueba B', N'002234', 1, '22222222-2222-4222-8222-222222222222'),
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3', N'Paciente de prueba C', N'003234', 2, '11111111-1111-4111-8111-111111111111'),
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4', N'Paciente de prueba D', N'004234', 2, '22222222-2222-4222-8222-222222222222')
) AS source (Id, FullName, DocumentNumber, CityId, ManagerId)
ON target.Id = CONVERT(uniqueidentifier, source.Id)
WHEN NOT MATCHED THEN INSERT
    (Id, FullName, DocumentCountryCode, DocumentTypeCode, DocumentNumber, NormalizedDocumentNumber, Phone, Email, CityId, TreatmentStartDate, AssignedManagerId, CreatedAtUtc, CreatedBy)
VALUES
    (CONVERT(uniqueidentifier, source.Id), source.FullName, 'CO', 'NATIONAL_ID', source.DocumentNumber, source.DocumentNumber, N'+57 300 000 0000', NULL, source.CityId, '2026-08-01', CONVERT(uniqueidentifier, source.ManagerId), SYSUTCDATETIME(), CONVERT(uniqueidentifier, source.ManagerId));
