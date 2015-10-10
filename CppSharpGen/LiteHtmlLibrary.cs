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
         options.LibraryName = "LiteHtmlSharpLib";

         string outputDir = Path.GetFullPath("output");
         if (Directory.Exists(outputDir))
         {
            Directory.CreateDirectory(outputDir);
         }

         options.OutputDir = outputDir;
         options.OutputNamespace = "LiteHtmlSharpLib";
         options.OutputDebug = true;
         options.CompileCode = false;

         options.addArguments("-std=c++11");
         options.addArguments("-stdlib=libc++");

        

         foreach (var liteHtmlHeaderFile in Directory.GetFiles(Path.Combine("litehtml", "src"), "html.h", SearchOption.AllDirectories))
         {
            var fullPath = Path.GetFullPath(liteHtmlHeaderFile);
            Console.WriteLine("Adding header file: " + fullPath);
            options.Headers.Add(fullPath);
         }

         var liblitehtmla = Path.GetFullPath("liblitehtml.dylib");
         Console.WriteLine("Adding library: " + liblitehtmla);
         options.Libraries.Add("liblitehtml.dylib");

         //options.Headers.Add(Path.GetFullPath("Sample.h"));
         //options.Libraries.Add(Path.GetFullPath("Sample.dylib"));
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

