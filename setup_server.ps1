# ═══════════════════════════════════════
# 背水对战平台 - Windows Server 一键部署脚本
# ═══════════════════════════════════════

Write-Host "=== 开始部署背水对战平台服务端 ===" -ForegroundColor Green

# 1. 安装 PostgreSQL
Write-Host "[1/5] 安装 PostgreSQL 16..." -ForegroundColor Yellow
$pgInstaller = "$env:TEMP\postgresql-installer.exe"
Invoke-WebRequest -Uri "https://get.enterprisedb.com/postgresql/postgresql-16.6-1-windows-x64.exe" -OutFile $pgInstaller
Start-Process -Wait -FilePath $pgInstaller -ArgumentList "--unattendedmodeui minimal --mode unattended --superpassword Beishui@2026 --servicename PostgreSQL --serverport 5432"
Write-Host "  PostgreSQL 安装完成" -ForegroundColor Green

# 2. 安装 .NET 9.0 运行时
Write-Host "[2/5] 安装 .NET 9.0 Runtime..." -ForegroundColor Yellow
$dotnetInstaller = "$env:TEMP\dotnet-installer.exe"
Invoke-WebRequest -Uri "https://builds.dotnet.microsoft.com/dotnet/Runtime/9.0.4/dotnet-runtime-9.0.4-win-x64.exe" -OutFile $dotnetInstaller
Start-Process -Wait -FilePath $dotnetInstaller -ArgumentList "/install /quiet /norestart"
Write-Host "  .NET 9.0 安装完成" -ForegroundColor Green
$env:Path = [Environment]::GetEnvironmentVariable("Path", "Machine")

# 3. 创建数据库
Write-Host "[3/5] 初始化数据库..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
$pgBin = "C:\Program Files\PostgreSQL\16\bin"
if (Test-Path "$pgBin\psql.exe") {
    & "$pgBin\psql.exe" -U postgres -c "CREATE DATABASE beishui;" 2>$null
    Write-Host "  数据库 beishui 创建完成" -ForegroundColor Green
} else {
    Write-Host "  PostgreSQL 路径未找到，请手动创建数据库" -ForegroundColor Red
}

# 4. 防火墙开放端口
Write-Host "[4/5] 配置防火墙..." -ForegroundColor Yellow
$ports = @(
    @{Port=22; Protocol="TCP"; Name="SSH"},
    @{Port=80; Protocol="TCP"; Name="HTTP"},
    @{Port=443; Protocol="TCP"; Name="HTTPS"},
    @{Port=5000; Protocol="TCP"; Name="Beishui-API"},
    @{Port=5001; Protocol="TCP"; Name="Beishui-API-SSL"},
    @{Port=8080; Protocol="TCP"; Name="Beishui-Alt"},
    @{Port=3478; Protocol="UDP"; Name="TURN"},
    @{Port=5349; Protocol="TCP"; Name="TURN-SSL"}
)
foreach ($p in $ports) {
    New-NetFirewallRule -DisplayName $p.Name -Direction Inbound -Protocol $p.Protocol -LocalPort $p.Port -Action Allow -ErrorAction SilentlyContinue | Out-Null
}
Write-Host "  防火墙端口配置完成" -ForegroundColor Green

# 5. 创建项目目录
Write-Host "[5/5] 创建项目目录..." -ForegroundColor Yellow
$dirs = @(
    "C:\beishui\api",
    "C:\beishui\database",
    "C:\beishui\logs",
    "C:\beishui\turn"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Path $d -Force | Out-Null
}
Write-Host "  项目目录创建完成" -ForegroundColor Green

# 完成
Write-Host ""
Write-Host "══════════════════════════════════════" -ForegroundColor Green
Write-Host "✅ 部署完成！" -ForegroundColor Green
Write-Host "══════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "数据库连接信息:" -ForegroundColor Cyan
Write-Host "  Host: localhost" -ForegroundColor White
Write-Host "  Port: 5432" -ForegroundColor White
Write-Host "  User: postgres" -ForegroundColor White
Write-Host "  Pass: Beishui@2026" -ForegroundColor White
Write-Host "  DB:   beishui" -ForegroundColor White
Write-Host ""
Write-Host "项目目录: C:\beishui\" -ForegroundColor Cyan
Write-Host ""
Write-Host "检查版本:" -ForegroundColor Yellow
dotnet --version 2>$null
if ($?) { Write-Host "  .NET: $(dotnet --version)" -ForegroundColor Green }
else { Write-Host "  .NET: 未安装" -ForegroundColor Red }
Write-Host ""
Write-Host "下一步：部署后端 API" -ForegroundColor Magenta
