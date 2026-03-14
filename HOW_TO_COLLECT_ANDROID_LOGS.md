# How to Collect Android Logs from Meta Quest

## Quick Method (Recommended)

### Option 1: Using ADB Command Line

1. **Connect your Quest headset via USB**
2. **Enable Developer Mode and USB Debugging** on the headset
3. **Open a terminal/command prompt**
4. **Run this command**:

```bash
adb logcat -s ImmUnityPlugin Unity -v time > imm_quest_logs.txt
```

This will:
- Filter logs to show only `ImmUnityPlugin` and `Unity` tags
- Include timestamps
- Save to `imm_quest_logs.txt` file

5. **Launch your app on the Quest**
6. **Let it run for 30 seconds**
7. **Press Ctrl+C to stop logging**
8. **Share the `imm_quest_logs.txt` file**

### Option 2: Using Meta Quest Developer Hub (MQDH)

1. **Open Meta Quest Developer Hub**
2. **Connect your Quest headset**
3. **Go to Device Manager**
4. **Click on your device**
5. **Select "Logcat" tab**
6. **Set filter**: `ImmUnityPlugin|Unity`
7. **Launch your app**
8. **Copy the logs** or use "Save to File" button

### Option 3: Using Android Studio

1. **Open Android Studio**
2. **Go to View → Tool Windows → Logcat**
3. **Connect your Quest via USB**
4. **Select your device** from dropdown
5. **Set filter**: `tag:ImmUnityPlugin|tag:Unity`
6. **Launch your app**
7. **Right-click in Logcat → Save to File**

## What to Capture

### Minimum Required Logs:
- **From app launch** to when you expect IMMPlayer to start
- **At least 30 seconds** of runtime
- **Include any errors** shown in red

### Ideal Log Capture:
```bash
# Clear old logs first
adb logcat -c

# Start capturing
adb logcat -v time > full_quest_logs.txt

# Launch your app on Quest
# Wait 30 seconds
# Press Ctrl+C

# Then filter for relevant logs
adb logcat -d -s ImmUnityPlugin Unity -v time > filtered_logs.txt
```

## Log Filtering Tips

### Filter by Package Name (if you know it):
```bash
adb logcat --pid=$(adb shell pidof -s com.YourCompany.YourApp)
```

### Filter by Multiple Tags:
```bash
adb logcat -s ImmUnityPlugin:D Unity:I AndroidRuntime:E
```

### Show Only Errors:
```bash
adb logcat *:E
```

### Real-time Colored Output (Linux/Mac):
```bash
adb logcat -v color
```

## Troubleshooting ADB Connection

### If "adb: command not found":
- **Windows**: Add Android SDK platform-tools to PATH
  - Default: `C:\Users\YourName\AppData\Local\Android\Sdk\platform-tools`
- **Mac**: `brew install android-platform-tools`
- **Linux**: `sudo apt install adb`

### If device not detected:
```bash
# Check if device is connected
adb devices

# If no devices shown:
# 1. Enable Developer Mode on Quest
# 2. Enable USB Debugging in Quest settings
# 3. Accept "Allow USB Debugging" prompt on headset
# 4. Try different USB cable/port
```

### If permission denied:
```bash
# Restart ADB server
adb kill-server
adb start-server
adb devices
```

## What We're Looking For

The logs will tell us:
1. ✅ Is the native plugin loading? (`UnityPluginLoad()`)
2. ✅ Is Unity calling the C# scripts? (`ImmPlayerManager.Awake()`)
3. ✅ Is Init() being called? (`Init() called`)
4. ✅ Which step is failing? (Timer, Sound, Renderer, Player)
5. ✅ What error codes are returned?
6. ✅ Is the graphics API correct? (Should be OpenGL ES 3.0)

## Example of What Good Logs Look Like

```
01-07 10:30:15.123 I/ImmUnityPlugin: === UnityPluginLoad() called ===
01-07 10:30:15.124 I/ImmUnityPlugin: ImmUnityPlugin native library loaded successfully
01-07 10:30:15.125 I/ImmUnityPlugin: === Graphics Device Event: Initialize ===
01-07 10:30:15.126 I/ImmUnityPlugin: Using OpenGL ES 3.0 (Android)
01-07 10:30:15.200 I/Unity: === ImmPlayerManager.Awake() called ===
01-07 10:30:15.201 I/Unity: Platform: Android
01-07 10:30:15.250 I/ImmUnityPlugin: === Init() called ===
01-07 10:30:15.251 I/ImmUnityPlugin: Timer initialized successfully
01-07 10:30:15.300 I/ImmUnityPlugin: Renderer initialized successfully
01-07 10:30:15.350 I/ImmUnityPlugin: IMM Player initialized successfully
```

## Quick Commands Reference

```bash
# Clear logs
adb logcat -c

# View live logs (filtered)
adb logcat -s ImmUnityPlugin Unity

# Save logs to file
adb logcat -d > logs.txt

# View last 500 lines
adb logcat -t 500

# Follow logs in real-time
adb logcat -v time | grep -E "ImmUnityPlugin|Unity"
```

---

**After collecting logs, share the log file so we can diagnose the issue!**

