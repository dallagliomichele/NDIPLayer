
Performance
- Verificare su hardware reale WDM/WASAPI e Dante/ASIO: coppie assegnate e canali silenziosi
- verificare decodifica e composizione dei MOV usa con ffmep (potenziali ottimizzazioni)
- Verificare la possibilità di fare cache dei thumbnail ed evitare i falsh da refresh con molti asset

Funzionalità

- aggiungiamo un watcher per la cartella osservata, non aggironiamo automaticamente ma segnaliamo con un pallino notifica rosso che la cartella è cambiata, il tooltip segnalerà quanti file ci sono da aggiornare
- aggiunta shortcut ctrl+a per selezionare tutti gli asset del corrente blocco (l'asset correntemente selezionato e mandato in preview nn cambia, e se l'utente clicca altrove per deselezionare si ritorna alla selezione unica, se clicca un'altra volta si deseleziona anche il contenuto in preview come ora)
- nel menu di scelta della cartella sorgente "browse for folder" visualizziamo anche cartelle preferite?
- [x] Seek dei video e punti di entrata e uscita salvati per file; il Program riproduce il tratto impostato.
- [x] Autostart per file video: se disattivato, Program si prepara sul frame di entrata e parte con Play.
- [x] In Impostazioni > Video e NDI, "Copia risoluzione Preview" legge una volta le dimensioni originali dell'asset selezionato e le applica al formato NDI.
-aggiungiamo una voce al menù contestuale che si apre clickando col bottone destro su un asset: "crea copia" -> dovrebbe creare un'istanza dell'asset a cui possiamo associare altre impostazioni, senz aper forza creare una copia del file su disco. In questo modo possiamo avere un asset settato ad esempio su zoom 50% e una sua versione con zoom 100%



GUI
- [x] Modalità scura selezionabile nelle Impostazioni e salvata localmente.
- [x] Fascia superiore dei monitor con stato NDI e pulsante di avvio/arresto.
- [x] Pulsanti delle righe superiori allineati.
- [x] Cornice rossa del Program e verde della Preview estesa ai monitor.

Documentazione
- aggiungiamo i tooltip a tutti i bottoni, si attivano dopo 2s di mouse over
