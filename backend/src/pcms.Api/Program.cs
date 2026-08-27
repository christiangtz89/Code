using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using pcms.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using pcms.Api.Middleware;
using pcms.Application.Auth;

var builder = WebApplication.CreateBuilder(args);


// Controllers

builder.Services.AddControllers();
builder.Services.AddInfrastructure(
    builder.Configuration);

// Swagger

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter JWT token"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

// JWT Authentication

// JWT Authentication

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        ))
            };
    });

var permissionCodes = PermissionCodes.All;
builder.Services.AddAuthorization(options =>
{
    foreach (var code in permissionCodes)
    {
        var impliedManageCode = code.EndsWith(".View", StringComparison.Ordinal)
            ? code[..^5] + ".Manage"
            : null;

        options.AddPolicy(code, policy => policy.RequireAssertion(context =>
            context.User.HasClaim("permission", code) ||
            (impliedManageCode is not null && context.User.HasClaim("permission", impliedManageCode))));
    }
});

builder.Services.AddCors(options =>
{
options.AddPolicy("Frontend", policy =>
{
    policy
        .WithOrigins("http://localhost:5173", "http://192.168.1.15:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
});
});


var app = builder.Build();

//Swagger


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.UseStaticFiles();

//Middleware

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


app.Run();
