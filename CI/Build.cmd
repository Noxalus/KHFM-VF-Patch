@echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

"%programfiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Restore /p:Configuration=Release /p:Platform=x86
"%programfiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Build /p:Configuration=Release /p:Platform=x86
"%programfiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Rebuild /p:Configuration=Release /p:Platform=x86

:exit
popd

@echo on