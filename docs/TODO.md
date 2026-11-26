
Performance
- [x] Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- [x] verificare decodifica e composizione dei MOV usa con ffmpeg (potenziali ottimizzazioni)
- [x] Cache PNG persistente per progetto, riuso delle miniature in memoria durante i refresh e svuotamento da Impostazioni. Un thread CPU a bassa priorità serve prima le richieste della vista e prepara il resto a player inattivo.
  - Prova locale con 24 miniature a 128 px, inclusi 6 MOV. Caricamento in 1,96 s a cache fredda e 57 ms a cache calda. Nel test del sender NDI a 1080p50 con dissolvenze, il 95° percentile degli intervalli è stato 21,18 ms senza richieste e 21,27 ms durante la generazione.

Funzionalità
- [x] Monitoraggio della cartella sorgente con pallino rosso e conteggio dei file da aggiornare. Scansione applicata con Aggiorna.
- [x] Ctrl+A seleziona gli asset visibili del blocco corrente mantenendo la Preview. Un clic nell’area vuota ripristina la selezione singola, il secondo la cancella.
- [x] Cartelle preferite nei menu Apri cartella e Ricollega cartella.
- [x] Seek dei video e punti di entrata e uscita salvati per file; il Program riproduce il tratto impostato.
- [x] Autostart per file video: se disattivato, Program si prepara sul frame di entrata e parte con Play.
- [x] In Impostazioni > Video e NDI, "Copia risoluzione Preview" legge una volta le dimensioni originali dell'asset selezionato e le applica al formato NDI.
- [x] Crea copia nel menu contestuale di un asset, con impostazioni indipendenti e lo stesso file sorgente.
- [x] aggiunta comando "swap" di default ON: quando il media in preview va in program il media che era in program si sposta in preview




GUI - UX
- [x] Modalità scura selezionabile nelle Impostazioni e salvata localmente.
- [x] Fascia superiore dei monitor con stato NDI e pulsante di avvio/arresto.
- [x] Pulsanti delle righe superiori allineati.
- [x] Cornice rossa del Program e verde della Preview estesa ai monitor.
- [x] aggiungere alla fine della riga "libreria" il nome della cartella corrente
- [x] lanciare un alert pop-up alla chiusura "sei sicuro?"
- [x] spostare i VU meter laterlamente per sfruttare meglio lo spazio
- [x] rendere globali gli shortcut di messa in onda (enter) e di clear (esc), ovvero indipendenti da quale parte dell'applicazione sia in focus al  momento, con l'eccezione dei campi di inserimento del testo
- [x] aggiungere uno status "modificata" alle immagini /video nella vista elenco quando abbiamo cambiato scala o posizione o velocita etc etc. Nella vista thumbnail aggiungerei una leggera cornice arancio
- [x] durante il rescale dell'anteprima effettuare il rescale sulla base della posizione del mouse
- [x] racchiudi i comandi della barra inferiore tematicamente, e separali in frame
- [x] segnaliamo nell'elenco dei media se il media è un video con una leggerissima evidenziazione grigia nella riga
- [x] spostiamo i tasti pausa e rewind in basso a sinistra nei riquadri e facciamoli comparire solo se il media è un video, i VUmeter vanno a tutta altezza, leggermente ridotti in larghezza. 
- [x] nella preview, accanto alla risoluzione, nel caso di un video aggiungiamo la durata
- inseriamo un indicazione degli FPS per renderci conto di eventuali affaticamenti della riproduzione
- [ ] stringiamo lo spazio tra i tasti del blocco e il cmapo cerca


Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
