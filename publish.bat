@echo off
chcp 65001 >nul
title 背水对战平台 - 发布打包

echo ========================================
echo   背水对战平台 v1.0.0 - 发布打包
echo ========================================
echo.

:: 清理旧发布
echo [1/3] 清理旧发布...
if exist "publish" rmdir /s /q "publish"

:: 编译发布
echo [2/3] 编译发布单文件...
dotnet publish BeiShuiCS2.csproj -c Release -o publish

echo.
echo [3/3] ✅ 发布完成！
echo.
echo 输出目录: publish\
echo 主程序: publish\BeiShuiCS2.exe
echo.
echo ⚠ 反编译保护说明:
echo   1. 此版本已启用 SuppressIldasm 保护
echo   2. 如需更强混淆, 下载 ConfuserEx:
echo      https://github.com/yck1509/ConfuserEx/releases
echo   3. 将 publish\BeiShuiCS2.exe 拖入 ConfuserEx 加壳
echo.
echo 运行: 直接运行 publish\BeiShuiCS2.exe
echo.
pause
