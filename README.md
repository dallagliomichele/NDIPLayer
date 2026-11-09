# Watchout Image NDI · versione 0.3.1

App Windows x64 per inviare immagini su un flusso NDI con alpha. Interfaccia in italiano. Repository privato: https://github.com/dallagliomichele/NDIPLayer. Sorgenti e cronologia condivisi tramite GitHub.

## Avvio

**Doppio clic per andare in onda:** con NDI già avviato, fai doppio clic sinistro sul file nell'elenco o sulla miniatura. Viene inviata l'immagine cliccata con scala/posizione correnti e con il cut o la dissolvenza impostati, esattamente come con INVIA. Il clic singolo prepara solo la selezione; il doppio clic nell'area vuota non invia nulla. Se NDI è fermo o l'immagine non è leggibile, viene mostrato lo stato e non viene avviato un nuovo flusso automaticamente.

**Novità 0.3.1:** nell'anteprima **SELEZIONE** puoi spostare l'immagine trascinando con il tasto sinistro all'interno del bordo. La rotellina ingrandisce/riduce attorno al centro attuale dell'immagine (fattore 10% per scatto); **Maiusc + rotellina** usa il 2% per regolazioni fini. La scala resta tra 1% e 400%. I campi numerici si aggiornano insieme al mouse e restano modificabili a mano. Il monitor PROGRAMMA non è interattivo.

I campi **Centro X / Centro Y** rappresentano ora il centro dell'immagine nelle coordinate del flusso, con origine nell'angolo in alto a sinistra: a 1920×1080 il centro iniziale è **X=960, Y=540**; a 1280×720 è **X=640, Y=360**. Reset posizione ripristina questo centro e scala 100%. Un'altezza dispari può mostrare mezzo pixel. Cambiare risoluzione conserva lo spostamento rispetto al centro del nuovo formato. Le vecchie preferenze vengono visualizzate nella nuova convenzione mantenendo invariata l'inquadratura: internamente si mantengono gli offset salvati, senza migrazioni distruttive o conversioni ripetute.

Il mouse modifica solo la selezione preparata: **premi INVIA (oppure fai doppio clic sul file) per applicare le modifiche al flusso**, con la transizione scelta. Le posizioni vengono salvate per immagine al rilascio del trascinamento; scala e campi numerici al completamento della modifica. La sorgente selezionata viene decodificata una volta e riutilizzata durante le trasformazioni, senza tenere bloccato il file originale; Aggiorna la rilegge dal disco.

**Novità 0.3.1:** il catalogo ignora i file con attributo Windows Nascosto e i file di servizio che iniziano con `._` (AppleDouble del Mac) oppure `.-`. Durante la ricorsione vengono saltate anche le cartelle nascoste, quelle con nome che inizia con un punto e `__MACOSX`. I filtri valgono per elenco e miniature; nessun file viene cancellato. Premi Aggiorna per rileggere una cartella già aperta.

Il campo **Nome canale NDI**, in alto, permette di scegliere il nome della sorgente. Il valore viene salvato nelle preferenze; il default rimane `Watchout Image NDI`. Spazi iniziali/finali vengono rimossi e sono supportati accenti tramite UTF-8. Nomi vuoti, troppo lunghi o con caratteri `\ / : * ? " < > |` e caratteri di controllo vengono rifiutati. Per rinominare un canale già attivo: arresta NDI, modifica il nome, riavvia NDI e seleziona la nuova sorgente in WATCHOUT. Il nome non si modifica mentre il flusso è attivo.

**Novità 0.3.1:** la libreria a sinistra permette di scegliere **Elenco** o **Miniature**. Il cursore **Dimensione** regola le miniature da 64 a 256 pixel; la scelta viene salvata insieme alle preferenze. Le miniature si caricano in sottofondo per gli elementi visualizzati, con una cache limitata a 128 immagini. Per cartelle diverse con nomi uguali, il percorso completo resta visibile nell'elenco e nel suggerimento del file.

