@echo off

cd %~dp0
dotnet publish --self-contained -r win-x64

