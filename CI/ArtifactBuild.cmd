@echo off

pushd "%~dp0"

cd ..\KHFM-VF-Patch\bin

powershell Compress-7Zip "x86\Release" -ArchiveFileName "SampleX86.zip" -Format Zip
powershell Compress-7Zip "x64\Release" -ArchiveFileName "SampleX64.zip" -Format Zip

:exit
popd

@echo on