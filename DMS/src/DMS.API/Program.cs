using DMS.API.Middleware;
using DMS.API.Messaging;
using DMS.BLL;
using DMS.DAL;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.HostName), "RabbitMQ host is required.")
    .Validate(settings => settings.Port is > 0 and <= 65535, "RabbitMQ port must be between 1 and 65535.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.UserName), "RabbitMQ username is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Password), "RabbitMQ password is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.QueueName), "RabbitMQ queue name is required.")
    .ValidateOnStart();
builder.Services.AddHostedService<RabbitMqConnection>();

// Application layers
builder.Services.AddBusinessLogic();
builder.Services.AddDataAccess(builder.Configuration);

var app = builder.Build();

// Apply pending EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
