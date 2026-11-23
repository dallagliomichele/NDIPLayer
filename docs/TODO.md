
Performance
- Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- verificare decodifica e composizione dei MOV usa con ffmep (potenziali ottimizzazioni)
- Verificare la possibilità di fare cache dei thumbnail ed evitare i falsh da refresh con molti asset

Funzionalità

- aggiungiamo un watcher per la cartella osservata, non aggironiamo automaticamente ma segnaliamo con un pallino notifica rosso che la cartella è cambiata, il tooltip segnalerà quanti file ci sono da aggiornare
- aggiunta shortcut ctrl+a per selezionare tutti gli asset del corrente blocco (l'asset correntemente selezionato e mandato in preview nn cambia, e se l'utente clicca altrove per deselezionare si ritorna alla selezione unica, se clicca un'altra volta si deseleziona anche il contenuto in preview come ora)
- nel menu di scelta della cartella sorgente "browse for folder" visualizziamo anche cartelle preferite?
- Seek dei video, puntamento frame di entrata e di uscita
- Autostart settabile per i file video- Questo per avere la possibilità di mettere in onda sul primo frame e farli partire su chiamata. 
- nel menu impostazioni, al tab "video e ndi" aggiungiamo un tasto "copia risoluzione preview" collegato a una funzione che legge l'attuale contenuto dalla preview (una volta sola sulla preview corrente) e lo applica alle dimensioni del flusso NDI


GUI
- [x] Modalità scura selezionabile nelle Impostazioni e salvata localmente.
- [x] Fascia superiore dei monitor con stato NDI e pulsante di avvio/arresto.
- [x] Pulsanti delle righe superiori allineati.
- [x] Cornice rossa del Program e verde della Preview estesa ai monitor.

Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
