#include "Globals.h"
#include "DocContainer.h"

using namespace litehtml;
using namespace std;

std::vector<DocContainer*> _containers;

void TriggerTestCallback(DocContainer* container, int x, const litehtml::tchar_t* testString)
{
   container->GetCallbacks().TestCallback(x, testString);
}

__declspec(dllexport) 
void Delete(DocContainer* container)
{
   auto foundAt = std::find(_containers.begin(), _containers.end(), container);
   if (foundAt != _containers.end())
   {
      _containers.erase(_containers.begin() + (foundAt - _containers.begin()));
   }
   delete container;
}

__declspec(dllexport) void Init(DocumentCalls& docCalls, SetCallbacksFunc initCallbacks)
{
   DocContainer* container = new DocContainer();
   _containers.push_back(container);

   initCallbacks(container->GetCallbacks());

   docCalls.TriggerTestCallback = TriggerTestCallback;
   docCalls.Delete = Delete;

   container->SetDocumentCalls(docCalls);
}

const char* EchoTest(const char* testStr)
{
   string test = testStr;
   char* newBuff = new char[test.size() + 1];
   memcpy(newBuff, test.c_str(), test.size() + 1);
   return newBuff;
}

int GetWidthTest(DocContainer* container)
{
   return container->GetWidth();
}

DocContainer* CreateDocContainer()
{
   DocContainer* container = new DocContainer();
   return container;
}