import { InstanceBase, InstanceStatus } from '@companion-module/base'
import { NDIPlayerClient } from './client.js'
import { buildActions, buildPresets } from './definitions.js'
export const UpgradeScripts = []
export default class NDIPlayerInstance extends InstanceBase {
 async init(config) {
  this.setVariableDefinitions({
   connection: { name: 'Collegamento TCP' }, number: { name: 'Numero composto · ultima conferma TCP' },
   last_response: { name: 'Ultima risposta TCP' }, last_command: { name: 'Ultimo comando' },
   last_error: { name: 'Ultimo errore' }, last_selected_number: { name: 'Ultimo numero selezionato con conferma' },
   last_block: { name: 'Ultimo blocco restituito da un comando' },
  })
  this.setActionDefinitions(buildActions(command => this.sendCommand(command)))
  this.setFeedbackDefinitions({
   connected: { name: 'Socket TCP collegato', type: 'boolean', defaultStyle: { bgcolor: 0x126b36, color: 0xffffff }, options: [], callback: () => this.connected === true },
   last_error: { name: "L'ultimo comando ha prodotto un errore", type: 'boolean', defaultStyle: { bgcolor: 0x9b1d20, color: 0xffffff }, options: [], callback: () => Boolean(this.errorText) },
  })
  await this.configUpdated(config)
 }
 getConfigFields() {
  return [
   { id: 'help', type: 'static-text', label: 'NDIPlayer', value: 'In NDIPlayer abilita Comandi Companion… (porta predefinita 51235). GO richiede Program avviato. ENTER prepara solo Preview.', width: 12 },
   { id: 'host', type: 'textinput', label: 'IP o nome del PC con NDIPlayer', default: '127.0.0.1', width: 8 },
   { id: 'port', type: 'number', label: 'Porta TCP', default: 51235, min: 1, max: 65535, width: 4 },
   { id: 'timeout', type: 'number', label: 'Attesa risposta (secondi)', default: 20, min: 16, max: 60, width: 6 },
  ]
 }
 async configUpdated(config) {
  this.client?.dispose(); this.client = null; this.connected = false; this.errorText = ''; this.recallBlocked = false
  const { presets, structure } = buildPresets(this.label); this.setPresetDefinitions(structure, presets)
  this.values = { connection: 'Scollegato', number: '?', last_response: '', last_command: '', last_error: '', last_selected_number: '?', last_block: '?' }
  this.setVariableValues(this.values); this.checkAllFeedbacks()
  const host = String(config.host ?? '127.0.0.1').trim(); const port = Number(config.port ?? 51235); const seconds = Number(config.timeout ?? 20)
  if (!host || /[\s\r\n]/.test(host) || !Number.isInteger(port) || port < 1 || port > 65535 || !Number.isFinite(seconds) || seconds < 16 || seconds > 60) {
   this.updateStatus(InstanceStatus.BadConfig, 'Indirizzo, porta o attesa non validi'); return
  }
  const client = new NDIPlayerClient({ host, port, timeout: seconds * 1000 }); this.client = client
  client.on('state', (connected, message) => {
   if (this.client !== client) return
   this.connected = connected
   this.values.connection = connected ? 'Collegato' : 'Scollegato'
   if (!connected) { this.values.number = '?'; this.values.last_selected_number = '?'; this.values.last_block = '?' }
   this.setVariableValues(this.values); this.checkFeedbacks('connected')
   this.updateStatus(connected ? InstanceStatus.Ok : message === 'Collegamento in corso' ? InstanceStatus.Connecting : InstanceStatus.ConnectionFailure, message)
  })
  client.on('response', response => { if (this.client === client) this.applyResponse(response) })
  client.start()
 }
 applyResponse({ ok, line, command }) {
  this.values.last_response = line; this.values.last_command = command; this.errorText = ok ? '' : line; this.values.last_error = this.errorText
  if (ok) {
   if (/^(OK NUMBER|OK CANCEL|OK SELECT |OK BLOCK |OK NEXT$|OK PREV$)/.test(line)) this.recallBlocked = false
   if (line.startsWith('OK NUMBER')) this.values.number = line.slice(9).trim()
   else if (line === 'OK CANCEL') this.values.number = ''
   else if (line.startsWith('OK SELECT ')) { this.values.last_selected_number = line.slice(10); this.values.number = '' }
   else if (line.startsWith('OK BLOCK ')) { this.values.last_block = line.slice(9); this.values.last_selected_number = '?'; this.values.number = '' }
   else if (line === 'OK NEXT' || line === 'OK PREV') { this.values.last_selected_number = '?'; this.values.number = '' }
  } else if (command.startsWith('SELECT ') || command === 'ENTER') { this.recallBlocked = true; if (command.startsWith('SELECT ')) this.values.number = command.slice(7) }
  this.setVariableValues(this.values); this.checkFeedbacks('last_error')
  if (!ok) this.log('warn', line)
 }
 async sendCommand(command) {
  try {
   if (command === 'INVALID') this.recallBlocked = true
   if (command === 'GO' && this.recallBlocked) throw new Error('Richiamo non valido: correggi la selezione o premi CANCEL prima di GO')
   if (!this.client) throw new Error('Connessione non configurata')
   await this.client.send(command)
  } catch (error) {
   this.errorText = error.message; this.values.last_command = command; this.values.last_error = this.errorText
   this.setVariableValues(this.values); this.checkFeedbacks('last_error'); this.log('warn', error.message)
  }
 }
 async destroy() { this.client?.dispose(); this.client = null; this.connected = false }
}
