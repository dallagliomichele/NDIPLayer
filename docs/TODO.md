
Performance
- Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- verificare decodifica e composizione dei MOV usa con ffmep (potenziali ottimizzazioni)
- Verificare la possibilità di fare cache dei thumbnail ed evitare i falsh da refresh con molti asset

Funzionalità
- [x] Monitoraggio della cartella sorgente con pallino rosso e conteggio dei file da aggiornare. Scansione applicata con Aggiorna.
- [x] Ctrl+A seleziona gli asset visibili del blocco corrente mantenendo la Preview. Un clic nell’area vuota ripristina la selezione singola, il secondo la cancella.
- [x] Cartelle preferite nei menu Apri cartella e Ricollega cartella.
- [x] Seek dei video e punti di entrata e uscita salvati per file; il Program riproduce il tratto impostato.
- [x] Autostart per file video: se disattivato, Program si prepara sul frame di entrata e parte con Play.
- [x] In Impostazioni > Video e NDI, "Copia risoluzione Preview" legge una volta le dimensioni originali dell'asset selezionato e le applica al formato NDI.
- [x] Crea copia nel menu contestuale di un asset, con impostazioni indipendenti e lo stesso file sorgente.



GUI
- [x] Modalità scura selezionabile nelle Impostazioni e salvata localmente.
- [x] Fascia superiore dei monitor con stato NDI e pulsante di avvio/arresto.
- [x] Pulsanti delle righe superiori allineati.
- [x] Cornice rossa del Program e verde della Preview estesa ai monitor.
- aggiungere alla fine della riga "libreria" il nome della cartella corrente
- lanciare un alert pop-up alla chiusura "sei sicuro?"
- spostare i VU meter laterlamente per sfruttare meglio lo spazio

Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
