if ! which "/usr/local/bin/cmake" >/dev/null ;  then
    echo "CMake must be installed to build LiteHtml.dylib"
    exit 2
fi

cd lib_dylib
rm -rf build
mkdir build
cd build
/usr/local/bin/cmake ..
make -j8

