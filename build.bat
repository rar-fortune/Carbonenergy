@echo off
cd /d "%~dp0"
cls
echo =======================================================
echo   CARBON ENERGY: MADEN MUDURU - DERLEME ARACI
echo =======================================================
echo.
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [HATA] .NET Framework C# derleyicisi bulunamadi: %CSC%
    pause
    exit /b 1
)

echo [1/2] C# Kaynak dosyalari derleniyor...
"%CSC%" /target:winexe /optimize+ /win32icon:app.ico /out:CarbonEnergy.exe /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll GameModel.cs WorldRenderer.cs GameForm.cs Program.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [BASARILI] CarbonEnergy.exe basariyla olusturuldu!
    echo Boyut:
    dir CarbonEnergy.exe | findstr CarbonEnergy.exe
    echo.
    echo Calistirmak icin CarbonEnergy.exe dosyasina cift tiklayin.
) else (
    echo.
    echo [HATA] Derleme sirasinda hata olustu!
)

echo.
pause
