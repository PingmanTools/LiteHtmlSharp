#pragma once

#include "Globals.h"

class ManagedCallbacks
{
public:
   ManagedCallbacks();
   ~ManagedCallbacks();

   DrawBorders_function DrawBorders = 0;
   DrawBackground_function DrawBackground = 0;
   Test_function TestFunction = 0;
};
