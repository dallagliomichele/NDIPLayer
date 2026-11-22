# Proposte ancora non sviluppate

## Collaudo audio sull'impianto

- [ ] Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi, Program e Preview, gain e mute, loop, clear, sincronismo con WATCHOUT e riproduzione prolungata. I test automatici non sostituiscono questo collaudo; seguire [AUDIO.md](AUDIO.md).

Questa è una lista di possibili sviluppi ricavati dai limiti dichiarati nel repository, non una roadmap approvata. Non ho trovato marcatori `TODO` o `FIXME` espliciti nei sorgenti.

| Proposta | Evidenza e stato attuale |
| --- | --- |
| Rendering GPU dei MOV | Immagini statiche e dissolvenze usano Direct3D 11, con priorità alla GPU discreta e fallback alla integrata o a GDI+ se Direct3D manca. La decodifica e composizione dei MOV usa ancora FFmpeg. |
| Avanzamento e temporizzazione delle playlist | `README.md`, sezione «Playlist a blocchi», dichiara che non esistono; `src/Projects.cs` gestisce blocchi e ordine, mentre l'invio rimane manuale. |
| Aggiornamento automatico della libreria | `README.md`, sezione «Architettura e sviluppo», dichiara l'assenza di file watcher; oggi l'utente usa **Aggiorna**. |
| Stato Program interrogabile dal modulo Companion | `companion-module-ndiplayer/README.md`, sezione «Variabili», indica l'assenza di interrogazioni per Program, file in onda e countdown. |
| Seek, ulteriori formati e HDR | I MOV supportano pausa e riavvio, ma non seek arbitrario; HDR e ulteriori formati immagine richiederebbero anche una revisione della pipeline BGRA a 8 bit. |

Valutare priorità e requisiti con l'uso reale prima di implementarli. Per la verifica dell'attuale procedura di build e della documentazione vedere [PROBLEMI.md](PROBLEMI.md).


-nella schermata di impostazioni video non autocorreggere  la cifra dei campi risoluzione fino a che l'utnete non ha tolto focus al campo
-inserire due tasti con preset 192x1080 e 3840x2160
