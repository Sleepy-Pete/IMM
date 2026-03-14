# CMakeLists.txt for libImmImporter - IMM file importer for Android

cmake_minimum_required(VERSION 3.22.1)

project(ImmImporter)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Define paths
set(IMPORTER_SRC ${CMAKE_CURRENT_SOURCE_DIR}/src)
set(CORE_SRC ${CMAKE_CURRENT_SOURCE_DIR}/../libImmCore/src)
set(CODE_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/..)

# Collect all source files
file(GLOB_RECURSE IMPORTER_SOURCES
    ${IMPORTER_SRC}/*.cpp
)

# Exclude Windows-specific files
list(FILTER IMPORTER_SOURCES EXCLUDE REGEX ".*windows.*")
list(FILTER IMPORTER_SOURCES EXCLUDE REGEX ".*win32.*")

# Create static library
add_library(ImmImporter STATIC ${IMPORTER_SOURCES})

# Include directories
target_include_directories(ImmImporter PUBLIC
    ${IMPORTER_SRC}
    ${CORE_SRC}
    ${CODE_ROOT}
)

# Add compile definitions
target_compile_definitions(ImmImporter PRIVATE
    ANDROID
    __ANDROID__
    PLATFORM_ANDROID
)

# Link against ImmCore
target_link_libraries(ImmImporter
    ImmCore
)

