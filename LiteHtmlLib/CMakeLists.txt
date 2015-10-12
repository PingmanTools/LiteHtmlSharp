cmake_minimum_required(VERSION 2.8)

project(litehtml)

set(CMAKE_CXX_FLAGS "-m32 -DCMAKE_OSX_ARCHITECTURES=i386 -std=c++11 -stdlib=libc++ -O3")
set(CMAKE_C_FLAGS "-m32 -DCMAKE_OSX_ARCHITECTURES=i386 -std=c99 -O3")

set(SOURCE_LITEHTMLLIB 
                    dllmain.cpp
                    DocContainer.cpp
                    Globals.cpp
                    LiteHtmlLib.cpp
                    ManagedCallbacks.cpp
                    myclass.cpp
)

set(SOURCE_LITEHTML src/background.cpp
                    src/box.cpp
                    src/context.cpp
                    src/css_length.cpp
                    src/css_selector.cpp
                    src/document.cpp
                    src/el_anchor.cpp
                    src/el_base.cpp
                    src/el_before_after.cpp
                    src/el_body.cpp
                    src/el_break.cpp
                    src/el_cdata.cpp
                    src/el_comment.cpp
                    src/el_div.cpp
                    src/element.cpp
                    src/el_font.cpp
                    src/el_image.cpp
                    src/el_link.cpp
                    src/el_para.cpp
                    src/el_script.cpp
                    src/el_space.cpp
                    src/el_style.cpp
                    src/el_table.cpp
                    src/el_td.cpp
                    src/el_text.cpp
                    src/el_title.cpp
                    src/el_tr.cpp
                    src/html.cpp
                    src/html_tag.cpp
                    src/iterators.cpp
                    src/media_query.cpp
                    src/style.cpp
                    src/stylesheet.cpp
                    src/table.cpp
                    src/utf8_strings.cpp
                    src/web_color.cpp
)

set(SOURCE_GUMBO    src/gumbo/attribute.c
                    src/gumbo/char_ref.c
                    src/gumbo/error.c
                    src/gumbo/parser.c
                    src/gumbo/string_buffer.c
                    src/gumbo/string_piece.c
                    src/gumbo/tag.c
                    src/gumbo/tokenizer.c
                    src/gumbo/utf8.c
                    src/gumbo/util.c
                    src/gumbo/vector.c
)

# add_library(litehtml STATIC ${SOURCE_LITEHTMLLIB} ${SOURCE_LITEHTML} ${SOURCE_GUMBO})
add_library(litehtml SHARED ${SOURCE_LITEHTMLLIB} ${SOURCE_LITEHTML} ${SOURCE_GUMBO})

