using System.Collections.Generic;
using AppKit;
namespace LiteHtmlSharp.Mac;
public class LiteHtmlNSButton : NSButton, ICustomTagView
{
    public bool HasSetup { get; private set; }
    public string Href { get; private set; }
    public string ID { get; private set; }
    public string Value { get; private set; }
    public NSView View => this;
    public LiteHtmlNSButton()
    {
        BezelStyle = NSBezelStyle.RegularSquare;
        Title = "Button";
    }
    public void Setup(IReadOnlyDictionary<string, string> attributes)
    {
        HasSetup = true;
        if (attributes.TryGetValue("id", out var id))
            ID = id;
        if (attributes.TryGetValue("value", out var value))
            Title = Value = value;
        if (attributes.TryGetValue("href", out var href))
            Href = href;
        if (attributes.TryGetValue("bezel", out var bezel) && int.TryParse(bezel, out var style))
            BezelStyle = (NSBezelStyle)style;
    }
}
