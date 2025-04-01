﻿using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

public partial class Interop
{
    [JSImport("setupBuffer", "main.js")]
    internal static partial void SetupBuffer([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> rgbaView, int width, int height);

    [JSImport("outputImage", "main.js")]
    internal static partial Task OutputImage();

    [JSImport("setupSoundBuffer", "main.js")]
    internal static partial void SetupSoundBuffer([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> soundBuffer, int length);

    [JSImport("outputSound", "main.js")]
    internal static partial Task OutputSound();
    
    [JSImport("outputSoundBuffer", "main.js")]
    internal static partial void OutputSoundBuffer([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> soundBuffer, int length);

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