using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

public partial class Interop
{
    [JSImport("setupBuffer", "main.js")]
    internal static partial void SetupBuffer([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> rgbaView, int width, int height);

    [JSImport("outputImage", "main.js")]
    internal static partial Task OutputImage();

    [JSExport]
    internal static Task KeyDown(string keyCode)
    {
        Game.OnKeyDown(keyCode);
        return Task.CompletedTask;
    }

    [JSExport]
    internal static Task KeyUp(string keyCode)
    {
        Game.OnKeyUp(keyCode);
        return Task.CompletedTask;
    }

    public static WebGame Game { get; set; }
}