Al centro/destra ci sono due monitor affiancati, ridimensionabili allargando la finestra o spostando il separatore della libreria:

- **SELEZIONE**, bordo azzurro: immagine preparata, con scala e posizione. Cambiare selezione o regolazioni non modifica il programma finché non premi INVIA o fai doppio clic sul file.
- **PROGRAMMA**, bordo rosso: copia degli ultimi frame dopo che la chiamata sincrona al mittente NDI è terminata. Mostra cut, alpha delle dissolvenze e clear; si aggiorna fino a 10 volte al secondo per contenere il carico UI, indipendentemente dai 50 fps nominali del flusso. Non cambia quando scegli un'altra immagine nella libreria. Arresto ed errori sono indicati esplicitamente.

Il Programma è un **monitor locale dei frame consegnati al SDK NDI**, non un ricevitore NDI o un ritorno da WATCHOUT: non verifica perdita di rete, connessione del ricevitore, decodifica o compositing sul server. Per confermare l'uscita finale serve controllare WATCHOUT. I due monitor usano gli stessi confini del fotogramma e nessuna guida viene trasmessa.

Nella versione 0.3.1 l'anteprima mostra un **bordo azzurro che delimita esattamente il fotogramma di uscita** nel rapporto configurato (per esempio 16:9 a 1920×1080). La scacchiera compare solo dentro quel bordo e rappresenta l'alpha; l'area scura esterna non fa parte del flusso. Ridimensionando la finestra, immagine e fotogramma vengono scalati insieme senza cambiare proporzioni. Bordo e scritte sono guide locali e non vengono trasmessi. L'anteprima mostra la selezione: per applicarla in onda premi INVIA o fai doppio clic sul file.

1. Windows 10/11 x64 con .NET Framework 4.8. Esegui `NDIPlayer-0.3.1-Setup-win-x64.exe`: include il redistributable ufficiale **NDI 6 Runtime 6.3.2.0**, senza NDI Tools. Se necessario apre il setup del runtime, con licenza propria da accettare e possibile richiesta amministratore. Poi installa l'app per l'utente corrente e crea il collegamento nel menu Start. Non serve il .NET SDK.
2. Avvia NDIPlayer dal menu Start. Se usi i soli sorgenti o il vecchio ZIP portabile, il runtime resta da installare separatamente. Informazioni NDI: https://ndi.video/ . NDI® is a registered trademark of Vizrt NDI AB.
3. Apri una cartella: include sottocartelle, evita collegamenti circolari, segnala cartelle non accessibili. Aggiorna ripete la scansione.
4. Seleziona un file: anteprima su scacchiera, che indica la trasparenza e non viene trasmessa. PNG, JPEG, BMP, GIF, TIFF e ICO; per formati multipagina/animati viene utilizzato il primo frame. HEIC, WebP, SVG e RAW non sono supportati in questa versione.
5. Imposta dimensioni e fps prima di avviare NDI. Default 1920×1080 progressivo, 50 fps; larghezza pari, massimo 3840×2160 e 60 fps. Arresta NDI per cambiare formato.
6. Scala 100% = fit senza distorsioni. Centro X aumenta verso destra, Centro Y verso il basso, in pixel del flusso; per l'immagine centrata valgono metà larghezza e metà altezza del flusso. Scala relativa al fit, regolazioni separate per immagine e salvate automaticamente.
7. Avvia NDI: parte trasparente. Premi **INVIA selezione** o fai doppio clic sul file per andare in onda; la sola selezione modifica l'anteprima. Cut se Dissolvenza non selezionata; altrimenti transizione della durata in ms impostata. Cambiare regolazioni richiede un nuovo INVIA.
8. **CLEAR trasparente** invia alpha zero, con cut/dissolvenza secondo la scelta corrente. Per un clear immediato disattiva Dissolvenza. Arrestare NDI chiude la sorgente e il comportamento del ricevitore dipende da WATCHOUT: usare CLEAR prima di arrestare.

