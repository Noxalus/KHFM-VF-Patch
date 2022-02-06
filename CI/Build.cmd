@REM @echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

@REM if exist Debug rd /s /q Debug
@REM if exist Release rd /s /q Release
@REM if exist x64 rd /s /q x64

@REM "%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Restore /p:Configuration=Release /p:Platform=x64
"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Restore /p:Configuration=Release /p:Platform=x86
"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Build /p:Configuration=Release /p:Platform=x86
"%programfiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe" KHFM-VF-Patch.sln /t:Rebuild /p:Configuration=Release /p:Platform=x86

powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-EN" -ArchiveFileName "KH1FM-Magic-EN" -Format Zip
rm -r "Resources\Patches\KH1FM-Magic-EN"

dir

:exit
popd

dir

cd ..

dir

@REM @echo on