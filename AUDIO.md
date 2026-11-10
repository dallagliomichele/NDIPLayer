# Audio NDIPlayer 0.13.0

L'audio dei video esce solo dalle schede audio del PC. I flussi NDI Program e Preview restano video con alpha, senza audio.

## Configurazione

1. Pulisci Program, poi apri **Uscite audio…**.
2. In ciascuna sezione, Program e Ascolto Preview, spunta **Abilita lettura audio**, scegli la scheda **WDM/WASAPI** oppure il driver **ASIO**, e la coppia stereo di uscita (1–2, 3–4, 9–10 ecc.).
3. Premi **Applica**. Un dispositivo mancante o una coppia inesistente produce un errore: non viene scelta automaticamente un'altra scheda.
4. Se usi lo stesso driver per entrambe le sezioni scegli coppie diverse. Il driver viene aperto una sola volta e i canali non utilizzati ricevono silenzio. Installa prima il driver ASIO x64 del produttore; il pacchetto non include driver Dante o ASIO.

Il motore lavora a 48 kHz stereo per sorgente. Il driver ASIO deve supportare 48 kHz; imposta buffer e clock dal pannello del produttore. WDM utilizza WASAPI condiviso e la conversione al formato della scheda. Program e Preview possono usare anche dispositivi distinti.

La configurazione delle uscite si cambia con Program vuoto, per evitare riaperture della scheda durante la messa in onda. Se carichi un progetto mentre Program è occupato, le nuove uscite vengono applicate al prossimo clear. Gain e mute restano modificabili dal vivo.

## Uso

- Seleziona un video e imposta **Gain video dB** da −60 a +12 dB. Il valore appartiene al file ed è condiviso dalle sue occorrenze nei blocchi. Viene salvato nel progetto e aggiornato anche se il file è già in riproduzione.
- **Mute Program** e **Mute Preview** agiscono separatamente, mantenendo la riproduzione sincronizzata. Preview riparte dalla selezione e non è un ritorno del Program.
- L'audio segue Loop e le dissolvenze del video. A fine video senza Loop diventa silenzioso mentre resta l'ultimo fotogramma. Un video senza traccia audio resta silenzioso.
- Clear in dissolvenza sfuma anche l'audio. Clear in cut applica una rampa audio minima di circa 5 ms contro i clic, oltre alla latenza del buffer della scheda.
- I vumeter stereo mostrano RMS, picco, mantenimento del picco e avviso CLIP. Il segnale viene limitato a ±1 prima dell'uscita: se appare CLIP, riduci il gain per evitare distorsione.

I livelli sono misurati sul PCM consegnato al backend, dopo mix, gain, mute e dissolvenze. Non misurano volume di Windows, conversioni successive, routing Dante o livello fisico dei convertitori. Nessuna normalizzazione automatica.

## Sincronismo e diagnosi

Con l'audio abilitato il video segue il clock di presentazione del dispositivo; per ASIO il tempo viene stimato dai callback e dalla latenza dichiarata dal driver. Latenza NDI, WATCHOUT, display e scheda dipendono dall'impianto. **Correzione video ms** (−500…+500) consente una taratura separata: un valore positivo ritarda il video rispetto all'audio.

Prova un filmato con lampo e impulso sonoro sull'uscita reale di WATCHOUT; confronta più cicli e ripeti dopo cambi di buffer. Con due dispositivi indipendenti non è garantito un clock hardware comune. In caso di errore del driver il software segnala l'errore e prosegue il video sul clock interno; pulisci Program e riapplica le uscite per ripristinare l'audio.

I test automatici verificano PCM decodificato da un MOV reale, routing multicanale con dispositivo simulato, gain, mute, fade, clip, loop, fine file, clock e persistenza. Non sostituiscono il collaudo con la tua scheda WDM o Dante/ASIO: verifica coppie, assenza di suoni su canali non assegnati, sincronismo, clear e riproduzione prolungata prima dell'uso in trasmissione.

## Installazione e dipendenze

L'installer include NAudio 1.10.0 con licenza Ms-PL, FFmpeg LGPL e il redistributable ufficiale NDI. Non servono NDI Tools né un SDK .NET; occorre .NET Framework 4.8 e il driver della scheda. Le licenze NAudio sono nella sottocartella `audio` dell'installazione.

Per lo sviluppo, `installer/prepare-audio.ps1` scarica il pacchetto NuGet ufficiale e ne verifica SHA-256; `build.ps1` lo richiama se manca `bin/NAudio.dll`. Le prove automatiche non aprono schede audio reali.
