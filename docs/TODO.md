
Performance
- Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- verificare decodifica e composizione dei MOV usa con ffmpeg (potenziali ottimizzazioni)
- Verificare la possibilità di fare cache dei thumbnail ed evitare i falsh da refresh con molti asset. Separerei il lavoro in un solo processo persistente a bassa priorità, con una cache PNG su disco. Oggi la libreria ha già un worker in background e una cache RAM da 128 thumbnail, ma la cache viene ricostruita quando cambia l’elenco. Inoltre, la generazione passa dal renderer condiviso con il playout e può contendere lo stesso lock Direct3D usato durante le dissolvenze. [LibraryBrowser.cs (line 97)](/C:/Users/Riccardo/Desktop/my_repos/NDIPLayer/src/LibraryBrowser.cs:97) · [Core.cs (line 42)](/C:/Users/Riccardo/Desktop/my_repos/NDIPLayer/src/Core.cs:42)

Darei precedenza agli elementi visibili e farei il pre-cache del resto a player inattivo. Una chiave basata su percorso sorgente, dimensione del file, ultima modifica e risoluzione della thumbnail permette di riutilizzare la cache anche dopo un riavvio. Terrei il worker thumbnail su CPU, così il pre-cache può essere sospeso durante cue e dissolvenze senza contendere il renderer GPU. Verificherei il risultato misurando il tempo di apertura della libreria a cache fredda e calda e gli intervalli tra frame NDI mentre il worker è attivo.

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


Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
