using System.Data.SqlClient;
using System.Text;
using Integrations.Data.Entities;
using Services.Hubs;
using Services.Stores;
using Services.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Integrations.WhisperService;
using Services.RecordingService;
using Services.AuthService;
using Services.LobbyService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Sql context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<AIScoringService>();
builder.Services.AddSingleton<WhisperService>();

// SignalR configuration
builder.Services.AddSignalR();

builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddSingleton<ISongStore, SongStore>();

builder.Services.AddScoped<IRecordingService, RecordingService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<ILobbyService, LobbyService>();

// JWT konfigūracija
var key = Encoding.ASCII.GetBytes("tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

await SongStore.InitializeAsync();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<LobbyHub>("/lobbyHub");
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();