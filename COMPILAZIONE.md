# Compilazione

Eseguire i comandi da PowerShell nella radice del repository su Windows x64. Servono .NET Framework 4.8 con `csc.exe` in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\`, accesso alla rete per la prima preparazione e PowerShell con `Invoke-WebRequest`/`Expand-Archive`.

## Applicazione e test

La build usa le quattro librerie SharpDX 4.2.0 per Direct3D 11 e NAudio 1.10.0 presenti in `third-party/binaries/`, copiandole in `bin/`. Se mancano dal repository, gli script `installer/prepare-gpu.ps1` e `prepare-audio.ps1` possono ancora scaricare i pacchetti verificati con SHA-256. Le DLL devono restare accanto all'eseguibile.

```powershell
./build.ps1
```

`build.ps1` prepara NAudio 1.10.0 e, se mancano FFmpeg o FFprobe, richiama `prepare-video.ps1`, che scarica e verifica lo ZIP FFmpeg fissato nello script e prepara `bin/video/` con eseguibili e DLL. Poi compila `bin/WatchoutImageNDI.exe` e `bin/Tests.exe` ed esegue i test. Per usare lo ZIP originale conservato localmente, eseguire prima `./installer/prepare-video.ps1 -Archive C:\percorso\ffmpeg-n8.1.3-27-g8dca1586f4-win64-lgpl-shared-8.1.zip`. La build mensile rimane disponibile dal fornitore solo per due anni: conservare lo ZIP verificato e le dipendenze in `bin\video-package` in un archivio duraturo. Una preparazione completamente offline richiede anche il pacchetto NAudio e i sorgenti FFmpeg e script di build corrispondenti già nella cache.

Per la prova opzionale del runtime NDI reale, installarlo prima e poi eseguire:

```powershell
./bin/Tests.exe --ndi
```

Per eseguire l'app compilata fuori dall'installer occorre anche il runtime NDI 6 installato. `bin/NAudio.dll` e `bin/video/` devono restare accanto all'eseguibile nella struttura prodotta dagli script.

## Installer Windows

Installare Inno Setup 6.5+ o 7 e procurarsi il redistributable ufficiale NDI 6.4.0.2. Lo script verifica SHA-256 e firma Authenticode del file: un download aggiornato a un'altra versione va verificato e richiede aggiornare il pin nello script.

```powershell
./installer/build-installer.ps1 -RuntimeFile C:\percorso\NDI6Runtime.exe -IsccPath C:\percorso\ISCC.exe
```

Lo script prepara sempre FFmpeg dall'archivio verificato, lancia la build e crea `dist/NDIPlayer-0.13.5-Setup-win-x64.exe`; non usa una copia già presente in `bin\video` come sostituto della dipendenza verificata. Per usare uno ZIP FFmpeg locale aggiungere `-VideoArchive C:\percorso\archivio.zip`. Il setup include l'app, NAudio, FFmpeg e il redistributable NDI; verifica .NET 4.8 sul PC di destinazione. La compilazione dell'installer richiede che il runtime fornito corrisponda all'hash atteso in `installer/build-installer.ps1`.

## Installazione dipendenze di sviluppo

Per preparare il runtime NDI e verificare .NET Framework, Node.js e Inno Setup:

```powershell
./install-dependencies.ps1 -InstallInnoSetup
```

Il runtime NDI e l'installer di Inno Setup vengono scaricati in `dependencies/`, esclusa da Git. I binari sono verificati con SHA-256 e firma Authenticode. Il comando installa Inno Setup in modalità silenziosa se non è già presente; Node.js 26 e .NET Framework 4.8 devono essere già installati.

## Modulo Bitfocus Companion

Con Node.js 26 e npm:

```powershell
cd companion-module-ndiplayer
npm ci
npm test
npm run package
npm run check
cd ..
./companion-module-ndiplayer/test-integration.ps1
```

## Build completa

Per ricompilare applicazione e test, creare il pacchetto del modulo Companion e produrre l'installer in un solo passaggio:

```powershell
./build-release.ps1 -RuntimeFile C:\percorso\NDI6Runtime.exe -IsccPath C:\percorso\ISCC.exe
```

È possibile fornire un archivio FFmpeg locale con `-VideoArchive`. Il comando usa `node` e `npm` presenti nel `PATH`; per indicare eseguibili diversi usare `-NodePath` e `-NpmPath`.

Il test d'integrazione richiede l'app già compilata; usa un host C# con sender simulato. Il pacchetto generato è `companion-module-ndiplayer/ndiplayer-1.0.0.tgz`. Le pipeline `.github/workflows/windows.yml` e `companion-module.yml` replicano rispettivamente build/installer e packaging/test del modulo.
