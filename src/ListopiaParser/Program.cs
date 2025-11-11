using ListopiaParser;
using ListopiaParser.Configs;
using ListopiaParser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;


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
builder.Services.AddHttpClient<ListopiaService>();
builder.Services.AddHttpClient<HardcoverService>();
builder.Services.AddHttpClient<ClipService>();
// builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
// {
//     var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
//     dataSourceBuilder.UseVector();
//     return dataSourceBuilder.Build();
// });
builder.Services.AddPostgresVectorStore(connString);
builder.Services.AddHostedService<ListopiaParserRunner>();

var host = builder.Build();
await host.RunAsync();