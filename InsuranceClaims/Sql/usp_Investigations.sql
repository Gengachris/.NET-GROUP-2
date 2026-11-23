CREATE OR ALTER PROCEDURE usp_Investigations_List @claimId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT investigationId, investigationDate, findings, recommendedAmount, remarks, investigationStatus
    FROM Investigations
    WHERE claimId = @claimId
    ORDER BY investigationDate DESC;
END
GO
CREATE OR ALTER PROCEDURE usp_Investigations_Add
    @claimId int, @userId int, @investigationDate date,
    @findings nvarchar(max), @recommendedAmount decimal(18,2), @remarks nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @adjusterId int;
    SELECT @adjusterId = a.adjusterId
    FROM Claims c
    INNER JOIN Adjusters a ON c.adjusterId = a.adjusterId
    WHERE c.claimId = @claimId AND c.status = N'Under Investigation' AND a.userId = @userId;

    IF @adjusterId IS NULL
        THROW 50001, 'Only the assigned adjuster can add an investigation.', 1;
    IF EXISTS (SELECT 1 FROM Investigations WHERE claimId = @claimId AND investigationStatus = N'In Progress')
        THROW 50002, 'Finish or delete the open investigation first.', 1;
    IF @recommendedAmount <= 0 OR LEN(LTRIM(RTRIM(ISNULL(@findings, N'')))) = 0
        THROW 50003, 'Findings are required and the recommended amount must be greater than zero.', 1;

    INSERT INTO Investigations
        (claimId, adjusterId, investigationDate, findings, recommendedAmount, remarks, investigationStatus)
    VALUES
        (@claimId, @adjusterId, @investigationDate, @findings, @recommendedAmount, ISNULL(@remarks, N''), N'In Progress');
END
GO
CREATE OR ALTER PROCEDURE usp_Investigations_Submit
    @investigationId int, @claimId int, @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;

    UPDATE Investigations
    SET investigationStatus = N'Submitted'
    WHERE investigationId = @investigationId AND claimId = @claimId
      AND investigationStatus = N'In Progress'
      AND adjusterId = (SELECT adjusterId FROM Adjusters WHERE userId = @userId);

    IF @@ROWCOUNT = 0
        THROW 50004, 'Only your own in-progress investigation can be submitted.', 1;

    UPDATE Claims
    SET status = N'Pending Approval'
    WHERE claimId = @claimId AND status = N'Under Investigation';

    IF @@ROWCOUNT = 0
        THROW 50005, 'This claim is not under investigation.', 1;

    INSERT INTO ClaimHistory (claimId, oldStatus, NewStatus, changedBy, changeDate, remarks)
    VALUES (@claimId, N'Under Investigation', N'Pending Approval', @userId, GETDATE(), N'Investigation submitted.');

    COMMIT;
END
GO
CREATE OR ALTER PROCEDURE usp_Investigations_Delete
    @investigationId int, @claimId int, @userId int
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Investigations
    WHERE investigationId = @investigationId AND claimId = @claimId
      AND investigationStatus = N'In Progress'
      AND adjusterId = (SELECT adjusterId FROM Adjusters WHERE userId = @userId);

    IF @@ROWCOUNT = 0
        THROW 50006, 'A submitted investigation cannot be deleted.', 1;
END