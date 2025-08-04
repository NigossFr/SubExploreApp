@echo off
echo Creating permanent debug keystore...

keytool -genkey -v -keystore debug.keystore -alias androiddebugkey -keyalg RSA -keysize 2048 -validity 10000 -storepass android -keypass android -dname "CN=Android Debug,O=Android,C=US"

echo.
echo Debug keystore created: debug.keystore
echo.
echo To get the SHA-1 fingerprint:
echo keytool -list -v -keystore debug.keystore -alias androiddebugkey -storepass android -keypass android
echo.
pause