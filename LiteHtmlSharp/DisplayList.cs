using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LiteHtmlSharp.Interop;
namespace LiteHtmlSharp;

public sealed class DisplayList
{
    private readonly Document owner;
    private readonly int generation;
    private readonly byte[] stream, blob;
    private readonly Dictionary<ulong, string> text = new();
    internal DisplayList(Document owner, int generation, byte[] stream, byte[] blob)
    {
        this.owner = owner;
        this.generation = generation;
        this.stream = stream;
        this.blob = blob;
        Validate();
    }
    internal void EnsureValid()
    {
        if (owner.IsDisposed || generation != owner.Generation)
            throw new InvalidOperationException("Display list font handles expired after document reload or disposal.");
    }
    internal void Replay(Container target, Action action)
    {
        if (!ReferenceEquals(target, owner.Owner))
            throw new ArgumentException("A display list must be replayed by its owning container.");
        owner.Replay(this, action);
    }
    /// <summary>Whether two snapshots contain identical drawing commands and resources.</summary>
    public bool ContentEquals(DisplayList? other) => other != null &&
        ReferenceEquals(owner, other.owner) && generation == other.generation &&
        stream.AsSpan().SequenceEqual(other.stream) && blob.AsSpan().SequenceEqual(other.blob);
    public int StreamLength => stream.Length;
    public int BlobLength => blob.Length;
    public Enumerator GetEnumerator()
    {
        EnsureValid();
        return new(stream);
    }
    public string GetText(lh_span span)
    {
        EnsureValid();
        return text.TryGetValue(SpanKey(span), out var value) ? value : throw new InvalidDataException("Span does not identify a display string.");
    }
    public ReadOnlySpan<lh_color_stop> GetStops(lh_span span)
    {
        EnsureValid();
        var data = Bytes(span);
        if (data.Length % Unsafe.SizeOf<lh_color_stop>() != 0)
            throw new InvalidDataException("Invalid gradient stop length.");
        return MemoryMarshal.Cast<byte, lh_color_stop>(data);
    }
    private ReadOnlySpan<byte> Bytes(lh_span span)
    {
        if (span.offset > (uint)blob.Length || span.length > (uint)blob.Length - span.offset)
            throw new InvalidDataException("Display blob span is out of bounds.");
        return blob.AsSpan((int)span.offset, (int)span.length);
    }
    private static ulong SpanKey(lh_span span) => ((ulong)span.offset << 32) | span.length;
    private void CacheText(lh_span span)
    {
        var key = SpanKey(span);
        if (!text.ContainsKey(key))
            text.Add(key, Encoding.UTF8.GetString(Bytes(span)));
    }
    private void Validate()
    {
        var e = new Enumerator(stream);
        while (e.MoveNext())
        {
            var c = e.Current;
            switch (c.Type)
            {
                case CommandType.SolidFill:
                    c.Read<lh_cmd_solid_fill>();
                    break;
                case CommandType.LinearGradient:
                    CheckStops(c.Read<lh_cmd_linear_gradient>().stops);
                    break;
                case CommandType.RadialGradient:
                    CheckStops(c.Read<lh_cmd_radial_gradient>().stops);
                    break;
                case CommandType.ConicGradient:
                    CheckStops(c.Read<lh_cmd_conic_gradient>().stops);
                    break;
                case CommandType.Image:
                    var im = c.Read<lh_cmd_image>();
                    CacheText(im.url);
                    CacheText(im.base_url);
                    break;
                case CommandType.Text:
                    CacheText(c.Read<lh_cmd_text>().utf8);
                    break;
                case CommandType.ListMarker:
                    var m = c.Read<lh_cmd_list_marker>();
                    CacheText(m.image);
                    CacheText(m.baseurl);
                    break;
                case CommandType.Borders:
                    c.Read<lh_cmd_borders>();
                    break;
                case CommandType.SetClip:
                    c.Read<lh_cmd_set_clip>();
                    break;
                case CommandType.PushTransform:
                    c.Read<lh_cmd_push_transform>();
                    break;
                case CommandType.PushOpacity:
                    var opacity = c.Read<lh_cmd_push_opacity>().opacity;
                    if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
                        throw new InvalidDataException("Invalid group opacity.");
                    break;
                case CommandType.PopOpacity:
                case CommandType.DelClip:
                case CommandType.PopTransform:
                    break;
                default:
                    throw new InvalidDataException($"Unknown display command {c.Type}.");
            }
        }
    }
    private void CheckStops(lh_span span)
    {
        if (Bytes(span).Length % Unsafe.SizeOf<lh_color_stop>() != 0)
            throw new InvalidDataException("Invalid gradient stop length.");
    }
    public ref struct Enumerator
    {
        private ReadOnlySpan<byte> remaining;
        public CommandView Current
        {
            get; private set;
        }
        internal Enumerator(ReadOnlySpan<byte> bytes)
        {
            remaining = bytes;
            Current = default;
        }
        public bool MoveNext()
        {
            if (remaining.IsEmpty)
                return false;
            if (remaining.Length < 8)
                throw new InvalidDataException("Truncated command header.");
            var h = MemoryMarshal.Read<lh_cmd_header>(remaining);
            if (h.size < 8 || h.size % 8 != 0 || h.size > (uint)remaining.Length)
                throw new InvalidDataException("Invalid command record size.");
            Current = new((CommandType)h.type, remaining.Slice(8, (int)h.size - 8));
            remaining = remaining[(int)h.size..];
            return true;
        }
    }
    public readonly ref struct CommandView
    {
        private readonly ReadOnlySpan<byte> payload;
        public CommandType Type
        {
            get;
        }
        internal CommandView(CommandType type, ReadOnlySpan<byte> payload)
        {
            Type = type;
            this.payload = payload;
        }
        public T Read<T>() where T : unmanaged
        {
            if (payload.Length < Unsafe.SizeOf<T>())
                throw new InvalidDataException("Truncated display command.");
            return MemoryMarshal.Read<T>(payload);
        }
    }
}
