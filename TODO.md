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
| Controlli video aggiuntivi | `README.md`, sezione «Video MOV», dichiara assenza di seek e pausa; HDR e ulteriori formati immagine richiederebbero anche una revisione della pipeline BGRA a 8 bit. |

Valutare priorità e requisiti con l'uso reale prima di implementarli. Per la verifica dell'attuale procedura di build e della documentazione vedere [PROBLEMI.md](PROBLEMI.md).


Gestendo i file video:
- vogliamo la possibilità di mettere un pulsante toggle play/pause che fermi la riproduzione con le classiche icone
- accanto al pulsante play/pause mettiamo un pulsante per il riavvio. Quando il video è in riproduzione riparte dall'inizio, quando invece è in pausa vedremo il primo fotogramma e il video rimarrà in pausa
- all'interno del progetto aggiungiamo la posibilità di impostare un selettore di velocità di riproduzione, per ora con range -2x - 5x 


Correzioni:
- rimuovere il contatore di frame inviati in basso a sinistra nella GUI

-una piccola modifica alla segnalazione di attività del NDI: quando il flusso NDI program è attivo il riquadro attorno al contenuto video del program ssi dovrebbe colorare di rosso, altrimenti rimane nero. Ugualmente per la preview, ma il frame si colora di verde.
  