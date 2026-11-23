
-- 1. SHOW MY ADJUSTERS
CREATE OR ALTER PROCEDURE usp_Adjusters_List
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.adjusterId,
        a.firstName,
        a.lastName,
        a.licenseNumber,
        a.isActive,
        b.branchName,
        u.userName
    FROM Adjusters a
    INNER JOIN Branches b ON a.branchId = b.branchId
    INNER JOIN Users u ON a.userId = u.userId
    ORDER BY a.lastName, a.firstName;
END
GO

-- 2. GET ONE ADJUSTER
CREATE OR ALTER PROCEDURE usp_Adjusters_Get
    @adjusterId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT adjusterId, userId, firstName, lastName,
           licenseNumber, email, phone, branchId, isActive
    FROM Adjusters
    WHERE adjusterId = @adjusterId;
END
GO

-- 3. REGISTER AN ADJUSTER
CREATE OR ALTER PROCEDURE usp_Adjusters_Save
    @userId INT,
    @firstName NVARCHAR(50),
    @lastName NVARCHAR(50),
    @licenseNumber NVARCHAR(50),
    @email NVARCHAR(100),
    @phone NVARCHAR(20),
    @branchId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM Users u
        INNER JOIN Roles r ON u.roleId = r.roleId
        WHERE u.userId = @userId
          AND r.roleName = N'Claims Adjuster'
          AND u.isActive = 1
    )
        THROW 50001, 'Choose an active Claims Adjuster user.', 1;

    INSERT INTO Adjusters
    (userId, firstName, lastName, licenseNumber,
     email, phone, branchId, isActive)
    VALUES
    (@userId, @firstName, @lastName, @licenseNumber,
     @email, @phone, @branchId, 1);
END
GO

-- 4. UPDATE AN ADJUSTER
CREATE OR ALTER PROCEDURE usp_Adjusters_Update
    @adjusterId INT,
    @userId INT,
    @firstName NVARCHAR(50),
    @lastName NVARCHAR(50),
    @licenseNumber NVARCHAR(50),
    @email NVARCHAR(100),
    @phone NVARCHAR(20),
    @branchId INT,
    @isActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Adjusters
    SET userId = @userId,
        firstName = @firstName,
        lastName = @lastName,
        licenseNumber = @licenseNumber,
        email = @email,
        phone = @phone,
        branchId = @branchId,
        isActive = @isActive
    WHERE adjusterId = @adjusterId;
END
GO

-- 5. DELETE  AN ADJUSTER
CREATE OR ALTER PROCEDURE usp_Adjusters_Remove
    @adjusterId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM Claims
        WHERE adjusterId = @adjusterId
    )
    OR EXISTS (
        SELECT 1 FROM Investigations
        WHERE adjusterId = @adjusterId
    )
        UPDATE Adjusters
        SET isActive = 0
        WHERE adjusterId = @adjusterId;
    ELSE
        DELETE FROM Adjusters
        WHERE adjusterId = @adjusterId;
END
GO

-- 6. DISPLAY CLAIMS ASSIGNED TO AN ADJUSTER
CREATE OR ALTER PROCEDURE usp_Adjuster_Claims
    @adjusterId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT claimNumber, status, claimAmount
    FROM Claims
    WHERE adjusterId = @adjusterId
    ORDER BY claimDate DESC;
END
GO
