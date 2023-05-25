using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.DbContexts;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Messaging;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.

//IMapper mapper = MappingConfig.RegisterMaps().CreateMaper();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connString));
builder.Services.AddHostedService<RabbitMQCheckoutConsumer>();
builder.Services.AddHostedService<RabbitMQPaymentConsumer>();
builder.Services.AddControllers();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var optionBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionBuilder.UseSqlServer(connString);
builder.Services.AddSingleton(new OrderRepository(optionBuilder.Options));
builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
builder.Services.AddSingleton<IMessageBus, AzureServicebusMessageBus>();
builder.Services.AddSingleton<IRabbitMQOrderMessageSender, RabbitMQOrderMessageSender>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7029/";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ApiScope", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("scope", "mango");
        });
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mango.Services.OrderAPI", Version = "v1" });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Enter 'Bearer' [space] and your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            },
                            Scheme="oauth2",
                            Name="Bearer",
                            In=ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseAzureServiceBusConsumer();

app.Run();
