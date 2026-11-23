CREATE OR ALTER PROCEDURE usp_Claims_ForEdit
    @claimId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT claimId, policyHolderId, branchId, claimDate, incidentDate,
           claimAmount, incidentLocation, incidentDescription, status
    FROM Claims
    WHERE claimId = @claimId
      AND status IN (N'Submitted', N'Under Review', N'Pending Documents');
END
GO
CREATE OR ALTER PROCEDURE usp_Claims_Update
    @claimId int,
    @policyHolderId int,
    @branchId int,
    @claimDate date,
    @incidentDate date,
    @claimAmount decimal(18,2),
    @location nvarchar(200),
    @description nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @claimAmount <= 0
        THROW 50001, 'The amount must be greater than zero.', 1;
    IF @incidentDate > @claimDate
        THROW 50002, 'The incident date cannot be after the claim date.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@location, N'')))) = 0 OR LEN(LTRIM(RTRIM(ISNULL(@description, N'')))) = 0
        THROW 50003, 'Location and description are required.', 1;

    UPDATE Claims
    SET policyHolderId = @policyHolderId, branchId = @branchId,
        claimDate = @claimDate, incidentDate = @incidentDate, claimAmount = @claimAmount,
        incidentLocation = @location, incidentDescription = @description
    WHERE claimId = @claimId
      AND status IN (N'Submitted', N'Under Review', N'Pending Documents');

    IF @@ROWCOUNT = 0
        THROW 50004, 'This claim can no longer be edited.', 1;
END