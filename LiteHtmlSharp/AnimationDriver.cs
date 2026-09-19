namespace LiteHtmlSharp;

/// <summary>
/// Drives CSS motion using native frame results and a host's repeating UI timer.
/// All methods, timer callbacks, and document operations must run on the same UI thread.
/// The host renders through Render, calls FrameCompleted after drawing, and Synchronize before input.
/// </summary>
public sealed class AnimationDriver : IDisposable
{
    private readonly Document document;
    private readonly Action<TimeSpan?> schedule;
    private readonly Action requestFrame;
    private readonly Func<RectF> visibleBounds;
    private readonly TimeProvider time;
    private readonly double requestedCadence;
    private double Cadence => Math.Max(requestedCadence, document.Owner.MinimumAnimationFrameInterval.TotalMilliseconds);
    private long started;
    private int generation;
    private bool enabled, disposed;
    private TimeSpan? scheduled;
    private double nextDelay = -1;

    /// <summary>The current timer interval, or null when asleep.</summary>
    public TimeSpan? NextDelay => scheduled;

    /// <summary>True when a viewport change must resample motion, including motion asleep off-screen.</summary>
    public bool NeedsAnimationSample { get; private set; }

    /// <param name="schedule">Change the repeating UI timer interval; null stops it. Called only when the interval changes. Invoke Pulse when due.</param>
    /// <param name="requestFrame">Queue a render and draw outside native callbacks.</param>
    /// <param name="visibleBounds">Visible rectangle in document coordinates, including scroll offset.</param>
    public AnimationDriver(Document document, Action<TimeSpan?> schedule, Action requestFrame,
        Func<RectF> visibleBounds, TimeProvider? timeProvider = null, TimeSpan? frameInterval = null)
    {
        this.document = document ?? throw new ArgumentNullException(nameof(document));
        this.schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        this.requestFrame = requestFrame ?? throw new ArgumentNullException(nameof(requestFrame));
        this.visibleBounds = visibleBounds ?? throw new ArgumentNullException(nameof(visibleBounds));
        time = timeProvider ?? TimeProvider.System;
        requestedCadence = frameInterval?.TotalMilliseconds ?? 0;
        if (frameInterval.HasValue && (!double.IsFinite(requestedCadence) || requestedCadence <= 0)) throw new ArgumentOutOfRangeException(nameof(frameInterval));
        started = time.GetTimestamp();
        generation = document.Generation;
        document.Loaded += OnLoaded;
        document.Owner.AnimationFrameIntervalChanged += OnFrameIntervalChanged;
    }

    private void OnFrameIntervalChanged()
    {
        if (disposed || document.IsDisposed) return;
        // Enforce a raised minimum immediately; the queued frame recomputes shorter deadlines.
        FrameCompleted();
        if (enabled && document.HasLoadedHtml) requestFrame();
    }

    public void Resume()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        enabled = true;
        if (document.HasLoadedHtml) requestFrame();
    }

    public void Suspend()
    {
        if (disposed) return;
        enabled = false;
        SetSchedule(null);
    }

    private void OnLoaded()
    {
        started = time.GetTimestamp();
        generation = document.Generation;
        NeedsAnimationSample = false;
        nextDelay = -1;
        SetSchedule(null);
        if (enabled) requestFrame();
    }

    public void Synchronize()
    {
        if (disposed || document.IsDisposed || !document.HasLoadedHtml) return;
        if (generation != document.Generation) OnLoaded();
        document.SetTime(time.GetElapsedTime(started).TotalMilliseconds);
    }

    /// <summary>Runs one frame and returns the interval for subsequent timer pulses.</summary>
    /// <param name="renderFrame">Synchronously render and draw, using Render and FrameCompleted.</param>
    public TimeSpan? Pulse(Action renderFrame)
    {
        ArgumentNullException.ThrowIfNull(renderFrame);
        if (disposed || !enabled) return null;
        if (document.IsDisposed) { SetSchedule(null); return null; }
        // Present the final frame even if this pulse crosses the animation's end.
        // Render advances time and obtains the scheduling result in one native call.
        try { renderFrame(); }
        catch { SetSchedule(null); throw; }
        return NextDelay;
    }

    /// <summary>Render using the current clock and retain the returned scheduling hint.</summary>
    public float Render(float maxWidth)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (generation != document.Generation) OnLoaded();
        var frame = document.RenderFrame(maxWidth, time.GetElapsedTime(started).TotalMilliseconds,
            visibleBounds(), Cadence);
        NeedsAnimationSample = frame.Active;
        nextDelay = frame.NextDelay;
        return frame.Width;
    }

    /// <summary>Apply the frame's hint without querying the document again.</summary>
    public TimeSpan? FrameCompleted()
    {
        if (disposed || document.IsDisposed) return null;
        SetSchedule(!enabled || nextDelay < 0 ? null :
            TimeSpan.FromMilliseconds(Math.Clamp(Math.Max(nextDelay, Cadence), 1, 86400000)));
        return NextDelay;
    }

    private void SetSchedule(TimeSpan? delay)
    {
        // Keep the existing recurring timer for sub-millisecond deadline noise.
        if (scheduled == delay || (scheduled is { } before && delay is { } after &&
            Math.Abs((before - after).TotalMilliseconds) < 1)) return;
        scheduled = delay;
        schedule(delay);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        enabled = false;
        document.Loaded -= OnLoaded;
        document.Owner.AnimationFrameIntervalChanged -= OnFrameIntervalChanged;
        SetSchedule(null);
    }
}
