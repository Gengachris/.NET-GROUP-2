/*
    Step 0. One migration for the claim office.
    Run against InsuranceClaims on Essie-gee\SQLEXPRESS.
    Safe to run again: each change is skipped when it is already there.

    Kept as they are: claimAmount, approvedAmount, ClaimHistory.NewStatus,
    Payments.TransactionReference, Payments.paymentStatus.
    paymentStatus is the payment status. Values used by the new system:
    Pending, Confirmed, Cancelled. No second status column is added.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* PolicyHolders: dates, cover, excess, rowVersion */
IF COL_LENGTH('dbo.PolicyHolders', 'policyStartDate') IS NULL
    ALTER TABLE dbo.PolicyHolders ADD policyStartDate date NULL;

IF COL_LENGTH('dbo.PolicyHolders', 'policyEndDate') IS NULL
    ALTER TABLE dbo.PolicyHolders ADD policyEndDate date NULL;

IF COL_LENGTH('dbo.PolicyHolders', 'coverAmount') IS NULL
    ALTER TABLE dbo.PolicyHolders ADD coverAmount decimal(18,2) NULL;

IF COL_LENGTH('dbo.PolicyHolders', 'excessAmount') IS NULL
    ALTER TABLE dbo.PolicyHolders ADD excessAmount decimal(18,2) NULL;

IF COL_LENGTH('dbo.PolicyHolders', 'rowVersion') IS NULL
    ALTER TABLE dbo.PolicyHolders ADD rowVersion rowversion NOT NULL;

/* Users: branch and approval limit. Both stay nullable. */
IF COL_LENGTH('dbo.Users', 'branchId') IS NULL
    ALTER TABLE dbo.Users ADD branchId int NULL;

IF COL_LENGTH('dbo.Users', 'approvalLimit') IS NULL
    ALTER TABLE dbo.Users ADD approvalLimit decimal(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Branches')
    ALTER TABLE dbo.Users
        ADD CONSTRAINT FK_Users_Branches
        FOREIGN KEY (branchId) REFERENCES dbo.Branches (branchId);

/* Claims: who opened the file, excess taken off, payable amount, rowVersion.
   claimAmount and approvedAmount already exist. */
IF COL_LENGTH('dbo.Claims', 'registeredBy') IS NULL
    ALTER TABLE dbo.Claims ADD registeredBy int NULL;

IF COL_LENGTH('dbo.Claims', 'excessApplied') IS NULL
    ALTER TABLE dbo.Claims ADD excessApplied decimal(18,2) NULL;

IF COL_LENGTH('dbo.Claims', 'payableAmount') IS NULL
    ALTER TABLE dbo.Claims ADD payableAmount decimal(18,2) NULL;

IF COL_LENGTH('dbo.Claims', 'rowVersion') IS NULL
    ALTER TABLE dbo.Claims ADD rowVersion rowversion NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Claims_RegisteredBy')
    ALTER TABLE dbo.Claims
        ADD CONSTRAINT FK_Claims_RegisteredBy
        FOREIGN KEY (registeredBy) REFERENCES dbo.Users (userId);

/* Investigations: the reserve is set once, when the investigation is submitted. */
IF COL_LENGTH('dbo.Investigations', 'reserveAmount') IS NULL
    ALTER TABLE dbo.Investigations ADD reserveAmount decimal(18,2) NULL;

/* Payments: who was paid, and who confirmed. TransactionReference stays. */
IF COL_LENGTH('dbo.Payments', 'payeeName') IS NULL
    ALTER TABLE dbo.Payments ADD payeeName nvarchar(200) NULL;

IF COL_LENGTH('dbo.Payments', 'confirmedBy') IS NULL
    ALTER TABLE dbo.Payments ADD confirmedBy int NULL;

IF COL_LENGTH('dbo.Payments', 'confirmedAt') IS NULL
    ALTER TABLE dbo.Payments ADD confirmedAt datetime NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_ConfirmedBy')
    ALTER TABLE dbo.Payments
        ADD CONSTRAINT FK_Payments_ConfirmedBy
        FOREIGN KEY (confirmedBy) REFERENCES dbo.Users (userId);

/* Diary notes. A note never changes the status and is never deleted. */
IF OBJECT_ID('dbo.ClaimNotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClaimNotes
    (
        noteId    int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        claimId   int NOT NULL,
        userId    int NOT NULL,
        note      nvarchar(max) NOT NULL,
        createdAt datetime NOT NULL CONSTRAINT DF_ClaimNotes_createdAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ClaimNotes_Claims FOREIGN KEY (claimId) REFERENCES dbo.Claims (claimId),
        CONSTRAINT FK_ClaimNotes_Users FOREIGN KEY (userId) REFERENCES dbo.Users (userId)
    );
END

/* In-app notices. No email. */
IF OBJECT_ID('dbo.Notices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notices
    (
        noticeId  int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        userId    int NOT NULL,
        claimId   int NOT NULL,
        message   nvarchar(500) NOT NULL,
        isRead    bit NOT NULL CONSTRAINT DF_Notices_isRead DEFAULT (0),
        createdAt datetime NOT NULL CONSTRAINT DF_Notices_createdAt DEFAULT (GETDATE()),
        CONSTRAINT FK_Notices_Users FOREIGN KEY (userId) REFERENCES dbo.Users (userId),
        CONSTRAINT FK_Notices_Claims FOREIGN KEY (claimId) REFERENCES dbo.Claims (claimId)
    );
END

/* Evidence files. The bytes live outside wwwroot. This table stores the name only. */
IF OBJECT_ID('dbo.EvidenceFiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvidenceFiles
    (
        fileId       int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        claimId      int NOT NULL,
        originalName nvarchar(260) NOT NULL,
        storedName   nvarchar(260) NOT NULL,
        contentType  nvarchar(100) NOT NULL,
        sizeBytes    bigint NOT NULL,
        uploadedBy   int NOT NULL,
        uploadedAt   datetime NOT NULL CONSTRAINT DF_EvidenceFiles_uploadedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_EvidenceFiles_Claims FOREIGN KEY (claimId) REFERENCES dbo.Claims (claimId),
        CONSTRAINT FK_EvidenceFiles_Users FOREIGN KEY (uploadedBy) REFERENCES dbo.Users (userId)
    );
END

COMMIT TRANSACTION;

PRINT 'Migration finished.';
