@echo off
chcp 65001 >nul
rem เปิดเกมที่ build แล้วในเครื่อง (Builds\WebGL) ที่ http://localhost:5190/  · ปิดหน้าต่างนี้หรือกด Ctrl+C เพื่อหยุด
rem ต้องมี Python 3 (python --version)  · build ก่อนด้วย build-webgl.bat ถ้ายังไม่มีโฟลเดอร์ Builds\WebGL
cd /d "%~dp0Builds\WebGL" || (echo ยังไม่มี Builds\WebGL - ให้รัน build-webgl.bat ก่อน & pause & exit /b 1)
start "" "http://localhost:5190/"
python -m http.server 5190
