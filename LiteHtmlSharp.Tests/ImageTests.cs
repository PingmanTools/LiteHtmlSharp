using Xunit;

namespace LiteHtmlSharp.Tests;

public class ImageTests
{
    private sealed class Frames(int loops = 0, params int[] milliseconds) : IImageSource
    {
        private readonly object[] frames = milliseconds.Select(_ => new object()).ToArray();
        public SizeF Size => new(24, 12);
        public int FrameCount => frames.Length;
        public IReadOnlyList<TimeSpan> FrameDelays { get; } = milliseconds.Select(value => TimeSpan.FromMilliseconds(value)).ToArray();
        public int LoopCount => loops;
        public int Disposals { get; private set; }
        public object GetFrame(int index) => frames[index];
        public void Dispose() => Disposals++;
    }

    private sealed class ImageContainer(FrameClock clock) : Container(frameClock: clock)
    {
        public readonly TaskCompletionSource<IImageSource?> Decode = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int DecodeCalls;
        public int Measurements;
        public CancellationToken Token;
        public readonly List<object?> Painted = [];
        protected override ValueTask<IImageSource?> LoadImageSourceAsync(string source, string baseUrl, CancellationToken cancellationToken)
        {
            DecodeCalls++;
            Token = cancellationToken;
            return new(Decode.Task);
        }
        public override float TextWidth(string text, nuint font) { Measurements++; return base.TextWidth(text, font); }
        public override void DrawImage(Layer layer, string source, string baseUrl) => Painted.Add(GetImageFrame(source, baseUrl));
    }

