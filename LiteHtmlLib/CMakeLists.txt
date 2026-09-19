cmake_minimum_required(VERSION 3.21)
project(LiteHtmlLib LANGUAGES C CXX)

set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "Build upstream dependencies statically" FORCE)
set(LITEHTML_ENABLE_LINT OFF CACHE BOOL "Enable upstream lint" FORCE)
set(LITEHTML_BUILD_TESTING OFF CACHE BOOL "Build upstream external tests" FORCE)
set(EXTERNAL_GUMBO OFF CACHE BOOL "Use bundled Gumbo" FORCE)
option(LITEHTML_UTF8_ONLY "LiteHtmlSharp receives Unicode strings encoded as UTF-8" ON)
add_subdirectory(../litehtml ${CMAKE_CURRENT_BINARY_DIR}/upstream)

add_library(LiteHtmlLib SHARED
    src/host_container.cpp
    src/host_element.cpp
    src/display_list.cpp
    src/lh_api.cpp
)
target_compile_definitions(LiteHtmlLib PRIVATE LH_BUILD)
target_include_directories(LiteHtmlLib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)
target_link_libraries(LiteHtmlLib PRIVATE litehtml)
set_target_properties(LiteHtmlLib PROPERTIES
    OUTPUT_NAME litehtml
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON
)
if(APPLE)
    # Hide symbols pulled in from static litehtml/Gumbo dependencies too.
    set(LH_EXPORTS "${CMAKE_CURRENT_SOURCE_DIR}/exports.macos")
    target_link_options(LiteHtmlLib PRIVATE "LINKER:-exported_symbols_list,${LH_EXPORTS}" "LINKER:-dead_strip")
    set_property(TARGET LiteHtmlLib APPEND PROPERTY LINK_DEPENDS "${LH_EXPORTS}")
elseif(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    set(LH_EXPORTS "${CMAKE_CURRENT_SOURCE_DIR}/exports.linux")
    target_link_options(LiteHtmlLib PRIVATE "LINKER:--version-script=${LH_EXPORTS}" "LINKER:--gc-sections")
    set_property(TARGET LiteHtmlLib APPEND PROPERTY LINK_DEPENDS "${LH_EXPORTS}")
    foreach(target LiteHtmlLib litehtml gumbo)
        target_compile_options(${target} PRIVATE -ffunction-sections -fdata-sections)
    endforeach()
endif()
if(WIN32)
    set_target_properties(LiteHtmlLib PROPERTIES OUTPUT_NAME LiteHtmlLib PREFIX "")
    if(MINGW)
        target_link_options(LiteHtmlLib PRIVATE -static)
    endif()
endif()

include(CTest)
if(BUILD_TESTING)
    add_executable(lh_header_c11 tests/header_c11.c)
    target_include_directories(lh_header_c11 PRIVATE include)
    set_target_properties(lh_header_c11 PROPERTIES C_STANDARD 11 C_STANDARD_REQUIRED ON C_EXTENSIONS OFF)
    add_test(NAME lh_header_c11 COMMAND lh_header_c11)

    add_executable(lh_smoke tests/smoke.cpp)
    target_link_libraries(lh_smoke PRIVATE LiteHtmlLib)
    if(MSVC)
        target_compile_options(lh_smoke PRIVATE /UNDEBUG)
    else()
        target_compile_options(lh_smoke PRIVATE -UNDEBUG)
    endif()
    add_test(NAME lh_smoke COMMAND lh_smoke)

    add_library(lh_test_canvas STATIC
        ../litehtml/containers/test/test_container.cpp
        ../litehtml/containers/test/Bitmap.cpp
        ../litehtml/containers/test/Font.cpp
        ../litehtml/containers/test/lodepng.cpp)
    target_include_directories(lh_test_canvas PUBLIC ../litehtml/containers/test)
    target_link_libraries(lh_test_canvas PUBLIC litehtml)

    foreach(module encodings geometry_policy parser_cleanup motion_timing motion_transform)
        add_executable(lh_${module} ../litehtml/tests/${module}.cpp)
        target_link_libraries(lh_${module} PRIVATE litehtml)
        add_test(NAME lh_${module} COMMAND lh_${module})
    endforeach()
    foreach(module renderer_cleanup motion_style motion_rendering)
        add_executable(lh_${module} ../litehtml/tests/canvas/${module}.cpp)
        target_link_libraries(lh_${module} PRIVATE lh_test_canvas)
        add_test(NAME lh_${module} COMMAND lh_${module})
    endforeach()
endif()
