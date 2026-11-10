# Watchout Image NDI · versione 0.13.3

App Windows x64 per inviare immagini su un flusso NDI con alpha. Interfaccia in italiano. Repository privato: https://github.com/dallagliomichele/NDIPLayer. Sorgenti e cronologia condivisi tramite GitHub.

## Modulo Companion 1.0.0

Modulo importabile dedicato a Companion **5.1.2**: 12 azioni, 23 preset, tastierino, feedback TCP e risposte di errore. [Guida](companion-module-ndiplayer/README.md) e [pacchetto .tgz](companion-module-ndiplayer/releases/ndiplayer-1.0.0.tgz). Il modulo controlla il ricevitore TCP già presente in NDIPlayer 0.11.1; non richiede un nuovo installer. Sorgenti e prove sono nella cartella companion-module-ndiplayer.

## Novità 0.13.3 · Colore Loop e numerazione automatica

Il countdown dei video in loop usa un colore fisso, arancione iniziale, modificabile in Impostazioni > Countdown. I video senza loop conservano le tre fasce configurabili.

Tasto destro > Numera automaticamente la selezione assegna numeri consecutivi nell'ordine attuale di elenco o miniature. I vecchi numeri dei selezionati vengono esclusi dal calcolo; si parte da uno oltre il massimo numero dei file non selezionati nel blocco (o in Tutta la libreria). Se non restano numeri, si parte da 1. Selezionando tutti i file si rinumera quindi da 1. I file non selezionati e gli altri blocchi mantengono i propri numeri. L'operazione non cambia il Program e non rinomina file. I numeri vengono salvati nel progetto; non sono permessi conflitti o superamenti di 9 cifre.

## Novità 0.13.2 · Colori countdown configurabili

In Impostazioni > Countdown scegli soglia gialla, soglia rossa e i tre colori, poi premi Applica countdown. Valori iniziali: verde sopra 30 secondi, giallo da 30 a oltre 10 secondi, rosso da 10 a zero, incluso il video terminato. Vale anche per il tempo al prossimo loop. Impostazioni salvate nelle preferenze e nel progetto. Il pulsante dei valori predefiniti prepara i valori iniziali, da confermare con Applica. Nessun duplicato del timer né modifiche al suo disegno con doppio buffer.

## Correzione 0.13.1 · Countdown unico

Rimosso il countdown duplicato dalla barra superiore; resta quello sotto Program. Il timer è ora un controllo disegnato direttamente, senza la gestione del testo della Label nativa e senza richieste di layout a ogni decimo. Il monitor Program torna in un contenitore dedicato con doppio buffer. La barra superiore conserva stato, nome del file e formato.

## Novità 0.13.0 · Comandi ordinati e impostazioni separate

Barre distinte Progetto e Libreria, stato Program sempre visibile e finestra separata Impostazioni con sezioni Video/NDI, Audio, Companion e Informazioni. Audio e Companion aprono da qui i rispettivi pannelli di configurazione e test. Ogni monitor conserva il proprio comando di avvio/arresto. Gli errori dei servizi rimangono in una riga separata, con dettagli nelle Impostazioni, senza spostare i pulsanti.

Loop video è sempre presente nella riga di invio (abilitato per i MOV), insieme a Riavvia anteprima video; il countdown rimane sotto il Program con tempo al prossimo loop o tempo rimanente (il duplicato nella barra superiore introdotto in 0.13.0 è stato rimosso in 0.13.1). Gain, mute, posizionamento e dissolvenze rimangono nei comandi principali. I divisori e il ripristino dell'ultimo progetto restano disponibili.

## Novità 0.12.4 · Divisorio monitor e ultimo progetto

Il divisorio orizzontale tra Preview e Program si trascina per assegnare spazio ai due monitor. La posizione viene salvata alla chiusura insieme al divisorio della libreria e ripristinata sulla stessa configurazione schermi, entro i limiti disponibili. All'avvio viene riletto dal disco l'ultimo progetto aperto o salvato, senza avviare automaticamente il Program NDI; se manca o non è leggibile, appare un avviso e rimangono disponibili le preferenze locali. Nuovo progetto azzera il riferimento all'ultimo file. Le modifiche al progetto continuano a richiedere il salvataggio.

## Novità 0.12.3 · Disposizione monitor

Preview sopra e Program sotto, ciascuno con il proprio vumeter; countdown nel riquadro Program. La posizione del divisorio tra libreria e monitor viene salvata insieme alla finestra e ripristinata sulla stessa configurazione monitor, limitandola allo spazio disponibile. Queste preferenze restano locali e indipendenti dal progetto.

## Novità 0.12.2 · Posizione finestra

Posizione, dimensione e stato massimizzato vengono salvati alla chiusura, nelle preferenze locali della postazione. Si ripristinano solo se nomi, risoluzione, disposizione, monitor principale e aree utilizzabili dei monitor coincidono; altrimenti l'apertura usa la posizione predefinita di Windows. Lo stato minimizzato non viene ripristinato. Le preferenze finestra sono indipendenti dal progetto.

## Novità 0.12.1 · Test stringhe Companion

Due pulsanti nel pannello Companion inviano separatamente le stringhe prima foto e clear usando i valori non ancora salvati, senza cambiare Program. Funzionano anche con notifiche automatiche disabilitate; eseguono realmente il comando sulla destinazione configurata.

