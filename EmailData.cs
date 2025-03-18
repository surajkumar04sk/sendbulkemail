using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sendbulkemail;

public class EmailData
{
    public string? Name { get; set; }
    public string? EmailAddress { get; set; }
    public string? Template { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public List<AttachmentInfo> Attachments { get; set; } = new();

    public string GetFormattedBody()
    {
        if (string.IsNullOrEmpty(Template) || string.IsNullOrEmpty(Name))
            return Body ?? string.Empty;

        // Replace placeholders in the template
        return Template.Replace("{Name}", Name)
                      .Replace("{name}", Name)
                      .Replace("{EMAIL}", EmailAddress ?? string.Empty)
                      .Replace("{email}", EmailAddress ?? string.Empty);
    }
}

public class AttachmentInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
