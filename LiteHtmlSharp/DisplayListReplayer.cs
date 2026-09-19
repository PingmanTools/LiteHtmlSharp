using System.Numerics;
using System.Buffers;
using LiteHtmlSharp.Interop;
namespace LiteHtmlSharp;

public static class DisplayListReplayer
{
    public static void Replay(DisplayList list, Container container) => list.Replay(container, () => Run(list, container));
    private static unsafe void Run(DisplayList list, Container c)
    {
        var capacity = list.StreamLength / 8 + 1;
        byte[]? rented = null;
        Span<byte> scopes = capacity <= 128 ? stackalloc byte[128] : (rented = ArrayPool<byte>.Shared.Rent(capacity));
        var depth = 0;
        try
        {
            if (c.ScaleFactor != 1)
            {
                c.PushTransform(Matrix4x4.CreateScale(c.ScaleFactor, c.ScaleFactor, 1));
                scopes[depth++] = 1;
            }
            foreach (var command in list)
            {
                switch (command.Type)
                {
                    case CommandType.SolidFill:
                        {
                            var p = command.Read<lh_cmd_solid_fill>();
                            c.FillRect(Layer.From(p.layer), ColorRgba.From(p.color));
                            break;
                        }
                    case CommandType.LinearGradient:
                        {
                            var p = command.Read<lh_cmd_linear_gradient>();
                            c.DrawLinearGradient(Layer.From(p.layer), p.x0, p.y0, p.x1, p.y1, list.GetStops(p.stops), p.color_space, p.hue_interp);
                            break;
                        }
                    case CommandType.RadialGradient:
                        {
                            var p = command.Read<lh_cmd_radial_gradient>();
                            c.DrawRadialGradient(Layer.From(p.layer), p.cx, p.cy, p.rx, p.ry, list.GetStops(p.stops), p.color_space, p.hue_interp);
                            break;
                        }
                    case CommandType.ConicGradient:
                        {
                            var p = command.Read<lh_cmd_conic_gradient>();
                            c.DrawConicGradient(Layer.From(p.layer), p.cx, p.cy, p.angle, p.radius, list.GetStops(p.stops), p.color_space, p.hue_interp);
                            break;
                        }
                    case CommandType.Image:
                        {
                            var p = command.Read<lh_cmd_image>();
                            c.DrawImage(Layer.From(p.layer), list.GetText(p.url), list.GetText(p.base_url));
                            break;
                        }
                    case CommandType.Text:
                        {
                            var p = command.Read<lh_cmd_text>();
                            var decorationOpacity = (p.padding & 0x80000000u) != 0 ? (p.padding & 255) / 255f : 1f;
                            c.DrawText(RectF.From(p.pos), ColorRgba.From(p.color), p.font, list.GetText(p.utf8), decorationOpacity);
                            break;
                        }
                    case CommandType.Borders:
                        {
                            var p = command.Read<lh_cmd_borders>();
                            c.DrawBorders(Borders.From(p.borders), RectF.From(p.pos), p.root != 0);
                            break;
                        }
                    case CommandType.ListMarker:
                        {
                            var p = command.Read<lh_cmd_list_marker>();
                            c.DrawListMarker(RectF.From(p.pos), ColorRgba.From(p.color), p.marker_type, p.index, p.font, list.GetText(p.image), list.GetText(p.baseurl));
                            break;
                        }
                    case CommandType.SetClip:
                        {
                            var p = command.Read<lh_cmd_set_clip>();
                            c.PushClip(RectF.From(p.pos), BorderRadii.From(p.radius));
                            scopes[depth++] = 0;
                            break;
                        }
                    case CommandType.DelClip:
                        if (depth == 0 || scopes[depth - 1] != 0)
                            throw new InvalidDataException("Unbalanced clip commands.");
                        depth--;
                        c.PopClip();
                        break;
                    case CommandType.PushTransform:
                        {
                            var p = command.Read<lh_cmd_push_transform>();
                            var m = p.m;
                            c.PushTransform(new(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]));
                            scopes[depth++] = 1;
                            break;
                        }
                    case CommandType.PushOpacity:
                        c.PushOpacity(command.Read<lh_cmd_push_opacity>().opacity);
                        scopes[depth++] = 2;
                        break;
                    case CommandType.PopOpacity:
                        if (depth == 0 || scopes[depth - 1] != 2)
                            throw new InvalidDataException("Unbalanced opacity commands.");
                        depth--;
                        c.PopOpacity();
                        break;
                    case CommandType.PopTransform:
                        if (depth == 0 || scopes[depth - 1] != 1)
                            throw new InvalidDataException("Unbalanced transform commands.");
                        depth--;
                        c.PopTransform();
                        break;
                }
            }
        }
        finally
        {
            try
            {
                while (depth != 0)
                {
                    var scope = scopes[--depth];
                    if (scope == 2)
                        c.PopOpacity();
                    else if (scope == 1)
                        c.PopTransform();
                    else
                        c.PopClip();
                }
            }
            finally
            {
                if (rented != null)
                    ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
