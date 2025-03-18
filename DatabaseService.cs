using Dapper;
using Microsoft.Data.SqlClient;

namespace sendbulkemail;

public class DatabaseService
{
    private readonly string _connectionString;
    private const int BatchSize = 100;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    private void EnsureDatabase()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailAudit')
            BEGIN
                CREATE TABLE EmailAudit (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    EmailAddress NVARCHAR(255) NOT NULL,
                    Template NVARCHAR(MAX) NULL,
                    Subject NVARCHAR(500) NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    ErrorMessage NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    ProcessedAt DATETIME2 NULL,
                    RetryCount INT NOT NULL DEFAULT 0
                )
                CREATE INDEX IX_EmailAudit_Status ON EmailAudit(Status)
                CREATE INDEX IX_EmailAudit_EmailAddress ON EmailAudit(EmailAddress)
            END";

        connection.Execute(sql);
    }

    public async Task CreateAuditEntriesBulkAsync(IEnumerable<EmailData> emailDataList)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO EmailAudit (Name, EmailAddress, Template, Subject, Status, CreatedAt)
            VALUES (@Name, @EmailAddress, @Template, @Subject, 'Pending', GETUTCDATE())";

        // Process in batches of BatchSize
        var batches = emailDataList.Select((x, i) => new { Index = i, Data = x })
                                 .GroupBy(x => x.Index / BatchSize)
                                 .Select(g => g.Select(x => x.Data));

        foreach (var batch in batches)
        {
            await connection.ExecuteAsync(sql, batch);
        }
    }

    public async Task UpdateAuditStatusAsync(string emailAddress, string status, string? errorMessage = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE EmailAudit
            SET Status = @Status,
                ErrorMessage = @ErrorMessage,
                ProcessedAt = CASE WHEN @Status IN ('Completed', 'Failed') THEN GETUTCDATE() ELSE NULL END,
                RetryCount = CASE WHEN @Status = 'Failed' THEN RetryCount + 1 ELSE RetryCount END
            WHERE EmailAddress = @EmailAddress
            AND Status = 'Pending'";

        await connection.ExecuteAsync(sql, new
        {
            EmailAddress = emailAddress,
            Status = status,
            ErrorMessage = errorMessage
        });
    }

    public async Task<IEnumerable<EmailAudit>> GetFailedEmailsForRetryAsync(int maxRetries = 3)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM EmailAudit 
            WHERE Status = 'Failed' 
            AND RetryCount < @maxRetries 
            ORDER BY CreatedAt";

        return await connection.QueryAsync<EmailAudit>(sql, new { maxRetries });
    }

    public async Task<(int Total, int Pending, int Completed, int Failed)> GetProgressStatusAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT 
                COUNT(*) as Total,
                SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as Pending,
                SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) as Completed,
                SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as Failed
            FROM EmailAudit";

        return await connection.QueryFirstAsync<(int Total, int Pending, int Completed, int Failed)>(sql);
    }
} 