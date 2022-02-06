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

powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-EN" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Magic-EN.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-FR" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Magic-FR.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Stranger" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Stranger.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Textures" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Textures.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Voices" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Voices.zip" -Format Zip

dir

:exit
popd

dir

cd ..

dir

@REM @echo on