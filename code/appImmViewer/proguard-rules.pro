# IMM Viewer ProGuard Rules
# Add project specific ProGuard rules here.

# Keep native methods
-keepclasseswithmembernames class * {
    native <methods>;
}

# Keep MainActivity and its native methods
-keep class org.linuxfoundation.imm.player.MainActivity {
    public static native <methods>;
}

# Keep all classes with @Keep annotation
-keep @androidx.annotation.Keep class * {*;}
-keepclassmembers class * {
    @androidx.annotation.Keep *;
}

# Keep Kotlin coroutines
-keepnames class kotlinx.coroutines.internal.MainDispatcherFactory {}
-keepnames class kotlinx.coroutines.CoroutineExceptionHandler {}
-keepclassmembernames class kotlinx.** {
    volatile <fields>;
}

# Keep NativeActivity
-keep class android.app.NativeActivity { *; }

# Keep all classes in the player package
-keep class org.linuxfoundation.imm.player.** { *; }

# Suppress warnings for missing classes
-dontwarn org.linuxfoundation.imm.player.**

