if ! which "/usr/local/bin/cmake" >/dev/null ;  then
    echo "CMake must be installed to build LiteHtml.dylib"
    exit 2
fi

cd lib_static
rm -rf build
mkdir build
cd build

export XCODE_XCCONFIG_FILE=/Users/mattlittle/Projects/pp.net/LiteHtmlSharp/LiteHtmlLib/lib_static/scripts/NoCodeSign.xcconfig
/usr/local/bin/cmake .. -G Xcode -DCMAKE_TOOLCHAIN_FILE=ios-nocodesign-9-1-arm64.cmake
make -j8