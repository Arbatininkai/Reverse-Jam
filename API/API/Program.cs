using System.Text;
using Integrations.Data.Entities;
using Services.Hubs;
using Services.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Integrations.WhisperService;
using Services.RecordingService;
using Services.AuthService;
using Services.LobbyService;
using Services.SongService;
using Services.AiScoringService;

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAIScoringService, AIScoringService>();
builder.Services.AddSingleton<WhisperService>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddSingleton<ISongStore, SongStore>();

builder.Services.AddScoped<IRecordingService, RecordingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<ISongService, SongService>();

// JWT configuration
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

var servicesRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "Services");
var recordingsFolder = Path.Combine(servicesRoot, "recordings");
Directory.CreateDirectory(recordingsFolder);

// Serve recordings as static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(recordingsFolder),
    RequestPath = "/Services/recordings"
});

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
