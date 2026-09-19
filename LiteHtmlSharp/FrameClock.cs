namespace LiteHtmlSharp;

/// <summary>Advances image frames using one timer, or explicit elapsed time for a host-owned clock.</summary>
public sealed class FrameClock : IDisposable
{
    private sealed class Playback
    {
        internal required long[] Delays;
        internal required long Duration;
        internal required int Loops;
        internal decimal Elapsed;
        internal long Timestamp;
        internal int Frame;
        internal bool Finished;
        internal bool Visible = true;
    }

    private readonly object gate = new();
    private readonly Dictionary<IImageSource, Playback> sources = new(ReferenceEqualityComparer.Instance);
    private readonly TimeProvider time;
    private readonly SynchronizationContext? context;
    private readonly ITimer? timer;
    private bool disposed;
    private int queued;
    public event Action<IImageSource>? FrameChanged;
    public Exception? LastCallbackError
    {
        get; private set;
    }

    public FrameClock(bool automatic = true, SynchronizationContext? synchronizationContext = null,
        TimeProvider? timeProvider = null)
    {
        time = timeProvider ?? TimeProvider.System;
        context = synchronizationContext ?? SynchronizationContext.Current;
        if (automatic)
            timer = time.CreateTimer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Add(IImageSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (sources.ContainsKey(source)) return;
        }
        if (source.FrameCount < 1 || source.FrameDelays.Count != source.FrameCount || source.LoopCount < 0)
            throw new ArgumentException("Image frame metadata is invalid.", nameof(source));
        var delays = new long[source.FrameCount];
        long duration = 0;
        for (var i = 0; i < delays.Length; i++)
        {
            // Missing GIF delays must not create an unbounded zero-duration animation.
            delays[i] = source.FrameDelays[i] <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(100).Ticks : source.FrameDelays[i].Ticks;
            duration = checked(duration + delays[i]);
        }
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            sources.TryAdd(source, new Playback { Delays = delays, Duration = duration, Loops = source.LoopCount, Timestamp = time.GetTimestamp(), Finished = delays.Length == 1 });
            UpdateTimer();
        }
    }

    public void Remove(IImageSource source)
    {
        lock (gate)
        {
            sources.Remove(source);
            if (!disposed)
                UpdateTimer();
        }
    }

    public int GetFrameIndex(IImageSource source)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return sources.TryGetValue(source, out var playback) ? playback.Frame : 0;
        }
    }

    /// <summary>Stop scheduling hidden images without pausing their elapsed timeline.</summary>
    public void SetVisible(IImageSource source, bool visible)
    {
        lock (gate)
        {
            if (disposed || !sources.TryGetValue(source, out var playback) || playback.Visible == visible) return;
            playback.Visible = visible;
            if (visible && timer != null && !playback.Finished)
            {
                var now = time.GetTimestamp();
                playback.Elapsed += time.GetElapsedTime(playback.Timestamp, now).Ticks;
                playback.Timestamp = now;
                Sample(playback);
            }
            UpdateTimer();
        }
    }

    private static void Sample(Playback playback)
    {
        if (playback.Loops > 0 && playback.Elapsed >= (decimal)playback.Duration * playback.Loops)
        {
            playback.Frame = playback.Delays.Length - 1;
            playback.Finished = true;
        }
        else
        {
            var position = playback.Elapsed % playback.Duration;
            var index = 0;
            while (index < playback.Delays.Length - 1 && position >= playback.Delays[index])
                position -= playback.Delays[index++];
            playback.Frame = index;
            if (playback.Loops == 0)
                playback.Elapsed %= playback.Duration;
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        if (timer != null)
            throw new InvalidOperationException("Explicit advancement requires an automatic=false clock.");
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        AdvanceCore(elapsed);
    }

    private void AdvanceCore(TimeSpan? elapsed)
    {
        List<IImageSource>? changed = null;
        lock (gate)
        {
            if (disposed)
                return;
            var now = time.GetTimestamp();
            foreach (var (source, playback) in sources)
            {
                if (playback.Finished)
                    continue;
                playback.Elapsed += (elapsed ?? time.GetElapsedTime(playback.Timestamp, now)).Ticks;
                playback.Timestamp = now;
                var frame = playback.Frame;
                Sample(playback);
                if (playback.Visible && frame != playback.Frame)
                    (changed ??= []).Add(source);
            }
            UpdateTimer();
        }
        if (changed != null)
            foreach (var source in changed)
                FrameChanged?.Invoke(source);
    }

    private void UpdateTimer()
    {
        if (timer == null || Volatile.Read(ref queued) != 0) return;
        var now = time.GetTimestamp();
        decimal? next = null;
        foreach (var playback in sources.Values)
        {
            if (playback.Finished || !playback.Visible) continue;
            var elapsed = playback.Elapsed + time.GetElapsedTime(playback.Timestamp, now).Ticks;
            decimal remaining;
            if (playback.Loops > 0 && elapsed >= (decimal)playback.Duration * playback.Loops)
                remaining = 0;
            else
            {
                var position = elapsed % playback.Duration;
                var index = 0;
                while (index < playback.Delays.Length - 1 && position >= playback.Delays[index])
                    position -= playback.Delays[index++];
                remaining = playback.Delays[index] - position;
            }
            next = next is null ? remaining : Math.Min(next.Value, remaining);
        }
        timer.Change(next is null ? Timeout.InfiniteTimeSpan :
            TimeSpan.FromTicks((long)Math.Clamp(decimal.Ceiling(next.Value), TimeSpan.TicksPerMillisecond,
                TimeSpan.TicksPerDay)), Timeout.InfiniteTimeSpan);
    }

    private void OnTimer(object? state)
    {
        if (Interlocked.Exchange(ref queued, 1) != 0)
            return;
        void Dispatch()
        {
            try
            {
                AdvanceCore(null);
            }
            catch (Exception error) { LastCallbackError = error; }
            finally
            {
                Volatile.Write(ref queued, 0);
                lock (gate) { if (!disposed) UpdateTimer(); }
            }
        }
        if (context == null)
            Dispatch();
        else
        {
            try
            {
                context.Post(_ => Dispatch(), null);
            }
            catch (Exception error) { LastCallbackError = error; Volatile.Write(ref queued, 0); }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            timer?.Dispose();
            sources.Clear();
        }
    }
}
