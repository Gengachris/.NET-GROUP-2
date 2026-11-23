CREATE OR ALTER PROCEDURE usp_Claims_Assign
    @claimId int,
    @adjusterId int,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(50);
    SELECT @old = status FROM Claims WHERE claimId = @claimId;

    IF @old NOT IN (N'Submitted', N'Under Review', N'Pending Documents', N'Under Investigation')
        THROW 50001, 'An adjuster can be assigned only before the claim is sent for approval.', 1;
    IF NOT EXISTS (SELECT 1 FROM Adjusters WHERE adjusterId = @adjusterId AND isActive = 1)
        THROW 50002, 'That adjuster is not active.', 1;

    UPDATE Claims
    SET adjusterId = @adjusterId, status = N'Under Investigation'
    WHERE claimId = @claimId;

    INSERT INTO ClaimHistory (claimId, oldStatus, NewStatus, changedBy, changeDate, remarks)
    VALUES (@claimId, @old, N'Under Investigation', @userId, GETDATE(), N'Adjuster assigned.');
END