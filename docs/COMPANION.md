# NDIPlayer 0.11.0 · Controllo da Bitfocus Companion

## Modulo dedicato per Companion 5.1.2

È disponibile il pacchetto **ndiplayer-1.1.0.tgz**: importalo in Modules → Import module package, aggiungi la connessione NDIPlayer e trascina i 25 preset sulle pagine. [Guida del modulo dedicato](../companion-module-ndiplayer/README.md). [Pacchetto versionato](../companion-module-ndiplayer/releases/ndiplayer-1.1.0.tgz). Richiede NDIPlayer 0.11.0 o successivo (PLAY e PAUSE richiedono la versione con i controlli video). Il collegamento generico descritto sotto rimane una alternativa.

## Collegamento

1. In NDIPlayer apri **Comandi Companion…**, abilita la ricezione e scegli la porta (predefinita **51235**). L'IP locale `0.0.0.0` ascolta su tutte le interfacce del PC; `127.0.0.1` consente il collegamento solo dallo stesso PC.
2. In Companion aggiungi una connessione **Generic TCP and UDP**, scegli **TCP**, imposta come Target IP l'indirizzo del PC con NDIPlayer e come Target Port la porta scelta sopra. Se i due programmi sono sullo stesso PC usa `127.0.0.1`.
3. Per ogni pulsante aggiungi **Send Command**, scrivi il comando della tabella e scegli **LF (\n)** come terminatore. Non aggiungere i caratteri letterali `\n` nel testo se il modulo aggiunge già il terminatore. Anche CRLF è accettato.
4. Se vuoi leggere l'esito, abilita **Save TCP Response** e conversione **String** nel modulo. Il campo `$(NOME:tcp_response)` contiene l'ultima risposta. `OK` significa comando accettato/eseguito localmente, non conferma di ricezione da WATCHOUT. `OK CLEAR_FADE` conferma l'avvio del clear: la precedente notifica di Program vuoto arriva al completamento del clear.

