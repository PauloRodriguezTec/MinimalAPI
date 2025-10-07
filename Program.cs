using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.DTO;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.Dominio.Serviços;
using MinimalAPI.Infraestrutura.Db;
using MySqlX.XDevAPI.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServiço, AdministradorServiço>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options =>
    {
        options.UseMySql(
            builder.Configuration.GetConnectionString("mysql"),
            ServerVersion.AutoDetect(
                builder.Configuration.GetConnectionString("mysql")));
    }
);

var app = builder.Build();


app.MapGet("/", () => Results.Json(new Home()));

app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IAdministradorServiço administradorServiço) =>
{
    if (administradorServiço.Login(loginDTO) != null)
    {
        return Results.Ok("Login com sucesso.");
    }
    else
    {
        return Results.Unauthorized();
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();


