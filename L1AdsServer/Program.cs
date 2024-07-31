using L1AdsServer.Configuration;
using L1AdsServer.Core;
using L1AdsServer.Core.NewFolder;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var homeAssistantConfig = configuration.GetSection("HomeAssistant").Get<HomeAssistantConfig>();

builder.Services.AddLogging(builder => builder.AddConsole());

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.ResponseHeaders.Add("MyResponseHeader");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.CombineLogs = true;
});

builder.Services.AddHttpClient(nameof(DataControl), client =>
{
    client.BaseAddress = homeAssistantConfig?.Uri;
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + homeAssistantConfig?.BearerToken);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin();
    });
});

// Add services to the container.
builder.Services.AddSingleton<IDataExtractor, DataExtractor>();

builder.Services.AddSingleton<IBlindControl, BlindControl>();
builder.Services.AddSingleton<IDimmerControl, DimmerControl>();
builder.Services.AddSingleton<IDoorControl, DoorControl>();
builder.Services.AddSingleton<IInputControl, InputControl>();
builder.Services.AddSingleton<ILedControl, LedControl>();
builder.Services.AddSingleton<ISwitchControl, SwitchControl>();
builder.Services.AddSingleton<IDataControl, DataControl>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
