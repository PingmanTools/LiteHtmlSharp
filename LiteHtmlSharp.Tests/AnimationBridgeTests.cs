using System.Reflection;
using System.Runtime.InteropServices;
using LiteHtmlSharp.Interop;
using Xunit;

namespace LiteHtmlSharp.Tests;

public class AnimationBridgeTests
{
    [Fact]
    public void ClockDrivesOpacityAndStopsAfterFiniteAnimation()
    {
        using var host = new RecordingContainer();
        host.Document.Load("<style>@keyframes fade{from{opacity:1}to{opacity:0}} div{width:20px;height:20px;background:red;animation:fade 1s linear forwards}</style><div></div>");
        host.Document.SetTime(500);
        host.Document.Render(300);
        var list = host.Document.Draw(0, 0, host.Viewport);
        bool found = false, group = false;
        foreach (var command in list)
            if (command.Type == CommandType.PushOpacity)
            {
                Assert.Equal(.5f, command.Read<lh_cmd_push_opacity>().opacity);
                group = true;
            }
            else if (command.Type == CommandType.SolidFill)
            {
                var fill = command.Read<lh_cmd_solid_fill>();
                if (fill.color.r == 255) { Assert.Equal(255, (int)fill.color.a); found = true; }
            }
        Assert.True(found && group);
        Assert.True(host.Document.AnimationsActive);
        host.Document.SetTime(1000);
        host.Document.Render(300);
        Assert.False(host.Document.AnimationsActive);
    }

    [Theory]
    [InlineData(0u, 1f)]
    [InlineData(0x80000080u, 128f / 255)]
    [InlineData(0x80000000u, 0f)]
    public void ReplaysLegacyAndExplicitDecorationOpacity(uint encoded, float expected)
    {
        using var host = new RecordingContainer();
        host.Document.Load("<p>text</p>");
        host.Document.Render(300);
        var list = host.Document.Draw(0, 0, host.Viewport);
        // Feed legacy and extended wire payloads through the actual replay path.
        var stream = (byte[])typeof(DisplayList).GetField("stream", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list)!;
        for (var offset = 0; offset < stream.Length;)
        {
            var header = MemoryMarshal.Read<lh_cmd_header>(stream.AsSpan(offset));
            if (header.type == (uint)CommandType.Text)
            {
                var payload = stream.AsSpan(offset + Marshal.SizeOf<lh_cmd_header>());
                var text = MemoryMarshal.Read<lh_cmd_text>(payload);
                text.padding = encoded;
                MemoryMarshal.Write(payload, in text);
            }
            offset += checked((int)header.size);
        }
        DisplayListReplayer.Replay(list, host);
        Assert.NotEmpty(host.DecorationOpacities);
        Assert.All(host.DecorationOpacities, value => Assert.Equal(expected, value));
    }
}
