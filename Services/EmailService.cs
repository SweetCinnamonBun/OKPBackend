using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using OKPBackend.Models.DTO.Users;

namespace OKPBackend.Services
{
    public class EmailService
    {
        public EmailService()
        {

        }

        public async Task<bool> SendEmailAsync(EmailSendDto emailSendDto)
        {
            // DotNetEnv.Env.Load();
            // string mail_api_key = Environment.GetEnvironmentVariable("mail_api_key");
            // string mail_secret = Environment.GetEnvironmentVariable("mail_secret");
            // string from = Environment.GetEnvironmentVariable("from");
            // string application_name = Environment.GetEnvironmentVariable("application_name");

            //Creates a new mailjet client
            MailjetClient client = new MailjetClient("26735861fe7236dd9d154a50aef6fc70", "ac75fdfcf8bf85656524e54c4b9fa249");

            // Builds the specific email to the user.
            var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact("roman.klemiato15@gmail.com", "RakennustenKaupunki"))
            .WithSubject(emailSendDto.Subject).WithHtmlPart(emailSendDto.Body).WithTo(new SendContact(emailSendDto.To)).Build();

            // Sends the email to the user and returns a response.
            var response = await client.SendTransactionalEmailAsync(email);

            if (response.Messages != null)
            {
                if (response.Messages[0].Status == "success")
                {
                    return true;
                }
            }

            return false;

        }


    }
}