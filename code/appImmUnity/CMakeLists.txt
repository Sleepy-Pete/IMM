# CMakeLists.txt for ImmUnityPlugin Android build
cmake_minimum_required(VERSION 3.22.1)

project(ImmUnityPlugin)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Compiler flags
add_compile_options(
    -std=c++17
    -fexceptions
    -frtti
    -Wall
    -Wno-unused-variable
    -Wno-unused-function
)

# Platform definitions
add_definitions(
    -DANDROID
    -D__ANDROID__
    -DPLATFORM_ANDROID
    -DGLES_SILENCE_DEPRECATION
)

# Root directories
set(IMM_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/../..)
set(CODE_ROOT ${IMM_ROOT}/code)
set(SRC_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/src)
set(THIRDPARTY_ROOT ${IMM_ROOT}/thirdparty)

# Include directories
include_directories(
    ${SRC_ROOT}
    ${CODE_ROOT}
    ${CODE_ROOT}/libImmCore/src
    ${CODE_ROOT}/libImmImporter/src
    ${CODE_ROOT}/libImmPlayer/src
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/opus
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/vorbis
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/ogg
)

# Build libImmCore
add_subdirectory(${CODE_ROOT}/libImmCore ${CMAKE_CURRENT_BINARY_DIR}/libImmCore)

# Build libImmImporter
add_subdirectory(${CODE_ROOT}/libImmImporter ${CMAKE_CURRENT_BINARY_DIR}/libImmImporter)

# Build libImmPlayer
add_subdirectory(${CODE_ROOT}/libImmPlayer ${CMAKE_CURRENT_BINARY_DIR}/libImmPlayer)

# Unity plugin source files
set(UNITY_PLUGIN_SOURCES
    ${SRC_ROOT}/main.cpp
)

# Create the Unity plugin shared library
add_library(ImmUnityPlugin SHARED
    ${UNITY_PLUGIN_SOURCES}
)

# Third-party library paths
set(THIRDPARTY_LIB_DIR ${THIRDPARTY_ROOT}/android/arm64-v8a/lib)
set(JPEG_LIB ${THIRDPARTY_LIB_DIR}/libjpeg.a)
set(PNG_LIB ${THIRDPARTY_LIB_DIR}/libpng.a)
set(Z_LIB ${THIRDPARTY_LIB_DIR}/libz.a)
set(OGG_LIB ${THIRDPARTY_LIB_DIR}/libogg.a)
set(VORBIS_LIB ${THIRDPARTY_LIB_DIR}/libvorbis.a)
set(VORBISENC_LIB ${THIRDPARTY_LIB_DIR}/libvorbisenc.a)
set(VORBISFILE_LIB ${THIRDPARTY_LIB_DIR}/libvorbisfile.a)
set(OPUS_LIB ${THIRDPARTY_LIB_DIR}/libopus.a)

# Link against IMM libraries
target_link_libraries(ImmUnityPlugin
    # IMM libraries
    ImmCore
    ImmImporter
    ImmPlayer

    # Third-party libraries
    ${JPEG_LIB}
    ${PNG_LIB}
    ${Z_LIB}
    ${OGG_LIB}
    ${VORBIS_LIB}
    ${VORBISENC_LIB}
    ${VORBISFILE_LIB}
    ${OPUS_LIB}

    # Android system libraries
    android
    log
    EGL
    GLESv3
    jnigraphics

    # C++ standard library
    c++_shared
)

# Set output directory
set_target_properties(ImmUnityPlugin PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/android-build/arm64-v8a"
)

