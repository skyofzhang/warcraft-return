@echo off
chcp 65001 >nul
set "PROJECT_PATH=%~dp0"
set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

:: 查找 Unity 可执行文件（按常见安装路径）
set "UNITY_EXE="
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe"
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe"
if exist "C:\Program Files\Unity\Editor\Unity.exe" set "UNITY_EXE=C:\Program Files\Unity\Editor\Unity.exe"

if "%UNITY_EXE%"=="" (
    echo 未找到 Unity 可执行文件。
    echo 请将本脚本中的 UNITY_EXE 路径改为您本机的 Unity 安装路径，例如：
    echo   C:\Program Files\Unity\Hub\Editor\2022.3.xx\Editor\Unity.exe
    echo 或手动在 Unity 中点击菜单：WarcraftReturn -^> 一键配置工程与场景
    pause
    exit /b 1
)

echo 正在使用 Unity 批处理模式执行一键配置...
echo 项目路径: %PROJECT_PATH%
echo Unity: %UNITY_EXE%
echo.

"%UNITY_EXE%" -batchmode -projectPath "%PROJECT_PATH%" -executeMethod SetupWarcraftReturnProject.Execute -quit -logFile "%PROJECT_PATH%\Logs\SetupProject.log"

if %ERRORLEVEL% equ 0 (
    echo 一键配置已完成。请查看上方输出或 Logs\SetupProject.log 确认。
) else (
    echo 执行可能未成功，请查看 Logs\SetupProject.log 或直接在 Unity 中打开项目并点击菜单执行。
)
pause
