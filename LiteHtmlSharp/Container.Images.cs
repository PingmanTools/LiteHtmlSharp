namespace LiteHtmlSharp;

public partial class Container
{
    private sealed class ImageEntry
    {
        internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly CancellationTokenSource Cancellation = new();
        internal CancellationToken Token => Cancellation.Token;
        internal bool Retired;
        internal IImageSource? Source;
        internal bool Redraw;
        internal int ReadyGeneration = -1;
        internal string Label = "", BaseLabel = "";
    }

    private readonly object imageGate = new();
    private readonly Dictionary<(string Source, string BaseUrl), ImageEntry> images = [];
    private readonly SynchronizationContext? imageContext = SynchronizationContext.Current;
    private bool imagesDisposed;
    private HashSet<ImageEntry> activeImages = [];
    private HashSet<ImageEntry>? pendingImages;
    private int imageGeneration;
    public FrameClock ImageClock
    {
        get;
    }
    /// <summary>Raised once a source has its intrinsic size. Hosts can schedule layout outside native callbacks.</summary>
    public event Action<string, string>? ImageReady;
    public event Action<string, string, Exception>? ImageLoadFailed;
    public Exception? LastImageError
    {
        get; private set;
    }

    protected virtual ValueTask<IImageSource?> LoadImageSourceAsync(string source, string baseUrl,
        CancellationToken cancellationToken) => ValueTask.FromResult<IImageSource?>(null);

    protected virtual (string Source, string BaseUrl) ResolveImageKey(string source, string baseUrl) => (source, baseUrl);

    public Task LoadImageAsync(string source, string baseUrl, bool redrawOnReady = true)
    {
        var key = ResolveImageKey(source, baseUrl);
        ImageEntry entry;
        lock (imageGate)
        {
            ObjectDisposedException.ThrowIf(imagesDisposed, this);
            if (images.TryGetValue(key, out entry!))
            {
                entry.Redraw |= redrawOnReady;
                (pendingImages ?? activeImages).Add(entry);
                if (pendingImages == null && entry.Source != null) ImageClock.Add(entry.Source);
                return entry.Completion.Task;
            }
            entry = new ImageEntry { Redraw = redrawOnReady, Label = source, BaseLabel = baseUrl };
            images.Add(key, entry);
            (pendingImages ?? activeImages).Add(entry);
        }
        _ = DecodeImage(source, baseUrl, key, entry);
        return entry.Completion.Task;
    }

    private async Task DecodeImage(string source, string baseUrl, (string Source, string BaseUrl) key, ImageEntry entry)
    {
        IImageSource? decoded = null;
        var token = entry.Token;
        try
        {
            decoded = await LoadImageSourceAsync(key.Source, key.BaseUrl, token).ConfigureAwait(false);
            lock (imageGate)
            {
                if (imagesDisposed || entry.Retired)
                    return;
                if (decoded != null)
                {
                    if (activeImages.Contains(entry)) ImageClock.Add(decoded);
                    entry.Source = decoded;
                    decoded = null;
                }
            }
            if (entry.Source != null) NotifyReady(entry);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            LastImageError = error;
            NotifyImage(entry, () => ImageLoadFailed?.Invoke(source, baseUrl, error));
        }
        finally
        {
            try
            {
                decoded?.Dispose();
            }
            catch (Exception error) { LastImageError = error; }
            finally
            {
                entry.Cancellation.Dispose();
                entry.Completion.TrySetResult();
            }
        }
    }

    private void NotifyReady(ImageEntry entry) => NotifyImage(entry, () =>
    {
        lock (imageGate)
        {
            if (entry.ReadyGeneration == imageGeneration) return;
            entry.ReadyGeneration = imageGeneration;
        }
        ImageReady?.Invoke(entry.Label, entry.BaseLabel);
        if (entry.Redraw) RequestRedraw(Viewport);
    });

    internal void BeginImageLoad()
    {
        lock (imageGate) pendingImages = [];
    }

    internal void EndImageLoad(bool success)
    {
        ImageEntry[] ready = [], retired = [];
        IImageSource[] released = [];
        try
        {
            lock (imageGate)
            {
                var requested = pendingImages;
                pendingImages = null;
                if (requested == null || imagesDisposed) return;
                if (success)
                {
                    activeImages = requested;
                    imageGeneration++;
                }
                // A failed replacement preserves the old page, but its newly
                // requested images must not accumulate either.
                retired = images.Values.Where(entry => !activeImages.Contains(entry)).ToArray();
                foreach (var entry in retired) entry.Retired = true;
                foreach (var key in images.Where(pair => pair.Value.Retired).Select(pair => pair.Key).ToArray())
                    images.Remove(key);
                var retainedSources = activeImages.Where(entry => entry.Source != null).Select(entry => entry.Source!)
                    .ToHashSet<IImageSource>(ReferenceEqualityComparer.Instance);
                released = retired.Where(entry => entry.Source != null).Select(entry => entry.Source!)
                    .Distinct<IImageSource>(ReferenceEqualityComparer.Instance)
                    .Where(source => !retainedSources.Contains(source)).ToArray();
                foreach (var source in released) ImageClock.Remove(source);
                foreach (var entry in retired) entry.Source = null;
                if (success)
                {
                    ready = activeImages.Where(entry => entry.Source != null).ToArray();
                    foreach (var entry in ready) ImageClock.Add(entry.Source!);
                }
            }
        }
        finally
        {
            ReleaseImages(retired, released);
        }
        foreach (var entry in ready) NotifyReady(entry);
    }

