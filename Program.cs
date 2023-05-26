using LTD.Data.LTDLive;
using Microsoft.EntityFrameworkCore;
using PriceMatrixApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<LTDLiveContext>(opts =>
    opts.UseInMemoryDatabase("PM")
);
builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(c =>
{
    c.AllowAnyOrigin();
});

app.MapControllers();

app.MapPost("/seed", async (DataSeeder seeder) => await seeder.Seed());

app.Run();