## Novità 0.12.0 · Audio locale

Audio video abilitabile separatamente per Program e ascolto Preview, con scelta WDM/WASAPI o ASIO e coppia stereo. Un solo motore apre il driver condiviso; Program e Preview richiedono coppie diverse se usano la stessa scheda. Gain per file da −60 a +12 dB, mute indipendenti e vumeter stereo RMS/peak/clip dopo gain e dissolvenza. Loop, fine video e clear seguono il video. Le impostazioni sono salvate nel progetto.

[Guida audio e collaudo](AUDIO.md). Per cambiare schede o abilitazioni occorre prima pulire Program; gain e mute sono regolabili durante la riproduzione. Il pacchetto include NAudio 1.10.0 (Ms-PL), non i driver delle schede. Le prove audio automatiche usano uscite simulate: WDM, Dante/ASIO e sincronismo sull'impianto richiedono collaudo hardware.

Il codice audio è separato in `AudioModel`, `AudioDecoder`, `AudioEngine`, `AudioDevices` e `AudioUi`; `tests/AudioTests.cs` verifica routing, mixer, clock, livelli, gain, loop, fine file, errori e progetti senza emettere suoni sulle schede reali.
## Correzione 0.11.1

Eliminata la cancellazione separata dello sfondo del countdown: testo e sfondo vengono disegnati insieme con doppio buffer, anche durante i cambi di colore. Il contenitore Program usa lo stesso sfondo scuro e non vengono richiesti ridisegni quando il valore non cambia. Conteggio, comandi Companion e flusso NDI mantengono il comportamento della 0.11.0.

## Novità 0.11.0 · Countdown, Companion e navigazione

**Countdown Program** sotto il monitor: tempo alla fine senza Loop, tempo al giro successivo con Loop, arancione negli ultimi cinque secondi. Mostra Avvio video prima dei primi frame e Video terminato alla fine senza Loop. Durata letta dal flusso video con ffprobe incluso; posizione associata ai frame consegnati al mittente NDI. Non cambia se scegli un altro file in Preview. Precisione visualizzata al decimo di secondo, con quantizzazione agli fps di uscita; non è un ritorno da WATCHOUT. Se la durata non è disponibile, viene indicato senza inventare un tempo. In una dissolvenza fra video, il contatore segue il video entrante; verso foto/clear segue l'uscente fino a fine transizione.

**Comandi Companion…** abilita la ricezione TCP, predefinita sulla porta 51235, indipendente dalle notifiche esistenti. Comandi: NEXT, PREV, NEXT_BLOCK, PREV_BLOCK, SELECT n, DIGIT n, BACKSPACE, CANCEL, ENTER, GO, CLEAR_FADE e CLEAR_CUT. Tutti funzionano senza focus su NDIPlayer. La selezione prepara Preview; GO invia. Il tastierino Companion compone il numero, ENTER seleziona e GO manda in onda. La tastiera USB resta invariata. Configurazione, esempi e risposte: [guida Companion](COMPANION.md), inclusa nell'installazione.

**Miniature**: destra/sinistra percorrono i file in sequenza passando fra le righe. NEXT/PREV TCP usano esattamente lo stesso ordine e si fermano agli estremi.

**Ordinamento elenco**: clic su Immagine, Numero, Stato o Percorso; secondo clic per invertire, freccia ▲/▼ nella colonna attiva. I numeri sono numerici (1, 2, 10), con i file senza numero in fondo. **Ordine manuale** ripristina l'ordine originale del blocco. L'ordinamento è solo visuale, resta attivo passando alle miniature o a un altro blocco e dura fino al ripristino o al riavvio dell'app. Non riscrive la playlist e non cambia Program. Stato viene ordinato fotografando gli indicatori al clic/aggiornamento della lista: non si riordina continuamente mentre cambi selezione o vai in onda. Per trascinare o spostare manualmente le foto nella playlist occorre prima tornare a Ordine manuale.

## Video MOV, Loop e posizionamento (0.10.0)

Il catalogo ora include `.mov`, anche nelle sottocartelle e nei blocchi. Miniature, numeri per blocco, richiamo da tastiera e doppio clic funzionano anche sui video. Il MOV viene riprodotto **senza audio**, come concordato. Il contenitore MOV può usare codec diversi: provati file H.264, QuickTime Animation (QTRLE) con alpha e ProRes 4444 con alpha. Non tutti i codec/profili MOV sono garantiti; un errore di decodifica viene mostrato nell'app.

- Seleziona il video: l'anteprima parte automaticamente. Se Preview NDI è acceso, invia questa riproduzione alla regia.
- **Loop video**: ripete il clip senza scadenza; la scelta è salvata per file insieme a scala e posizione. Disattivato: alla fine resta l'ultimo fotogramma. Il cambio Loop in anteprima viene applicato a Program al prossimo invio.
- **Riavvia anteprima video**: riparte dall'inizio senza cambiare Program.
- **INVIA / Invio / doppio clic**: avvia una riproduzione Program indipendente, sempre dall'inizio, con la transizione impostata. Passare a un altro blocco/file lascia il video in onda invariato.
- Scala, centro X/Y, trascinamento e rotellina funzionano come per le foto. Le modifiche preparano l'anteprima; per applicarle a Program occorre inviare nuovamente. Dopo una regolazione l'anteprima video riparte dall'inizio, con un breve ritardo per evitare di riavviare il decoder a ogni movimento del mouse.
- Clear, Esc/doppio Esc e arresto trasparente valgono anche per i video. Durante una dissolvenza normale il clip uscente continua a scorrere; interrompendo ripetutamente una transizione può essere usato come partenza il fotogramma composto corrente.

