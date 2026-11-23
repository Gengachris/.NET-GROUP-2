CREATE OR ALTER PROCEDURE usp_Claims_List
    @search nvarchar(100),
    @status nvarchar(30),
    @userId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role nvarchar(50);
    SELECT @role = r.roleName
    FROM Users u
    INNER JOIN Roles r ON u.roleId = r.roleId
    WHERE u.userId = @userId;

    SELECT c.claimId, c.claimNumber, c.status, c.claimAmount,
           ph.firstName + ' ' + ph.lastName AS policyHolderName,
           b.branchName
    FROM Claims c
    INNER JOIN PolicyHolders ph ON c.policyHolderId = ph.policyHolderId
    INNER JOIN Branches b ON c.branchId = b.branchId
    WHERE (@search = N'' OR c.claimNumber LIKE N'%' + @search + N'%'
           OR ph.firstName LIKE N'%' + @search + N'%'
           OR ph.lastName LIKE N'%' + @search + N'%'
           OR ph.policyNumber LIKE N'%' + @search + N'%')
      AND (@status = N'' OR c.status = @status)
      AND (@role <> N'Claims Adjuster' OR c.adjusterId =
           (SELECT adjusterId FROM Adjusters WHERE userId = @userId))
    ORDER BY c.claimDate DESC;
END