# Utilizzo rapido

## Avvio e preparazione

1. Su Windows 10/11 x64 installa l'app con `NDIPlayer-<versione>-Setup-win-x64.exe`. Sono richiesti .NET Framework 4.8 e NDI Runtime 6; il setup avvia l'installazione del runtime se necessario.
2. Apri **NDIPlayer** e scegli **Apri cartella**. La libreria mostra immagini supportate e video `.mov`; **Aggiorna** ripete la scansione.
3. Imposta formato, fps e nomi dei due flussi nelle **Impostazioni**. Avvia **Program** per la messa in onda e, se serve, **Preview** per la regia. Cerca le sorgenti NDI dal ricevitore WATCHOUT.

## Messa in onda

- Seleziona un file: appare nel monitor **Selezione**. Regola scala e centro con i campi, trascinamento e rotellina. Il monitor **Program** resta sul contenuto già inviato.
- Premi **INVIA selezione**, fai doppio clic sul file oppure usa il richiamo numerico seguito da **Invio** per mandarlo in Program. Scegli cut o dissolvenza e la durata di cambio foto.
- **CLEAR trasparente** pulisce Program mantenendo attivo il sender. **Esc** pulisce in dissolvenza con la durata dedicata; un secondo Esc rapido esegue il cut. Per terminare la trasmissione usa **Arresta Program** o **Arresta Preview** separatamente.
- Il monitor Program e gli indicatori mostrano lo stato locale del sender: verifica sul ricevitore il segnale effettivamente ricevuto.

## Organizzazione e progetti

Crea blocchi, aggiungi file, riordinali, cerca nel blocco e assegna numeri di richiamo per file. **Salva progetto** crea un `.ndiproj` con playlist e impostazioni; i file multimediali rimangono nella cartella originale. Quando sposti la libreria su un altro PC, conserva i percorsi relativi oppure usa **Ricollega cartella**. Aprire un progetto o cambiare selezione non manda automaticamente un nuovo file in onda.

## Video, audio e Companion

Per i MOV puoi attivare **Loop video** e vedere il countdown nel monitor Program. In basso a destra di ogni monitor, accanto al vumetro, trovi play/pausa e riavvio: i monitor sono indipendenti e i comandi si attivano quando il monitor riproduce un MOV. Il riavvio mantiene l'eventuale pausa. Il selettore **Velocità video ×** (0,5×–5×) è nella barra comandi in basso: agisce sulla Preview, la riavvia e attiva Mute Preview; Program usa la velocità salvata al prossimo invio. La velocità viene salvata per file nel progetto. L'audio è opzionale: in **Uscite audio** scegli scheda e coppia stereo per Program e ascolto Preview. L'audio esce localmente, non tramite NDI. Pulisci Program prima di cambiare configurazione delle uscite; gain e mute si regolano durante la riproduzione. Vedi [AUDIO.md](AUDIO.md).

Per il controllo remoto abilita **Comandi Companion** nell'app (porta iniziale 51235) e importa `companion-module-ndiplayer/releases/ndiplayer-1.1.0.tgz` in Bitfocus Companion 5.1.2. Configura IP e porta nella connessione. I comandi di selezione preparano Preview; **GO** invia in Program. Le notifiche dall'app verso Companion usano una configurazione TCP separata (porta iniziale 51234). Vedi [COMPANION.md](COMPANION.md) e [la guida del modulo](../companion-module-ndiplayer/README.md).