    private void ReleaseImages(ImageEntry[] entries, IImageSource[] sources, bool throwOnError = false)
    {
        List<Exception>? errors = null;
        foreach (var entry in entries)
        {
            if (entry.Completion.Task.IsCompleted) continue;
            try { entry.Cancellation.Cancel(); }
            catch (ObjectDisposedException) { } // Decoding already completed.
            catch (Exception error) { LastImageError = error; (errors ??= []).Add(error); }
        }
        foreach (var source in sources)
        {
            try { source.Dispose(); }
            catch (Exception error) { LastImageError = error; (errors ??= []).Add(error); }
        }
        if (throwOnError && errors != null) throw new AggregateException(errors);
    }

    private void NotifyImage(ImageEntry entry, Action notification)
    {
        void Invoke()
        {
            lock (imageGate)
            {
                if (imagesDisposed || !activeImages.Contains(entry))
                    return;
            }
            try
            {
                notification();
            }
            catch (Exception error) { LastImageError = error; }
        }
        // Loading can complete synchronously inside a native callback; UI work must wait for it to return.
        try
        {
            if (imageContext != null)
                imageContext.Post(_ => Invoke(), null);
            else
                ThreadPool.QueueUserWorkItem(_ => Invoke());
        }
        catch (Exception error) { LastImageError = error; }
    }

    private IImageSource? FindImage(string source, string baseUrl)
    {
        var key = ResolveImageKey(source, baseUrl);
        lock (imageGate)
            return !imagesDisposed && images.TryGetValue(key, out var entry) ? entry.Source : null;
    }

    /// <summary>Returns the currently composited platform handle; its lifetime belongs to this container.</summary>
    public object? GetImageFrame(string source, string baseUrl)
    {
        var key = ResolveImageKey(source, baseUrl);
        lock (imageGate)
        {
            var image = !imagesDisposed && images.TryGetValue(key, out var entry) ? entry.Source : null;
            return image?.GetFrame(ImageClock.GetFrameIndex(image));
        }
    }

    internal void UpdateImageVisibility(DisplayList list)
    {
        lock (imageGate)
        {
            if (imagesDisposed || activeImages.Count == 0) return;
            var visible = new HashSet<IImageSource>(ReferenceEqualityComparer.Instance);
            foreach (var command in list)
            {
                string? source = null, baseUrl = null;
                if (command.Type == CommandType.Image)
                {
                    var image = command.Read<Interop.lh_cmd_image>();
                    source = list.GetText(image.url); baseUrl = list.GetText(image.base_url);
                }
                else if (command.Type == CommandType.ListMarker)
                {
                    var marker = command.Read<Interop.lh_cmd_list_marker>();
                    source = list.GetText(marker.image); baseUrl = list.GetText(marker.baseurl);
                }
                if (!string.IsNullOrEmpty(source) &&
                    images.TryGetValue(ResolveImageKey(source, baseUrl!), out var entry) && entry.Source != null)
                    visible.Add(entry.Source);
            }
            foreach (var entry in activeImages)
                if (entry.Source != null) ImageClock.SetVisible(entry.Source, visible.Contains(entry.Source));
        }
    }

    private void OnImageFrameChanged(IImageSource source)
    {
        lock (imageGate)
        {
            if (imagesDisposed || !activeImages.Any(entry => ReferenceEquals(entry.Source, source)))
                return;
        }
        RequestRedraw(Viewport);
    }

    private void DisposeImages()
    {
        IImageSource[] owned;
        ImageEntry[] entries;
        lock (imageGate)
        {
            if (imagesDisposed)
                return;
            imagesDisposed = true;
            entries = images.Values.ToArray();
            foreach (var entry in entries) entry.Retired = true;
            owned = images.Values.Where(entry => entry.Source != null).Select(entry => entry.Source!).Distinct<IImageSource>(ReferenceEqualityComparer.Instance).ToArray();
            foreach (var entry in entries) entry.Source = null;
            images.Clear();
            activeImages.Clear();
            pendingImages = null;
        }
        ImageClock.FrameChanged -= OnImageFrameChanged;
        ImageClock.Dispose();
        ReleaseImages(entries, owned, throwOnError: true);
    }
}
