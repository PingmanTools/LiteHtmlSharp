using System.Runtime.CompilerServices;
using Xunit;

namespace LiteHtmlSharp.Tests;

public class DiagnosticsTests
{
    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public void CountersTrackReloadsAndFailedLoadsWithoutAccumulatingElements()
    {
        Collect();
        var baseline = LibInterop.GetDiagnostics();
        var host = new Container();
        try
        {
            Assert.Equal(new LiteHtmlDiagnostics(1, 1, 0, 0, 0), host.Document.GetDiagnostics());
            host.ShouldCreateElementCallback = tag => tag == "widget";
            host.CreateElementCallback = (string tag, IReadOnlyDictionary<string, string> attrs, out SizeF size) =>
            {
                size = new(10, 10);
                return 1;
            };
            for (var i = 1; i <= 100; i++)
            {
                host.Render("<widget></widget>");
                Assert.Equal(new LiteHtmlDiagnostics(1, 1, 1, i, 1), host.Document.GetDiagnostics());
            }
            // Duplicate IDs cause native load rollback. Keep the previous completed snapshot.
            Assert.Throws<InvalidOperationException>(() => host.Render("<widget></widget><widget></widget>"));
            Assert.Equal(new LiteHtmlDiagnostics(1, 1, 1, 100, 1), host.Document.GetDiagnostics());
            host.Render("<p>replacement</p>");
            Assert.Equal(new LiteHtmlDiagnostics(1, 1, 0, 101, 1), host.Document.GetDiagnostics());
            var global = LibInterop.GetDiagnostics();
            Assert.Equal(baseline.TotalContainerCount + 1, global.TotalContainerCount);
            Assert.Equal(baseline.NativeDocumentCount + 1, global.NativeDocumentCount);
            Assert.Equal(baseline.HasDocument + 1, global.HasDocument);
            Assert.Equal(baseline.ParseCount + 101, global.ParseCount);
            Assert.Equal(baseline.CustomElementCount, global.CustomElementCount);
        }
        finally { host.Dispose(); }
        host.Dispose();
        Assert.Equal(default, host.Document.GetDiagnostics());
        Assert.Equal(baseline, LibInterop.GetDiagnostics());
    }

    [Fact]
    public void RegistryDoesNotRootAbandonedContainers()
    {
        Collect();
        var baseline = LibInterop.GetDiagnostics();
        var weak = AbandonContainer();
        Collect();
        Assert.False(weak.IsAlive);
        Assert.Equal(baseline, LibInterop.GetDiagnostics());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AbandonContainer()
    {
        var host = new Container();
        host.Render("<p>abandoned</p>");
        return new WeakReference(host);
    }
}
