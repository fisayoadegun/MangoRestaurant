using Mango.Services.Email.Extensions;
using Mango.Services.Email.Messaging;
using Mango.Services.Email.Repository;
using Mango.Services.Email.Settings;
using Mango.Services.Emal.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
var optionBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionBuilder.UseSqlServer(connString);

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connString));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddControllers();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
builder.Services.AddHostedService<RabbitMQPaymentConsumer>();
builder.Services.AddHostedService<RabbitMQOrderConsumer>();
//builder.Services.AddSingleton(new EmailRepository(optionBuilder.Options));
builder.Services.AddSingleton(provider =>
{
	var dbContextOptions = optionBuilder.Options;
	var mailSettings = provider.GetRequiredService<IOptions<MailSettings>>();

	return new EmailRepository(dbContextOptions, mailSettings);
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseAzureServiceBusConsumer();

app.Run();
