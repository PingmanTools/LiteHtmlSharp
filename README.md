

# Background and "features"

LiteHtmlSharp is a C# cross-platform library for HTML/CSS(2/3) visualization - self contained, fast and small.  
The HTML parser/renderer is based on the fantastic [litehtml](https://github.com/litehtml/litehtml) C++ library, 
and the drawing is done with platform-native controls (currently WPF and Cocoa).

> litehtml is the lightweight HTML rendering engine with CSS2/CSS3 support. Note, litehtml itself does
> not draw any text, pictures or other graphics and litehtml does not depend of any image/draw/font library. 
> You are free to use any library to draw images, fonts and any other graphics. litehtml just parses
> HTML/CSS and places the HTML elements into right position (renders HTML).


### HTML Visualization?  What does that mean?

We wanted to use HTML/css to do splash screen, about-box design, new version availability messaging, new 
user "greeting", getting started documentation, etc.  We wanted to have this tightly integrated, completely
in our control (no platform right-click menus, no Windows Internet Explorer compatibility problems) and 
appropriately sized (no 50mb distributions that require a chrome browser process running).

We couldn't find anything for .NET / C# that met our needs, but [litehtml](https://github.com/litehtml/litehtml)
sparked our imagination, and it's a stable, awesome library that knows how to do layout (but not draw) for
just about everything we needed.

So we wrapped [litehtml](https://github.com/litehtml/litehtml) to make it work for C#, then built a drawing
abstraction layer and OSX and WPF implementations.  Cool.  It *could* work for Android, iOS, Linx, too - we've
just not done the visualization part for that yet.  Several other parts are not yet fully flushed out, too.


## LiteHtmlLib (C++)
The first layer of this library is a C++ project (see the LiteHtmlLib directory) that wraps the litehtml 
[document_container](https://github.com/litehtml/litehtml/wiki/document_container) abstract class
in a way that the C# PInvoke system can interact with. This code along with the litehtml C++ is compiled
into a native binary that our C# wrapper consumes with DLLImports. We've provided precompiled win32/64 DLLs
and a OSX 32/64 dylib.

### Compiling DLL on Windows
Use the `LiteHtmlLib/LiteHtmlLib.vcxproj` project in Visual Studio 2015.

### Compiling on OSX
Install CMake. Open `LiteHtmlLib/LiteHtml.dyblib.mdproj` in Xamarin Studio and run build. 

Or just call the build script from terminal:

```bash
cd LiteHtmlLib
sh build.sh
```

### Compiling on Linux
The above build script should work but hasn't been tested.


## LiteHtmlSharp
Second layer is a C# shared project that provides a C# abstract class equivalent of the C++ document_container. 
Most of the of the structs and class methods have been ported.  

#### Usage Documentation
If you are wanting to just display HTML in a WPF or Xamarin.Mac/MonoMac app, then use our platform
implementations (see below). Otherwise you will have to derive the base C# container and implement
the drawing API for your platform. The `Container` class definition more-or-less mirrors the
original C++ one but with C#-style syntax. The API should be close enough to be able to
use the original litehtml usage documentation: 
[document_container](https://github.com/litehtml/litehtml/wiki/document_container) and
[how to use litehtml](https://github.com/litehtml/litehtml/wiki/How-to-use-litehtml)

Our WPF & Mac implementations can be used as examples when implementing a new platform. 
Some cool, unimplemented ideas:

* An OpenTK implementation (provides html -> OpenGL rendering on most platforms)
* A Mono.Cairo implementation
* A Winforms GTK+ (System.Drawing) implementation



## Platform Implementations

### Core Graphics (Apple platforms)
The LiteHtmlSharp.CoreGraphics shared project contains `CGContainer` (derived from litehtml container) which draws to
a `CoreGraphics.CGContext` instance. Xamarin.iOS and Xamarin.Mac Unified projects can use this project.

### Xamarin.Mac
The `LiteHtmlSharp.Mac` library contains a litehtml-`NSView` and a litehtml-`NSWindow`. Usage examples:

#### LiteHtmlNSView
```cs
var myView = new LiteHtmlNSView(new CGRect(0, 0, 400, 500), File.ReadAllText("master.css"));
myView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(url);
myView.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(url);
myView.LoadHtml(File.ReadAllText("index.html"));
windowContentView.AddSubview(myView);
```

#### LiteHtmlNSWindow
```cs
var htmlWindow = new LiteHtmlNSWindow(
                    new CGRect(0, 0, 400, 400), 
                    NSWindowStyle.Closable | NSWindowStyle.Titled | NSWindowStyle.Resizable, 
                    File.ReadAllText("master.css")
                );
htmlWindow.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(url);
htmlWindow.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(url);
htmlWindow.LiteHtmlView.LoadHtml(File.ReadAllText("index.html"));
htmlWindow.MakeKeyAndOrderFront(this);
```

### WPF
```cs
// code
```