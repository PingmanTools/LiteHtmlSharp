#include "stdafx.h"
#include "Globals.h"

#include "DocContainer.h"
#include "ManagedCallbacks.h"

ManagedCallbacks _callbacks;
DocContainer _container;

__declspec(dllexport) void SetDrawBorders(DrawBorders_function callback)
{
   _callbacks.DrawBorders = callback;
}

__declspec(dllexport) void SetDrawBackground(DrawBackground_function callback)
{

}

__declspec(dllexport) void SetTestFunction(Test_function callback)
{
   _callbacks.TestFunction = callback;
   //_callbacks.TestFunction(50);
}

__declspec(dllexport) void Init()
{
   _container.RegisterManagedClass(&_callbacks);
}