Le preferenze sono in `%LOCALAPPDATA%/WatchoutImageNDI/settings.json`. La cartella e i percorsi immagini sono locali: sulle altre postazioni riconfigura la cartella. Non vengono caricati file grafici su GitHub.

## Verifica preliminare alpha

Consultazione documentazione ufficiale: 9 novembre 2026.

- [NDI frame types](https://docs.ndi.video/all/developing-with-ndi/sdk/frame-types): BGRA è 8 bit per canale con alpha **non premoltiplicato**. UYVA include un piano alpha. La larghezza deve essere divisibile per due.
- [NDI sender](https://docs.ndi.video/all/developing-with-ndi/sdk/ndi-send): il clock video del sender regola la cadenza; l'invio sincrono permette il riuso del buffer dopo il ritorno.
- [WATCHOUT 7 capture devices](https://docs.dataton.com/guide/watchout/devices/capture-devices.html): UYVA è supportato per sorgenti NDI con alpha.
- [WATCHOUT 7 NDI capture](https://docs.dataton.com/guide/watchout/devices/ndi-capture.html): configurazione dell'ingresso NDI.

La compatibilità è supportata documentalmente per WATCHOUT 7, non certificata su una macchina WATCHOUT in questa sessione. Non si estende automaticamente a WATCHOUT 6. Confermare la versione esatta prima dell'impiego in produzione. BGRA è il formato fornito al SDK; il formato decodificato dal ricevitore può essere UYVA. Non si tratta di chroma key e non si usa NDI HX.

## Collaudo sull'impianto

Su rete cablata, consenti NDI nel firewall privato. In WATCHOUT crea un ingresso NDI e scegli `NOME-PC (nome canale scelto)`; con le preferenze iniziali sarà `NOME-PC (Watchout Image NDI)`. Usa formato Auto o UYVA quando disponibile. Sovrapponilo a uno sfondo colorato/movimento sullo stage e **sull'uscita reale**.

- PNG con bordi semitrasparenti: lo sfondo deve restare visibile senza frange nere.
- JPEG quadrato: nessuna deformazione e bande laterali trasparenti.
- Scala 50%, X +100/Y -100: confronto con l'anteprima.
- Cut e dissolvenza 1000 ms fra immagini e verso clear.
- Clear: sfondo interamente visibile, anche dopo diversi secondi.
- 1920×1080p50 per almeno 30 minuti sulla macchina di spettacolo; misurare CPU e continuità effettiva dei frame. Il formato nominale non garantisce prestazioni su qualsiasi hardware.

## Architettura e sviluppo

Tecnologia: C# / WinForms, .NET Framework 4.8 x64, GDI+ per decodifica/rendering; SDK NDI nativo tramite ABI C. Scelta per produrre subito un eseguibile Windows senza pacchetti esterni, usando il compilatore già presente. Un futuro renderer GPU può sostituire quello CPU senza cambiare sender e libreria.

- `src/Core.cs`: catalogo ricorsivo, geometria, render BGRA e interpolazione alpha corretta.
- `src/Ndi.cs`: `IVideoSender`, binding NDI, playout indipendente dalla UI, clock SDK e rilascio risorse sul thread di invio.
- `src/App.cs`: UI, monitor di selezione/programma e persistenza locale.
- `src/LibraryBrowser.cs`: libreria virtuale elenco/miniature, decodifica in sottofondo, cache limitata e annullamento dei caricamenti quando cambia cartella/dimensione.
- `src/PreviewCanvas.cs`: rendering dei monitor con guide separate dai frame trasmessi.
- `tests/FeatureTests.cs`: monitor verificato contro un mittente controllato, cut/clear/dissolvenza, fallimento invio, isolamento snapshot e caricamento miniature.
- `tests/TransformTests.cs`: drag/rotellina, sincronizzazione campi e rendering, reset, compatibilità delle posizioni salvate, cambio risoluzione e cache della sorgente.
- `tests/Tests.cs`: geometria, alpha reale PNG, transizioni, clear, scansione, layout ABI e playout con sender simulato; test opzionale sul runtime reale.

Build dalla cartella progetto con PowerShell: `./build.ps1`. Test runtime opzionale: `./bin/Tests.exe --ndi`. Non installa o scarica dipendenze. `.github/workflows/windows.yml` costruisce, esegue test e genera un artifact su Windows dopo la pubblicazione autorizzata.

Limiti MVP: nessuna ricerca o file watcher; scansione cartelle e decodifica dell'immagine selezionata sulla UI, miniature in sottofondo; rendering/fade CPU in spazio dei valori sRGB, nessuna gestione colore ICC professionale, nessuna garanzia realtime. I file molto grandi possono rallentare la UI e il monitor locale. Il sender invia continuamente anche immagini statiche; errori NDI visibili nello stato, nessuna riconnessione automatica. Nessun audio. Un'istanza per processo.

## GitHub e più postazioni

Repository privato autorizzato: https://github.com/dallagliomichele/NDIPLayer, ramo `main`.

Da altre postazioni: clonare il repository, eseguire `build.ps1`, installare NDI Runtime e selezionare la propria cartella. Prima di lavorare fare pull, poi commit e push delle modifiche. GitHub ospita codice e cronologia; playout e immagini restano sulla postazione Windows nella rete WATCHOUT.

Nessuna licenza open source applicata automaticamente. Verificare termini NDI prima di distribuire il prodotto o includere runtime/SDK proprietari.

## Installer completo 0.3.1

Il pacchetto contiene app, documentazione e redistributable NDI originale. Non scarica il runtime durante l'installazione. .NET Framework 4.8 è un prerequisito verificato dal setup; se assente, installarlo tramite Windows Update. Non si accetta la licenza NDI in silenzio: il setup ufficiale viene aperto quando il runtime manca o è più vecchio. Un runtime uguale o successivo già rilevato viene conservato. Annullando il setup NDI, non si prosegue con l'app.

Disinstalla NDIPlayer da Impostazioni > App. Le preferenze personali e il runtime condiviso rimangono; quest'ultimo si rimuove separatamente da Windows. L'installer dell'app non è firmato digitalmente; il redistributable incorporato conserva la firma originale Vizrt AG.

Per ricostruire, scarica il [redistributable ufficiale](https://downloads.ndi.tv/SDK/NDI_SDK/NDI%206%20Runtime.exe), poi esegui `installer/build-installer.ps1 -RuntimeFile C:\percorso\NDI6Runtime.exe -IsccPath C:\percorso\ISCC.exe`. Serve Inno Setup 6.5+ o 7; verificare i termini del compilatore per l'uso previsto. Il file generato è in `dist/`. Lo script verifica hash e firma prima di incorporare il redistributable. Non caricare il compiler o i binari proprietari nei sorgenti Git.

Runtime verificato: versione 6.3.2.0, firma Authenticode valida Vizrt AG, SHA-256 `7EE73EEDB56402BCA5100868353DBAB4E944B6C37D2D9881580698A3B61346CD`. Se NDI aggiorna il download, la build si ferma: verificare firma/versione e aggiornare il pin prima di ricostruire.

Verifiche effettuate: compilazione installer con runtime incorporato e 76 test applicativi, incluso sender NDI reale sulla postazione di sviluppo. Non è stata eseguita un'installazione/disinstallazione su Windows pulito: prima della distribuzione operativa verificare il setup su una macchina di prova senza NDI Tools, inclusi annullamento licenza, avvio, clear e disinstallazione. Le prove WATCHOUT restano quelle indicate sopra.
