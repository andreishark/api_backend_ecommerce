using api_service.ApiConfiguration;
using DatabaseLibrary;
using Microsoft.Extensions.FileProviders;
using MongoDatabaseLibrary;
using MongoDatabaseLibrary.Factories;
using MongoDatabaseLibrary.Repositories;

var builder = WebApplication.CreateBuilder ( args );

// Add services to the container.

builder.Services.AddControllers ( )
    .AddNewtonsoftJson ( );
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer ( );
builder.Services.AddSwaggerGen ( );

builder.Services.AddSingleton<IMongoDbFactory> ( new CustomMongoDbFactory (
    builder.Configuration.GetSection (
        nameof ( MongoDbSettings )
        )
        .Get<MongoDbSettings> ( )
    ) );
builder.Services.AddSingleton<IApiConfiguration> ( new ApiConfiguration (
    builder.Configuration.GetSection (
        SettingsMap.Path
    )
    .Get<SettingsMap> ( )
) );
builder.Services.AddSingleton<ICatalogItemRepository, MongoDbCatalogItemRepository> ( );

var app = builder.Build ( );

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment ( ) )
{
    app.UseSwagger ( );
    app.UseSwaggerUI ( );
}

app.UseFileServer ( new FileServerOptions
{
    FileProvider = new PhysicalFileProvider (
        Path.Combine ( Directory.GetCurrentDirectory ( ), "Static" )
    ),
    RequestPath = "/static",
    EnableDefaultFiles = true
} );

app.UseHttpsRedirection ( );

app.UseAuthorization ( );

app.MapControllers ( );

app.Run ( );
