using ListopiaParser;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
var connString = Environment.GetEnvironmentVariable("PGVECTOR_CONN") ?? string.Empty;

var builder = Host.CreateApplicationBuilder();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env}.json", true, true)
    .AddUserSecrets<Program>(true, true)
    .AddEnvironmentVariables();

builder.Services.Configure<ListopiaOptions>(builder.Configuration.GetSection("ListopiaOptions"));
builder.Services.Configure<HardcoverOptions>(builder.Configuration.GetSection("HardcoverOptions"));
builder.Services.Configure<ClipOptions>(builder.Configuration.GetSection("ClipOptions"));
builder.Services.Configure<PgVectorOptions>(builder.Configuration.GetSection("PgVectorOptions"));
builder.Services.AddHttpClient<IListopiaService, ListopiaService>();
builder.Services.AddHttpClient<IHardcoverService, HardcoverService>();
builder.Services.AddHttpClient<IClipService, ClipService>();
builder.Services.AddPostgresVectorStore(connString);
builder.Services.AddHostedService<ListopiaParserRunner>();

var host = builder.Build();
await host.RunAsync();