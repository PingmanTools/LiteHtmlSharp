cmake_minimum_required(VERSION 3.10)

project(litehtml)

# Detect architecture
if(CMAKE_SYSTEM_PROCESSOR MATCHES "aarch64|arm64")
    set(ARCH_NAME "arm64")
else()
    set(ARCH_NAME "x64")
endif()

message(STATUS "Building for Linux ${ARCH_NAME}")

# Compiler flags for Linux
# Define __declspec(x) as visibility attribute for GCC/Clang (Windows compatibility)
set(CMAKE_CXX_FLAGS "-std=c++11 -O3 -fPIC -fvisibility=hidden -D'__declspec(x)=__attribute__((visibility(\"default\")))'")
set(CMAKE_C_FLAGS "-std=c99 -O3 -fPIC -fvisibility=hidden -D'__declspec(x)=__attribute__((visibility(\"default\")))'")


# Enable position-independent code for shared library
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# LiteHtmlLib wrapper sources
set(SOURCE_LITEHTMLLIB
    ../../src/dllmain.cpp
    ../../src/DocContainer.cpp
    ../../src/Globals.cpp
    ../../src/TagElement.cpp
)

# Core litehtml engine sources
set(SOURCE_LITEHTML
    ../../../litehtml/src/background.cpp
    ../../../litehtml/src/box.cpp
    ../../../litehtml/src/context.cpp
    ../../../litehtml/src/css_length.cpp
    ../../../litehtml/src/css_selector.cpp
    ../../../litehtml/src/document.cpp
    ../../../litehtml/src/el_anchor.cpp
    ../../../litehtml/src/el_base.cpp
    ../../../litehtml/src/el_before_after.cpp
    ../../../litehtml/src/el_body.cpp
    ../../../litehtml/src/el_break.cpp
    ../../../litehtml/src/el_cdata.cpp
    ../../../litehtml/src/el_comment.cpp
    ../../../litehtml/src/el_div.cpp
    ../../../litehtml/src/element.cpp
    ../../../litehtml/src/el_font.cpp
    ../../../litehtml/src/el_image.cpp
    ../../../litehtml/src/el_link.cpp
    ../../../litehtml/src/el_para.cpp
    ../../../litehtml/src/el_script.cpp
    ../../../litehtml/src/el_space.cpp
    ../../../litehtml/src/el_style.cpp
    ../../../litehtml/src/el_table.cpp
    ../../../litehtml/src/el_td.cpp
    ../../../litehtml/src/el_text.cpp
    ../../../litehtml/src/el_title.cpp
    ../../../litehtml/src/el_tr.cpp
    ../../../litehtml/src/html.cpp
    ../../../litehtml/src/html_tag.cpp
    ../../../litehtml/src/iterators.cpp
    ../../../litehtml/src/media_query.cpp
    ../../../litehtml/src/style.cpp
    ../../../litehtml/src/stylesheet.cpp
    ../../../litehtml/src/table.cpp
    ../../../litehtml/src/utf8_strings.cpp
    ../../../litehtml/src/web_color.cpp
)

# Gumbo HTML parser sources
set(SOURCE_GUMBO
    ../../../litehtml/src/gumbo/attribute.c
    ../../../litehtml/src/gumbo/char_ref.c
    ../../../litehtml/src/gumbo/error.c
    ../../../litehtml/src/gumbo/parser.c
    ../../../litehtml/src/gumbo/string_buffer.c
    ../../../litehtml/src/gumbo/string_piece.c
    ../../../litehtml/src/gumbo/tag.c
    ../../../litehtml/src/gumbo/tokenizer.c
    ../../../litehtml/src/gumbo/utf8.c
    ../../../litehtml/src/gumbo/util.c
    ../../../litehtml/src/gumbo/vector.c
)

# Include directories
include_directories(
    ../../src
    ../../../litehtml/src
    ../../../litehtml/include
)

# Build shared library
add_library(litehtml SHARED ${SOURCE_LITEHTMLLIB} ${SOURCE_LITEHTML} ${SOURCE_GUMBO})

# Set output name without 'lib' prefix duplication
set_target_properties(litehtml PROPERTIES
    OUTPUT_NAME "litehtml"
    PREFIX "lib"
    SUFFIX ".so"
)

# Link against standard libraries only (no Cairo/GTK needed - Avalonia handles rendering)
target_link_libraries(litehtml
    stdc++
    m
)

# Install to runtimes directory
install(TARGETS litehtml
    LIBRARY DESTINATION ${CMAKE_SOURCE_DIR}/../../runtimes/linux-${ARCH_NAME}/native
)