Fit mantiene le proporzioni di visualizzazione (anche pixel non quadrati), con aree esterne trasparenti. L'alpha incorporato è mantenuto per i codec che lo forniscono e che il decoder legge. L'uscita resta BGRA a 8 bit: questa versione non gestisce HDR, audio, seek o pause. Program e Preview sono decoder separati: due video o un video su entrambi i flussi usano più CPU/memoria. I monitor locali restano a 10 aggiornamenti/s; il flusso NDI usa gli fps impostati. Il breve test NDI reale a 1920x1080p50 non sostituisce la prova dei tuoi MOV e del carico della postazione in WATCHOUT.

L'installer include **FFmpeg LGPLv3+** e le sue librerie, licenze, sorgenti FFmpeg e ricette di build nella sottocartella `video`. Non servono codec di sistema o installazioni di NDI Tools. Provenienza e SHA256: `third-party/FFmpeg-NOTICE.txt`. Per preparare i sorgenti su una nuova postazione, prima di `build.ps1` esegui `installer/prepare-video.ps1`; puoi passare `-Archive` con lo ZIP originale verificato. La build del fornitore del 9 novembre è conservata online solo temporaneamente: archivia lo ZIP per gli sviluppi successivi. L'installer completo resta utilizzabile offline.

## Correzione miniature

Corretto un errore ObjectDisposedException nella coda miniature: le richieste tardive della lista durante ricostruzione o chiusura non accedono più alla coda eliminata. Aggiunti cinque controlli di regressione sul ciclo di vita del caricamento miniature.

## Richiamo da tastiera e numerazione per blocco (0.10.0)

Scegli il blocco, poi con il tasto destro sulla foto usa **Numero nel blocco…**. La stessa foto può avere, ad esempio, `01` in Apertura e `12` in Intervallo. Anche lo stesso numero può essere riutilizzato in blocchi diversi. **Tutta la libreria** ha una numerazione separata.

Con il focus sull'elenco o sull'anteprima, digita il numero (anche dal tastierino numerico): la foto viene selezionata solo in anteprima. **Invio** la manda in onda con la transizione impostata. Il campo **Numero → Invio** mostra il richiamo corrente; puoi correggerlo con Backspace oppure Ctrl+A. Il richiamo cerca in tutto il blocco e azzera l'eventuale filtro Cerca. Dopo l'invio il campo si svuota per il numero successivo. Cambio blocco, selezione manuale ed Esc azzerano il richiamo; cambiare blocco non cambia il Program in onda. Nei campi di testo e nei valori numerici la tastiera continua a modificare il campo normalmente.

`1`, `01` e `001` richiamano lo stesso numero: non è consentito assegnarli a foto diverse nello stesso blocco. Numeri assenti, file mancanti, immagini illeggibili e duplicati già presenti nei vecchi progetti sono segnalati e **Invio non invia la foto precedente**. Program deve essere già avviato.

I progetti precedenti vengono letti e i numeri globali già assegnati sono copiati una sola volta nei blocchi che contengono le rispettive foto. Da quel momento le numerazioni sono indipendenti. I nuovi salvataggi usano il formato progetto 2, leggibile dalla versione 0.10.0; conserva il vecchio file se devi aprirlo con versioni precedenti.

## Avvio

## Arresto trasparente, menu foto e numeri di riferimento (0.10.0)

Prima di chiudere un canale NDI, il programma lo pulisce in **cut ad alpha zero**, invia più fotogrammi trasparenti per almeno 200 ms e solo dopo chiude il mittente. **Arresta Program** pulisce e arresta solo Program; **Arresta Preview** pulisce e arresta solo Preview. Chiudendo l'app viene eseguita la stessa sequenza per entrambi. La foto selezionata e le sue regolazioni restano disponibili per il successivo avvio. Se il mittente è già in errore, il mancato invio del clear finale viene segnalato. Il risultato sul ricevitore resta soggetto alla ricezione dei frame prima della disconnessione.

Il clear finale del Program produce anche l'evento Companion "vuoto", se prima era presente una foto. In chiusura dell'app si attende per un tempo limitato il completamento degli invii TCP pendenti; non vengono duplicati comandi già inviati.

Con **tasto destro sulla foto**, sia nell'elenco sia nelle miniature:

- **Aggiungi a blocco…** mostra i blocchi disponibili e permette di aggiungere direttamente la foto, oppure l'intera selezione multipla. **Nuovo blocco…** ne crea uno senza cambiare il Program in onda.
- **Assegna / modifica numero…** aggiunge un numero di riferimento alla foto cliccata; con una selezione multipla il menu specifica il nome della singola foto interessata. Sono ammesse da 1 a 9 cifre e vengono conservati gli zeri iniziali, ad esempio `001`. Un campo vuoto o **Rimuovi numero** cancella solo il riferimento.

