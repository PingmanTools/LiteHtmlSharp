#include "TagElement.h"

TagElement::TagElement(std::shared_ptr<litehtml::document> doc) :litehtml::html_tag(doc)
{
}


TagElement::~TagElement()
{
}

ElementInfo& TagElement::GetManagedInfo()
{
   litehtml::tstring strToReturn;

   for (auto attr : this->m_attrs)
   {
      strToReturn.append(attr.first);
      strToReturn.append("=");
      strToReturn.append(attr.second);
      strToReturn.append("\n");
   }

   auto pos = this->get_placement();

   _managedInfo.PosX = pos.x;
   _managedInfo.PosY = pos.y;
   _managedInfo.Width = width();
   _managedInfo.Height = height();
   _managedInfo.Attributes = strToReturn.c_str();

   return _managedInfo;
}
