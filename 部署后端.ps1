# ═══════════════════════════════════════════════════════════════
# 背水对战平台 - 服务端后端部署脚本
# 在服务器远程桌面里以 PowerShell(管理员) 运行
# ═══════════════════════════════════════════════════════════════

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  背水对战平台 - 批量环境部署" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ─── 1. 检查 MySQL ───
Write-Host "[1/6] 检查 MySQL 服务..." -ForegroundColor Yellow
$mysqlService = Get-Service "MySQL*" -ErrorAction SilentlyContinue
if (-not $mysqlService) {
    Write-Host "  MySQL 未安装，请先手动安装" -ForegroundColor Red
    Write-Host "  下载: https://dev.mysql.com/downloads/installer/" -ForegroundColor Red
} elseif ($mysqlService.Status -ne "Running") {
    net start MySQL
    Start-Sleep 2
    Write-Host "  MySQL 已启动" -ForegroundColor Green
} else {
    Write-Host "  MySQL 运行中" -ForegroundColor Green
}

# ─── 2. 检查 .NET ───
Write-Host "[2/6] 检查 .NET Runtime..." -ForegroundColor Yellow
$dotnetVer = dotnet --version 2>$null
if ($dotnetVer -match "^\d+\.\d+") {
    Write-Host "  .NET $dotnetVer 已安装" -ForegroundColor Green
} else {
    Write-Host "  .NET 未安装，请下载安装:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime" -ForegroundColor Red
}

# ─── 3. 初始化数据库 ───
Write-Host "[3/6] 初始化数据库..." -ForegroundColor Yellow
$mysqlBin = "C:\Program Files\MySQL\MySQL Server 8.0\bin"
if (Test-Path "$mysqlBin\mysql.exe") {
    & "$mysqlBin\mysql.exe" -u root -pBeishui@2026 -e "CREATE DATABASE IF NOT EXISTS beishui CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  数据库 beishui 就绪" -ForegroundColor Green
    } else {
        Write-Host "  数据库创建失败，请检查密码" -ForegroundColor Red
    }
}

# ─── 4. 防火墙端口 ───
Write-Host "[4/6] 配置防火墙端口..." -ForegroundColor Yellow
$ports = @(
    @{Port=22;  Proto="TCP"; Name="SSH"},
    @{Port=80;  Proto="TCP"; Name="HTTP"},
    @{Port=443; Proto="TCP"; Name="HTTPS"},
    @{Port=5000; Proto="TCP"; Name="API"},
    @{Port=5001; Proto="TCP"; Name="API-SSL"},
    @{Port=3478; Proto="UDP"; Name="TURN"},
    @{Port=3478; Proto="TCP"; Name="TURN-TCP"}
)
$count = 0
foreach ($p in $ports) {
    $ruleName = "BS-$($p.Name)"
    $existing = netsh advfirewall firewall show rule name="$ruleName" 2>$null
    if ($existing -match $ruleName) {
        Write-Host "  $ruleName 已存在，跳过" -ForegroundColor DarkGray
    } else {
        netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=$($p.Proto) localport=$($p.Port) | Out-Null
        $count++
    }
}
if ($count -gt 0) {
    netsh advfirewall firewall add rule name="BS-RELAY" dir=in action=allow protocol=UDP localport=49152-65535 | Out-Null
}
Write-Host "  端口配置完成（新增 $count 条规则）" -ForegroundColor Green

# ─── 5. 项目目录 ───
Write-Host "[5/6] 创建项目目录..." -ForegroundColor Yellow
$dirs = @("C:\beishui\api", "C:\beishui\database", "C:\beishui\logs", "C:\beishui\turn", "C:\beishui\backup")
foreach ($d in $dirs) {
    if (-not (Test-Path $d)) {
        New-Item -ItemType Directory -Path $d -Force | Out-Null
        Write-Host "  创建 $d" -ForegroundColor Gray
    }
}
Write-Host "  目录就绪" -ForegroundColor Green