Il numero appare nella colonna **Numero** dell'elenco e davanti al nome nelle miniature (`#001 · foto.png`), ed è ricercabile con Cerca. Non rinomina il file, non modifica l'ordine della playlist e non viene sovrimpresso nei flussi NDI. È associato alla foto nel singolo blocco, salvato nel progetto e mantenuto quando si ricollega la cartella immagini. Blocchi diversi hanno numerazioni indipendenti.

I comandi di trasmissione sono raccolti in **due sezioni affiancate, sempre visibili sopra la barra degli strumenti**:

- **Program**: Avvia Program / Arresta Program, indicatore dedicato e campo **Nome flusso NDI** per WATCHOUT.
- **Preview**: Avvia Preview / Arresta Preview, indicatore dedicato e campo **Nome flusso NDI** indipendente per la regia.

Entrambi i nomi sono salvati nel progetto e nelle preferenze locali. Modificare il nome Program non cambia Preview. Per rinominare un flusso basta arrestare quel flusso; l'altro può restare attivo. I nomi devono essere validi e diversi fra loro per salvare un progetto e non possono coincidere quando entrambi i mittenti sono attivi. I vecchi progetti senza un nome Preview esplicito ricevono il precedente valore derivato da Program con suffisso ` - Preview`, che diventa poi modificabile indipendentemente.

Grigio **SPENTO** significa che quel flusso è disabilitato; arancione **AVVIO** che attende il primo frame; verde **ACCESO** che il mittente sta inviando; rosso **ERRORE** segnala un avvio fallito o un invio interrotto. Gli indicatori rappresentano lo stato locale del mittente, non confermano la ricezione sul dispositivo remoto. Entrambi partono spenti e i comandi restano indipendenti. Un flusso ACCESO può trasmettere un fotogramma trasparente: CLEAR / Esc puliscono il contenuto ma non spengono il mittente. La precedente casella di abilitazione Preview è stata sostituita dal pulsante nella sua sezione.

## Due flussi NDI: Programma e Preview regia (0.10.0)

Premi **Avvia Preview**, nella sezione Preview, per rendere disponibile la seconda sorgente NDI con il nome scelto nel suo campo. La scritta sotto il campo mostra il nome effettivo del mittente attivo. Il ricevitore della regia può selezionare `NOME-PC (nome Preview scelto)`.

La sorgente Preview trasmette l'immagine preparata nel riquadro SELEZIONE, con la stessa scala, posizione, aspect ratio e alpha. Si aggiorna automaticamente quando scegli una foto, la sposti, la ridimensioni o cambi blocco/filtro. È il fotogramma pulito: scacchiera, bordi, scritte e resto dell'interfaccia non vengono trasmessi. Senza una selezione valida è trasparente. Il Programma continua a cambiare solo con INVIA/doppio clic o CLEAR.

I flussi si avviano e arrestano separatamente: Avvia Program / Arresta Program controlla il Programma, Avvia Preview / Arresta Preview controlla Preview. Puoi preparare il segnale regia anche con Programma fermo. Preview non viene riattivato automaticamente al riavvio. Entrambi usano lo stesso formato e fps; il formato è modificabile dopo aver arrestato entrambi i flussi, mentre ogni nome è modificabile quando il rispettivo flusso è fermo. Se carichi un progetto durante la trasmissione, i flussi attivi conservano i propri nomi e formato fino all'arresto, mentre la Preview continua a seguire la selezione del progetto caricato. Due sorgenti comportano due invii NDI e il relativo uso di rete e CPU.

**ESC = clear in dissolvenza; doppio ESC rapido = cut.** Il campo **Cambio foto ms** regola le transizioni fra immagini; il nuovo campo **Clear Esc ms** regola separatamente il clear da tastiera, da 1 a 60000 ms, inizialmente 500 ms. Con il focus nella finestra principale, il primo Esc sfuma il fotogramma corrente ad alpha zero con la durata Clear Esc, anche se Dissolvenza è disattivata. Un secondo tocco fisico entro **350 ms** interrompe la dissolvenza e pulisce subito in cut. Tenere premuto Esc non vale come doppio tocco e non riavvia il clear. Selettore, anteprima e flusso Preview della regia restano invariati. Il normale pulsante CLEAR continua a rispettare cut/dissolvenza e durata Cambio foto. Nei dialoghi Esc annulla il dialogo; non è una scorciatoia globale di Windows. La durata dedicata viene salvata nel progetto e nelle preferenze locali.

## Bitfocus Companion via TCP

Apri **Companion TCP…** nella barra principale. Imposta **Abilita notifiche Companion**, IP del computer Companion, porta TCP e le due stringhe. Tutti i valori sono salvati nel progetto e nelle preferenze. L'indirizzo iniziale è `127.0.0.1`, la porta iniziale `51234`; modificane i valori per la tua regia. In Companion abilita il **TCP Listener** e verifica che la sua porta corrisponda. Vedi [TCP/UDP Control](https://companion.free/user-guide/v5.1/remote-control/tcp-udp/) e [Settings](https://companion.free/user-guide/v5.1/config/settings/).

- **Da vuoto alla prima foto:** invia la prima stringa quando un frame Program con una foto viene consegnato al mittente NDI dopo lo stato vuoto.
- **Clear completato: nessuna foto:** invia la seconda quando il clear è terminato e il Program è trasparente, sia per dissolvenza sia per cut/doppio Esc.

