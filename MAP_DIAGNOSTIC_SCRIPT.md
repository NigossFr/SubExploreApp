# Map Diagnostic Script

## Quick API Key Test
```bash
# Test if API key is valid
curl "https://maps.googleapis.com/maps/api/js?key=AIzaSyDAKkZk5ceq0-hFQDO00D26tWfjSp2RCaM&callback=test"
```

## Android Logcat Debugging
```bash
# Monitor map-related logs
adb logcat | grep -i "maps\|google\|gms"
```

## Build Commands
```bash
# Clean rebuild
cd "D:\Developpement\SubExploreApp\SubExplore"
dotnet clean
dotnet build

# Deploy to Android
dotnet build -f net8.0-android
```

## Common Issues Checklist
- [ ] Google Play Services installed on device/emulator
- [ ] Internet connectivity available
- [ ] Location permissions granted
- [ ] API key billing enabled in Google Cloud Console
- [ ] Package name matches: com.companyname.subexplore
- [ ] Maps SDK for Android enabled in Google Cloud Console