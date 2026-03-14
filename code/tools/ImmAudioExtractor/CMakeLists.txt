cmake_minimum_required(VERSION 3.10)

project(ImmAudioExtractor)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Add the executable
add_executable(ImmAudioExtractor
    ImmAudioExtractor.cpp
)

# Include directories
target_include_directories(ImmAudioExtractor PRIVATE
    ${CMAKE_SOURCE_DIR}/code
)

# Link against required libraries
target_link_libraries(ImmAudioExtractor
    libImmCore
    libImmImporter
)

# Platform-specific settings
if(WIN32)
    # Windows-specific settings
    target_compile_definitions(ImmAudioExtractor PRIVATE _CRT_SECURE_NO_WARNINGS)
endif()

if(UNIX AND NOT APPLE)
    # Linux-specific settings
    target_link_libraries(ImmAudioExtractor pthread dl)
endif()