Non viene ripetuto il comando durante i cambi foto, non vengono generati eventi dal Preview, dal cambio blocco, dalla ricerca o dal semplice caricamento di un progetto. L'avvio iniziale a trasparente non manda la stringa di vuoto. Se il clear viene interrotto da un'altra foto, il Program non passa per lo stato vuoto e non emette la stringa di clear. Dopo un clear completato, la prossima foto genera nuovamente il primo evento. Anche l'arresto del Program ora esegue un clear finale e genera il messaggio di vuoto quando quel frame è stato inviato con successo.

Ogni campo contiene **un comando su una riga**, senza scrivere `\\n`: il programma aggiunge automaticamente il terminatore LF richiesto da Companion. Una stringa vuota disabilita il solo messaggio corrispondente. Per esempio, dopo aver creato in Companion una variabile custom `ndi_on_air`, puoi configurare:

```
CUSTOM-VARIABLE ndi_on_air SET-VALUE 1
CUSTOM-VARIABLE ndi_on_air SET-VALUE 0
```

La prima riga va nel campo prima foto, la seconda nel campo clear. In alternativa puoi usare comandi `LOCATION pagina/riga/colonna PRESS` riferiti a pulsanti configurati da te. Le stringhe non sono precompilate per evitare azioni su pulsanti non scelti.

**Test connessione** apre e chiude una connessione TCP senza inviare comandi: controlla l'esito nella finestra di configurazione e nella barra principale. Le notifiche reali vengono messe in coda in ordine su un thread separato, dopo l'invio del frame Program, con timeout di connessione e scrittura di 1 secondo. Viene aperta una connessione per comando; un errore viene mostrato e non ferma NDI. Non ci sono ritentativi automatici, per evitare duplicazioni di azioni. "Stringa inviata" indica la scrittura TCP, non certifica l'esecuzione del comando in Companion o la ricezione del video WATCHOUT. Modificando configurazione si annullano i messaggi ancora in coda; quelli già consegnati non vengono ripetuti. L'abilitazione a Program già occupato non riproduce retroattivamente l'evento: si attendono le transizioni successive.

La dissolvenza del Programma e la frequenza del monitor locale sono invariate: il monitor locale resta a 10 aggiornamenti/secondo e non rappresenta la fluidità del segnale ricevuto da WATCHOUT.

La casella **Sempre in primo piano**, nella barra superiore, mantiene NDIPlayer sopra le altre finestre. Disattivala per tornare al comportamento normale. La scelta viene salvata localmente in `settings.json.window.json`, accanto alle preferenze dell'app, e ripristinata al successivo avvio. È indipendente dal progetto: aprire un altro progetto non cambia questa preferenza e non modifica il flusso NDI.

## Playlist a blocchi, ricerca e progetti (0.10.0)

1. Apri la cartella delle immagini e scegli **Tutta la libreria**. Crea i blocchi con **Nuovo blocco** e assegna un nome (es. Apertura, Relatore 1, Intervallo).
2. Seleziona le foto con Ctrl / Maiusc e usa **Aggiungi foto…** per scegliere il blocco di destinazione. Puoi anche trascinare la selezione sul nome del blocco. La stessa foto può appartenere a più blocchi senza duplicazioni; aggiungerla di nuovo allo stesso blocco non crea doppioni.
3. Scegli un blocco per vedere solo le sue foto, in elenco o miniature. Usa **Cerca** per filtrare nomi e percorsi dentro quel blocco; × azzera il filtro. In Tutta la libreria la ricerca riguarda l'intero catalogo e le immagini referenziate dalle playlist. Un file mancante rimane riconoscibile con FILE MANCANTE e non può essere inviato.
4. Riordina le foto trascinandole sulla foto davanti alla quale inserirle, oppure con **Foto ↑ / ↓**. Trascina i nomi dei blocchi per spostarli sopra/sotto un altro blocco, o usa **Blocco ↑ / ↓**. Rinomina, Rimuovi foto ed Elimina blocco modificano solo le playlist, senza eliminare gli originali dal disco.
5. In elenco la colonna **Stato** distingue SELEZIONATA e IN ONDA; nelle miniature il bordo azzurro indica la selezione, il bordo rosso e il cartellino IN ONDA indicano il programma. Entrambi possono comparire sulla stessa immagine. Durante una dissolvenza vengono segnate le immagini effettivamente presenti nei frame inviati, incluse le eventuali transizioni interrotte. CLEAR rimuove gli indicatori alla fine della transizione. Una scritta globale mantiene visibili i nomi in onda anche se non sono nel blocco o filtro corrente. In caso di errore indica l'ultimo inviato: non certifica la ricezione WATCHOUT.
6. Usa **Salva progetto** / **Salva con nome** per creare un file `.ndiproj`. Contiene cartella, blocchi e ordine, trasformazioni condivise per immagine, formato/fps/nome NDI, transizione e preferenze della libreria. **Apri progetto** li ripristina. L'asterisco nel titolo segnala modifiche non esportate; prima di chiudere, aprire o creare un progetto viene chiesto se salvarle. Le preferenze locali restano salvate automaticamente fra avvii dell'app.
7. Per un'altra postazione copia progetto e cartella delle foto mantenendo la disposizione relativa: i percorsi sono salvati rispetto al progetto, quando sono sullo stesso disco. Le immagini non sono incorporate. **Ricollega cartella** aggiorna in blocco i percorsi appartenenti alla precedente cartella principale, comprese playlist e trasformazioni; i riferimenti esterni restano invariati. Ogni sovrascrittura del progetto conserva il precedente `.bak`.

