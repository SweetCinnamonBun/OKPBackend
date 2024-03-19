using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OKPBackend.Data;
using OKPBackend.Models.Domain;
using OKPBackend.Models.DTO.Users;

namespace OKPBackend.Repositories.Users
{
    public class SQLUsersRepository : IUsersRepository
    {
        private readonly OKPDbContext dbContext;
        private readonly IConfiguration configuration;

        public SQLUsersRepository(OKPDbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        public string CreateJWTToken(User user)
        {
            // DotNetEnv.Env.Load();
            // string? jwt_key = Environment.GetEnvironmentVariable("jwt_key");
            //Create claims

            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim("Id", user.Id));
            claims.Add(new Claim("capitalized", user.NormalizedUserName));


            // foreach (var role in roles)
            // {
            //     claims.Add(new Claim(ClaimTypes.Role, role));
            // }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("36c472de-f62d-4f2a-b009-cf24bbb4d8cf"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: DateTime.Now.AddMinutes(45), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        // public async Task<User?> CreateAsync(UserRegisterDto userRegisterDto)
        // {
        //     string passwordHash = BCrypt.Net.BCrypt.HashPassword(userRegisterDto.Password);

        //     var newUser = new User()
        //     {
        //         Username = userRegisterDto.Username,
        //         Email = userRegisterDto.Email,
        //         PasswordHash = passwordHash,
        //         Role = "User"
        //     };

        //     await dbContext.Users.AddAsync(newUser);
        //     await dbContext.SaveChangesAsync();
        //     return newUser;
        // }

        // public string CreateJWTToken(User user, List<string> roles)
        // {
        //     throw new NotImplementedException();
        // }

        // public async Task<User?> GetByIdAsync(Guid id)
        // {
        //     var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        //     if (user == null)
        //     {
        //         return null;
        //     }

        //     return user;
        // }

        // public async Task<User?> GetByUsername(UserLoginDto userLoginDto)
        // {
        //     var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == userLoginDto.Username.ToLower());

        //     if (user == null)
        //     {
        //         return null;
        //     }

        //     return user;
        // }
    }
}