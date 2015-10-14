#include "TagElement.h"

TagElement::TagElement(std::shared_ptr<litehtml::document> doc) :litehtml::element(doc)
{
}


TagElement::~TagElement()
{
}

ElementInfo& TagElement::GetManagedInfo()
{
   _managedInfo.PosX = this->left();
   _managedInfo.PosY = this->top();
   return _managedInfo;
}
