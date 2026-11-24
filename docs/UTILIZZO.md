# Utilizzo rapido

## Avvio e preparazione

In **Impostazioni > Video e NDI** puoi attivare la modalità scura, che comprende anche gli elenchi di asset e blocchi. La scelta viene salvata nelle preferenze locali della postazione. Stato NDI, nome del flusso e pulsante di avvio/arresto si trovano sopra ciascun monitor. Quando il rispettivo flusso è spento o in errore, lo sfondo è nero e la cornice del fotogramma è verde per Preview o rossa per Program. Quando è attivo, lo sfondo assume quel colore e la cornice del fotogramma scompare. Questi colori non vengono trasmessi via NDI.

1. Su Windows 10/11 x64 installa l'app con `NDIPlayer-<versione>-Setup-win-x64.exe`. Sono richiesti .NET Framework 4.8 e NDI Runtime 6; il setup avvia l'installazione del runtime se necessario.
2. Apri **NDIPlayer** e scegli **Apri cartella > Sfoglia…**. La libreria mostra immagini supportate e video `.mov`. Il pallino rosso nella barra Libreria segnala file aggiunti, modificati o rimossi nella cartella sorgente. Passa sul pallino per vedere il numero di percorsi interessati, poi premi **Aggiorna** per applicare le modifiche.
3. Imposta formato, fps e nomi dei due flussi nelle **Impostazioni**. Avvia **Program** per la messa in onda e, se serve, **Preview** per la regia. Cerca le sorgenti NDI dal ricevitore WATCHOUT.

Nel menu **Apri cartella** puoi aggiungere la cartella corrente ai preferiti o rimuoverla. I preferiti sono salvati sulla postazione e disponibili anche in **Ricollega cartella**.

## Messa in onda

- Seleziona un file: appare nel monitor **Selezione**. Regola scala e centro con i campi, trascinamento e rotellina. Il monitor **Program** resta sul contenuto già inviato.
- Premi **INVIA selezione**, fai doppio clic sul file oppure usa il richiamo numerico seguito da **Invio** per mandarlo in Program. Scegli cut o dissolvenza e la durata di cambio foto.
- **CLEAR trasparente** pulisce Program mantenendo attivo il sender. **Esc** pulisce in dissolvenza con la durata dedicata; un secondo Esc rapido esegue il cut. Per terminare la trasmissione usa **Arresta Program** o **Arresta Preview** separatamente.
- Il monitor Program e gli indicatori mostrano lo stato locale del sender: verifica sul ricevitore il segnale effettivamente ricevuto.

## Organizzazione e progetti

Crea blocchi, aggiungi file, riordinali, cerca nel blocco e assegna numeri di richiamo per file. **Salva progetto** crea un `.ndiproj` con playlist e impostazioni; i file multimediali rimangono nella cartella originale. Quando sposti la libreria su un altro PC, conserva i percorsi relativi oppure usa **Ricollega cartella**. Aprire un progetto o cambiare selezione non manda automaticamente un nuovo file in onda.

**Ctrl+A** seleziona gli asset visibili del blocco corrente, rispettando il filtro **Cerca**, e lascia in Preview l’asset già caricato. Nell’area vuota dell’elenco o delle miniature, il primo clic torna a quell’asset e il secondo svuota la Preview. Nei campi di testo Ctrl+A seleziona il testo.

Con il tasto destro su un asset, **Crea copia** aggiunge un’istanza nella libreria e, se sei in un blocco, la inserisce subito dopo l’originale. Parte dalle stesse regolazioni e puoi cambiare scala, posizione, loop, punti video, Autostart, velocità e gain separatamente. Assegna alla copia un numero di richiamo dal menu contestuale. Originale e copia leggono lo stesso file. I progetti con copie usano il formato `.ndiproj` versione 3.

## Video, audio e Companion

Per i MOV puoi attivare **Loop video** e vedere il countdown nel monitor Program. In basso a sinistra di ogni monitor trovi play/pausa e riavvio quando il monitor riproduce un MOV; i due monitor sono indipendenti. La Preview mostra accanto alla risoluzione la durata originale del video. Il riavvio mantiene l'eventuale pausa. Il selettore **Velocità video ×** (0,5×–5×) è nella barra comandi in basso: agisce sulla Preview, la riavvia e attiva Mute Preview; Program usa la velocità salvata al prossimo invio. La velocità viene salvata per file nel progetto. L'audio è opzionale: in **Uscite audio** scegli scheda e coppia stereo per Program e ascolto Preview. L'audio esce localmente, non tramite NDI. Pulisci Program prima di cambiare configurazione delle uscite; gain e mute si regolano durante la riproduzione. Vedi [AUDIO.md](AUDIO.md).

Per il controllo remoto abilita **Comandi Companion** nell'app (porta iniziale 51235) e importa `companion-module-ndiplayer/releases/ndiplayer-1.1.0.tgz` in Bitfocus Companion 5.1.2. Configura IP e porta nella connessione. I comandi di selezione preparano Preview; **GO** invia in Program. Le notifiche dall'app verso Companion usano una configurazione TCP separata (porta iniziale 51234). Vedi [COMPANION.md](COMPANION.md) e [la guida del modulo](../companion-module-ndiplayer/README.md).

Per ogni MOV puoi impostare **Entrata s** e **Uscita s** nella barra inferiore; `0` come uscita usa la fine del file. **Seek s > Vai** sposta solo la riproduzione Preview; **Segna entrata** e **Segna uscita** acquisiscono la posizione corrente della Preview. Il Program parte sempre dal punto di entrata e si ferma all'uscita, oppure ripete quel tratto se Loop è attivo. Disattivando **Autostart**, INVIA prepara Program sul primo frame dell'intervallo: avvialo con Play sul monitor o con PLAY da Companion. Queste scelte sono salvate nel progetto per ciascun file. In **Impostazioni > Video e NDI**, **Copia risoluzione Preview** imposta larghezza e altezza NDI alle dimensioni originali dell'asset selezionato; richiede entrambi i flussi spenti e non cambia gli fps.

### Swap Program / Preview

Il comando **Swap Program / Preview** è attivo per default e viene salvato nel progetto. Dopo un invio riuscito, il media precedentemente in Program viene selezionato in Preview; i video ripartono dal punto di entrata configurato. Se necessario viene aperta Tutta la libreria e rimossa la ricerca. Se Program è vuoto o il media precedente non è più disponibile nel progetto, la Preview rimane invariata. Durante una dissolvenza viene usato il media destinatario dell’ultimo invio. Disattiva Swap per mantenere in Preview il media appena inviato. Il clear non esegue lo swap.
