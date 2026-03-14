# CMakeLists.txt for libImmPlayer - IMM playback engine for Android

cmake_minimum_required(VERSION 3.22.1)

project(ImmPlayer)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Define paths
set(PLAYER_SRC ${CMAKE_CURRENT_SOURCE_DIR}/src)
set(CORE_SRC ${CMAKE_CURRENT_SOURCE_DIR}/../libImmCore/src)
set(IMPORTER_SRC ${CMAKE_CURRENT_SOURCE_DIR}/../libImmImporter/src)
set(CODE_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/..)

# Collect all source files
file(GLOB_RECURSE PLAYER_SOURCES
    ${PLAYER_SRC}/*.cpp
)

# Exclude Windows-specific files
list(FILTER PLAYER_SOURCES EXCLUDE REGEX ".*windows.*")
list(FILTER PLAYER_SOURCES EXCLUDE REGEX ".*win32.*")
list(FILTER PLAYER_SOURCES EXCLUDE REGEX ".*d3d.*")
list(FILTER PLAYER_SOURCES EXCLUDE REGEX ".*dx.*")

# Exclude pretessellated paint renderer (requires shader compilation step)
list(FILTER PLAYER_SOURCES EXCLUDE REGEX ".*pretessellated.*")

# Create static library
add_library(ImmPlayer STATIC ${PLAYER_SOURCES})

# Include directories
target_include_directories(ImmPlayer PUBLIC
    ${PLAYER_SRC}
    ${CORE_SRC}
    ${IMPORTER_SRC}
    ${CODE_ROOT}
)

# Add compile definitions
target_compile_definitions(ImmPlayer PRIVATE
    ANDROID
    __ANDROID__
    PLATFORM_ANDROID
    GLES_SILENCE_DEPRECATION
)

# Link against ImmCore and ImmImporter
target_link_libraries(ImmPlayer
    ImmCore
    ImmImporter
)

