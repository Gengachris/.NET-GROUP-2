CREATE OR ALTER PROCEDURE usp_Claims_Register
    @policyHolderId int,
    @branchId int,
    @claimDate date,
    @incidentDate date,
    @claimAmount decimal(18,2),
    @location nvarchar(200),
    @description nvarchar(max),
    @userId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @claimAmount <= 0
        THROW 50001, 'The amount must be greater than zero.', 1;
    IF @incidentDate > @claimDate
        THROW 50002, 'The incident date cannot be after the claim date.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@location, N'')))) = 0 OR LEN(LTRIM(RTRIM(ISNULL(@description, N'')))) = 0
        THROW 50003, 'Location and description are required.', 1;

    DECLARE @prefix nvarchar(20) = CONCAT(N'CLM-', YEAR(GETDATE()), N'-');
    DECLARE @next int =
        (SELECT ISNULL(MAX(TRY_CAST(RIGHT(claimNumber, 4) AS int)), 0) + 1
         FROM Claims WHERE claimNumber LIKE @prefix + N'%');

    INSERT INTO Claims
        (claimNumber, claimDate, incidentDate, claimAmount, approvedAmount,
         incidentDescription, incidentLocation, status, rejectionReason,
         policyHolderId, branchId, adjusterId)
    VALUES
        (@prefix + RIGHT(CONCAT(N'0000', @next), 4), @claimDate, @incidentDate, @claimAmount, NULL,
         @description, @location, N'Submitted', NULL, @policyHolderId, @branchId, NULL);

    INSERT INTO ClaimHistory (claimId, oldStatus, NewStatus, changedBy, changeDate, remarks)
    VALUES (SCOPE_IDENTITY(), N'None', N'Submitted', @userId, GETDATE(), N'Claim registered.');
END