Riferimento del modulo: [guida ufficiale Bitfocus](https://github.com/bitfocus/companion-bundled-modules/blob/main/generic-tcp-udp/companion/HELP.md).

Il collegamento funziona indipendentemente dal focus della finestra. Program deve essere già avviato per GO e clear. La ricezione è separata dalle notifiche **Companion TCP…**, normalmente dirette alla porta 51234 di Companion. Configurazione di ricezione e notifiche sono salvate nel progetto e nelle preferenze. Se il collegamento arriva da un altro PC, Windows deve consentire l'ingresso TCP sulla porta configurata per NDIPlayer. Il protocollo è TCP in chiaro senza autenticazione, destinato a reti di regia locali protette dal firewall: all'attivazione l'app mostra un avviso nella configurazione e nello stato di ascolto, anche quando la configurazione viene ripristinata.

## Comandi

| Comando | Effetto |
| --- | --- |
| `NEXT` | Seleziona il file visibile successivo in Preview |
| `PREV` | Seleziona il file visibile precedente in Preview |
| `NEXT_BLOCK` | Entra nel blocco successivo e seleziona il primo file nell'ordine visualizzato |
| `PREV_BLOCK` | Entra nel blocco precedente e seleziona il primo file nell'ordine visualizzato |
| `SELECT 12` | Seleziona in Preview il numero 12 del blocco attivo |
| `DIGIT 0` … `DIGIT 9` | Aggiunge una cifra al numero remoto composto, visibile nella barra superiore |
| `BACKSPACE` | Rimuove l'ultima cifra composta |
| `CANCEL` | Azzera il numero remoto e l'eventuale errore di richiamo |
| `ENTER` | Conferma il numero remoto composto e seleziona il file in Preview |
| `GO` | Invia la selezione in Program con il cut/dissolvenza scelto |
| `CLEAR_FADE` | Pulisce Program in dissolvenza usando **Clear Esc ms** |
| `CLEAR_CUT` | Pulisce immediatamente Program in cut |
| `PLAY` | Riprende il video in onda nel Program (video e audio); su un video terminato senza Loop lo riavvia dall'inizio |
| `PAUSE` | Mette in pausa il video in onda nel Program (video e audio) |

NEXT/PREV rispettano l'ordine visualizzato, anche quando ordinato per colonna o filtrato con Cerca. Si fermano agli estremi. Il cambio blocco azzera Cerca; mantiene l'ordinamento visuale scelto e non cambia Program. I comandi blocco attraversano solo i blocchi reali: dalla vista Tutta la libreria NEXT_BLOCK entra nel primo; PREV_BLOCK non entra in Tutta la libreria e si ferma al primo blocco.

SELECT/ENTER cercano in tutto il blocco e azzerano il filtro Cerca quando trovano la foto. Numeri diversi per la stessa foto in blocchi diversi sono rispettati. `1`, `01` e `001` sono lo stesso richiamo. Un numero inesistente, ambiguo o un file non disponibile produce ERR e impedisce un GO sulla vecchia selezione: correggi il numero oppure usa CANCEL, NEXT/PREV o una selezione manuale. Anche un numero composto ma non confermato impedisce GO. ENTER senza un numero produce ERR.

La composizione remota è separata dalla tastiera USB: i tasti DIGIT non cambiano Preview fino a ENTER. La tastiera USB mantiene numero → Preview e Invio → Program. Il buffer remoto si azzera dopo un richiamo riuscito, un cambio file/blocco o una nuova selezione manuale. Gli invii vengono eseguiti nell'ordine di ogni connessione; non c'è ripetizione automatica in caso di errore.

I clear lasciano NDI acceso e Preview invariata. Un CLEAR_FADE già in corso non viene riavviato; CLEAR_CUT lo interrompe in cut. A clear completato viene generata la normale notifica di Program vuoto, se abilitata.

PLAY e PAUSE agiscono solo sul video attualmente in Program; la Preview si controlla dall'app. Sono idempotenti: PAUSE su un video già fermo e PLAY su un video già in riproduzione rispondono `OK PLAY` / `OK PAUSE` senza effetti. Con Program spento o senza un video in onda rispondono ERR (`ERR Program non attivo`, `ERR Nessun video in onda`). La velocità di riproduzione non è controllabile da Companion.

## Esempio di tastierino

Crea dieci pulsanti con i comandi `DIGIT 0` … `DIGIT 9`, uno con `BACKSPACE`, uno con `CANCEL`, uno con `ENTER` e uno con `GO`.

Per preparare il numero 12: premi **1**, **2**, **ENTER**. Verifica la Preview, poi premi **GO**. Per un pulsante dedicato usa direttamente **SELECT 12**, seguito da GO quando vuoi mandarlo in onda.

## Dettagli del protocollo

- Un comando ASCII per riga, terminatore LF oppure CRLF. I nomi dei comandi non distinguono maiuscole e minuscole.
- `ASSET_STATUS` restituisce in Base64 JSON il titolo e il numero dell'asset selezionato in Preview e degli asset inviati in Program; il modulo dedicato aggiorna le variabili Companion ogni 500 ms.
- Numeri da 1 a 9 cifre; DIGIT accetta una sola cifra.
- Risposte UTF-8 con terminatore LF, ad esempio `OK NEXT`, `OK SELECT 12`, `OK GO`, `ERR Numero assente nel blocco`.
- Sono accettati pacchetti TCP frammentati o più righe nello stesso pacchetto e connessioni persistenti. Massimo quattro connessioni simultanee e una coda UI limitata; una linea oltre 128 caratteri viene rifiutata.
- Un comando in attesa su un'interfaccia occupata viene annullato dopo 15 secondi. Se era già in esecuzione, la risposta segnala di verificarne l'esito nell'app: non viene ripetuto automaticamente.
- Il cambio configurazione o l'arresto della ricezione chiude i collegamenti e invalida i vecchi comandi non ancora eseguiti. Nessun comando avvia automaticamente un canale NDI.

## Prova delle stringhe di notifica

Nel pannello **Companion TCP…**, i pulsanti **Invia test prima foto** e **Invia test clear** inviano la rispettiva stringa all'IP e alla porta attualmente inseriti, senza premere Applica e senza modificare Program o Preview. Funzionano anche con le notifiche automatiche disabilitate. Eseguono realmente il comando in Companion: controlla la destinazione prima di premerli. Una stringa vuota viene segnalata senza invio. **Test connessione** continua a verificare soltanto la porta, senza eseguire comandi.
