cmake_minimum_required(VERSION 3.10)

project(LiteHtmlLib)

# Detect target architecture from environment or default
if(NOT DEFINED TARGET_ARCH)
    set(TARGET_ARCH "x64")
endif()

message(STATUS "Building for Windows ${TARGET_ARCH}")

# Compiler-specific flags
if(MSVC)
    # MSVC: static CRT (/MT), UTF-8 source, optimize
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /utf-8 /O2 /DLITEHTML_UTF8")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /utf-8 /O2")
    # Use static CRT (/MT) so no runtime dependencies
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded")
else()
    # MinGW/Clang: static link everything
    set(CMAKE_CXX_FLAGS "-std=c++11 -O3 -DLITEHTML_UTF8")
    set(CMAKE_C_FLAGS "-std=c99 -O3")
endif()

# For MinGW, we need to export symbols properly
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)

# LiteHtmlLib wrapper sources
set(SOURCE_LITEHTMLLIB
    ../src/dllmain.cpp
    ../src/DocContainer.cpp
    ../src/Globals.cpp
    ../src/TagElement.cpp
)

# Core litehtml engine sources
set(SOURCE_LITEHTML
    ../../litehtml/src/background.cpp
    ../../litehtml/src/box.cpp
    ../../litehtml/src/context.cpp
    ../../litehtml/src/css_length.cpp
    ../../litehtml/src/css_selector.cpp
    ../../litehtml/src/document.cpp
    ../../litehtml/src/el_anchor.cpp
    ../../litehtml/src/el_base.cpp
    ../../litehtml/src/el_before_after.cpp
    ../../litehtml/src/el_body.cpp
    ../../litehtml/src/el_break.cpp
    ../../litehtml/src/el_cdata.cpp
    ../../litehtml/src/el_comment.cpp
    ../../litehtml/src/el_div.cpp
    ../../litehtml/src/element.cpp
    ../../litehtml/src/el_font.cpp
    ../../litehtml/src/el_image.cpp
    ../../litehtml/src/el_link.cpp
    ../../litehtml/src/el_para.cpp
    ../../litehtml/src/el_script.cpp
    ../../litehtml/src/el_space.cpp
    ../../litehtml/src/el_style.cpp
    ../../litehtml/src/el_table.cpp
    ../../litehtml/src/el_td.cpp
    ../../litehtml/src/el_text.cpp
    ../../litehtml/src/el_title.cpp
    ../../litehtml/src/el_tr.cpp
    ../../litehtml/src/html.cpp
    ../../litehtml/src/html_tag.cpp
    ../../litehtml/src/iterators.cpp
    ../../litehtml/src/media_query.cpp
    ../../litehtml/src/style.cpp
    ../../litehtml/src/stylesheet.cpp
    ../../litehtml/src/table.cpp
    ../../litehtml/src/utf8_strings.cpp
    ../../litehtml/src/web_color.cpp
)

# Gumbo HTML parser sources
set(SOURCE_GUMBO
    ../../litehtml/src/gumbo/attribute.c
    ../../litehtml/src/gumbo/char_ref.c
    ../../litehtml/src/gumbo/error.c
    ../../litehtml/src/gumbo/parser.c
    ../../litehtml/src/gumbo/string_buffer.c
    ../../litehtml/src/gumbo/string_piece.c
    ../../litehtml/src/gumbo/tag.c
    ../../litehtml/src/gumbo/tokenizer.c
    ../../litehtml/src/gumbo/utf8.c
    ../../litehtml/src/gumbo/util.c
    ../../litehtml/src/gumbo/vector.c
)

# Include directories
include_directories(
    ../src
    ../../litehtml/src
    ../../litehtml/include
)

# Build shared library (DLL)
add_library(LiteHtmlLib SHARED ${SOURCE_LITEHTMLLIB} ${SOURCE_LITEHTML} ${SOURCE_GUMBO})

# Set output name
set_target_properties(LiteHtmlLib PROPERTIES
    OUTPUT_NAME "LiteHtmlLib"
    PREFIX ""
    SUFFIX ".dll"
)

# Link libraries
if(MSVC)
    # MSVC with static CRT needs no extra link flags
else()
    # MinGW/Clang: link statically so no libc++/libunwind dependencies
    target_link_libraries(LiteHtmlLib
        -static
        stdc++
    )
endif()
