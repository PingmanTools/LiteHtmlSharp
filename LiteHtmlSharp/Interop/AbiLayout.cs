using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace LiteHtmlSharp.Interop;

public static unsafe class AbiLayout
{
    private static readonly Lazy<bool> verified = new(Check);
    public static void Validate()
    {
        NativeLoader.Initialize();
        _ = verified.Value;
    }
    private static int ManagedSize(Type type) => type.IsPointer || type.IsFunctionPointer ? IntPtr.Size : (int)typeof(AbiLayout).GetMethod(nameof(Size), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(type).Invoke(null, null)!;
    private static int Size<T>() => Unsafe.SizeOf<T>();
    private static bool Check()
    {
        if (NativeMethods.lh_abi_version() != 2)
            throw new BadImageFormatException("Unsupported litehtml ABI version; expected 2.");
        lh_layout_entry* entries;
        int count;
        NativeMethods.lh_abi_layout(&entries, &count);
        if (entries == null || count <= 0 || count > 10000)
            throw new BadImageFormatException("Invalid litehtml ABI layout table.");
        var types = typeof(AbiLayout).Assembly.GetTypes().Where(t => t.Namespace == typeof(AbiLayout).Namespace && t.IsValueType && !t.IsEnum && t.Name.StartsWith("lh_") && t.Name != "lh_document").ToDictionary(t => t.Name);
        var seen = new HashSet<string>();
        for (var i = 0; i < count; i++)
        {
            var e = entries[i];
            var name = Marshal.PtrToStringUTF8((nint)e.structure)!;
            var field = Marshal.PtrToStringUTF8((nint)e.field)!;
            if (!types.TryGetValue(name, out var type))
                throw new BadImageFormatException($"Unknown native ABI structure {name}.");
            int size, offset;
            if (field.Length == 0)
            {
                size = ManagedSize(type);
                offset = 0;
                seen.Add(name);
            }
            else if (name == "lh_font_desc" && field == "padding" && IntPtr.Size == 8)
            {
                size = 4;
                offset = ManagedSize(type) - 4;
            }
            else
            {
                var f = type.GetField(field) ?? throw new BadImageFormatException($"Unknown native ABI field {name}.{field}.");
                size = ManagedSize(f.FieldType);
                offset = (int)Marshal.OffsetOf(type, field);
                seen.Add(name + "." + field);
            }
            if (size != e.size || offset != e.offset)
                throw new BadImageFormatException($"ABI mismatch {name}.{field}: managed size/offset {size}/{offset}, native {e.size}/{e.offset}.");
        }
        foreach (var (name, type) in types)
        {
            if (!seen.Contains(name))
                throw new BadImageFormatException($"Native layout omits {name}.");
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!seen.Contains(name + "." + f.Name))
                    throw new BadImageFormatException($"Native layout omits {name}.{f.Name}.");
        }
        return true;
    }
}
