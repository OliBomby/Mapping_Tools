@echo off
setlocal
cd /d "%~dp0"
if not exist bin mkdir bin
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars32.bat"
if errorlevel 1 exit /b 1
cl /nologo /W4 /EHsc /MT Injector.cpp /Fo:bin\Injector.obj /Fe:bin\Injector.exe
if errorlevel 1 exit /b 1
cl /nologo /W4 /EHsc /MT /LD Bootstrap.cpp /Fo:bin\Bootstrap.obj /Fe:bin\Bootstrap.dll /link /IMPLIB:bin\Bootstrap.lib
if errorlevel 1 exit /b 1
set "bridgeAssembly=EditorBridge-%RANDOM%-%RANDOM%.dll"
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /warn:4 /warnaserror /target:library /platform:x86 /out:bin\%bridgeAssembly% Bridge.cs EditorCircle.cs
if errorlevel 1 exit /b 1
>bin\bridge-assembly.txt echo %bridgeAssembly%
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /warn:4 /warnaserror /out:bin\Client.exe Client.cs
if errorlevel 1 exit /b 1
