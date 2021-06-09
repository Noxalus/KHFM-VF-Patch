@echo off

pushd "%~dp0"
cd ..\KHFM-VF-Patc

powershell Compress-7Zip "bin\x86\Release" -ArchiveFileName "SampleX86.zip" -Format Zip
powershell Compress-7Zip "bin\x64\Release" -ArchiveFileName "SampleX64.zip" -Format Zip

:exit
popd

@echo on