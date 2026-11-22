# Architettura di NDIPlayer

Analisi del repository alla versione applicativa 0.13.8 e del modulo Companion 1.0.0. L'applicazione è un processo Windows x64 con interfaccia WinForms; il modulo Bitfocus Companion è un progetto Node.js separato.

## Flusso dei dati

```text
Cartella immagini / video MOV
        │
        ├─ Library + LibraryBrowser → selezione, blocchi, numeri, miniature
        │                                  │
        │                                  └─ PreviewCanvas (monitor locale)
        └─ Renderer / VideoSession (FFmpeg) ─→ frame BGRA con alpha
                                              │
                            ┌─────────────────┴─────────────────┐
                            │                                   │
                     Playout Program                     Playout Preview
                            │                                   │
                     NdiSender Program                   NdiSender Preview
                            └───────────────┬───────────────────┘
                                            NDI Runtime

AudioDecoder (FFmpeg) → AudioEngine (NAudio) → WDM/WASAPI o ASIO locali
Companion TCP in ingresso → comandi UI; eventi Program → TCP in uscita
```

Il monitor Program visualizza i frame che il sender locale ha accettato; non riceve il segnale da WATCHOUT. L'audio dei MOV esce dalle schede locali e non viene inserito nei due flussi NDI.

## Componenti

| Area | File principali | Ruolo |
| --- | --- | --- |
| Interfaccia e stato | `src/App.cs`, `ShellUi.cs`, `WindowPlacement.cs`, `CountdownUi.cs` | Finestra principale, controlli, timer UI e preferenze locali. `MainWindow` è una classe `partial` divisa tra più file. |
| Libreria e rendering | `Core.cs`, `GpuRenderer.cs`, `LibraryBrowser.cs`, `LibraryNavigation.cs`, `LibraryActions.cs`, `PreviewCanvas.cs`, `ImageNumbers.cs` | Scansione, filtri, blocchi, miniature, trasformazioni e frame BGRA. GDI+ decodifica le immagini; Direct3D 11 compone immagini e dissolvenze sulla GPU. |
| Progetti | `Projects.cs`, `ProjectUi.cs` | Serializzazione JSON `.ndiproj` versione 2, percorsi relativi quando possibile, backup `.bak` e ricollegamento cartella. Le immagini restano esterne al progetto. |
| Video | `Video.cs`, `VideoClock.cs`, `VideoUi.cs` | Processi `ffmpeg.exe`/`ffprobe.exe`, decodifica MOV, loop, durata e countdown. Program e Preview possono avere decoder distinti. |
| NDI | `Ndi.cs`, `PreviewOutput.cs` | Binding P/Invoke al runtime NDI nativo, due sender, thread di playout, transizioni, clear trasparente e snapshot del frame inviato. |
| Audio | `AudioModel.cs`, `AudioDecoder.cs`, `AudioEngine.cs`, `AudioDevices.cs`, `AudioUi.cs` | Decodifica audio, mix e dissolvenze, gain/mute, vumeter e uscite locali tramite NAudio. |
| Controllo remoto | `CompanionControl.cs`, `CompanionControlUi.cs`, `CompanionTcp.cs`, `CompanionUi.cs` | Server TCP per comandi, client TCP per notifiche verso Companion e relative impostazioni. |
| Modulo Companion | `companion-module-ndiplayer/src/*.js` | Client TCP con riconnessione, azioni, preset, feedback e variabili di Bitfocus Companion. |

Le preferenze dell'app sono in `%LOCALAPPDATA%\WatchoutImageNDI\settings.json`; geometria della finestra e ultimo progetto sono in `settings.json.window.json`. I due flussi NDI condividono formato e fps, ma hanno nomi e avvio/arresto indipendenti. L'invio al Program è manuale; la selezione aggiorna l'anteprima.

## Toolchain e dipendenze

| Strumento | Impiego | Dove è configurato |
| --- | --- | --- |
| Windows 10/11 x64, .NET Framework 4.8 | Esecuzione dell'app | `README.md`, `installer/NDIPlayer.iss` |
| C# / WinForms / Direct3D 11; `csc.exe` di .NET Framework | Compilazione x64 senza file `.csproj` | `build.ps1`, `installer/prepare-gpu.ps1` |
| SharpDX 4.2.0 | Binding Direct3D 11, DXGI e compilatore shader | `installer/prepare-gpu.ps1`, `third-party/SharpDX-LICENSE.txt` |
| NDI 6 Runtime 6.4.0.2 | SDK nativo per l'invio NDI, caricato tramite ABI C | `src/Ndi.cs`, `installer/build-installer.ps1` |
| FFmpeg/ffprobe n8.1, build LGPL del 9 novembre 2026 | Decodifica MOV e audio, metadati | `installer/prepare-video.ps1`, `third-party/FFmpeg-NOTICE.txt` |
| NAudio 1.10.0 | Uscite audio WDM/WASAPI e ASIO | `installer/prepare-audio.ps1`, `third-party/NAudio-NOTICE.txt` |
| PowerShell | Preparazione dipendenze, build e test | `build.ps1`, `installer/*.ps1` |
| Inno Setup 6.5+ o 7 | Installer Windows | `installer/NDIPlayer.iss` |
| Node.js 26, npm, `@companion-module/base` 2.1.3, `@companion-module/tools` 3.1.1 | Modulo Companion e pacchetto `.tgz` | `companion-module-ndiplayer/package.json` |
| GitHub Actions | Build Windows, test e artifact dell'installer e del modulo | `.github/workflows/*.yml` |

`tests/*.cs` contiene i test applicativi, compilati nello stesso eseguibile `bin/Tests.exe`; `companion-module-ndiplayer/tests/*.js` contiene i test del modulo. Le prove hardware NDI, WATCHOUT e schede audio richiedono collaudo sull'impianto reale.
