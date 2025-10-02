using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.DTO;
using MinimalAPI.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<DbContexto>(options =>
    {
        options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"))))
    }
);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/login", (LoginDTO loginDTO) =>
{
    if (loginDTO.Email == "adm@teste.com" && loginDTO.Senha == "123456")
    {
        return Results.Ok("Login com sucesso.");
    }
    else
    {
        return Results.Unauthorized();
    }
});

app.Run();


