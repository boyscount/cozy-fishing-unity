@echo off
chcp 65001 >nul
rem build เกมเป็นเว็บ (WebGL) ลง Builds\WebGL — ปิด Unity Editor ของโปรเจกต์นี้ก่อน · ใช้เวลาราว 10-15 นาที
rem   build-webgl.bat        = ตัวจริง (ไฟล์เล็กสุด สำหรับขึ้นเว็บ)
rem   build-webgl.bat fast   = ทดสอบแบบเร็ว (ไฟล์ใหญ่กว่า build เร็วกว่า)
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if /i "%1"=="fast" (set FAST=1) else (set FAST=)
cd /d "%~dp0"
echo กำลัง build... ดูความคืบหน้าได้ที่ Logs\build_local.log
%UNITY% -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod ProjectTools.BuildWebGL -logFile Logs\build_local.log
findstr /c:"Build Finished" Logs\build_local.log
pause
