@echo off 

PowerShell.exe -ExecutionPolicy Bypass -File %cd%\build-scripts\Build.ps1 %*
