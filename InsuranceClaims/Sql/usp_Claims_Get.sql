CREATE OR ALTER PROCEDURE usp_Claims_Get
    @claimId int,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role nvarchar(50);
    SELECT @role = r.roleName
    FROM Users u
    INNER JOIN Roles r ON u.roleId = r.roleId
    WHERE u.userId = @userId;

    SELECT c.claimNumber, c.claimDate, c.incidentDate, c.claimAmount,
           c.incidentLocation, c.incidentDescription, c.status,
           ph.firstName + ' ' + ph.lastName AS policyHolderName, ph.policyNumber,
           b.branchName
    FROM Claims c
    INNER JOIN PolicyHolders ph ON c.policyHolderId = ph.policyHolderId
    INNER JOIN Branches b ON c.branchId = b.branchId
    WHERE c.claimId = @claimId
      AND (@role <> N'Claims Adjuster' OR c.adjusterId =
           (SELECT adjusterId FROM Adjusters WHERE userId = @userId));
END