using BookStore.Application.IService.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Auth
{
    public class FakeEmailSender : IEmailSender
    {
        private readonly ILogger<FakeEmailSender> _logger;
        public FakeEmailSender(ILogger<FakeEmailSender> logger)
        {
            _logger = logger;
        }
        public Task SendEmailAsync(string to, string subject, string html)
        {
            _logger.LogInformation("SendEmail to {to}: {subject}\n{html}", to, subject, html);
            return Task.CompletedTask;
        }
    }
}
