using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using WebBaiGiangAPI.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebBaiGiangAPI.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerFileOperationFilter>();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WEB BAI GIANG API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer YOUR_ACCESS_TOKEN"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Lỗi xác thực JWT: " + context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Token chỉ có hiệu lực trong 30 phút
    options.SlidingExpiration = false;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/auth/logout-google");
            return Task.CompletedTask;
        },
        OnRedirectToLogin = context =>
        {
            context.Response.Redirect("/auth/logout-google");
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = "82468203109-vve147pek30kr98ma6gh0d3qv4l945n8.apps.googleusercontent.com";
    options.ClientSecret = "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";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.SaveTokens = true; // Quan trọng! Lưu token để lấy sau này.
})
.AddOAuth("GitHub", options =>
{
    options.ClientId = "Ov23liuHEt1qMiYVQoVH";
    options.ClientSecret = "c484f41075592eff2efd0952ac2df0a546b2e93a";
    options.CallbackPath = new PathString("/signin-github");
    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    options.UserInformationEndpoint = "https://api.github.com/user";
    options.SaveTokens = true;
});
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; // 100MB
});
builder.Services.AddHttpClient<ZoomService>();

//builder.Services.AddHostedService<AttendanceReportService>();
//builder.Services.AddHostedService<AbsenceLateWarningService>();
var app = builder.Build(); 
app.UseCors("AllowAll");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi seed dữ liệu: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.ConfigObject.AdditionalItems["https"] = true;
    });
}
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();

app.Run();
