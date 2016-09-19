using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LiteHtmlSharp
{
   public static class PInvokes
   {
#if __MonoCS__
      const string LiteHtmlLibFile = "litehtml";
      public const CharSet cs = CharSet.Ansi;
#elif WIN64
      const string LiteHtmlLibFile = "x64\\LiteHtmlLib.dll";
      public const CharSet cs = CharSet.Unicode;
#else
      const string LiteHtmlLibFile = "x86\\LiteHtmlLib.dll";
      public const CharSet cs = CharSet.Unicode;
#endif

      public const CallingConvention cc = CallingConvention.Cdecl;

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern int GetWidthTest(IntPtr docContainer);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern IntPtr CreateDocContainer();

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern IntPtr EchoTest(IntPtr testStr);

      private static IntPtr StringToHGlobalUTF8(string s, out int length)
      {
         if (s == null)
         {
            length = 0;
            return IntPtr.Zero;
         }

         var bytes = Encoding.UTF8.GetBytes(s);
         var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
         Marshal.Copy(bytes, 0, ptr, bytes.Length);
         Marshal.WriteByte(ptr, bytes.Length, 0);
         length = bytes.Length;

         return ptr;
      }

      public static IntPtr StringToHGlobalUTF8(string s)
      {
         int temp;
         return StringToHGlobalUTF8(s, out temp);
      }

      public static string PtrToStringUTF8(IntPtr ptr)
      {
         if (ptr == IntPtr.Zero)
         {
            return null;
         }

         var i = 0;
         while (Marshal.ReadByte(ptr, i) != 0)
         {
            i++;
         }

         var bytes = new byte[i];
         Marshal.Copy(ptr, bytes, 0, i);

         return Encoding.UTF8.GetString(bytes, 0, i);
      }
   }
}

