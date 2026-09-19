using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
namespace LiteHtmlSharp.Interop;

internal sealed class CallbackState(Container container)
{
    private readonly WeakReference<Container> target = new(container);
    internal Exception? Error;
    internal Container? CleanupOwner;
    internal Container Container => CleanupOwner ?? (target.TryGetTarget(out var c) ? c : throw new ObjectDisposedException(nameof(Container)));
    internal void ThrowPending()
    {
        var e = Error;
        Error = null;
        if (e != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw();
    }
}
internal static unsafe class HostThunks
{
    private static CallbackState State(void* user) => (CallbackState)GCHandle.FromIntPtr((nint)user).Target!;
    private static Container Host(void* user) => State(user).Container;
    private static void Record(void* user, Exception error)
    {
        try
        {
            State(user).Error ??= error;
        }
        catch { }
    }
    private static string Text(sbyte* p) => p == null ? "" : Marshal.PtrToStringUTF8((nint)p)!;
    private static void Write(lh_sink* sink, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        fixed (byte* p = bytes)
            if (sink != null && sink->write != null)
                sink->write(sink->ctx, (sbyte*)p, bytes.Length);
    }
    internal static lh_host Create(void* user) => new()
    {
        user = user,
        create_font = &create_font,
        delete_font = &delete_font,
        text_width = &text_width,
        pt_to_px = &pt_to_px,
        default_font_size = &default_font_size,
        default_font_name = &default_font_name,
        load_image = &load_image,
        get_image_size = &get_image_size,
        set_caption = &set_caption,
        set_base_url = &set_base_url,
        set_cursor = &set_cursor,
        on_anchor_click = &on_anchor_click,
        on_element_click = &on_element_click,
        on_mouse_event = &on_mouse_event,
        transform_text = &transform_text,
        import_css = &import_css,
        get_viewport = &get_viewport,
        get_media_features = &get_media_features,
        get_language = &get_language,
        should_create_element = &should_create_element,
        create_element = &create_element,
        request_redraw = &request_redraw
    };
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nuint create_font(void* u, lh_font_desc* f, lh_font_metrics* m)
    {
        try
        {
            var d = new FontDescription(Text(f->family), f->size, f->style, f->weight, f->decoration_line, f->decoration_thickness, f->decoration_thickness_predefined, f->decoration_thickness_units, f->decoration_style, ColorRgba.From(f->decoration_color), Text(f->emphasis_style), ColorRgba.From(f->emphasis_color), f->emphasis_position);
            var h = Host(u).CreateFont(d, out var metrics);
            *m = metrics.Native;
            return h;
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void delete_font(void* u, nuint f)
    {
        try
        {
            Host(u).DeleteFont(f);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static float text_width(void* u, sbyte* t, nuint f)
    {
        try
        {
            return Host(u).TextWidth(Text(t), f);
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static float pt_to_px(void* u, float p)
    {
        try
        {
            return Host(u).PointToPixels(p);
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static float default_font_size(void* u)
    {
        try
        {
            return Host(u).DefaultFontSize;
        }
        catch (Exception error)
        {
            Record(u, error);
            return 16;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void default_font_name(void* u, lh_sink* s)
    {
        try
        {
            Write(s, Host(u).DefaultFontName);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void load_image(void* u, sbyte* s, sbyte* b, byte r)
    {
        try
        {
            Host(u).LoadImage(Text(s), Text(b), r != 0);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void get_image_size(void* u, sbyte* s, sbyte* b, float* w, float* h)
    {
        try
        {
            var z = Host(u).GetImageSize(Text(s), Text(b));
            *w = z.Width;
            *h = z.Height;
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void set_caption(void* u, sbyte* s)
    {
        try
        {
            Host(u).SetCaption(Text(s));
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void set_base_url(void* u, sbyte* s)
    {
        try
        {
            Host(u).SetBaseUrl(Text(s));
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void set_cursor(void* u, sbyte* s)
    {
        try
        {
            Host(u).SetCursor(Text(s));
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void on_anchor_click(void* u, sbyte* s, nuint e)
    {
        try
        {
            Host(u).OnAnchorClick(Text(s), e);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte on_element_click(void* u, nuint e)
    {
        try
        {
            return (byte)(Host(u).OnElementClick(e) ? 1 : 0);
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void on_mouse_event(void* u, nuint e, int k)
    {
        try
        {
            Host(u).OnMouseEvent(e, k);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void transform_text(void* u, sbyte* s, int k, lh_sink* o)
    {
        try
        {
            Write(o, Host(u).TransformText(Text(s), k));
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void import_css(void* u, sbyte* s, sbyte* b, lh_sink* t, lh_sink* bo)
    {
        try
        {
            var r = Host(u).ImportCss(Text(s), Text(b));
            Write(t, r.Css);
            Write(bo, r.BaseUrl);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void get_viewport(void* u, lh_rect* o)
    {
        try
        {
            *o = Host(u).Viewport.Native;
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void get_media_features(void* u, lh_media_features* o)
    {
        try
        {
            *o = Host(u).MediaFeatures.Native;
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void get_language(void* u, lh_sink* l, lh_sink* c)
    {
        try
        {
            var v = Host(u).GetLanguage();
            Write(l, v.Language);
            Write(c, v.Culture);
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte should_create_element(void* u, sbyte* t)
    {
        try
        {
            return (byte)(Host(u).ShouldCreateElement(Text(t)) ? 1 : 0);
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int create_element(void* u, sbyte* t, lh_attr* a, int count, float* w, float* h)
    {
        try
        {
            var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
                attrs[Text(a[i].name)] = Text(a[i].value);
            var host = Host(u);
            var id = host.CreateElement(Text(t), attrs, out var size);
            host.Document.RecordCustomElement(id);
            *w = size.Width;
            *h = size.Height;
            return id;
        }
        catch (Exception error)
        {
            Record(u, error);
            return 0;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void request_redraw(void* u, lh_rect* p)
    {
        try
        {
            Host(u).RequestRedraw(RectF.From(*p));
        }
        catch (Exception error)
        {
            Record(u, error);
        }
    }
}
