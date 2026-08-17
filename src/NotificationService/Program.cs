using NotificationService;
using NotificationService.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Services.AddNotificationDependencies(builder.Configuration);

var host = builder.Build();
host.Run();
