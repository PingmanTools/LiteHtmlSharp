#include "TagElement.h"

TagElement::TagElement(std::shared_ptr<litehtml::document> doc) :litehtml::html_tag(doc)
{
}


TagElement::~TagElement()
{
}

ElementInfo& TagElement::GetManagedInfo()
{
   auto pos = this->get_placement();

   _managedInfo.PosX = pos.x;
   _managedInfo.PosY = pos.y;
   _managedInfo.Width = width();
   _managedInfo.Height = height();
   return _managedInfo;
}
