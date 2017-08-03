/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using Folium.Api.Models;
using Dapper;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using Hangfire;

namespace Folium.Api.Services {
    public interface IEmailService {
        [Queue("email")]
        Task SendEmail(Guid emailNotificationId);
    }    
    public class EmailService : IEmailService {
        private readonly IDbService _dbService;
        private readonly IOptions<Configuration> _applicationConfiguration;		
        private readonly ILogger<EmailService> _logger;
        private readonly IViewRenderService _viewRenderService;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<Configuration> applicationConfiguration,
            IViewRenderService viewRenderService,
            IDbService dbService) {
            _logger = logger;
            _applicationConfiguration = applicationConfiguration;
            _viewRenderService = viewRenderService;
            _dbService = dbService;
        }

        public async Task SendEmail(Guid emailNotificationId) {
            if (!_applicationConfiguration.Value.EmailNotificationsEnabled) {
                _logger.LogInformation($"Skipping email as notifications are disabled.");
                return;
            }

            EmailNotification emailNotification;
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                emailNotification = await connection.QueryFirstOrDefaultAsync<EmailNotification>(@"
                    SELECT *
                    FROM [dbo].[ActivityProjector.EmailNotification]
                    WHERE [Id] = @Id",
                    new
                    {
                        Id = emailNotificationId
                    });
            }

            if(emailNotification == null) {
                throw new ArgumentException($"Unable to send email with Email Notification Id of {emailNotificationId} as it does not exist.");
            }

            if(emailNotification.When < _applicationConfiguration.Value.IgnoreEmailNotificationsBefore) {
                _logger.LogInformation($"Skipping email as it is dated {emailNotification.When} which is before the ignore config date of {_applicationConfiguration.Value.IgnoreEmailNotificationsBefore}.");
                return;
            }

            var body = await _viewRenderService.RenderToStringAsync("EmailTemplate", emailNotification);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_applicationConfiguration.Value.SmtpAccountName, _applicationConfiguration.Value.SmtpAccountUser));
            foreach(var to in emailNotification.To.Split(',')) {
                mimeMessage.To.Add(new MailboxAddress(to));
            }
            mimeMessage.Subject = emailNotification.Subject;
            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient()) {
                await client.ConnectAsync(_applicationConfiguration.Value.SmtpServer, _applicationConfiguration.Value.SmtpPort, _applicationConfiguration.Value.SmtpUseSsl ? MailKit.Security.SecureSocketOptions.Auto : MailKit.Security.SecureSocketOptions.None);
                if (_applicationConfiguration.Value.SmtpRequiresAuthentication) {
                    await client.AuthenticateAsync(_applicationConfiguration.Value.SmtpAccountUser, _applicationConfiguration.Value.SmtpAccountPassword);
                }
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
        }
    }    
}