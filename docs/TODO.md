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
| Autostart settabile per i file video | Questo per avere la possibilità di mettere in onda sul primo frame e farli partire su chiamata. |

Valutare priorità e requisiti con l'uso reale prima di implementarli. Per la verifica dell'attuale procedura di build e della documentazione vedere [PROBLEMI.md](PROBLEMI.md).


- nel menu impostazioni, al tab "video e ndi" aggiungiamo un tasto "copia risoluzione preview" collegato a una funzione che legge l'attuale contenuto dalla preview (una volta sola sulal preview corrente) e lo applica alle dimensioni del flusso NDI
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
- modalità dark selezionabie nelle impostazioni
- estendiamo il colore rosso del program a tutto il frame program e il colore verde a tutto il frame preview
- aggiunta shortcut ctrl+a per selezionare tutti gli asset del corrente blocco (l'asset correntemente selezionato e mandato in preview nn cambia, e se l'utente clicca altrove per deselezionare si ritorna alla selezione unica, se clicca un'altra volta si deseleziona anche il contenuto in preview come ora)