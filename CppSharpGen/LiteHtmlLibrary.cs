using System;
using CppSharp;
using CppSharp.Generators;
using System.IO;

namespace CppSharpGen
{
   public class LiteHtmlLibrary : ILibrary
   {
      public LiteHtmlLibrary()
      {
      }


      public void Setup(Driver driver)
      {
         var options = driver.Options;
         options.GeneratorKind = GeneratorKind.CSharp;
         options.LibraryName = "LiteHtmlSharp";


         foreach (var liteHtmlHeaderFile in Directory.GetFiles(Path.Combine("litehtml", "src"), "*.h", SearchOption.AllDirectories))
         {
            Console.WriteLine("Adding header file: " + liteHtmlHeaderFile);
            options.Headers.Add(liteHtmlHeaderFile);
         }
      }

      public void SetupPasses(Driver driver)
      {

      }

      public void Preprocess(Driver driver, CppSharp.AST.ASTContext ctx)
      {

      }

      public void Postprocess(Driver driver, CppSharp.AST.ASTContext ctx)
      {

      }


   }
}

