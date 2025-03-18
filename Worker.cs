namespace sendbulkemail;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ExcelReader _excelReader;
    private readonly EmailSender _emailSender;
    private readonly DatabaseService _databaseService;
    private const int BatchSize = 50;
    private const int MaxRetries = 3;
    private const int ProgressReportInterval = 100;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Initialize ExcelReader with attachment base path
        var attachmentBasePath = _configuration["AttachmentBasePath"] ?? 
            Path.GetDirectoryName(_configuration["ExcelFilePath"] ?? "") ?? 
            throw new ArgumentNullException("AttachmentBasePath not configured");
        _excelReader = new ExcelReader(attachmentBasePath);
        
        // Initialize EmailSender with configuration
        _emailSender = new EmailSender(
            _configuration["Email:SmtpServer"] ?? throw new ArgumentNullException("SmtpServer"),
            int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
            _configuration["Email:FromEmail"] ?? throw new ArgumentNullException("FromEmail"),
            _configuration["Email:Password"] ?? throw new ArgumentNullException("Password")
        );

        // Initialize DatabaseService
        _databaseService = new DatabaseService(
            _configuration.GetConnectionString("DefaultConnection") ?? 
            throw new ArgumentNullException("DefaultConnection")
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessEmails(stoppingToken);
                await RetryFailedEmails(stoppingToken);
                
                // Wait for 5 minutes before checking for new emails
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email processing service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in email processing service");
        }
    }

    private async Task ProcessEmails(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting bulk email processing at: {time}", DateTimeOffset.Now);

            string excelFilePath = _configuration["ExcelFilePath"] ?? 
                throw new ArgumentNullException("ExcelFilePath not configured");

            var emailDataList = _excelReader.ReadEmailData(excelFilePath);
            _logger.LogInformation("Found {count} email records to process", emailDataList.Count);

            // Bulk insert all records
            await _databaseService.CreateAuditEntriesBulkAsync(emailDataList);

            // Process in batches
            var batches = emailDataList
                .Select((x, i) => new { Index = i, Data = x })
                .GroupBy(x => x.Index / BatchSize)
                .Select(g => g.Select(x => x.Data).ToList());

            int processedCount = 0;
            foreach (var batch in batches)
            {
                if (stoppingToken.IsCancellationRequested) break;

                await ProcessEmailBatch(batch, stoppingToken);
                processedCount += batch.Count;

                if (processedCount % ProgressReportInterval == 0)
                {
                    var progress = await _databaseService.GetProgressStatusAsync();
                    _logger.LogInformation(
                        "Progress - Total: {total}, Completed: {completed}, Failed: {failed}, Pending: {pending}",
                        progress.Total, progress.Completed, progress.Failed, progress.Pending);
                }
            }

            _logger.LogInformation("Bulk email processing completed at: {time}", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing bulk emails");
        }
    }

    private async Task ProcessEmailBatch(List<EmailData> batch, CancellationToken stoppingToken)
    {
        var tasks = batch.Select(async emailData =>
        {
            try
            {
                await _emailSender.SendEmailAsync(emailData);
                await _databaseService.UpdateAuditStatusAsync(emailData.EmailAddress, "Completed");
                _logger.LogInformation("Email sent successfully to: {email}", emailData.EmailAddress);
            }
            catch (Exception ex)
            {
                await _databaseService.UpdateAuditStatusAsync(emailData.EmailAddress, "Failed", ex.Message);
                _logger.LogError(ex, "Failed to send email to {email}", emailData.EmailAddress);
            }
        });

        await Task.WhenAll(tasks);
        await Task.Delay(200, stoppingToken); // Rate limiting between batches
    }

    private async Task RetryFailedEmails(CancellationToken stoppingToken)
    {
        try
        {
            var failedEmails = await _databaseService.GetFailedEmailsForRetryAsync(MaxRetries);
            if (!failedEmails.Any()) return;

            _logger.LogInformation("Retrying {count} failed emails", failedEmails.Count());

            foreach (var failedEmail in failedEmails)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var emailData = new EmailData
                    {
                        EmailAddress = failedEmail.EmailAddress,
                        Subject = failedEmail.Subject,
                        Body = failedEmail.Subject // You might want to store body in audit table as well
                    };

                    await _emailSender.SendEmailAsync(emailData);
                    await _databaseService.UpdateAuditStatusAsync(emailData.EmailAddress, "Completed");
                    _logger.LogInformation("Retry successful for email: {email}", emailData.EmailAddress);
                }
                catch (Exception ex)
                {
                    await _databaseService.UpdateAuditStatusAsync(failedEmail.EmailAddress, "Failed", ex.Message);
                    _logger.LogError(ex, "Retry failed for email: {email}", failedEmail.EmailAddress);
                }

                await Task.Delay(200, stoppingToken); // Rate limiting
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrying failed emails");
        }
    }
}
