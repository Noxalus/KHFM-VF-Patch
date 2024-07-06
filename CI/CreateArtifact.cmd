@echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

@REM Create Patches directory that will be copy in the artifacts
powershell New-Item -Path "Patches" -ItemType Directory

@REM Zip all sub-patches in the proper directory with the proper extension
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-EN" -ArchiveFileName "Patches\KH1FM-Magic-EN.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-FR" -ArchiveFileName "Patches\KH1FM-Magic-FR.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Stranger" -ArchiveFileName "Patches\KH1FM-Stranger.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Textures" -ArchiveFileName "Patches\KH1FM-Textures.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Voices" -ArchiveFileName "Patches\KH1FM-Voices.patch" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Videos" -ArchiveFileName "Patches\KH1FM-Videos.patch" -Format Zip

@REM ** Windows Platform **

@REM Copy Patches directory
powershell Copy-Item -Path "Patches" -Destination "bin\Release\net8.0\win-x86\publish\Resources" -Recurse

@REM Zip all patch files to upload them for release
powershell Compress-7Zip "bin\Release\net8.0\win-x86\publish" -ArchiveFileName "KHFM-VF-Patch-Windows.zip" -Format Zip

@REM ** Linux Platform **

@REM Copy Patches directory
powershell Copy-Item -Path "Patches" -Destination "bin\Release\net8.0\linux-x64\publish\Resources" -Recurse

@REM Zip all patch files to upload them for release
powershell Compress-7Zip "bin\Release\net8.0\linux-x64\publish" -ArchiveFileName "KHFM-VF-Patch-Linux.zip" -Format Zip

:exit
popd

@echo on