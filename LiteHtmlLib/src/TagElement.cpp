#include "TagElement.h"

#if defined(LITEHTML_UTF8)
#define _T(x) x
#else
#define _T(x) L ##x
#endif

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
      strToReturn.append(_T("="));
      strToReturn.append(attr.second);
      strToReturn.append(_T("\n"));
   }

   auto pos = this->get_placement();

   _managedInfo.PosX = pos.x;
   _managedInfo.PosY = pos.y;
   _managedInfo.Width = width();
   _managedInfo.Height = height();


   _attributes = std::string(strToReturn.begin(), strToReturn.end());
   _managedInfo.Attributes = _attributes.c_str();

   return _managedInfo;
}
