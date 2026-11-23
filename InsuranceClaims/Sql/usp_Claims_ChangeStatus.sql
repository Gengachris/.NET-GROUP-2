CREATE OR ALTER PROCEDURE usp_Claims_ChangeStatus
    @claimId int,
    @newStatus nvarchar(50),
    @userId int,
    @remarks nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(50);
    SELECT @old = status FROM Claims WHERE claimId = @claimId;

    IF @newStatus = N'Under Review' AND @old NOT IN (N'Submitted', N'Pending Documents')
        THROW 50001, 'That status change is not allowed.', 1;
    IF @newStatus = N'Pending Documents' AND @old <> N'Under Review'
        THROW 50001, 'That status change is not allowed.', 1;

    UPDATE Claims SET status = @newStatus WHERE claimId = @claimId;
    INSERT INTO ClaimHistory (claimId, oldStatus, NewStatus, changedBy, changeDate, remarks)
    VALUES (@claimId, @old, @newStatus, @userId, GETDATE(), @remarks);
END