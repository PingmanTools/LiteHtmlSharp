using Xunit;

namespace LiteHtmlSharp.Tests;

public class ImageIdentityTests
{
    private sealed class Source(float width) : IImageSource
    {
        public SizeF Size => new(width, 10);
        public int FrameCount => 2;
        public IReadOnlyList<TimeSpan> FrameDelays => [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)];
        public int LoopCount => 1;
        public int Disposals;
        public object GetFrame(int index) => this;
        public void Dispose() => Disposals++;
    }
    private sealed class ImageContainer() : Container(frameClock: new FrameClock(automatic: false))
    {
        public string BaseUrl = "https://a.example/page/";
        public Dictionary<string, TaskCompletionSource<IImageSource?>> Requests = [];
        public Dictionary<string, CancellationToken> Tokens = [];
        public Dictionary<string, Task> Loads = [];
        protected override bool ShouldCreateElement(string tag) => tag == "input";
        protected override int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
        {
            if (attributes.TryGetValue("data-image", out var image)) Loads[image] = LoadImageAsync(image, "");
            size = new(20, 10);
            return 1;
        }
        protected override (string Source, string BaseUrl) ResolveImageKey(string source, string baseUrl)
            => (new Uri(new Uri(string.IsNullOrEmpty(baseUrl) ? BaseUrl : baseUrl), source).AbsoluteUri, "");
        protected override ValueTask<IImageSource?> LoadImageSourceAsync(string source, string baseUrl, CancellationToken cancellationToken)
        {
            Assert.Empty(baseUrl);
            var result = new TaskCompletionSource<IImageSource?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Requests[source] = result;
            Tokens[source] = cancellationToken;
            return new(result.Task);
        }
    }

    [Fact]
    public async Task RelativeImagesBelongToTheirPageEvenWhenOldLoadCompletesLast()
    {
        using var container = new ImageContainer();
        var first = new Source(10);
        var second = new Source(20);
        var a = container.LoadImageAsync("logo.png", "");
        Assert.Same(a, container.LoadImageAsync("https://a.example/page/logo.png", ""));
        container.BaseUrl = "https://b.example/page/";
        var b = container.LoadImageAsync("logo.png", "");
        Assert.NotSame(a, b);
        container.Requests["https://b.example/page/logo.png"].SetResult(second);
        await b;
        container.Requests["https://a.example/page/logo.png"].SetResult(first);
        await a;
        Assert.Same(second, container.GetImageFrame("logo.png", ""));
        Assert.Equal(second.Size, container.GetImageSize("logo.png", ""));
        container.BaseUrl = "https://a.example/page/";
        Assert.Same(first, container.GetImageFrame("logo.png", ""));
        Assert.Equal(2, container.Requests.Count);
        container.Dispose();
        Assert.Equal(1, first.Disposals);
        Assert.Equal(1, second.Disposals);
    }

    private sealed class QueueContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => queue.Enqueue(() => callback(state));
        public void Drain() { while (queue.TryDequeue(out var action)) action(); }
    }
    private static ImageContainer Create(QueueContext context)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try { return new ImageContainer(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    [Fact]
    public async Task StaticNavigationStopsOldClockAndSuppressesLateImageNotifications()
    {
        var context = new QueueContext();
        using var container = Create(context);
        var notices = 0;
        container.ImageReady += (_, _) => notices++;
        container.RedrawRequested += _ => notices++;
        container.Document.Load("<img src='a.gif' width='10' height='10'>");
        var a = container.LoadImageAsync("a.gif", "");
        container.Requests["https://a.example/page/a.gif"].SetResult(new Source(10));
        await a;
        context.Drain();
        notices = 0;
        container.ImageClock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, notices);
        container.Document.Load("<img src='late.gif' width='10' height='10'>");
        var late = container.LoadImageAsync("late.gif", "");
        container.Document.Load("<p>Static page</p>");
        context.Drain();
        notices = 0;
        container.Requests["https://a.example/page/late.gif"].SetResult(new Source(10));
        await late;
        context.Drain();
        container.ImageClock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, notices);
    }

    [Fact]
    public async Task FailedLoadRetainsPreviousImageClock()
    {
        var context = new QueueContext();
        using var container = Create(context);
        container.Document.Load("<img src='a.gif' width='10' height='10'>");
        var loaded = container.LoadImageAsync("a.gif", "");
        container.Requests["https://a.example/page/a.gif"].SetResult(new Source(10));
        await loaded;
        context.Drain();
        Assert.Throws<InvalidOperationException>(() => container.Document.Load("<input><input>"));
        var redraws = 0;
        container.RedrawRequested += _ => redraws++;
        container.ImageClock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, redraws);
    }

    [Fact]
    public async Task RepeatedHtmlReplacementReleasesOldFramesAndReusesCurrentImages()
    {
        var context = new QueueContext();
        using var container = Create(context);
        var sources = new List<Source>();
        for (var i = 0; i < 200; i++)
        {
            container.Document.Load($"<style>@keyframes fade{{to{{opacity:.2}}}}div{{animation:fade 1s infinite}}</style><div>page {i}</div><img src='{i}.gif'>");
            if (i > 0) Assert.Equal(1, sources[i - 1].Disposals);
            var source = new Source(10);
            sources.Add(source);
            var load = container.LoadImageAsync($"{i}.gif", "");
            container.Requests[$"https://a.example/page/{i}.gif"].SetResult(source);
            await load;
            context.Drain();
            container.Document.SetTime(i * 16);
            container.Document.Render(100 + i % 2);
            container.Document.Draw(0, -i, container.Viewport);
            container.ImageClock.Advance(TimeSpan.FromMilliseconds(100));
            // Same URL in the next document remains owned and does not decode again.
            container.Document.Load($"<img src='{i}.gif'>");
            Assert.Same(load, container.LoadImageAsync($"{i}.gif", ""));
            Assert.Equal(0, source.Disposals);
            context.Drain();
        }
        container.Document.Load("<p>Finished</p>");
        context.Drain();
        Assert.All(sources, source => Assert.Equal(1, source.Disposals));
        Assert.Null(container.GetImageFrame("199.gif", ""));
        container.Dispose();
        Assert.All(sources, source => Assert.Equal(1, source.Disposals));
    }

    [Fact]
    public async Task ReplacementCancelsObsoleteDecodeAndDisposesItsLateResult()
    {
        var context = new QueueContext();
        using var container = Create(context);
        container.Document.Load("<img src='obsolete.gif'>");
        var load = container.LoadImageAsync("obsolete.gif", "");
        container.Document.Load("<p>New page</p>");
        Assert.True(container.Tokens["https://a.example/page/obsolete.gif"].IsCancellationRequested);
        var source = new Source(10);
        container.Requests["https://a.example/page/obsolete.gif"].SetResult(source);
        await load;
        context.Drain();
        Assert.Equal(1, source.Disposals);
        Assert.Null(container.GetImageFrame("obsolete.gif", ""));
    }

    [Fact]
    public async Task FailedReplacementReleasesNewImagesButPreservesOldOnes()
    {
        var context = new QueueContext();
        using var container = Create(context);
        container.Document.Load("<img src='kept.gif'>");
        var load = container.LoadImageAsync("kept.gif", "");
        var kept = new Source(10);
        container.Requests["https://a.example/page/kept.gif"].SetResult(kept);
        await load;
        context.Drain();
        Assert.Throws<InvalidOperationException>(() => container.Document.Load("<input data-image='failed.gif'><input>"));
        Assert.Equal(0, kept.Disposals);
        Assert.Same(kept, container.GetImageFrame("kept.gif", ""));
        Assert.True(container.Tokens["https://a.example/page/failed.gif"].IsCancellationRequested);
        var failed = new Source(10);
        container.Requests["https://a.example/page/failed.gif"].SetResult(failed);
        await container.Loads["failed.gif"];
        Assert.Equal(1, failed.Disposals);
    }

    [Fact]
    public async Task NativeReloadInvalidatesSnapshotEvenIfImageActivationThrows()
    {
        var context = new QueueContext();
        using var container = Create(context);
        container.Document.Load("<img src='a.gif' width='10' height='10'>");
        var loaded = container.LoadImageAsync("a.gif", "");
        container.Requests["https://a.example/page/a.gif"].SetResult(new Source(10));
        await loaded;
        container.Document.Render(100);
        var snapshot = container.Document.Draw(0, 0, container.Viewport);
        container.ImageClock.Dispose();
        Assert.Throws<ObjectDisposedException>(() => container.Document.Load("<img src='a.gif' width='10' height='10'>"));
        Assert.Null(container.Document.LastDisplayList);
        Assert.Throws<InvalidOperationException>(() => { _ = snapshot.GetEnumerator(); });
    }
}
