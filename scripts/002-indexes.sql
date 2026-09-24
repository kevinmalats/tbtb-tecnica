IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContactRevision_OccurredAt_Contact' AND object_id = OBJECT_ID(N'dbo.ContactRevision'))
BEGIN
    CREATE INDEX IX_ContactRevision_OccurredAt_Contact
    ON dbo.ContactRevision (OccurredAtUtc, ContactId)
    INCLUDE (RevisionNumber, Channel, Result);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contact_Manager_City_Id' AND object_id = OBJECT_ID(N'dbo.Contact'))
BEGIN
    CREATE INDEX IX_Contact_Manager_City_Id
    ON dbo.Contact (ManagerId, CityAtContactId, Id)
    INCLUDE (CurrentRevision, PatientId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FollowUp_Patient_ScheduledAt' AND object_id = OBJECT_ID(N'dbo.FollowUp'))
BEGIN
    CREATE INDEX IX_FollowUp_Patient_ScheduledAt
    ON dbo.FollowUp (PatientId, ScheduledAtUtc, Id);
END;
