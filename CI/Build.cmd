@REM @echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Restore /p:Configuration=Release /p:Platform=x86
"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Build /p:Configuration=Release /p:Platform=x86
"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Rebuild /p:Configuration=Release /p:Platform=x86

:exit
popd

@REM @echo on