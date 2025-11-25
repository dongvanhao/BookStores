using BookStore.Application.IService.Identity;
using System;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Identity
{
    public class EmailSenderFake : IEmailSender
    {
        public Task SendEmailAsync(string to, string subject, string html)
        {
            Console.WriteLine("========== FAKE EMAIL SENDER ==========");
            Console.WriteLine($"TO       : {to}");
            Console.WriteLine($"SUBJECT  : {subject}");
            Console.WriteLine("MESSAGE  :");
            Console.WriteLine(html);
            Console.WriteLine("=======================================");
            return Task.CompletedTask;
        }
    }
}
