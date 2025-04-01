// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const drawContext = document.getElementById('viewport').getContext('2d');
let _rgbaView = null;
let _width = null;
let _height = null;

let _soundBuffer = null;
let _soundBufferLength = 0;
let _audioCtx = null;
let _audioBuffer = null;
let _leftChannel = null;
let _rightChannel = null;

function setupBuffer(rgbaView, width, height) {
    _rgbaView = rgbaView;
    _width = width;
    _height = height;
}

function setupSoundBuffer(soundBuffer, length) {
    _soundBuffer = soundBuffer;
    _soundBufferLength = length;

    _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    _audioBuffer = _audioCtx.createBuffer(2, _soundBufferLength, 22050);
    _leftChannel = _audioBuffer.getChannelData(0);
    _rightChannel = _audioBuffer.getChannelData(1);
}

async function outputImage() {
    const rgbaCopy = new Uint8ClampedArray(_rgbaView.slice());
    const imageData = new ImageData(rgbaCopy, _width, _height, {});
    const image = await createImageBitmap(imageData);
    drawContext.drawImage(image, 0, 0, _width * 4, _height * 4);
}

async function outputSound() {
    const soundBufferCopy = new Uint8ClampedArray(_soundBuffer.slice());
    if (_audioCtx === undefined || _audioCtx === null || _audioCtx.state !== 'running') {
        return;
    }

    let j = 0;
    for (let i = 0; i < _soundBufferLength; i += 2) {
        _leftChannel[j] = soundBufferCopy[i] / 128.0 - 1.0;
        _rightChannel[j++] = soundBufferCopy[i + 1] / 128.0 - 1.0;
    }

    const source = _audioCtx.createBufferSource();
    source.buffer = _audioBuffer;
    source.connect(_audioCtx.destination);
    
    const promise = new Promise((resolve) => {
        source.onended = () => resolve();
    })
    source.start();
    
    await promise;
}

setModuleImports('main.js', {
    setupBuffer,
    setupSoundBuffer,
    outputImage,
    outputSound
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

window.addEventListener('keydown', async (e) => {
    if (e.isComposing || e.keyCode === 229) {
        return;
    }

    await exports.Interop.KeyDown(e.code);
});

window.addEventListener('keyup', async (e) => {
    if (e.isComposing || e.keyCode === 229) {
        return;
    }

    await exports.Interop.KeyUp(e.code);
});

const button = document.getElementById('audioButton');
button.addEventListener('click', toggleAudio);

async function toggleAudio() {
    if (_audioCtx === undefined || _audioCtx === null) {
        return;
    }

    if (_audioCtx.state === 'suspended') {
        await _audioCtx.resume();
        button.textContent = 'Suspend audio';
    }
    else if (_audioCtx.state === 'running') {
        await _audioCtx.suspend();
        button.textContent = 'Resume audio';
    }
}

await runMain();