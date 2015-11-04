@echo off
set config=%1
if "%config%" == "" (
   set config=Release
)
"%programfiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" targets\Nuget.PackageIndex.targets /p:Configuration="%config%" /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false