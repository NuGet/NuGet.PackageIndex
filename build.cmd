@echo off
set config=%1
if "%config%" == "" (
   set config=Release
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild targets\Nuget.PackageIndex.targets /p:Configuration="%config%" /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false