# Problemi e verifiche

## Verificati o corretti

- [x] La connessione Companion e `last_selected_number` si aggiornano; `preview_asset_number` funziona.
- [x] `number` indica il numero composto dal tastierino Companion, non l'asset in Program. Per quello usare `program_asset_number`.
- [x] La velocità video poteva dipendere dal costo di scala e posizione. La velocità ora è applicata alla timeline FFmpeg e il clock di riproduzione è indipendente dalla trasformazione.

## Da verificare sul sistema reale

- [ ] Riprodurre il timeout Companion con l'istanza reale e raccogliere stato/log: il modulo implementa la riconnessione automatica e i test simulati la coprono, ma il timeout segnalato non è stato riprodotto nell'integrazione reale.