**Invio sempre manuale:** cambiare blocco, selezione, ricerca, riordino o caricare un progetto non chiama INVIA né CLEAR. La foto già in onda continua anche se viene rimossa dal blocco, il file non è più disponibile, o apri un progetto diverso. Premi INVIA oppure fai doppio clic per cambiare programma. Se apri un progetto con NDI attivo, la trasmissione mantiene formato e nome correnti; le impostazioni del progetto vengono applicate al prossimo avvio NDI e l'interfaccia lo segnala. Il monitor Selezione continua a usare il formato realmente attivo fino all'arresto.

Non c'è avanzamento automatico né temporizzazione delle playlist in questa versione. Le trasformazioni sono per file e quindi comuni ai blocchi che usano la stessa foto.

## Utilizzo delle immagini

**Doppio clic per andare in onda:** con Program già avviato, fai doppio clic sinistro sul file nell'elenco, sulla miniatura oppure dentro il fotogramma del riquadro **SELEZIONE**. Viene inviata l'immagine selezionata con scala/posizione correnti e con il cut o la dissolvenza impostati, esattamente come con INVIA. Il clic singolo prepara solo la selezione; il doppio clic fuori dal fotogramma, nel riquadro Programma o nell'area vuota della libreria non invia nulla. Se Program è fermo o l'immagine non è leggibile, viene mostrato lo stato e non viene avviato un nuovo flusso automaticamente.

**Novità 0.10.0:** nell'anteprima **SELEZIONE** puoi spostare l'immagine trascinando con il tasto sinistro all'interno del bordo. La rotellina ingrandisce/riduce attorno al centro attuale dell'immagine (fattore 10% per scatto); **Maiusc + rotellina** usa il 2% per regolazioni fini. La scala resta tra 1% e 400%. I campi numerici si aggiornano insieme al mouse e restano modificabili a mano. Il monitor PROGRAMMA non è interattivo.

I campi **Centro X / Centro Y** rappresentano ora il centro dell'immagine nelle coordinate del flusso, con origine nell'angolo in alto a sinistra: a 1920×1080 il centro iniziale è **X=960, Y=540**; a 1280×720 è **X=640, Y=360**. Reset posizione ripristina questo centro e scala 100%. Un'altezza dispari può mostrare mezzo pixel. Cambiare risoluzione conserva lo spostamento rispetto al centro del nuovo formato. Le vecchie preferenze vengono visualizzate nella nuova convenzione mantenendo invariata l'inquadratura: internamente si mantengono gli offset salvati, senza migrazioni distruttive o conversioni ripetute.

Il mouse modifica solo la selezione preparata: **premi INVIA (oppure fai doppio clic sul file) per applicare le modifiche al flusso**, con la transizione scelta. Le posizioni vengono salvate per immagine al rilascio del trascinamento; scala e campi numerici al completamento della modifica. La sorgente selezionata viene decodificata una volta e riutilizzata durante le trasformazioni, senza tenere bloccato il file originale; Aggiorna la rilegge dal disco.

**Novità 0.10.0:** il catalogo ignora i file con attributo Windows Nascosto e i file di servizio che iniziano con `._` (AppleDouble del Mac) oppure `.-`. Durante la ricorsione vengono saltate anche le cartelle nascoste, quelle con nome che inizia con un punto e `__MACOSX`. I filtri valgono per elenco e miniature; nessun file viene cancellato. Premi Aggiorna per rileggere una cartella già aperta.

Il campo **Nome flusso NDI**, nella sezione Program, permette di scegliere il nome della sorgente. Il valore viene salvato nelle preferenze; il default rimane `Watchout Image NDI`. Spazi iniziali/finali vengono rimossi e sono supportati accenti tramite UTF-8. Nomi vuoti, troppo lunghi o con caratteri `\ / : * ? " < > |` e caratteri di controllo vengono rifiutati. Per rinominare un canale già attivo: arresta NDI, modifica il nome, riavvia NDI e seleziona la nuova sorgente in WATCHOUT. Il nome non si modifica mentre il flusso è attivo.

**Novità 0.10.0:** la libreria a sinistra permette di scegliere **Elenco** o **Miniature**. Il cursore **Dimensione** regola le miniature da 64 a 256 pixel; la scelta viene salvata insieme alle preferenze. Le miniature si caricano in sottofondo per gli elementi visualizzati, con una cache limitata a 128 immagini. Per cartelle diverse con nomi uguali, il percorso completo resta visibile nell'elenco e nel suggerimento del file.

Al centro/destra ci sono due monitor affiancati, ridimensionabili allargando la finestra o spostando il separatore della libreria:

