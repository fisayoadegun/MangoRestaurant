using MailKit.Security;
using Mango.Services.Email.Messages;
using Mango.Services.Email.Models;
using Mango.Services.Email.Settings;
using Mango.Services.Emal.DbContexts;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Utilities;

namespace Mango.Services.Email.Repository
{
    public class EmailRepository : IEmailRepository
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContext;
		private readonly MailSettings _mailSettings;


		public EmailRepository(DbContextOptions<ApplicationDbContext> dbContext, IOptions<MailSettings> mailSettings)
        {
            _dbContext = dbContext;
			_mailSettings = mailSettings.Value;
		}
        

        public async Task SendAndLogEmail(UpdatePaymentResultMessage message)
        {
            // implement an email sender or call some other class library
            EmailLog emailLog = new EmailLog()
            {
                Email = message.Email,
                EmailSent = DateTime.Now,
                Log = $"Order - {message.OrderId} has been created successfully."
            };

            await using var _db = new ApplicationDbContext(_dbContext);
            _db.EmailLogs.Add(emailLog);
            await _db.SaveChangesAsync();
        }

		public async Task SendOrderDetailsEmail(EmailOrderHeader message)
		{
			try
			{
				string dynamicContent = string.Join("", message.OrderDetails.Select(item => $"<tr><td align=\"left\" class=\"es-m-txt-c\" style=\"Margin:0;padding-left:20px;padding-right:20px;padding-top:25px;padding-bottom:25px\"><img class=\"adapt-img\" src=\"{item.ProductImage}\" alt title width=\"600\" style=\"display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic\"><h3 class=\"p_name\" style=\"Margin:0;line-height:36px;mso-line-height-rule:exactly;font-family:Raleway, Arial, sans-serif;font-size:24px;font-style:normal;font-weight:normal;color:#386641\">{item.ProductName}</h3><p style=\"Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:tahoma, verdana, segoe, sans-serif;line-height:24px;color:#4D4D4D;font-size:16px\">QTY:{item.Count}</p><h3 style=\"Margin:0;line-height:36px;mso-line-height-rule:exactly;font-family:Raleway, Arial, sans-serif;font-size:24px;font-style:normal;font-weight:normal;color:#386641\" class=\"p_price\">{item.Price:C}</h3></td></tr>"));
				string FilePath = Directory.GetCurrentDirectory() + "\\Templates\\WelcomeTemplate.html";
				StreamReader str = new StreamReader(FilePath);
				string MailText = str.ReadToEnd();
				str.Close();
				MailText = MailText.Replace("[username]", message.FirstName).Replace("[email]", message.Email).Replace("{{DynamicContent}}", dynamicContent).Replace("[Date]", DateTime.Now.ToString("dd/MM/yyy"));
				var email = new MimeMessage();
				email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
				email.To.Add(MailboxAddress.Parse(message.Email));
				email.Subject = "Your Order Details";
				var builder = new BodyBuilder();				
				builder.HtmlBody = MailText;
				email.Body = builder.ToMessageBody();
				using var smtp = new SmtpClient();
				smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
				smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
				await smtp.SendAsync(email);
				smtp.Disconnect(true);
			}
			catch (Exception ex)
			{

				throw;

			}
		}
	}
}
