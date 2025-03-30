using System;

Console.WriteLine("Loading...");
var gb = new WebGame();
Interop.Game = gb;

Console.WriteLine("Starting the emulator...");
gb.StartGame();
