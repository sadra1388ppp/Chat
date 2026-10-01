@echo off
set "APP=%LOCALAPPDATA%\Programs\Vibely"

if not exist "%APP%" mkdir "%APP%"
copy /Y "%~dp0Vibely.exe" "%APP%\Vibely.exe" >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
"$s=(New-Object -ComObject WScript.Shell); ^
$d=$env:USERPROFILE+'\Desktop\Vibely.lnk'; ^
$w=$s.CreateShortcut($d); ^
$w.TargetPath=$env:LOCALAPPDATA+'\Programs\Vibely\Vibely.exe'; ^
$w.WorkingDirectory=$env:LOCALAPPDATA+'\Programs\Vibely'; ^
$w.IconLocation=$env:LOCALAPPDATA+'\Programs\Vibely\Vibely.exe,0'; ^
$w.Save(); ^
$m=$env:APPDATA+'\Microsoft\Windows\Start Menu\Programs'; ^
if(!(Test-Path $m)){New-Item -ItemType Directory -Path $m -Force|Out-Null}; ^
$w=$s.CreateShortcut($m+'\Vibely.lnk'); ^
$w.TargetPath=$env:LOCALAPPDATA+'\Programs\Vibely\Vibely.exe'; ^
$w.WorkingDirectory=$env:LOCALAPPDATA+'\Programs\Vibely'; ^
$w.IconLocation=$env:LOCALAPPDATA+'\Programs\Vibely\Vibely.exe,0'; ^
$w.Save()"

start "" "%APP%\Vibely.exe"
exit /b 0
