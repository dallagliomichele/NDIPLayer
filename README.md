# Watchout Image NDI · prima versione 0.1

App Windows x64 per inviare immagini su un flusso NDI con alpha. Interfaccia in italiano. Repository privato: https://github.com/dallagliomichele/NDIPLayer. Sorgenti e cronologia condivisi tramite GitHub.

## Avvio

1. Windows 10/11 x64 con .NET Framework 4.8 e NDI Runtime x64 (oppure NDI Tools ufficiali). Non serve il .NET SDK. Il runtime NDI non è incluso: installarlo dal sito ufficiale https://ndi.video/tools/ rispettando le sue condizioni di licenza.
2. Avvia `bin/WatchoutImageNDI.exe` (nello ZIP distribuibile l'eseguibile è alla radice).
3. Apri una cartella: include sottocartelle, evita collegamenti circolari, segnala cartelle non accessibili. Aggiorna ripete la scansione.
4. Seleziona un file: anteprima su scacchiera, che indica la trasparenza e non viene trasmessa. PNG, JPEG, BMP, GIF, TIFF e ICO; per formati multipagina/animati viene utilizzato il primo frame. HEIC, WebP, SVG e RAW non sono supportati in questa versione.
5. Imposta dimensioni e fps prima di avviare NDI. Default 1920×1080 progressivo, 50 fps; larghezza pari, massimo 3840×2160 e 60 fps. Arresta NDI per cambiare formato.
6. Scala 100% = fit centrato senza distorsioni. X positivo verso destra, Y positivo verso il basso, in pixel del flusso; scala relativa al fit. Le regolazioni sono per immagine e vengono salvate automaticamente.
7. Avvia NDI: parte trasparente. Premi **INVIA selezione** per andare in onda; la sola selezione modifica l'anteprima. Cut se Dissolvenza non selezionata; altrimenti transizione della durata in ms impostata. Cambiare regolazioni richiede un nuovo INVIA.
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

Su rete cablata, consenti NDI nel firewall privato. In WATCHOUT crea un ingresso NDI e scegli `NOME-PC (Watchout Image NDI)`, formato Auto o UYVA quando disponibile. Sovrapponilo a uno sfondo colorato/movimento sullo stage e **sull'uscita reale**.

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
- `src/App.cs`: UI, anteprima e persistenza locale.
- `tests/Tests.cs`: geometria, alpha reale PNG, transizioni, clear, scansione, layout ABI e playout con sender simulato; test opzionale sul runtime reale.

Build dalla cartella progetto con PowerShell: `./build.ps1`. Test runtime opzionale: `./bin/Tests.exe --ndi`. Non installa o scarica dipendenze. `.github/workflows/windows.yml` costruisce, esegue test e genera un artifact su Windows dopo la pubblicazione autorizzata.

Limiti MVP: browser a elenco senza miniature/ricerca, scansione e decodifica sulla UI, nessun file watcher, nessuna anteprima separata del programma in onda; rendering/fade CPU in spazio dei valori sRGB, nessuna gestione colore ICC professionale, nessuna garanzia realtime. I file molto grandi possono rallentare la UI. Il sender invia continuamente anche immagini statiche; errori NDI visibili nello stato, nessuna riconnessione automatica. Nessun audio. Un'istanza per processo.

## GitHub e più postazioni

Repository privato autorizzato: https://github.com/dallagliomichele/NDIPLayer, ramo `main`.

Da altre postazioni: clonare il repository, eseguire `build.ps1`, installare NDI Runtime e selezionare la propria cartella. Prima di lavorare fare pull, poi commit e push delle modifiche. GitHub ospita codice e cronologia; playout e immagini restano sulla postazione Windows nella rete WATCHOUT.

Nessuna licenza open source applicata automaticamente. Verificare termini NDI prima di distribuire il prodotto o includere runtime/SDK proprietari.