    private sealed class QueueContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> pending = new();
        public override void Post(SendOrPostCallback callback, object? state) => pending.Enqueue(() => callback(state));
        public void Drain() { while (pending.TryDequeue(out var action)) action(); }
    }

    private static ImageContainer Create(QueueContext context, FrameClock clock)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try { return new ImageContainer(clock); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private sealed class ManualTime : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public int TimersCreated;
        public TestTimer Timer = null!;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            TimersCreated++;
            return Timer = new TestTimer(callback, state);
        }
        public void Elapse(int milliseconds)
        {
            timestamp += TimeSpan.FromMilliseconds(milliseconds).Ticks;
            Timer.Fire();
        }
    }

    private sealed class TestTimer(TimerCallback callback, object? state) : ITimer
    {
        public bool Disposed;
        public bool Running;
        public TimeSpan Due, Period;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Due = dueTime; Period = period;
            Running = dueTime != Timeout.InfiniteTimeSpan;
            return !Disposed;
        }
        public void Fire() { if (!Disposed && Running) callback(state); }
        public void Dispose() { Disposed = true; Running = false; }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    [Fact]
    public void InvisibleGifStopsTimerAndResumesAtElapsedPosition()
    {
        var time = new ManualTime();
        var context = new QueueContext();
        using var source = new Frames(0, 100, 100, 100);
        using var clock = new FrameClock(synchronizationContext: context, timeProvider: time);
        clock.Add(source);
        clock.SetVisible(source, false);
        Assert.False(time.Timer.Running);
        time.Elapse(250);
        clock.SetVisible(source, true);
        Assert.Equal(2, clock.GetFrameIndex(source));
        Assert.True(time.Timer.Running);
        Assert.Equal(TimeSpan.FromMilliseconds(50), time.Timer.Due);
        clock.SetVisible(source, false);
        clock.Remove(source);
        Assert.False(time.Timer.Running);
    }

    [Fact]
    public void OneTimerDispatchesMultipleSourcesAndStopsWhenFinished()
    {
        var context = new QueueContext();
        var time = new ManualTime();
        using var first = new Frames(1, 50, 50);
        using var second = new Frames(1, 20, 20);
        using var clock = new FrameClock(synchronizationContext: context, timeProvider: time);
        var changes = 0;
        clock.FrameChanged += _ => changes++;
        clock.Add(first);
        clock.Add(second);
        Assert.Equal(1, time.TimersCreated);
        Assert.Equal(TimeSpan.FromMilliseconds(20), time.Timer.Due);
        Assert.Equal(Timeout.InfiniteTimeSpan, time.Timer.Period);
        time.Elapse(20);
        Assert.Equal(0, changes);
        context.Drain();
        Assert.Equal(1, changes);
        time.Elapse(30);
        context.Drain();
        Assert.Equal(2, changes);
        time.Elapse(50);
        context.Drain();
        Assert.False(time.Timer.Running);
        clock.Dispose();
        Assert.True(time.Timer.Disposed);
    }

    [Fact]
    public void UnequalDelaysAndFiniteLoopsStopOnFinalFrame()
    {
        using var source = new Frames(2, 100, 250);
        using var clock = new FrameClock(automatic: false);
        var changes = 0;
        clock.FrameChanged += _ => changes++;
        clock.Add(source);
        clock.Advance(TimeSpan.FromMilliseconds(99));
        Assert.Equal(0, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(1, clock.GetFrameIndex(source));
        Assert.Equal(3, changes);
    }

    [Fact]
    public void MissingDelaysUseFallbackAndLargeElapsedTimeSkipsWholeLoops()
    {
        using var source = new Frames(0, 0, -1);
        using var clock = new FrameClock(automatic: false);
        clock.Add(source);
        clock.Advance(TimeSpan.FromMilliseconds(99));
        Assert.Equal(0, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromDays(1000));
        Assert.Equal(1, clock.GetFrameIndex(source));
        clock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, clock.GetFrameIndex(source));
    }

    [Fact]
    public async Task ReadyNotifiesOnceAndTicksReplaySameSnapshotWithoutMeasuring()
    {
        var context = new QueueContext();
        using var container = Create(context, new FrameClock(automatic: false));
        var source = new Frames(0, 100, 250);
        var ready = 0;
        var redraws = 0;
        container.ImageReady += (_, _) => ready++;
        container.RedrawRequested += _ => redraws++;
        var load = container.LoadImageAsync("sample.gif", "", false);
        var duplicate = container.LoadImageAsync("sample.gif", "", true);
        Assert.Same(load, duplicate);
        Assert.Equal(default, container.GetImageSize("sample.gif", ""));
        container.Decode.SetResult(source);
        await load;
        context.Drain();
        Assert.Equal(1, ready);
        Assert.Equal(1, redraws);
        Assert.Equal(1, container.DecodeCalls);
        Assert.Equal(source.Size, container.GetImageSize("sample.gif", ""));
        container.Document.Load("<img src='sample.gif' width='24' height='12'><span>unchanging text</span>");
        container.Document.Render(300);
        var snapshot = container.Document.Draw(0, 0, container.Viewport);
        var measurements = container.Measurements;
        container.RedrawRequested += _ => DisplayListReplayer.Replay(snapshot, container);
        container.ImageClock.Advance(TimeSpan.FromMilliseconds(99));
        Assert.Equal(1, redraws);
        container.ImageClock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, redraws);
        Assert.Contains(source.GetFrame(1), container.Painted);
        Assert.Same(snapshot, container.Document.LastDisplayList);
        Assert.Equal(measurements, container.Measurements);
        container.Dispose();
        Assert.Equal(1, source.Disposals);
        container.ImageClock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(2, redraws);
    }

    [Fact]
    public async Task DisposeCancelsLoadingAndDisposesLateDecodedSource()
    {
        var context = new QueueContext();
        var container = Create(context, new FrameClock(automatic: false));
        var source = new Frames(0, 100, 200);
        var notices = 0;
        container.ImageReady += (_, _) => notices++;
        container.RedrawRequested += _ => notices++;
        var load = container.LoadImageAsync("late.gif", "");
        container.Dispose();
        Assert.True(container.Token.IsCancellationRequested);
        container.Decode.SetResult(source);
        await load;
        context.Drain();
        Assert.Equal(1, source.Disposals);
        Assert.Equal(0, notices);
        Assert.Throws<ObjectDisposedException>(() => { _ = container.LoadImageAsync("another.gif", ""); });
    }

    [Fact]
    public async Task DecoderFailureIsObservableWithoutUnobservedTaskException()
    {
        var context = new QueueContext();
        using var container = Create(context, new FrameClock(automatic: false));
        var error = new InvalidDataException("Synthetic corrupt GIF");
        Exception? reported = null;
        container.ImageLoadFailed += (_, _, exception) => reported = exception;
        var load = container.LoadImageAsync("broken.gif", "");
        container.Decode.SetException(error);
        await load;
        context.Drain();
        Assert.Same(error, container.LastImageError);
        Assert.Same(error, reported);
        Assert.Null(container.GetImageFrame("broken.gif", ""));
    }

    [Fact]
    public void ClockDoesNotOwnSourceAndRemovedSourceDoesNotAdvance()
    {
        using var source = new Frames(0, 20, 40);
        var clock = new FrameClock(automatic: false);
        clock.Add(source);
        clock.Remove(source);
        clock.Advance(TimeSpan.FromMilliseconds(20));
        Assert.Equal(0, clock.GetFrameIndex(source));
        clock.Dispose();
        Assert.Equal(0, source.Disposals);
    }
}
