SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersion (
        Version varchar(100) NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        Checksum char(64) NOT NULL,
        AppliedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'dbo.Country', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Country (
        Code char(2) NOT NULL CONSTRAINT PK_Country PRIMARY KEY,
        Name nvarchar(80) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.City', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.City (
        Id int NOT NULL CONSTRAINT PK_City PRIMARY KEY,
        CountryCode char(2) NOT NULL,
        Name nvarchar(120) NOT NULL,
        CONSTRAINT FK_City_Country FOREIGN KEY (CountryCode) REFERENCES dbo.Country(Code),
        CONSTRAINT UQ_City_Country_Name UNIQUE (CountryCode, Name)
    );
END;

IF OBJECT_ID(N'dbo.DocumentType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentType (
        Code varchar(20) NOT NULL CONSTRAINT PK_DocumentType PRIMARY KEY,
        Name nvarchar(80) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Actor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Actor (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Actor PRIMARY KEY,
        DisplayName nvarchar(120) NOT NULL,
        Role varchar(20) NOT NULL,
        CONSTRAINT CK_Actor_Role CHECK (Role IN ('GESTOR', 'COORDINADORA'))
    );
END;

IF OBJECT_ID(N'dbo.Patient', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patient (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Patient PRIMARY KEY,
        FullName nvarchar(150) NOT NULL,
        DocumentCountryCode char(2) NOT NULL,
        DocumentTypeCode varchar(20) NOT NULL,
        DocumentNumber nvarchar(40) NOT NULL,
        NormalizedDocumentNumber nvarchar(40) NOT NULL,
        Phone nvarchar(30) NOT NULL,
        Email nvarchar(254) NULL,
        CityId int NOT NULL,
        TreatmentStartDate date NOT NULL,
        AssignedManagerId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        CONSTRAINT FK_Patient_DocumentCountry FOREIGN KEY (DocumentCountryCode) REFERENCES dbo.Country(Code),
        CONSTRAINT FK_Patient_DocumentType FOREIGN KEY (DocumentTypeCode) REFERENCES dbo.DocumentType(Code),
        CONSTRAINT FK_Patient_City FOREIGN KEY (CityId) REFERENCES dbo.City(Id),
        CONSTRAINT FK_Patient_Manager FOREIGN KEY (AssignedManagerId) REFERENCES dbo.Actor(Id),
        CONSTRAINT FK_Patient_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Actor(Id),
        CONSTRAINT UQ_Patient_Identity UNIQUE (DocumentCountryCode, DocumentTypeCode, NormalizedDocumentNumber)
    );
END;

IF OBJECT_ID(N'dbo.FollowUp', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FollowUp (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_FollowUp PRIMARY KEY,
        PatientId uniqueidentifier NOT NULL,
        ManagerId uniqueidentifier NOT NULL,
        ScheduledAtUtc datetime2(3) NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        CONSTRAINT FK_FollowUp_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient(Id),
        CONSTRAINT FK_FollowUp_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.Actor(Id),
        CONSTRAINT FK_FollowUp_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Actor(Id)
    );
END;

IF OBJECT_ID(N'dbo.Contact', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contact (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Contact PRIMARY KEY,
        PatientId uniqueidentifier NOT NULL,
        ManagerId uniqueidentifier NOT NULL,
        CityAtContactId int NOT NULL,
        CurrentRevision int NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_Contact_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient(Id),
        CONSTRAINT FK_Contact_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.Actor(Id),
        CONSTRAINT FK_Contact_City FOREIGN KEY (CityAtContactId) REFERENCES dbo.City(Id),
        CONSTRAINT FK_Contact_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Actor(Id),
        CONSTRAINT CK_Contact_CurrentRevision CHECK (CurrentRevision >= 1)
    );
END;

IF OBJECT_ID(N'dbo.ContactRevision', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactRevision (
        ContactId uniqueidentifier NOT NULL,
        RevisionNumber int NOT NULL,
        OccurredAtUtc datetime2(3) NOT NULL,
        Channel varchar(16) NOT NULL,
        Result varchar(20) NOT NULL,
        CorrectionReason nvarchar(500) NULL,
        RecordedAtUtc datetime2(3) NOT NULL,
        RecordedBy uniqueidentifier NOT NULL,
        CONSTRAINT PK_ContactRevision PRIMARY KEY (ContactId, RevisionNumber),
        CONSTRAINT FK_ContactRevision_Contact FOREIGN KEY (ContactId) REFERENCES dbo.Contact(Id),
        CONSTRAINT FK_ContactRevision_RecordedBy FOREIGN KEY (RecordedBy) REFERENCES dbo.Actor(Id),
        CONSTRAINT CK_ContactRevision_Number CHECK (RevisionNumber >= 1),
        CONSTRAINT CK_ContactRevision_Channel CHECK (Channel IN ('LLAMADA', 'WHATSAPP', 'CORREO')),
        CONSTRAINT CK_ContactRevision_Result CHECK (Result IN ('CONTACTADO', 'SIN_RESPUESTA', 'FALLIDO')),
        CONSTRAINT CK_ContactRevision_Reason CHECK ((RevisionNumber = 1 AND CorrectionReason IS NULL) OR (RevisionNumber > 1 AND LEN(LTRIM(RTRIM(CorrectionReason))) BETWEEN 10 AND 500))
    );
END;

COMMIT;
