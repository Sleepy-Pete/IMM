# CMakeLists.txt for libImmCore - Core IMM library for Android

cmake_minimum_required(VERSION 3.22.1)

project(ImmCore)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Define paths
set(CORE_SRC ${CMAKE_CURRENT_SOURCE_DIR}/src)
set(THIRDPARTY_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/../../thirdparty)
set(CODE_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/..)

# Collect all source files
file(GLOB_RECURSE CORE_SOURCES
    ${CORE_SRC}/libBasics/*.cpp
    ${CORE_SRC}/libCompression/*.cpp
    ${CORE_SRC}/libDataUtils/*.cpp
    ${CORE_SRC}/libImage/*.cpp
    ${CORE_SRC}/libMath/*.cpp
    ${CORE_SRC}/libMesh/*.cpp
    ${CORE_SRC}/libRender/*.cpp
    ${CORE_SRC}/libSound/*.cpp
    ${CORE_SRC}/libSystem/*.cpp
    ${CORE_SRC}/libWave/*.cpp
)

# Exclude Windows-specific files
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*windows.*")
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*win32.*")
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*d3d.*")
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*dx.*")
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*directx.*")

# Exclude desktop OpenGL 4.x files (Android uses OpenGL ES)
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*opengl4x.*")

# Exclude OPUS encoder (requires libopusenc which we don't have built)
list(FILTER CORE_SOURCES EXCLUDE REGEX ".*piWaveOPUS.*")

# Create static library
add_library(ImmCore STATIC ${CORE_SOURCES})

# Include directories
target_include_directories(ImmCore PUBLIC
    ${CORE_SRC}
    ${CODE_ROOT}
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/opus
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/vorbis
    ${THIRDPARTY_ROOT}/android/arm64-v8a/include/ogg
    ${THIRDPARTY_ROOT}/audio360-sdk/Audio360/include
)

# Add compile definitions
target_compile_definitions(ImmCore PRIVATE
    ANDROID
    __ANDROID__
    PLATFORM_ANDROID
    GLES_SILENCE_DEPRECATION
)

# Link against Android libraries
target_link_libraries(ImmCore
    android
    log
    EGL
    GLESv3
)

