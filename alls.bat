@echo off
cls

echo === 关闭游戏进程 ===
taskkill /f /im "Duckov.exe" >nul 2>&1

echo.
echo === 开始构建 炸鱼测试 Mod ===
call build.bat

if %ERRORLEVEL% neq 0 (
    echo 构建失败，终止执行！
    pause
    exit /b 1
)

echo.
echo === 复制 dll 到游戏目录 ===
cd /d "D:\CODE\DUCKOVMODS\GRENADEFISHING"
:: 定义路径
set SRC_DLL=D:\CODE\DUCKOVMODS\GRENADEFISHING\bin\Release\netstandard2.1\GrenadeFishing.dll
set DEST_DIR=C:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Mods\GrenadeFishing
set DEST_DLL=%DEST_DIR%\GrenadeFishing.dll

:: 检查源文件是否存在
if not exist "%SRC_DLL%" (
    echo 未找到构建输出文件：
    echo    %SRC_DLL%
    pause
    exit /b 1
)

:: 复制 DLL 文件
copy /Y "%SRC_DLL%" "%DEST_DLL%" >nul
if %ERRORLEVEL% neq 0 (
    echo 复制 dll 失败，请检查目标路径！
    pause
    exit /b 1
)

echo dll 已成功更新到：
echo    %DEST_DLL%
echo.

echo === 启动游戏 ===
call run.bat
