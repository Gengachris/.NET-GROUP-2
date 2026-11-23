CREATE OR ALTER PROCEDURE usp_Claims_History
    @claimId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.changeDate, h.oldStatus, h.NewStatus, h.remarks, u.userName
    FROM ClaimHistory h
    INNER JOIN Users u ON h.changedBy = u.userId
    WHERE h.claimId = @claimId
    ORDER BY h.changeDate;
END