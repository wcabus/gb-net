using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GB.Core.Controller;
using GB.Core.Gui;
using GB.Core.Sound;
using GB.WASM;

public sealed class WebGame : IController, IDisposable
{
    private IButtonListener _listener;
    private readonly WebDisplay _display = new();

    // private readonly ISoundOutput _soundOutput = new SoundOutput();
    private readonly ISoundOutput _soundOutput = new NullSoundOutput();
    private readonly Emulator _emulator;
    private readonly Dictionary<string, Button> _controls;

    private CancellationTokenSource _cancellation;

    public WebGame()
    {
        _emulator = new Emulator
        {
            Display = _display,
            SoundOutput = _soundOutput
        };

        _controls = new Dictionary<string, Button>
        {
            {"KeyA", Button.Left},
            {"ArrowLeft", Button.Left},
            {"KeyD", Button.Right},
            {"ArrowRight", Button.Right},
            {"KeyW", Button.Up},
            {"ArrowUp", Button.Up},
            {"KeyS", Button.Down},
            {"ArrowDown", Button.Down},
            {"KeyK", Button.A},
            {"Space", Button.A},
            {"KeyO", Button.B},
            {"Enter", Button.Start},
            {"Backspace", Button.Select}
        };

        _cancellation = new CancellationTokenSource();

        _emulator.Controller = this;
    }

    public void StartGame()
    {
        StopEmulator();

        _emulator.EnableBootRom = true;
        _emulator.RomStream = new MemoryStream(TestResources.SUPERMARIOLAND);
        _emulator.Run(_cancellation.Token);
    }

    public void StopEmulator()
    {
        if (!_emulator.Active)
        {
            return;
        }

        _emulator.Stop(_cancellation);
        _soundOutput.Stop();

        _cancellation = new CancellationTokenSource();
        _display.Enabled = false;
    }

    public void OnKeyDown(string keyCode)
    {
        var button = _controls.GetValueOrDefault(keyCode);
        if (button != null)
        {
            _listener?.OnButtonPress(button);
        }
    }

    public void OnKeyUp(string keyCode)
    {
        var button = _controls.GetValueOrDefault(keyCode);
        if (button != null)
        {
            _listener?.OnButtonRelease(button);
        }
    }

    void IController.SetButtonListener(IButtonListener listener)
    {
        _listener = listener;
    }

    public void Dispose()
    {
        _cancellation?.Dispose();
    }
}
