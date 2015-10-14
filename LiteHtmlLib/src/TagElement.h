#pragma once

#include "Globals.h"

struct ElementInfo
{
   int PosX;
   int PosY;
   int Width;
   int Height;
};

class TagElement : public litehtml::element
{
public:
   TagElement(std::shared_ptr<litehtml::document> doc);
   ~TagElement();

   ElementInfo& GetManagedInfo();

   int ID;

private:
   ElementInfo _managedInfo;
};