using BeiShui.AntiCheat;

var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services.AddControllers();
builder.Services.AddSingleton<AntiCheatStore>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.MapControllers();

Console.WriteLine($"[AntiCheat API] 启动 | 端口: {app.Urls.FirstOrDefault() ?? "5000"}");
Console.WriteLine($"[AntiCheat API] 数据目录: {AppDomain.CurrentDomain.BaseDirectory}data/");
Console.WriteLine($"[AntiCheat API] 零数据库 | 纯 JSON 存储");

app.Run();
