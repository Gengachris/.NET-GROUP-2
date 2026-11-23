USE InsuranceClaims;
GO

CREATE OR ALTER PROCEDURE usp_Approvals_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.claimId, c.claimNumber, c.claimDate, c.claimAmount,
           ph.firstName + ' ' + ph.lastName AS policyHolderName,
           b.branchName
    FROM Claims c
    INNER JOIN PolicyHolders ph ON c.policyHolderId = ph.policyHolderId
    INNER JOIN Branches b ON c.branchId = b.branchId
    WHERE c.status = N'Pending Approval'
    ORDER BY c.claimDate;
END
GO

CREATE OR ALTER PROCEDURE usp_Approvals_Decide
    @claimId int,
    @decision nvarchar(50),
    @approvedAmount decimal(18,2) = NULL,
    @rejectionReason nvarchar(max) = NULL,
    @remarks nvarchar(max) = NULL,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @oldStatus nvarchar(50), @claimAmount decimal(18,2);
    SELECT @oldStatus = status, @claimAmount = claimAmount
    FROM Claims WHERE claimId = @claimId;

    IF @oldStatus IS NULL
        THROW 50010, 'Claim not found.', 1;
    IF @oldStatus <> N'Pending Approval'
        THROW 50011, 'Only claims that are Pending Approval can be approved or rejected.', 1;

    IF @decision = N'Approved'
    BEGIN
        IF @approvedAmount IS NULL OR @approvedAmount <= 0
            THROW 50012, 'The approved amount must be greater than zero.', 1;
        IF @approvedAmount > @claimAmount
            THROW 50013, 'The approved amount cannot be more than the claimed amount.', 1;
    END
    ELSE IF @decision = N'Rejected'
    BEGIN
        IF LEN(LTRIM(RTRIM(ISNULL(@rejectionReason, N'')))) = 0
            THROW 50014, 'A rejection reason is required.', 1;
    END
    ELSE
        THROW 50015, 'Decision must be Approved or Rejected.', 1;

    DECLARE @note nvarchar(max) =
        ISNULL(NULLIF(LTRIM(RTRIM(@remarks)), N''), CONCAT(N'Claim ', LOWER(@decision), N'.'));

    BEGIN TRAN;

    UPDATE Claims
    SET status = @decision,
        approvedAmount  = CASE WHEN @decision = N'Approved' THEN @approvedAmount ELSE NULL END,
        rejectionReason = CASE WHEN @decision = N'Rejected' THEN @rejectionReason ELSE NULL END
    WHERE claimId = @claimId AND status = N'Pending Approval';

    IF @@ROWCOUNT = 0
        THROW 50016, 'The claim was changed by someone else. Please reload.', 1;

    INSERT INTO ClaimHistory (claimId, oldStatus, NewStatus, changedBy, changeDate, remarks)
    VALUES (@claimId, @oldStatus, @decision, @userId, GETDATE(), @note);

    COMMIT;
END
GO
