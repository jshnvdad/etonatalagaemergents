using System.Text;
using System.Text.Json.Serialization;
using EmergencyDesk.Server.Data;
using EmergencyDesk.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (MySQL) ----
var conn = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(conn, ServerVersion.AutoDetect(conn)));

// ---- Auth (JWT) ----
var jwtKey = builder.Configuration["Jwt:Key"]!;
var issuer = builder.Configuration["Jwt:Issuer"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = issuer,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>();

// Enums travel as text ("Pending", "EnRoute") so both apps stay readable.
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- Create database/tables on first run and seed the first admin ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Staff.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
        var cfg = app.Configuration.GetSection("SeedAdmin");
        db.Staff.Add(new Staff
        {
            Username = cfg["Username"] ?? "admin",
            PasswordHash = hasher.HashPassword(new object(), cfg["Password"] ?? "Admin123!"),
            DisplayName = cfg["DisplayName"] ?? "System Admin",
            Role = "Admin"
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Emergency server is running. API docs at /swagger");

app.Run();
