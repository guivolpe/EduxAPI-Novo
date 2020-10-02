﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EduxAPI.Contexts;
using EduxAPI.Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EduxAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        EduxContext _contexto = new EduxContext();

        // Definindo uma variável para transcorrer os métodos com as configs obtidas no appsettings.json
        private IConfiguration _config;

        // Definiçaõ do método construtor para podermos passar essas configs
        public LoginController(IConfiguration config)
        {
            _config = config;
        }


        private Usuario AuthenticateUser(Usuario login)
        {
            return _contexto.Usuario.Include(a => a.IdPerfilNavigation).FirstOrDefault(u => u.Email == login.Email && u.Senha == login.Senha);
        }

        // Criamos nosso método que vai gerar nosso Token
        private string GenerateJSONWebToken(Usuario userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Definimos nossas Claims (dados da sessão) para poderem ser capturadas
            // a qualquer momento enquanto o Token for ativo
            var claims = new[] {
        new Claim(JwtRegisteredClaimNames.NameId, userInfo.Nome),
        new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, userInfo.IdPerfilNavigation.Permissao)
    };

            // Configuramos nosso Token e seu tempo de vida
            var token = new JwtSecurityToken
                (
                    _config["Jwt:Issuer"],
                    _config["Jwt:Issuer"],
                    claims,
                    expires: DateTime.Now.AddMinutes(120),
                    signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Usamos a anotação "AllowAnonymous"
        //  para ignorar a autenticação neste método,
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] Usuario login)
        {
            // Definimos logo de cara como não autorizado
            IActionResult response = Unauthorized();

            // Autenticamos o usuário da API
            var user = AuthenticateUser(login);
            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }
    }
}