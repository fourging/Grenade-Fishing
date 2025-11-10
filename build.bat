@echo off
:: RadialMenu Mod 构建脚本（仅编译，不复制）

echo === 开始构建 RadialMenu Mod ===

:: 进入项目目录
cd /d "D:\CODE\DUCKOVMODS\GRENADEFISHING"

:: 编译项目（Release 模式）
dotnet build --configuration Release

:: 检查构建是否成功
if %ERRORLEVEL% neq 0 (
    echo 构建失败！
    exit /b 1
)

echo 构建成功！
exit /b 0
