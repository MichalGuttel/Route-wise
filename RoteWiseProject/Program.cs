using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using RoteWiseProject.Services;
using RoteWiseProject.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHttpClient<AuthenticationService>();
builder.Services.AddSingleton<AuthenticationService>();
//builder.Services.AddScoped<AuthenticationService>();


builder.Services.AddHttpClient<FlightService>();
builder.Services.AddScoped<FlightService>();


builder.Services.AddHttpClient<HotelService>();
builder.Services.AddScoped<HotelService>();


//builder.Services.AddHttpClient<AuthenticationService>();


//builder.Services.AddSingleton<Neo4jService>();
builder.Services.AddScoped<Neo4jService>();


builder.Services.AddScoped<DijkstraAlgorithm>();
//builder.Services.AddSingleton<DijkstraAlgorithm>();


// Register IConnectionSettings with the appropriate settings
builder.Services.AddSingleton<IConnectionSettings>(sp =>
    ConnectionSettings.CreateBasicAuth("bolt://localhost:7687/db/RouteGraph", "neo4j", "12345678"));


builder.Services.AddCors(option =>
{
    option.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    }
    );
});


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

app.UseCors("AllowAll");

app.Run();