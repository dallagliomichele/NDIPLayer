
Performance
- Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- verificare decoifica e composizione dei MOV usa con ffmep (potenziali ottimizzazioni)


Funzionalità
- Seek dei video, puntamento frame di entrata e di uscita
- Autostart settabile per i file video- Questo per avere la possibilità di mettere in onda sul primo frame e farli partire su chiamata. 
- nel menu impostazioni, al tab "video e ndi" aggiungiamo un tasto "copia risoluzione preview" collegato a una funzione che legge l'attuale contenuto dalla preview (una volta sola sulal preview corrente) e lo applica alle dimensioni del flusso NDI
- completare intergrazione companion:
-- variabili countdown, titolo e numero asset su variabile?
- aggiungiamo un watcher per la cartella osservata, non aggironiamo automaticamente ma segnaliamo con un pallino notifica rosso che la cartella è cambiata, il tooltip segnalerà quanti file ci sono da aggiornare
- aggiunta shortcut ctrl+a per selezionare tutti gli asset del corrente blocco (l'asset correntemente selezionato e mandato in preview nn cambia, e se l'utente clicca altrove per deselezionare si ritorna alla selezione unica, se clicca un'altra volta si deseleziona anche il contenuto in preview come ora)
- nel menu di scelta della cartella sorgente "browse for folder" visualizziamo anche cartelle preferite?

GUI
allineamenti interfaccia:
- modalità dark selezionabie nelle impostazioni
-- inseriamo un bordo superiore a preview e program dove inseriamo lo stato del flusso NDI e il bottone per avviarlo
-- allineiamo i bottoni delle righe superiori
- estendiamo il colore rosso del program a tutto il frame program e il colore verde a tutto il frame preview

Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
