using BeiShui.MatchService;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"] ?? "localhost:6379"));

builder.Services.AddSingleton<MatchMaker>();
builder.Services.AddHostedService<MatchWorker>();

var app = builder.Build();
app.Run();
