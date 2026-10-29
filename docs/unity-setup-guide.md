# Unity setup and WebGL run guide

## Required environment

- Windows demonstration machine used for verification: Unity `6000.3.25f1`, Universal 2D project.
- Install `Web Build Support` for that exact Editor version in Unity Hub. The verified module root is `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\WebGLSupport` and includes `UnityEditor.WebGL.Extensions.dll` plus Emscripten.
- Add and open `C:\Users\Arc\Downloads\Micro\Unity`, which contains `Assets`, `Packages` and `ProjectSettings`.
- Open `Assets/CPS/Scenes/Boot.unity` before using Play Mode. Boot routes to Main Menu, then the selected profile and difficulty route to Gameplay.

## Play the current build

1. Create a local nickname or select an existing profile.
2. Select Easy, Normal, or Hard and choose Start Cooking.
3. Choose Controller from Home: Keyboard, FSR, or Load Cell. Keyboard uses `A`/left arrow and `D`/`B`/right arrow: released is `0%`, pressed is `100%`.
4. Catch food, avoid trash, and keep HP above zero for the 60-second round. Missing food or catching trash costs one HP.
5. Spacebar/Escape pauses or resumes with a three-second countdown. Resume explicitly after focus loss or sensor recovery. Results offers Restart and Main Menu.

Keyboard and FSR do not report Newtons. Load Cell reports estimated N only after both sensors are fresh and a requested tare completes. See [USB controller guide](unity-usb-controller-guide.md) for firmware, pins, browser support and Inspector thresholds. This USB delivery does not change MQTT, Node-RED, or the existing Sheets integration.

## Build WebGL

Close any open Unity Editor for this checkout, then run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\Arc\Downloads\Micro\Unity' `
  -buildTarget WebGL `
  -executeMethod CPS.KitchenCatch.Editor.WebGLBuild.Build `
  -logFile 'C:\Users\Arc\Downloads\Micro\verification\micro-webgl-build.log'
```

The build entry point refuses missing/disabled Boot, MainMenu, or Gameplay scenes. It opens the effective `GameSessionController` instance from `Gameplay.unity`, then rejects missing references, controller/spawner player mismatches, and invalid difficulty/object definitions before building. A successful release is written to `Builds/WebGL`. Build output is generated delivery material and is intentionally not committed.

## Serve locally

Do not open `index.html` through a `file://` URL. Serve the folder over HTTP with the correct Brotli and MIME headers, or use Unity's Build and Run. In particular:

- `*.wasm.br`: `Content-Type: application/wasm`, `Content-Encoding: br`
- `*.js.br`: `Content-Type: application/javascript`, `Content-Encoding: br`
- `*.data.br`: `Content-Type: application/octet-stream`, `Content-Encoding: br`

The verification helper is `verification/task8-webgl-server.py`; it binds only to `127.0.0.1:8123` and serves `Unity/Builds/WebGL` relative to this repository. Use `verification/task8-webgl-dev-server.py` for `Unity/Builds/WebGL-Dev` on port `8124`.

## Current delivery limits

- The project uses the supplied font and audio; hardware-dependent sensor latency and calibration still need a physical playtest.
- USB controllers are implemented for Windows Editor and Web Serial browsers. No real port was opened and no board was flashed during automated verification. Estimated Newton values need validation after assembly.
- Browser background tabs are not expected to sustain foreground frame timing. Resume explicitly after focus returns.
- Difficulty feel and spawn fairness still need a hands-on user playtest. No difficulty values were changed without measured evidence.