# ─── 6. 诊断输出 ───
Write-Host "[6/6] 环境诊断..." -ForegroundColor Yellow
Write-Host ""
Write-Host "  ┌─────────────────────────────────────────┐" -ForegroundColor Cyan
Write-Host "  │           环境诊断报告                    │" -ForegroundColor Cyan
Write-Host "  ├─────────────────────────────────────────┤" -ForegroundColor Cyan

# MySQL
if (Test-Path "$mysqlBin\mysql.exe") {
    $mysqlStatus = "✅ $(& "$mysqlBin\mysql.exe" -V 2>$null)"
} else { $mysqlStatus = "❌ 未安装" }
Write-Host "  │ MySQL:    $($mysqlStatus.PadRight(35))│" -ForegroundColor White

# .NET
if ($dotnetVer) { $dotnetStatus = "✅ $dotnetVer" } else { $dotnetStatus = "❌ 未安装" }
Write-Host "  │ .NET:     $($dotnetStatus.PadRight(35))│" -ForegroundColor White

# 防火墙
$fwCount = (netsh advfirewall firewall show rule name="BS-*" 2>$null | Select-String "Rule Name" | Measure-Object).Count
Write-Host "  │ 防火墙规则: $($fwCount.ToString().PadRight(10))条                            │" -ForegroundColor White

# 目录
$dirOk = (Test-Path "C:\beishui\api") -and (Test-Path "C:\beishui\logs")
Write-Host "  │ 项目目录:  $('✅ 就绪' -or '❌ 缺失')                               │" -ForegroundColor White

Write-Host "  └─────────────────────────────────────────┘" -ForegroundColor Cyan
Write-Host ""

# ─── 生成连接配置 ───
$config = @"
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=beishui;User=root;Password=Beishui@2026;"
  },
  "Urls": "http://0.0.0.0:5000",
  "Jwt": {
    "Key": "BeishuiSecretKey2026_Dev_ChangeInProduction",
    "Issuer": "BeishuiCS2",
    "Audience": "BeishuiClient"
  },
  "TurnServer": {
    "Url": "turn:hni1.wch1.top:3478",
    "Username": "beishui",
    "Credential": "Beishui@2026"
  }
}
"@
$config | Out-File -FilePath "C:\beishui\api\appsettings.json" -Encoding utf8

Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ 环境部署完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📦 数据库连接:" -ForegroundColor Cyan
Write-Host "  Server: localhost:3306" -ForegroundColor White
Write-Host "  User: root / Password: Beishui@2026 / DB: beishui" -ForegroundColor White
Write-Host ""
Write-Host "📂 项目目录: C:\beishui\" -ForegroundColor Cyan
Write-Host "  ├── api/        - 后端程序" -ForegroundColor Gray
Write-Host "  ├── database/   - 数据库备份" -ForegroundColor Gray
Write-Host "  ├── logs/       - 日志文件" -ForegroundColor Gray
Write-Host "  ├── turn/       - TURN 配置" -ForegroundColor Gray
Write-Host "  └── backup/     - 数据备份" -ForegroundColor Gray
Write-Host ""
Write-Host "🔌 开放端口:" -ForegroundColor Cyan
Write-Host "  22(SSH) 80(HTTP) 443(HTTPS) 5000(API) 3478(TURN)" -ForegroundColor White
Write-Host ""
Write-Host "📋 部署后端:" -ForegroundColor Magenta
Write-Host "  将编译好的后端程序复制到 C:\beishui\api\" -ForegroundColor White
Write-Host "  然后在 C:\beishui\api\ 运行:" -ForegroundColor White
Write-Host "  dotnet BeishuiServer.dll" -ForegroundColor Yellow
Write-Host "  dotnet BeishuiServer.dll --urls http://0.0.0.0:5000" -ForegroundColor Yellow
