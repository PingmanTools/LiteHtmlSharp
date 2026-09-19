using Xunit;

namespace LiteHtmlSharp.Tests;

public class AnimationDriverTests
{
    private sealed class ManualTime : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(double milliseconds) => timestamp += TimeSpan.FromMilliseconds(milliseconds).Ticks;
    }

    [Fact]
    public void ContainerIntervalChangesLiveWithoutResettingTimeAndCapsFinalDeadline()
    {
        using var container = new RecordingContainer();
        container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(100);
        var time = new ManualTime();
        var changes = new List<TimeSpan?>();
        var requests = 0;
        using var driver = new AnimationDriver(container.Document, changes.Add, () => requests++,
            () => new RectF(0, 0, 100, 100), time);
        driver.Resume();
        container.Document.Load("<style>@keyframes fade{to{opacity:.2}}div{height:20px;animation:fade 1s linear forwards}</style><div></div>");
        void Frame() { driver.Render(100); driver.FrameCompleted(); }
        Frame();
        for (var i = 0; i < 5; i++)
        {
            time.Advance(100);
            Assert.Equal(TimeSpan.FromMilliseconds(100), driver.Pulse(Frame));
        }
        Assert.Single(changes);
        var before = requests;
        container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(250);
        Assert.Equal(TimeSpan.FromMilliseconds(250), driver.NextDelay);
        Assert.Equal(before + 1, requests);
        container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(50);
        Frame();
        Assert.Equal(TimeSpan.FromMilliseconds(50), driver.NextDelay);
        time.Advance(490); // Only 10 ms left: keep the 50 ms minimum.
        Assert.Equal(TimeSpan.FromMilliseconds(50), driver.Pulse(Frame));
        time.Advance(50);
        Assert.Null(driver.Pulse(Frame)); // Completion is based on elapsed time, not pulse count.
        driver.Dispose();
        before = requests;
        container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(100);
        Assert.Equal(before, requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.5)]
    [InlineData(86400001)]
    public void ContainerRejectsUnsupportedAnimationIntervals(double milliseconds)
    {
        using var container = new RecordingContainer();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(milliseconds));
    }

    [Fact]
    public void SharedDemoSleepsWhenOnlyItsStaticHeaderOrFooterIsVisible()
    {
        using var container = new RecordingContainer();
        container.Document.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "page.html")));
        var header = container.Document.RenderFrame(800, 0, new RectF(0, 0, 800, 100));
        Assert.True(header.Active);
        Assert.True(header.NextDelay < 0);
        var footer = container.Document.RenderFrame(800, 500,
            new RectF(0, container.Document.Height() - 100, 800, 100));
        Assert.True(footer.Active);
        Assert.True(footer.NextDelay < 0);
    }

    [Fact]
    public void RepeatingTimerKeepsItsIntervalAcrossPulsesAndStopsOnCompletion()
    {
        using var container = new RecordingContainer();
        var time = new ManualTime();
        var changes = new List<TimeSpan?>();
        using var driver = new AnimationDriver(container.Document, changes.Add, () => { },
            () => new RectF(0, 0, 100, 100), time);
        driver.Resume();
        container.Document.Load("<style>@keyframes fade{to{opacity:.2}}div{height:20px;animation:fade 1s linear forwards}</style><div></div>");
        void Frame() { driver.Render(100); driver.FrameCompleted(); }
        Frame();
        var cadence = driver.NextDelay;
        Assert.Single(changes);
        for (var i = 0; i < 30; ++i)
        {
            time.Advance(16.7);
            Assert.Equal(cadence, driver.Pulse(Frame));
        }
        Assert.Single(changes); // No timer stop/restart or host schedule queries per pulse.
        time.Advance(600);
        Assert.Null(driver.Pulse(Frame));
        Assert.Equal(2, changes.Count);
        Assert.Null(changes[1]);
        Assert.False(driver.NeedsAnimationSample);
    }

    [Fact]
    public void DriverSleepsThroughDelayAndPresentsFinalFrameThenStops()
    {
        using var container = new RecordingContainer();
        var time = new ManualTime();
        TimeSpan? due = null;
        var requests = 0;
        using var driver = new AnimationDriver(container.Document, value => due = value, () => requests++,
            () => new RectF(0, 0, 100, 100), time);
        driver.Resume();
        container.Document.Load("<style>@keyframes fade{to{opacity:.2}}div{width:20px;height:20px;animation:fade 1s linear 500ms forwards}</style><div></div>");
        driver.Synchronize();
        driver.Render(100);
        driver.FrameCompleted();
        Assert.Equal(500, due!.Value.TotalMilliseconds, 5);
        time.Advance(750);
        var before = requests;
        driver.Pulse(() => { driver.Render(100); driver.FrameCompleted(); });
        Assert.Equal(before, requests); // Pulse renders directly, without queueing another frame.
        driver.Render(100);
        driver.FrameCompleted();
        Assert.InRange(due!.Value.TotalMilliseconds, 1, 17);
        time.Advance(2000); // A late callback must still request the final render.
        driver.Pulse(() => { driver.Render(100); driver.FrameCompleted(); });
        driver.Render(100);
        driver.FrameCompleted();
        Assert.Null(due);
        Assert.False(container.Document.AnimationsActive);
    }

    [Fact]
    public void OffscreenMotionSleepsButResumesAtCurrentTimeAndReloadResetsClock()
    {
        using var container = new RecordingContainer();
        var time = new ManualTime();
        TimeSpan? due = null;
        var visible = new RectF(0, 0, 100, 100);
        using var driver = new AnimationDriver(container.Document, value => due = value, () => { }, () => visible, time);
        driver.Resume();
        const string html = "<style>html,body{margin:0}div{margin-top:500px;width:20px;height:20px;animation:fade 1s linear infinite}@keyframes fade{from{opacity:1}to{opacity:.2}}</style><div></div>";
        container.Document.Load(html);
        driver.Synchronize();
        driver.Render(100);
        driver.FrameCompleted();
        Assert.Null(due);
        Assert.True(driver.NeedsAnimationSample);
        time.Advance(750);
        visible = new RectF(0, 500, 100, 100);
        driver.Synchronize();
        driver.Render(100);
        driver.FrameCompleted();
        Assert.NotNull(due);
        // Replacement resets the timeline even when invoked directly on Document.
        container.Document.Load("<style>@keyframes fade{to{opacity:.2}}div{height:20px;animation:fade 1s 500ms}</style><div></div>");
        driver.Synchronize();
        visible = new RectF(0, 0, 100, 100);
        driver.Render(100);
        driver.FrameCompleted();
        Assert.Equal(500, due!.Value.TotalMilliseconds, 5);
    }

    [Fact]
    public void SuspendAndDisposeCancelSchedulingAndIgnoreLateCallbacks()
    {
        using var container = new RecordingContainer();
        var requests = 0;
        TimeSpan? due = null;
        var driver = new AnimationDriver(container.Document, value => due = value, () => requests++,
            () => new RectF(0, 0, 100, 100));
        driver.Resume();
        container.Document.Load("<p>text</p>");
        driver.Suspend();
        var before = requests;
        driver.Pulse(() => { driver.Render(100); driver.FrameCompleted(); });
        Assert.Equal(before, requests);
        Assert.Null(due);
        driver.Resume();
        Assert.True(requests > before);
        driver.Dispose();
        before = requests;
        driver.Pulse(() => { driver.Render(100); driver.FrameCompleted(); });
        container.Document.Load("<p>replacement</p>");
        Assert.Equal(before, requests);
        Assert.Null(due);
    }
}
