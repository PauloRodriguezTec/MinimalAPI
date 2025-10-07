using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalAPI.Dominio.DTO;
using MinimalAPI.Dominio.Entidades;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Infraestrutura.Db;

namespace MinimalAPI.Dominio.Serviços
{
    public class AdministradorServiço : IAdministradorServiço
    {
        private readonly DbContexto _contexto;
        public AdministradorServiço(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            var adm = _contexto.Administradores.Where
            (a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
            return adm;
            
        }
    }
}