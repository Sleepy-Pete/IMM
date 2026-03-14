# CMakeLists.txt for IMM Viewer Android Build
# This builds the native C++ libraries for Meta Quest (ARM64)

cmake_minimum_required(VERSION 3.22.1)

project(ImmViewerAndroid)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Set build type
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

# Define paths
set(IMM_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/../..)
set(THIRDPARTY_ROOT ${IMM_ROOT}/thirdparty)
set(SRC_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/src)

# Add compiler flags
add_compile_options(
    -Wall
    -Wextra
    -Wno-unused-parameter
    -fexceptions
    -frtti
    -fvisibility=hidden
)

# Add preprocessor definitions
add_definitions(
    -DANDROID
    -D__ANDROID__
    -DPLATFORM_ANDROID
    -DGLES_SILENCE_DEPRECATION
)

# Include directories for third-party libraries
include_directories(
    ${THIRDPARTY_ROOT}/ovr-mobile-sdk/VrApi/Include
    ${THIRDPARTY_ROOT}/ovr-platform-sdk/Include
    ${THIRDPARTY_ROOT}/audio360-sdk/Audio360/include
    ${THIRDPARTY_ROOT}/libjpeg-turbo/include
    ${THIRDPARTY_ROOT}/libpng/include
    ${THIRDPARTY_ROOT}/libogg/include
    ${THIRDPARTY_ROOT}/libvorbis/include
    ${THIRDPARTY_ROOT}/libopusenc/include
    ${THIRDPARTY_ROOT}/opus/include
    ${THIRDPARTY_ROOT}/zlib/include
)

# Include IMM library directories
include_directories(
    ${IMM_ROOT}/code
    ${IMM_ROOT}/code/libImmCore/src
    ${IMM_ROOT}/code/libImmImporter/src
    ${IMM_ROOT}/code/libImmPlayer/src
)

# Build libImmCore
add_subdirectory(${IMM_ROOT}/code/libImmCore ${CMAKE_CURRENT_BINARY_DIR}/libImmCore)

# Build libImmImporter
add_subdirectory(${IMM_ROOT}/code/libImmImporter ${CMAKE_CURRENT_BINARY_DIR}/libImmImporter)

# Build libImmPlayer
add_subdirectory(${IMM_ROOT}/code/libImmPlayer ${CMAKE_CURRENT_BINARY_DIR}/libImmPlayer)

# Collect Android-specific source files
file(GLOB ANDROID_CPP_SOURCES
    ${SRC_ROOT}/android/cpp/*.cpp
)

# Collect viewer source files
file(GLOB VIEWER_SOURCES
    ${SRC_ROOT}/viewer/*.cpp
    ${SRC_ROOT}/settings.cpp
    ${SRC_ROOT}/resolve.cpp
    ${SRC_ROOT}/simpleJSON/*.cpp
)

# Create the main native library
add_library(appImmPlayer SHARED
    ${ANDROID_CPP_SOURCES}
    ${VIEWER_SOURCES}
)

# Find third-party libraries
# Note: These libraries need to be built for Android ARM64 first
# See ANDROID_BUILD_GUIDE.md for instructions

# VrApi library (Oculus Mobile SDK)
find_library(VRAPI_LIB vrapi
    PATHS ${THIRDPARTY_ROOT}/ovr-mobile-sdk/VrApi/Libs/Android/${ANDROID_ABI}/Release
    NO_DEFAULT_PATH
)

# Audio360 library
find_library(AUDIO360_LIB Audio360
    PATHS ${THIRDPARTY_ROOT}/audio360-sdk/Audio360/Android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

# Image libraries
find_library(JPEG_LIB jpeg
    PATHS ${THIRDPARTY_ROOT}/libjpeg-turbo/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

find_library(PNG_LIB png16
    PATHS ${THIRDPARTY_ROOT}/libpng/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

# Audio codec libraries
find_library(OGG_LIB ogg
    PATHS ${THIRDPARTY_ROOT}/libogg/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

find_library(VORBIS_LIB vorbis
    PATHS ${THIRDPARTY_ROOT}/libvorbis/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

find_library(VORBISFILE_LIB vorbisfile
    PATHS ${THIRDPARTY_ROOT}/libvorbis/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

find_library(OPUS_LIB opus
    PATHS ${THIRDPARTY_ROOT}/opus/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

find_library(OPUSENC_LIB opusenc
    PATHS ${THIRDPARTY_ROOT}/libopusenc/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

# Compression library
find_library(Z_LIB z
    PATHS ${THIRDPARTY_ROOT}/zlib/android/${ANDROID_ABI}
    NO_DEFAULT_PATH
)

# Add android_native_app_glue library
add_library(native_app_glue STATIC
    ${ANDROID_NDK}/sources/android/native_app_glue/android_native_app_glue.c
)
target_include_directories(native_app_glue PUBLIC
    ${ANDROID_NDK}/sources/android/native_app_glue
)

# Link against all libraries
target_link_libraries(appImmPlayer
    # IMM libraries
    ImmCore
    ImmImporter
    ImmPlayer

    # Android system libraries
    android
    log
    EGL
    GLESv3
    native_app_glue

    # C++ standard library
    c++_shared
)

# Link third-party libraries if found
if(VRAPI_LIB)
    target_link_libraries(appImmPlayer ${VRAPI_LIB})
    message(STATUS "Found VrApi library: ${VRAPI_LIB}")
else()
    message(WARNING "VrApi library not found - app will not work on Quest!")
endif()

if(AUDIO360_LIB)
    target_link_libraries(appImmPlayer ${AUDIO360_LIB})
    message(STATUS "Found Audio360 library: ${AUDIO360_LIB}")
else()
    message(WARNING "Audio360 library not found - spatial audio will not work!")
endif()

if(JPEG_LIB)
    target_link_libraries(appImmPlayer ${JPEG_LIB})
endif()

if(PNG_LIB)
    target_link_libraries(appImmPlayer ${PNG_LIB})
endif()

if(OGG_LIB)
    target_link_libraries(appImmPlayer ${OGG_LIB})
endif()

if(VORBIS_LIB)
    target_link_libraries(appImmPlayer ${VORBIS_LIB})
endif()

if(VORBISFILE_LIB)
    target_link_libraries(appImmPlayer ${VORBISFILE_LIB})
endif()

if(OPUS_LIB)
    target_link_libraries(appImmPlayer ${OPUS_LIB})
endif()

if(OPUSENC_LIB)
    target_link_libraries(appImmPlayer ${OPUSENC_LIB})
endif()

if(Z_LIB)
    target_link_libraries(appImmPlayer ${Z_LIB})
endif()

# Set output name
set_target_properties(appImmPlayer PROPERTIES
    OUTPUT_NAME "appImmPlayer"
)