- **SELEZIONE**, bordo azzurro: immagine preparata, con scala e posizione. Cambiare selezione o regolazioni non modifica il programma finché non premi INVIA o fai doppio clic sul file.
- **PROGRAMMA**, bordo rosso: copia degli ultimi frame dopo che la chiamata sincrona al mittente NDI è terminata. Mostra cut, alpha delle dissolvenze e clear; si aggiorna fino a 10 volte al secondo per contenere il carico UI, indipendentemente dai 50 fps nominali del flusso. Non cambia quando scegli un'altra immagine nella libreria. Arresto ed errori sono indicati esplicitamente.

Il Programma è un **monitor locale dei frame consegnati al SDK NDI**, non un ricevitore NDI o un ritorno da WATCHOUT: non verifica perdita di rete, connessione del ricevitore, decodifica o compositing sul server. Per confermare l'uscita finale serve controllare WATCHOUT. I due monitor usano gli stessi confini del fotogramma e nessuna guida viene trasmessa.

Nella versione 0.10.0 l'anteprima mostra un **bordo azzurro che delimita esattamente il fotogramma di uscita** nel rapporto configurato (per esempio 16:9 a 1920×1080). La scacchiera compare solo dentro quel bordo e rappresenta l'alpha; l'area scura esterna non fa parte del flusso. Ridimensionando la finestra, immagine e fotogramma vengono scalati insieme senza cambiare proporzioni. Bordo e scritte sono guide locali e non vengono trasmessi. L'anteprima mostra la selezione: per applicarla in onda premi INVIA o fai doppio clic sul file.

1. Windows 10/11 x64 con .NET Framework 4.8. Esegui `NDIPlayer-0.13.3-Setup-win-x64.exe`: include il redistributable ufficiale **NDI 6 Runtime 6.3.2.0**, senza NDI Tools. Se necessario apre il setup del runtime, con licenza propria da accettare e possibile richiesta amministratore. Poi installa l'app per l'utente corrente e crea il collegamento nel menu Start. Non serve il .NET SDK.
2. Avvia NDIPlayer dal menu Start. Se usi i soli sorgenti o il vecchio ZIP portabile, il runtime resta da installare separatamente. Informazioni NDI: https://ndi.video/ . NDI® is a registered trademark of Vizrt NDI AB.
3. Apri una cartella: include sottocartelle, evita collegamenti circolari, segnala cartelle non accessibili. Aggiorna ripete la scansione.
4. Seleziona un file: anteprima su scacchiera, che indica la trasparenza e non viene trasmessa. PNG, JPEG, BMP, GIF, TIFF e ICO; per formati multipagina/animati viene utilizzato il primo frame. HEIC, WebP, SVG e RAW non sono supportati in questa versione.
5. Imposta dimensioni e fps prima di avviare NDI. Default 1920×1080 progressivo, 50 fps; larghezza pari, massimo 3840×2160 e 60 fps. Arresta entrambi i flussi per cambiare formato.
6. Scala 100% = fit senza distorsioni. Centro X aumenta verso destra, Centro Y verso il basso, in pixel del flusso; per l'immagine centrata valgono metà larghezza e metà altezza del flusso. Scala relativa al fit, regolazioni separate per immagine e salvate automaticamente.
7. Premi Avvia Program: il flusso parte trasparente. Premi **INVIA selezione** o fai doppio clic sul file per andare in onda; la sola selezione modifica l'anteprima. Cut se Dissolvenza non selezionata; altrimenti transizione della durata in ms impostata. Cambiare regolazioni richiede un nuovo INVIA.
8. **CLEAR trasparente** invia alpha zero, con cut/dissolvenza secondo la scelta corrente. Per un clear immediato disattiva Dissolvenza. Arrestare NDI ora invia automaticamente i frame trasparenti finali prima di chiudere la sorgente.

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

Tecnologia: C# / WinForms, .NET Framework 4.8 x64, GDI+ per decodifica/rendering; SDK NDI nativo tramite ABI C. Scelta per produrre subito un eseguibile Windows con NAudio per le uscite audio e FFmpeg per i video, usando il compilatore già presente. Un futuro renderer GPU può sostituire quello CPU senza cambiare sender e libreria.

