using System.Text;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OKPBackend.Data;
using OKPBackend.Mappings;
using OKPBackend.Models.Domain;
using OKPBackend.Repositories.Users;
using OKPBackend.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Information().CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OKPBackend", Version = "v1" });
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                },
                Scheme = "Oauth2",
                Name = JwtBearerDefaults.AuthenticationScheme,
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<OKPDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("OKPConnectionString")));

// if (builder.Environment.IsProduction())
// {
//     builder.Services.AddDbContext<OKPDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("OKPConnectionString")));
// }


// if (builder.Environment.IsProduction())
// {
//     var keyVaultURL = builder.Configuration.GetSection("KeyVault:KeyVaultURL");
//     var keyVaultClientId = builder.Configuration.GetSection("KeyVault:ClientId");
//     var keyVaultClientSecret = builder.Configuration.GetSection("KeyVault:ClientSecret");
//     var keyVaultDirectoryID = builder.Configuration.GetSection("KeyVault:DirectoryID");

//     var credential = new ClientSecretCredential(keyVaultDirectoryID.Value!.ToString(), keyVaultClientId.Value!.ToString(), keyVaultClientSecret.Value!.ToString());
//     builder.Configuration.AddAzureKeyVault(keyVaultURL.Value!.ToString(), keyVaultClientId.Value!.ToString(), keyVaultClientSecret.Value!.ToString(), new DefaultKeyVaultSecretManager());

//     var client = new SecretClient(new Uri(keyVaultURL.Value!.ToString()), credential);

//     builder.Services.AddDbContext<OKPDbContext>(options =>
//     {
//         options.UseSqlServer(client.GetSecret("ProdConnection").Value.Value.ToString());
//     });

// };

// DotNetEnv.Env.Load();
// string? db_password = Environment.GetEnvironmentVariable("db_password");

// string? connectionString = builder.Configuration.GetConnectionString("OKPConnectionString");
// connectionString = connectionString.Replace("{DatabasePassword}", db_password ?? "");


builder.Services.AddCors(x => x.AddPolicy("corspolicy", build =>
{
    build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

// builder.Services.AddDbContext<OKPDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("OKPConnectionString")));

builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));


//Repositories
builder.Services.AddScoped<IUsersRepository, SQLUsersRepository>();

//Services
builder.Services.AddScoped<EmailService>();



//AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // DotNetEnv.Env.Load();
    // string jwt_key = Environment.GetEnvironmentVariable("jwt_key");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        //builder.Configuration["Jwt:Key"]
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("36c472de-f62d-4f2a-b009-cf24bbb4d8cf"))

    };
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddTokenProvider<DataProtectorTokenProvider<User>>("OKPBackend").AddEntityFrameworkStores<OKPDbContext>().AddDefaultTokenProviders();

// Password requirements
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true; // Allow passwords without a non-alphanumeric character
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseCors("corspolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
