# Compilazione

Eseguire i comandi da PowerShell nella radice del repository su Windows x64. Servono .NET Framework 4.8 con `csc.exe` in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\`, accesso alla rete per la prima preparazione e PowerShell con `Invoke-WebRequest`/`Expand-Archive`.

## Applicazione e test

La build usa le quattro librerie SharpDX 4.2.0 per Direct3D 11 e NAudio 1.10.0 presenti in `third-party/binaries/`, copiandole in `bin/`. Se mancano dal repository, gli script `installer/prepare-gpu.ps1` e `prepare-audio.ps1` possono ancora scaricare i pacchetti verificati con SHA-256. Le DLL devono restare accanto all'eseguibile.

```powershell
./build.ps1
./build.ps1 --no-bump
```

`build.ps1` incrementa la patch in `VERSION` dopo la compilazione e i test. Con `--no-bump` compila e testa usando la versione corrente senza modificare `VERSION`. Lo script prepara NAudio 1.10.0 e, se mancano FFmpeg o FFprobe, richiama `prepare-video.ps1`, che scarica e verifica lo ZIP FFmpeg fissato nello script e prepara `bin/video/` con eseguibili e DLL. Poi compila `bin/WatchoutImageNDI.exe` e `bin/Tests.exe` ed esegue i test. Per usare lo ZIP originale conservato localmente, eseguire prima `./installer/prepare-video.ps1 -Archive C:\percorso\ffmpeg-n8.1.3-27-g8dca1586f4-win64-lgpl-shared-8.1.zip`. La build mensile rimane disponibile dal fornitore solo per due anni: conservare lo ZIP verificato e le dipendenze in `bin\video-package` in un archivio duraturo. Una preparazione completamente offline richiede anche il pacchetto NAudio e i sorgenti FFmpeg e script di build corrispondenti già nella cache.

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

Lo script prepara sempre FFmpeg dall'archivio verificato, lancia la build e crea `dist/NDIPlayer-<versione>-Setup-win-x64.exe`; non usa una copia già presente in `bin\video` come sostituto della dipendenza verificata. Per usare uno ZIP FFmpeg locale aggiungere `-VideoArchive C:\percorso\archivio.zip`. Il setup include l'app, NAudio, FFmpeg e il redistributable NDI; verifica .NET 4.8 sul PC di destinazione. La compilazione dell'installer richiede che il runtime fornito corrisponda all'hash atteso in `installer/build-installer.ps1`.

## Installazione dipendenze di sviluppo

Per preparare il runtime NDI e verificare .NET Framework, Node.js e Inno Setup:

```powershell
./install-dependencies.ps1 -InstallInnoSetup
```

Il runtime NDI e l'installer di Inno Setup vengono scaricati in `dependencies/`, esclusa da Git. I binari sono verificati con SHA-256 e firma Authenticode. Il comando installa Inno Setup in modalità silenziosa se non è già presente; Node.js 26 e .NET Framework 4.8 devono essere già installati.

## Modulo Bitfocus Companion

Con Node.js 26 e npm, dalla radice del repository:

```powershell
./build-companion.ps1
./build-companion.ps1 --minor
./build-companion.ps1 --major
./build-companion.ps1 --no-bump
./companion-module-ndiplayer/test-integration.ps1
```

`build-companion.ps1` esegue installazione dipendenze, test, sincronizzazione versione, packaging e verifica. Incrementa la patch per impostazione predefinita; `--minor` incrementa la versione minore e azzera la patch, `--major` incrementa la versione maggiore e azzera minore e patch. `--no-bump` usa la versione corrente. Le opzioni di incremento sono mutuamente esclusive; `--patch` è disponibile esplicitamente. La versione del modulo è indipendente da quella dell'app ed è aggiornata in `companion-module-ndiplayer/VERSION`, `package.json`, lockfile e manifest. Il pacchetto `.tgz` risultante usa il nuovo numero, così Companion accetta l'importazione come versione distinta.

## Build completa

Per ricompilare applicazione e test, creare il pacchetto del modulo Companion e produrre l'installer in un solo passaggio:

```powershell
./build-release.ps1 -RuntimeFile C:\percorso\NDI6Runtime.exe -IsccPath C:\percorso\ISCC.exe
```

È possibile fornire un archivio FFmpeg locale con `-VideoArchive`. Il comando usa `node` e `npm` presenti nel `PATH`; per indicare eseguibili diversi usare `-NodePath` e `-NpmPath`.

Per scegliere l'incremento della versione `X.Y.Z`, specificare una sola opzione:

```powershell
.\build-release.ps1 --patch  # 0.13.8 -> 0.13.9 (predefinito)
.\build-release.ps1 --minor  # 0.13.8 -> 0.14.0
.\build-release.ps1 --major  # 0.13.8 -> 1.0.0
.\build-release.ps1 --no-bump  # usa la versione corrente senza modificare VERSION
```

Specificare `--no-bump` da solo, senza `--patch`, `--minor` o `--major`. Il numero in `VERSION` viene aggiornato dopo la compilazione e i test dell'applicazione, a meno che si usi `--no-bump`; un eventuale errore successivo del modulo o dell'installer non annulla l'incremento. Per condividere la versione aggiornata occorre committare `VERSION`.

Il test d'integrazione richiede l'app già compilata; usa un host C# con sender simulato. Il pacchetto generato è `companion-module-ndiplayer/ndiplayer-<versione>.tgz`. Le pipeline `.github/workflows/windows.yml` e `companion-module.yml` replicano rispettivamente build/installer e packaging/test del modulo.
