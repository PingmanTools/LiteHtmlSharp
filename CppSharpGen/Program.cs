using System;
using CppSharp;

namespace CppSharpGen
{
   class MainClass
   {
      public static void Main(string[] args)
      {
         //var f = new Sample.Foo();
         //var test = Sample.Sample.FooAdd(f);



         ConsoleDriver.Run(new LiteHtmlLibrary());


      }
   }
}

