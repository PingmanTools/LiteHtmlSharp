#pragma once

#include "CallbackDelegates.h"

struct ElementInfo
{
   int PosX;
   int PosY;
   int Width;
   int Height;
   const litehtml::tchar_t* Attributes;
};

class TagElement : public litehtml::html_tag
{
public:
   TagElement(std::shared_ptr<litehtml::document> doc);
   ~TagElement();

   ElementInfo& GetManagedInfo();

   int ID;

private:
   ElementInfo _managedInfo;
};