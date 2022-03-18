using System.Net;
using MessageRetrieverAPI.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EmployeeContext>(options =>
    options.UseSqlite(builder.Configuration["SQLiteConnectionString"]));

builder.Services.AddDbContext<OrderContext>(options =>
    options.ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
           .UseCosmos(builder.Configuration["CosmosDBConnectionString"], databaseName: "OrderDatabase",
            options =>
            {
                options.ConnectionMode(ConnectionMode.Direct);
                options.WebProxy(new WebProxy());
            }));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services?.GetService<IServiceScopeFactory>()?.CreateAsyncScope();
    
    var cosmosDBOptions = scope?.ServiceProvider.GetRequiredService<DbContextOptions<OrderContext>>();
    await DataContextUtility.EnsureDbCreatedAndSeedAsync<OrderContext>(cosmosDBOptions);

    var sqlliteOptions = scope?.ServiceProvider.GetRequiredService<DbContextOptions<EmployeeContext>>();
    await DataContextUtility.EnsureDbCreatedAndSeedAsync<EmployeeContext>(sqlliteOptions);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
