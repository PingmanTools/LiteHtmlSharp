using System;
using System.Runtime.InteropServices;

namespace ConsoleTest
{
   class MainClass
   {

      [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
      public delegate void CallbackFunc(int someNumber);

      [DllImport("litehtml", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetTestFunction(CallbackFunc func);

      public static void Main(string[] args)
      {
         SetTestFunction(n =>
            {
               Console.WriteLine("WORKS: " + n);
            });
         Console.WriteLine("Hello World!");
      }
   }
}
