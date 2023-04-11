using System.Net;
using System.Text;
using api_service.ApiConfiguration;
using api_service.Utility;
using DatabaseLibrary;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using MongoDatabaseLibrary;
using MongoDatabaseLibrary.Factories;
using MongoDatabaseLibrary.Repositories;

var builder = WebApplication.CreateBuilder(args);
const string staticPath = "Static";

// Add services to the container.

builder.Services.AddControllers()
    .AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMongoDbFactory>(new CustomMongoDbFactory(
    builder.Configuration.GetSection(
            nameof(MongoDbSettings)
        )
        .Get<MongoDbSettings>()!
));
builder.Services.AddSingleton<IApiConfiguration>(new ApiConfiguration(
    builder.Configuration.GetSection(
            SettingsMap.Path
        )
        .Get<SettingsMap>()!
));
builder.Services.AddSingleton<ICatalogItemRepository, MongoDbCatalogItemRepository>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), staticPath)))
{
    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), staticPath));
}

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), staticPath)
    ),
    RequestPath = "/static",
    EnableDefaultFiles = true
});


// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();