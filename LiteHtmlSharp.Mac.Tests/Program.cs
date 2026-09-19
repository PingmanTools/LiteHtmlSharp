using CoreGraphics;
using LiteHtmlSharp;
using LiteHtmlSharp.CoreGraphics;

static void Require(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}
static byte[] Pixel(IImageSource source, int frame, int x, int y)
{
    var image = (CGImage)source.GetFrame(frame);
    var bytes = new byte[(int)(image.Width * image.Height * 4)];
    using var space = CGColorSpace.CreateDeviceRGB();
    using var bitmap = new CGBitmapContext(bytes, image.Width, image.Height, 8, image.Width * 4, space,
                                           CGImageAlphaInfo.PremultipliedLast);
    bitmap.DrawImage(new CGRect(0, 0, image.Width, image.Height), image);
    return bytes.AsSpan((y * (int)image.Width + x) * 4, 4).ToArray();
}
using var resource =
    System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("disposal.gif")!;
using var input = new MemoryStream();
resource.CopyTo(input);
var bytes = input.ToArray();
using var source = CGImageSourceFrames.Decode(bytes);
Require(source.FrameCount == 3 && source.LoopCount == 0, "GIF frame/loop metadata");
Require(source.FrameDelays.Select(x => x.TotalMilliseconds).SequenceEqual(new double[] { 80, 140, 220 }),
        "GIF frame delays");
Require(Pixel(source, 0, 0, 0).SequenceEqual(new byte[] { 255, 0, 0, 255 }), "initial red canvas");
Require(Pixel(source, 1, 0, 0).SequenceEqual(new byte[] { 0, 0, 255, 255 }), "blue overlay frame");
Require(Pixel(source, 2, 0, 0).SequenceEqual(new byte[] { 255, 0, 0, 255 }), "restore-previous disposal");
Require(Pixel(source, 2, 3, 3).SequenceEqual(new byte[] { 0, 128, 0, 255 }), "green frame rectangle");
using (var clock = new FrameClock(automatic: false))
{
    clock.Add(source);
    clock.Advance(TimeSpan.FromMilliseconds(80));
    Require(clock.GetFrameIndex(source) == 1, "clock advances decoded GIF");
    clock.Advance(TimeSpan.FromMilliseconds(140));
    Require(clock.GetFrameIndex(source) == 2, "clock retains disposal result");
}
try
{
    using var invalid = CGImageSourceFrames.Decode(new byte[] { 1, 2, 3 });
    throw new Exception("malformed input accepted");
}
catch (InvalidDataException)
{
}
try
{
    using var cancelled = CGImageSourceFrames.Decode(bytes, new CancellationToken(true));
    throw new Exception("cancelled input accepted");
}
catch (OperationCanceledException)
{
}
var backgroundBytes = bytes.ToArray();
var controls = Enumerable.Range(0, backgroundBytes.Length - 3)
                   .Where(i => backgroundBytes[i] == 0x21 && backgroundBytes[i + 1] == 0xf9 &&
                               backgroundBytes[i + 2] == 4)
                   .ToArray();
Require(controls.Length == 3, "fixture graphics controls");
backgroundBytes[controls[1] + 3] = (byte)((backgroundBytes[controls[1] + 3] & ~0x1c) | 8);
using (var background = CGImageSourceFrames.Decode(backgroundBytes))
{
    Require(Pixel(background, 2, 0, 0)[3] == 0, "restore-background disposal clears prior frame rectangle");
    Require(Pixel(background, 2, 3, 3).SequenceEqual(new byte[] { 0, 128, 0, 255 }),
            "restore-background next frame");
}
var repeatBytes = bytes.ToArray();
var appIndex =
    System.Text.Encoding.ASCII.GetString(repeatBytes).IndexOf("NETSCAPE2.0", StringComparison.Ordinal);
Require(appIndex >= 0, "fixture loop extension");
repeatBytes[appIndex + 13] = 2;
repeatBytes[appIndex + 14] = 0;
using (var repeat = CGImageSourceFrames.Decode(repeatBytes))
{
    Require(repeat.LoopCount == 3, "GIF repeats convert to complete plays");
}
Console.WriteLine("PASS: CoreGraphics GIF previous/background disposal, delays, loops, shared clock, " +
                  "malformed/cancelled input");
