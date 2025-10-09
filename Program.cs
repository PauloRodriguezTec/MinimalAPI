using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalAPI.Dominio.DTO;
using MinimalAPI.Dominio.Entidades;
using MinimalAPI.Dominio.Enums;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.Dominio.Serviços;
using MinimalAPI.Infraestrutura.Db;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key)) 
{
    key = "123456";
}

builder.Services.AddAuthentication(option =>
    {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
        });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServiço, AdministradorServiço>();
builder.Services.AddScoped<IVeiculosServiço, VeiculoServiço>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options =>
    {
        options.UseMySql(
            builder.Configuration.GetConnectionString("MySql"),
            ServerVersion.AutoDetect(
                builder.Configuration.GetConnectionString("MySql")));
    }
);

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administradores

string GerarTokenJwt(Administrador administrador)
{
    if (string.IsNullOrEmpty(key))
    {
        return string.Empty;
    }
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil) 
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );
    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServiço administradorServiço) =>
{
    var adm = administradorServiço.Login(loginDTO);
    if (adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
    {
        return Results.Unauthorized();
    }
}).WithTags("Administrador");

app.MapGet("/administrador", ([FromQuery] int? pagina, IAdministradorServiço administradorServiço) =>
{
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServiço.Todos(pagina);
    foreach (var adm in administradores)
    {
        adms.Add(new AdministradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = (Perfil)Enum.Parse(typeof(Perfil), adm.Perfil)
        });
    }
    return Results.Ok(adms);
}).RequireAuthorization().WithTags("Administrador");

app.MapGet("/administrador/{id}", ([FromRoute] int id, IAdministradorServiço administradorServiço) =>
{
    var administrador = administradorServiço.BuscaPorId(id);

    if (administrador == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new AdministradorModelView
        {
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = (Perfil)Enum.Parse(typeof(Perfil), administrador.Perfil)
        });
}).RequireAuthorization().WithTags("Administrador");

app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServiço administradorServiço) =>
{
    var validaçao = new ErrosDeValidaçao
    {
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(administradorDTO.Email))
    {
        validaçao.Mensagens.Add("O campo email não pode ser vazio.");
    }
    if (string.IsNullOrEmpty(administradorDTO.Senha))
    {
        validaçao.Mensagens.Add("O campo senha não pode ser vazio.");
    }
    if (administradorDTO.Perfil == null)
    {
        validaçao.Mensagens.Add("O campo perfil não pode ser vazio.");
    }

    if (validaçao.Mensagens.Count > 0)
    {
        return Results.BadRequest(validaçao);
    }
        var adm = new Administrador
        {
            Email = administradorDTO.Email,
            Senha = administradorDTO.Senha,
            Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
        };

    administradorServiço.Incluir(adm);
    return Results.Created($"/administrador/{adm.Id}",new AdministradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = (Perfil)Enum.Parse(typeof(Perfil), adm.Perfil)
        });
         
}).RequireAuthorization().WithTags("Administrador");
#endregion

#region Veiculos

ErrosDeValidaçao ValidaDTO(VeiculoDTO veiculoDTO)
{
    var validaçao = new ErrosDeValidaçao
    {
        Mensagens = new List<string>()
    };

    if (string.IsNullOrWhiteSpace(veiculoDTO.Nome))
    {
        validaçao.Mensagens.Add("O nome não pode ser vazio.");
    }
    if (string.IsNullOrWhiteSpace(veiculoDTO.Marca))
    {
        validaçao.Mensagens.Add("A marca não pode ficar em branco.");
    }
    if (veiculoDTO.Ano < 1950)
    {
        validaçao.Mensagens.Add("Não aceitamos carros anteriores a 1950.");
    }
    return validaçao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculosServiço administradorServiço) =>
{
    var validaçao = ValidaDTO(veiculoDTO);

    if (validaçao.Mensagens.Count > 0)
    {
        return Results.BadRequest(validaçao);
    }

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };
    administradorServiço.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

}).RequireAuthorization().WithTags("Veiculo");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculosServiço administradorServiço) =>
{
    var veiculos = administradorServiço.Todos(pagina);

    return Results.Ok(veiculos);
}).RequireAuthorization().WithTags("Veiculo");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculosServiço administradorServiço) =>
{
    var veiculo = administradorServiço.BuscaPorId(id);

    if (veiculo == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculo");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculosServiço administradorServiço) =>
{
    var veiculo = administradorServiço.BuscaPorId(id);
    if (veiculo == null)
    {
        return Results.NotFound();
    }

    var validaçao = ValidaDTO(veiculoDTO);
    if (validaçao.Mensagens.Count > 0)
    {
        return Results.BadRequest(validaçao);
    }

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    administradorServiço.Atualizar(veiculo);

    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculo");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculosServiço administradorServiço) =>
{
    var veiculo = administradorServiço.BuscaPorId(id);

    if (veiculo == null)
    {
        return Results.NotFound();
    }

    administradorServiço.Excluir(veiculo);

    return Results.NoContent();
}).RequireAuthorization().WithTags("Veiculo");
#endregion

#region app
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion


