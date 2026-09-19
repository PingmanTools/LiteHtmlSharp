using LiteHtmlSharp.Avalonia;
using Xunit;

public class ResourceLoaderTests
{
    [Fact]
    public void SynchronousResourceLoaderRunsOnTheCallingThread()
    {
        var caller = Environment.CurrentManagedThreadId;
        var calls = 0;
        using var host = new AvaloniaContainer("", (string _) => "", (string _) =>
        {
            Assert.Equal(caller, Environment.CurrentManagedThreadId);
            calls++;
            return Array.Empty<byte>();
        });
        var loads = Enumerable.Range(0, 12).Select(i => host.LoadImageAsync($"image-{i}.gif", "")).ToArray();
        Assert.Equal(12, calls);
        // No wait is needed: this test verifies callback invocation, not asynchronous decoding.
    }

}
