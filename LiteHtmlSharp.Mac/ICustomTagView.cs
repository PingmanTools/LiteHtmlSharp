using System.Collections.Generic;
using AppKit;
namespace LiteHtmlSharp.Mac;
public interface ICustomTagView
{
    bool HasSetup { get; }
    void Setup(IReadOnlyDictionary<string, string> attributes);
    NSView View { get; }
}
