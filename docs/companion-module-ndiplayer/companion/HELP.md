# NDIPlayer · modulo Companion 1.1.0

Per **Companion 5.1.2**, runtime Node 26. Richiede NDIPlayer 0.11.0 o successivo; consigliata la 0.11.1. I comandi PLAY e PAUSE richiedono la versione di NDIPlayer con i controlli video: con versioni precedenti rispondono ERR.

## Installazione

1. In Companion apri **Modules → Import module package** e scegli `ndiplayer-1.1.0.tgz` senza scompattarlo.
2. In **Connections** aggiungi **NDIPlayer**, versione **1.1.0**.
3. Imposta l'IP del PC con NDIPlayer e la porta TCP **51235**, oppure la porta scelta nell'app. Sullo stesso PC usa `127.0.0.1`.
4. In NDIPlayer abilita **Comandi Companion…**. Per GO, clear, PLAY e PAUSE, avvia prima Program nell'app.
5. In **Presets** scegli la connessione e trascina i 25 pulsanti sulle pagine: Navigazione, Program, Tastierino e Informazioni TCP.

È un modulo privato importabile, non una pubblicazione nello store Bitfocus. Il pacchetto include le dipendenze: non serve installare Node o npm per importarlo.

Riferimento: [documentazione ufficiale di importazione](https://github.com/bitfocus/companion/blob/main/docs/user-guide/3_config/modules.md).

## Pulsanti pronti

- PREV / NEXT, BLOCCO PREV / BLOCCO NEXT: preparano solo Preview.
- SELECT NUMERO: nell'azione cambia il campo Numero, predefinito 1. Rispetta la numerazione del blocco attivo.
- 0–9, BACKSPACE, CANCEL, ENTER PREVIEW: componi il numero, poi ENTER seleziona il file in Preview.
- GO: manda la selezione in Program. SELECT ed ENTER non eseguono GO automaticamente.
- CLEAR FADE: usa Clear Esc ms dell'app. CLEAR CUT: clear immediato. Lasciano NDI acceso e Preview invariata.
- PLAY / PAUSE: riprendono o fermano il video in onda nel Program (video e audio); la Preview si controlla dall'app. Senza un video in Program rispondono ERR. PLAY su un video terminato senza Loop lo riavvia dall'inizio; PAUSE su un video terminato risponde OK senza effetti.
- NUMERO: numero composto restituito dall'ultima conferma TCP.
- TCP: collegamento verde; ultimo errore rosso. Gli altri pulsanti diventano grigi quando scollegati.

## Variabili

`connection`, `number`, `last_response`, `last_command`, `last_error`, `last_selected_number`, `last_block`.

Descrivono le risposte ricevute da questa connessione. `?` significa dato non noto, ad esempio dopo una riconnessione. Gli interventi nell'app o da altri controller non vengono trasmessi spontaneamente al modulo. Lo stato e il countdown video sono interrogati ogni 500 ms; la foto in onda non viene esposta. Un socket collegato non conferma che Program NDI sia avviato o ricevuto da WATCHOUT.

Le notifiche in uscita da NDIPlayer verso Companion restano separate: il modulo usa solo la porta dei comandi in ingresso. Per comandare il programma non servono le due stringhe delle notifiche.

I comandi sono serializzati aspettando la risposta. Il modulo si ricollega automaticamente, senza conservare comandi offline e senza ripetere comandi dopo interruzioni o timeout. Se cade la connessione dopo GO, verifica l'esito nell'app prima di ripeterlo. Coda massima: 32 comandi. Attesa risposta predefinita: 20 secondi.

## Sviluppo

Node 26 e npm: `npm ci`, `npm test`, `npm run package`. Lo strumento ufficiale Bitfocus genera `ndiplayer-1.1.0.tgz` e valida il manifest. Codice MIT; il pacchetto include gli avvisi delle dipendenze.

Prove di integrazione su Windows: dopo la compilazione di NDIPlayer e del pacchetto, esegui `test-integration.ps1`. Verifica sia il sorgente sia il codice contenuto nel .tgz contro il ricevitore TCP di NDIPlayer, con un mittente video simulato. Sono passati 15 test, senza invii a WATCHOUT.

## Video Program e versione

Con una versione di NDIPlayer che supporta `VIDEO_STATUS` e `RESTART`, il modulo espone le variabili `video_state`, `video_countdown` e `video_remaining_seconds` e i feedback video in riproduzione, in pausa e terminato. I preset PLAY e PAUSE evidenziano lo stato; RESTART riparte mantenendo lo stato di pausa. Il countdown indica il tempo alla fine o al prossimo loop, tenendo conto della velocita. `?` indica un valore non disponibile. Con versioni precedenti dell'app lo stato video rimane sconosciuto.

La versione del modulo e indipendente da quella dell'app ed e definita in `companion-module-ndiplayer/VERSION`. `npm run version:patch`, `version:minor` o `version:major` aggiornano il file e sincronizzano package, lockfile e manifest. Anche `npm run package` e `npm run check` sincronizzano i metadati dal file VERSION.
