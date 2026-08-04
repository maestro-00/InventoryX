using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace InventoryX.Application.Services;

public class EmailSender : IEmailSender, IAttachmentEmailSender
{
    private readonly ILogger _logger;

    public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor,
        ILogger<EmailSender> logger)
    {
        Options = optionsAccessor.Value;
        _logger = logger;
    }

    public AuthMessageSenderOptions Options { get; }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrEmpty(Options.SendGridKey))
        {
            throw new Exception("SendGridKey is null or empty");
        }
        await Execute(Options.SendGridKey, subject, message, toEmail);
    }

    public async Task Execute(string apiKey, string subject, string message, string toEmail)
    {
        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(Options.SenderEmail, Options.SenderName),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        msg.AddTo(new EmailAddress(toEmail));

        // Disable click tracking.
        // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(msg);
        _logger.LogInformation(response.IsSuccessStatusCode
            ? $"Email to {toEmail} queued successfully!"
            : $"Failure Email to {toEmail}");
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string fileName, string contentType, byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Options.SendGridKey)) throw new InvalidOperationException("SendGridKey is null or empty");
        var client = new SendGridClient(Options.SendGridKey);
        var message = new SendGridMessage
        {
            From = new EmailAddress(Options.SenderEmail, Options.SenderName), Subject = subject,
            PlainTextContent = "Please see the attached document.", HtmlContent = htmlBody,
        };
        message.AddTo(new EmailAddress(to));
        message.AddAttachment(fileName, Convert.ToBase64String(content), contentType);
        message.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Email delivery failed with status {(int)response.StatusCode}.");
        _logger.LogInformation("Email with attachment {FileName} queued to {Recipient}", fileName, to);
    }
}
