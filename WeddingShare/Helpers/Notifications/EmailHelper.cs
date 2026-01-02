using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using Microsoft.Extensions.Localization;
using Razor.Templating.Core;
using WeddingShare.Resources.Templates.Email;

namespace WeddingShare.Helpers.Notifications
{
    public class EmailHelper : INotificationHelper
    {
        private readonly ISettingsHelper _settings;
        private readonly ISmtpClientWrapper _client;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        public EmailHelper(ISettingsHelper settings, ISmtpClientWrapper client, ILogger<EmailHelper> logger, IStringLocalizer<Lang.Translations> localizer)
        {
            _settings = settings;
            _client = client;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            return await this.SendTo(await _settings.GetOrDefault(Constants.Notifications.Smtp.Recipient, string.Empty), title, new BasicEmail()
            {
                Title = title,
                Message = message,
                Link = !string.IsNullOrWhiteSpace(actionLink) ? new BasicEmailLink()
                {
                    Heading = _localizer["Visit"].Value,
                    Value = actionLink
                } : null
            });
        }

        public async Task<bool> SendTo(string recipients, string title, BasicEmail model)
        {
            var body = await RazorTemplateEngine.RenderAsync("~/Resources/Templates/Email/Basic.cshtml", model);
            return await this.SendTo(recipients, title, body);
        }

        public async Task<bool> SendTo(string recipients, string title, string message)
        {
            var addressList = recipients?.Split(new char[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?.Select(x => new MailAddress(x));
            if (await _settings.GetOrDefault(Constants.Notifications.Smtp.Enabled, false))
            { 
                try
                {
                    if (addressList != null && addressList.Any())
                    { 
                        var host = await _settings.GetOrDefault(Constants.Notifications.Smtp.Host, string.Empty);
                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            var port = await _settings.GetOrDefault(Constants.Notifications.Smtp.Port, 587);
                            if (port > 0)
                            {
                                var from = await _settings.GetOrDefault(Constants.Notifications.Smtp.From, string.Empty);
                                if (!string.IsNullOrWhiteSpace(from))
                                {
                                    var sentToAll = true;
                                    using (var smtp = new SmtpClient(host, port))
                                    {
                                        var username = await _settings.GetOrDefault(Constants.Notifications.Smtp.Username, string.Empty);
                                        var password = await _settings.GetOrDefault(Constants.Notifications.Smtp.Password, string.Empty);
                                        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                                        {
                                            smtp.UseDefaultCredentials = false;
                                            smtp.Credentials = new NetworkCredential(username, password);
                                        }

                                        smtp.EnableSsl = await _settings.GetOrDefault(Constants.Notifications.Smtp.UseSSL, false);

                                        var sender = new MailAddress(from, await _settings.GetOrDefault(Constants.Notifications.Smtp.DisplayName, "WeddingShare"));
                                        foreach (var to in addressList)
                                        {
                                            try
                                            {
                                                await _client.SendMailAsync(smtp, new MailMessage(new MailAddress(from, await _settings.GetOrDefault(Constants.Notifications.Smtp.DisplayName, "WeddingShare")), to)
                                                {
                                                    Sender = sender,
                                                    Subject = title,
                                                    Body = message,
                                                    IsBodyHtml = true,
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, $"Failed to send email to '{to}' - {ex.Message}");
                                                sentToAll = false;
                                            }
                                        }
                                    }
                
                                    return sentToAll;
                                }
                                else
                                { 
                                    _logger.LogWarning($"Invalid SMTP sender specified");
                                }
                            }
                            else
                            { 
                                _logger.LogWarning($"Invalid SMTP port specified");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid SMTP host specified");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid SMTP recipient specified");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email with title '{title}' - {ex?.Message}");
                }
            }

            return false;
        }
    }
}