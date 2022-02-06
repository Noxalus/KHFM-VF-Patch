@REM @echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-EN" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Magic-EN.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Magic-FR" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Magic-FR.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Stranger" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Stranger.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Textures" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Textures.zip" -Format Zip
powershell Compress-7Zip "Resources\Patches\KH1FM-Voices" -ArchiveFileName "bin\x86\Release\net5.0-windows\Resources\KH1FM-Voices.zip" -Format Zip

echo "Version: %VERSION%"

:exit
popd

@REM @echo on