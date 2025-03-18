using ExcelDataReader;
using System.Data;
using System.Text;

namespace sendbulkemail;

public class ExcelReader
{
    private readonly string _attachmentBasePath;

    public ExcelReader(string attachmentBasePath)
    {
        // Register encoding provider for Excel reading
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _attachmentBasePath = attachmentBasePath;
    }

    public List<EmailData> ReadEmailData(string filePath)
    {
        var emailDataList = new List<EmailData>();

        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                DataTable dataTable = result.Tables[0];

                foreach (DataRow row in dataTable.Rows)
                {
                    var emailData = new EmailData
                    {
                        Name = row["Name"]?.ToString()?.Trim(),
                        EmailAddress = row["Email"]?.ToString()?.Trim(),
                        Template = row["Template"]?.ToString(),
                        Subject = row["Subject"]?.ToString()?.Trim()
                    };

                    // Process attachments if present
                    var attachments = row["Attachments"]?.ToString()?.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    if (attachments != null)
                    {
                        foreach (var attachment in attachments)
                        {
                            var attachmentInfo = ParseAttachmentInfo(attachment.Trim());
                            if (attachmentInfo != null)
                            {
                                emailData.Attachments.Add(attachmentInfo);
                            }
                        }
                    }

                    // Only add if we have the minimum required fields
                    if (!string.IsNullOrWhiteSpace(emailData.EmailAddress) && 
                        !string.IsNullOrWhiteSpace(emailData.Name) &&
                        !string.IsNullOrWhiteSpace(emailData.Subject))
                    {
                        emailDataList.Add(emailData);
                    }
                }
            }
        }

        return emailDataList;
    }

    private AttachmentInfo? ParseAttachmentInfo(string attachmentString)
    {
        try
        {
            // Expected format: "FileName|FilePath|ContentType"
            var parts = attachmentString.Split('|');
            if (parts.Length != 3) return null;

            var fileName = parts[0].Trim();
            var filePath = Path.Combine(_attachmentBasePath, parts[1].Trim());
            var contentType = parts[2].Trim();

            return new AttachmentInfo
            {
                FileName = fileName,
                FilePath = filePath,
                ContentType = contentType
            };
        }
        catch
        {
            return null;
        }
    }
}
