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
   _callbacks.DrawBackground = callback;
}

__declspec(dllexport) void SetTestFunction(Test_function callback)
{
   _callbacks.TestFunction = callback;
   _callbacks.TestFunction(50);
}

__declspec(dllexport) void Init(const litehtml::tchar_t* html)
{
   _container.RegisterManagedClass(&_callbacks, html);
}

__declspec(dllexport) void OnMouseMove()
{
   //_container._document->on_mouse_over(10, 10, 10, 10);
}