- `src/Core.cs`: catalogo ricorsivo, geometria, render BGRA e interpolazione alpha corretta.
- `src/Ndi.cs`: `IVideoSender`, binding NDI, playout indipendente dalla UI, clock SDK e rilascio risorse sul thread di invio.
- `src/App.cs`: UI, monitor di selezione/programma e persistenza locale.
- `src/LibraryBrowser.cs`: libreria virtuale elenco/miniature, decodifica in sottofondo, cache limitata e annullamento dei caricamenti quando cambia cartella/dimensione.
- `src/PreviewCanvas.cs`: rendering dei monitor con guide separate dai frame trasmessi.
- `tests/FeatureTests.cs`: monitor verificato contro un mittente controllato, cut/clear/dissolvenza, fallimento invio, isolamento snapshot e caricamento miniature.
- `tests/TransformTests.cs`: drag/rotellina, sincronizzazione campi e rendering, reset, compatibilità delle posizioni salvate, cambio risoluzione e cache della sorgente.
- `src/Projects.cs`: modello dei blocchi, filtro/riordino, formato progetto versionato, percorsi portabili e ricollegamento cartella.
- `src/ProjectUi.cs`: gestione progetti e blocchi, selezione multipla, drag and drop e ricerca.
- `src/PreviewOutput.cs`: invio indipendente della selezione alla regia e clear in dissolvenza via Esc; gestione condivisa del runtime in `src/Ndi.cs` per mantenere attivo l'altro sender quando uno viene chiuso.
- `tests/ProjectTests.cs`: cinque blocchi da sei immagini, portabilità, indicatori di programma e prove d'integrazione che escludono invii durante cambio blocco/ricerca/caricamento progetto.
- `tests/RoutingTests.cs`: separazione Preview/Programma, clear durante dissolvenza, arresto indipendente e due sender sul runtime reale.
- `src/EscapeGesture.cs`: riconoscimento di due pressioni fisiche rapide, con esclusione dell'autorepeat.
- `src/CompanionTcp.cs` e `src/CompanionUi.cs`: configurazione Companion, invio TCP in coda e notifiche basate sui frame Program effettivamente inviati.
- `tests/CompanionTests.cs`: ricevitore TCP locale di prova, UTF-8/newline, ordine e unicità degli eventi, clear completato, esclusione Preview, gestione errori e doppio Esc. Queste prove richiedono accesso TCP locale a 127.0.0.1, senza apparati esterni.
- `src/ImageNumbers.cs` e `src/LibraryActions.cs`: numeri di riferimento e menu contestuale per aggiunta ai blocchi.
- `tests/StopAndNumberTests.cs`: ultimi frame trasparenti prima della distruzione, arresto indipendente dei due mittenti, chiusura dell'app, menu contestuale, numeri e portabilità del progetto.
- `tests/Tests.cs`: geometria, alpha reale PNG, transizioni, clear, scansione, layout ABI e playout con sender simulato; test opzionale sul runtime reale.

Build dalla cartella progetto con PowerShell: `./build.ps1`. Test runtime opzionale: `./bin/Tests.exe --ndi`. Alla prima build scarica NAudio 1.10.0 dal pacchetto NuGet ufficiale, verificato con SHA-256; FFmpeg si prepara con `installer/prepare-video.ps1`. `.github/workflows/windows.yml` costruisce, esegue test e genera un artifact su Windows dopo la pubblicazione autorizzata.

Limiti: nessun file watcher; scansione cartelle e decodifica dell'immagine selezionata sulla UI, miniature in sottofondo; rendering/fade CPU in spazio dei valori sRGB, nessuna gestione colore ICC professionale, nessuna garanzia realtime. I file molto grandi possono rallentare la UI e il monitor locale. Il sender invia continuamente anche immagini statiche; errori NDI visibili nello stato, nessuna riconnessione automatica. Audio locale opzionale WDM/WASAPI e ASIO; nessun audio incorporato nei flussi NDI. Un'istanza per processo.

## GitHub e più postazioni

Repository privato autorizzato: https://github.com/dallagliomichele/NDIPLayer, ramo `main`.

Da altre postazioni: clonare il repository, eseguire `build.ps1`, installare NDI Runtime e selezionare la propria cartella. Prima di lavorare fare pull, poi commit e push delle modifiche. GitHub ospita codice e cronologia; playout e immagini restano sulla postazione Windows nella rete WATCHOUT.

Nessuna licenza open source applicata automaticamente. Verificare termini NDI prima di distribuire il prodotto o includere runtime/SDK proprietari.

## Installer completo 0.13.3

Il pacchetto contiene app, documentazione e redistributable NDI originale. Non scarica il runtime durante l'installazione. .NET Framework 4.8 è un prerequisito verificato dal setup; se assente, installarlo tramite Windows Update. Non si accetta la licenza NDI in silenzio: il setup ufficiale viene aperto quando il runtime manca o è più vecchio. Un runtime uguale o successivo già rilevato viene conservato. Annullando il setup NDI, non si prosegue con l'app.

Disinstalla NDIPlayer da Impostazioni > App. Le preferenze personali e il runtime condiviso rimangono; quest'ultimo si rimuove separatamente da Windows. L'installer dell'app non è firmato digitalmente; il redistributable incorporato conserva la firma originale Vizrt AG.

Per ricostruire, scarica il [redistributable ufficiale](https://downloads.ndi.tv/SDK/NDI_SDK/NDI%206%20Runtime.exe), poi esegui `installer/build-installer.ps1 -RuntimeFile C:\percorso\NDI6Runtime.exe -IsccPath C:\percorso\ISCC.exe`. Serve Inno Setup 6.5+ o 7; verificare i termini del compilatore per l'uso previsto. Il file generato è in `dist/`. Lo script verifica hash e firma prima di incorporare il redistributable. Non caricare il compiler o i binari proprietari nei sorgenti Git.

Runtime verificato: versione 6.3.2.0, firma Authenticode valida Vizrt AG, SHA-256 `7EE73EEDB56402BCA5100868353DBAB4E944B6C37D2D9881580698A3B61346CD`. Se NDI aggiorna il download, la build si ferma: verificare firma/versione e aggiornare il pin prima di ricostruire.

Verifiche effettuate: compilazione installer con runtime incorporato e 336 test applicativi, incluso sender NDI reale sulla postazione di sviluppo. Non è stata eseguita un'installazione/disinstallazione su Windows pulito: prima della distribuzione operativa verificare il setup su una macchina di prova senza NDI Tools, inclusi annullamento licenza, avvio, clear e disinstallazione. Le prove WATCHOUT restano quelle indicate sopra.
