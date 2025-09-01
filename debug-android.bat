@echo off
echo ==============================================
echo SubExplore Android Debug Diagnostic
echo ==============================================

echo.
echo 1. Checking ADB connection...
adb devices

echo.
echo 2. Checking Android SDK...
echo Android SDK: %ANDROID_SDK_ROOT%
echo Java: %JAVA_HOME%

echo.
echo 3. Project clean...
dotnet clean
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

echo.
echo 4. Building project...
dotnet build -f net8.0-android -c Debug -v minimal

echo.
echo 5. Checking APK...
dir bin\Debug\net8.0-android\*.apk

echo.
echo 6. Deployment attempt...
echo Ready to deploy. Press any key to continue or Ctrl+C to cancel
pause

dotnet run -f net8.0-android

echo.
echo Diagnostic complete. Check output above for errors.
pause