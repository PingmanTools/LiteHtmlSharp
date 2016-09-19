#pragma once

#include "CallbackDelegates.h"

using namespace std;
using namespace litehtml;

// container functions
extern "C" { __declspec(dllexport) void Init(DocumentCalls& docCalls, SetCallbacksFunc initCallbacks); }
extern "C" { __declspec(dllexport) const char* EchoTest(const char* testStr); }
extern "C" { __declspec(dllexport) DocContainer* CreateDocContainer(); }
extern "C" { __declspec(dllexport) int GetWidthTest(DocContainer* container); }