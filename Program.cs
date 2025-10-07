using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.DTO;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.Serviços;
using MinimalAPI.Infraestrutura.Db;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServiço, AdministradorServiço>();


builder.Services.AddDbContext<DbContexto>(options =>
    {
        options.UseMySql(
            builder.Configuration.GetConnectionString("mysql"),
            ServerVersion.AutoDetect(
                builder.Configuration.GetConnectionString("mysql")));
    }
);

var app = builder.Build();

//app.MapGet("/", () => "Hello World!");

app.MapPost("/login", ([FromBody]LoginDTO loginDTO, IAdministradorServiço administradorServiço) =>
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

app.Run();


