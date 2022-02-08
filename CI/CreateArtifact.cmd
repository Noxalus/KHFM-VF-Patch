@echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

@REM Create Patches directory
powershell New-Item -Path "bin\x86\Release\net5.0-windows\Resources\Patches" -ItemType Directory

@REM Zip all sub-patches in the proper directory with the proper extension
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-EN" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\Patches\KH1FM-Magic-EN.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-FR" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\Patches\KH1FM-Magic-FR.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Stranger" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\Patches\KH1FM-Stranger.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Textures" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\Patches\KH1FM-Textures.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Voices" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\Patches\KH1FM-Voices.patch" -Format Zip

@REM Zip all patch files to upload them for release
powershell Compress-7Zip "bin\x86\Release\net5.0-windows" -ArchiveFileName "KHFM-VF-Patch.zip" -Format Zip

:exit
popd

@echo on