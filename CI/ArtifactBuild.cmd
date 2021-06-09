@REM @echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patch

@REM powershell Compress-7Zip "bin\x64\Release" -ArchiveFileName "..\KHFM-VF-Patch-x64.zip" -Format Zip
powershell Compress-7Zip "bin\x86\Release\net5.0-windows" -ArchiveFileName "KHFM-VF-Patch-x86.zip" -Format Zip
echo %VERSION%

dir

:exit
popd

@REM @echo on