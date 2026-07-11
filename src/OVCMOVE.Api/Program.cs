using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

using OVCMOVE.Api.Extensions;
using OVCMOVE.Application;
using OVCMOVE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Local"))
{
    const string envFilePath = "./.env";
    if (File.Exists(envFilePath))
    {
        Env.Load(envFilePath);
    }
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddEnvironmentVariables(prefix: "OVCMOVE_");

builder.Services.AddMapping();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true, 
            ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
            ValidAudience = builder.Configuration["JwtConfig:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:SecretKey"] ?? throw new InvalidOperationException("Thiếu SecretKey"))),

            RoleClaimType = "role"
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://move.oispvolunteerclub.com"
              )
              .AllowAnyHeader()   
              .AllowAnyMethod()   
              .AllowCredentials();
    });
});
    
var app = builder.Build();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();   
app.UseAuthorization();

app.MapControllers();

app.Run();
