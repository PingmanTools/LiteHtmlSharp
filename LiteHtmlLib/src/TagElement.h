#pragma once

#include "html.h"

struct ElementInfo
{
	int PosX;
	int PosY;
	int Width;
	int Height;
	const char* Attributes;
	const char* Text;
};

class TagElement : public litehtml::html_tag
{
public:
	TagElement(std::shared_ptr<litehtml::document> doc);
	~TagElement();

	ElementInfo* GetManagedInfo();
	void SetManagedInfo(const ElementInfo managedInfo);

	int ID;

private:
	ElementInfo _managedInfo;
	std::string _attributes;
	std::string _text;
};