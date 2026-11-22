# Problemi e verifiche aperte

## Comandi TCP esposti sulla rete senza autenticazione

**Rischio accettato per l'uso previsto.** L'app è destinata a reti locali broadcast dedicate e protette dal firewall. `src/CompanionTcp.cs` imposta l'indirizzo di ascolto predefinito a `0.0.0.0` e `src/CompanionControl.cs` accetta comandi TCP senza autenticazione: qualunque host che raggiunga la porta configurata può inviare comandi. La finestra di configurazione e lo stato di ascolto avvisano dell'assenza di autenticazione; usare `127.0.0.1` se Companion gira sullo stesso PC. Non è stato aggiunto un controllo di accesso.

## Build da checkout senza FFmpeg

**Corretto nella procedura.** `build.ps1` richiama `prepare-video.ps1` quando mancano FFmpeg o FFprobe, prima di compilare ed eseguire i test. Per usare un archivio conservato localmente si può prepararlo prima con `-Archive`, come indicato in [COMPILAZIONE.md](COMPILAZIONE.md).

## Documentazione storica non allineata

**Allineata.** Il README distingue l'audio locale corrente dall'assenza di audio nei flussi NDI, rimanda alle impostazioni correnti del countdown e collega la sezione «Avvio» alle istruzioni operative.

## Collaudi sull'impianto

**Video funzionante, audio sull'impianto ancora da collaudare.** Il funzionamento video è stato confermato dall'utente. Le prove audio automatiche hanno verificato decodifica, routing simulato, gain, mute, loop, clear e clock, ma non certificano le uscite fisiche, Dante/ASIO o il sincronismo finale. Il collaudo audio è in [TODO.md](TODO.md). Resta inoltre da provare l'installazione su Windows pulito; non è una verifica già eseguita.

## Riproducibilità delle dipendenze

**Mitigato, conservazione permanente ancora da organizzare.** `installer/prepare-video.ps1` usa una build LGPL mensile identificata esplicitamente e verificata tramite SHA-256; l'installer la prepara sempre dall'archivio verificato. Il fornitore conserva le build mensili per due anni, non per sempre. Lo ZIP e le cache dei sorgenti vanno conservati in un archivio duraturo ancora da scegliere; vedere [COMPILAZIONE.md](COMPILAZIONE.md).
