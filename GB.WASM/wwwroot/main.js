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

function setupBuffer(rgbaView, width, height) {
    _rgbaView = rgbaView;
    _width = width;
    _height = height;
}

async function outputImage() {
    const rgbaCopy = new Uint8ClampedArray(_rgbaView.slice());
    const imageData = new ImageData(rgbaCopy, _width, _height, {});
    const image = await createImageBitmap(imageData);
    drawContext.drawImage(image, 0, 0, _width * 4, _height * 4);
}

setModuleImports('main.js', {
    setupBuffer,
    outputImage,
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

await runMain();