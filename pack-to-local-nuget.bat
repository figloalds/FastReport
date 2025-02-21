echo off

del /f /q dfenet.version
rem git rev-list --count master >> dfenet.version
echo 2082> dfenet.version
set /p revision=< dfenet.version
set /p rev=<rev
set /A rev=rev+1
echo %rev% > rev

set args=-o .\_nuget -p:PackageVersion=2025.1.%revision%.%rev%

dotnet pack FastReport.Compat\FastReport.Compat %args%
dotnet pack FastReport.OpenSource %args%
dotnet pack Extras\OpenSource\FastReport.OpenSource.Export.PdfSimple %args%

if errorlevel 1 (
	echo BUILD PROCESS FAILED.
) else (
	echo BUILD PROCESS SUCCEDED!
)

copy /d /v /n /y .\_nuget\*.2025.1.%revision%.%rev%.nupkg ..\NugetLocal