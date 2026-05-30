using System.Text;
using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Hubs;
using BeiShui.ApiGateway.Middleware;
using BeiShui.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// === 数据库 ===
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(builder.Configuration.GetConnectionString("Default"),
        new MySqlServerVersion(new Version(8, 0, 0))));

// === Redis ===
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"] ?? "localhost:6379"));

builder.Services.AddSingleton<UserConnectionManager>();
builder.Services.AddSingleton<ServerManagerService>();
builder.Services.AddSingleton<QuickMatchHandler>();
builder.Services.AddHostedService<DuelMatchHandler>();
builder.Services.AddHostedService<VoiceCleanupService>();

// === JWT 认证 ===
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BeiShuiDefaultJwtKeyForDevelopment_ChangeInProduction!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BeiShui";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BeiShuiClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// === SignalR ===
builder.Services.AddSignalR();

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// === Health Check ===
builder.Services.AddHealthChecks();

// === Controllers ===
builder.Services.AddControllers();

var app = builder.Build();

// === Middleware ===
app.UseCors();
app.UseMiddleware<RateLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// === Map Hubs ===
app.MapHub<MatchHub>("/hubs/match");
app.MapHub<GameHub>("/hubs/game");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<CallHub>("/callhub");
app.MapHub<BroadcastHub>("/hubs/broadcast");

app.Run();
