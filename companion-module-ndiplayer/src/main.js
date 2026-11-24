import { InstanceBase, InstanceStatus } from '@companion-module/base'
import { NDIPlayerClient } from './client.js'
import { buildActions, buildPresets } from './definitions.js'
export const UpgradeScripts = []
export default class NDIPlayerInstance extends InstanceBase {
 async init(config) {
  this.setVariableDefinitions({
   connection: { name: 'Collegamento TCP' }, number: { name: 'Numero composto · tastierino' },
   last_response: { name: 'Ultima risposta TCP' }, last_command: { name: 'Ultimo comando' },
   last_error: { name: 'Ultimo errore' }, last_selected_number: { name: 'Ultimo numero selezionato con conferma' },
   last_block: { name: 'Ultimo blocco restituito da un comando' },
   video_state: { name: 'Stato video Program' }, video_countdown: { name: 'Tempo rimanente video Program' }, video_remaining_seconds: { name: 'Secondi rimanenti video Program' },
   preview_asset_title: { name: 'Titolo asset Preview' }, program_asset_title: { name: 'Titolo asset Program' },
   preview_asset_number: { name: 'Numero asset Preview' }, program_asset_number: { name: 'Numero asset Program' },
  })
  this.setActionDefinitions(buildActions(command => this.sendCommand(command)))
  this.setFeedbackDefinitions({
   connected: { name: 'Socket TCP collegato', type: 'boolean', defaultStyle: { bgcolor: 0x126b36, color: 0xffffff }, options: [], callback: () => this.connected === true },
   video_playing: { name: 'Video Program in riproduzione', type: 'boolean', defaultStyle: { bgcolor: 0x126b36, color: 0xffffff }, options: [], callback: () => this.values.video_state === 'playing' },
   video_paused: { name: 'Video Program in pausa', type: 'boolean', defaultStyle: { bgcolor: 0x9b7000, color: 0xffffff }, options: [], callback: () => this.values.video_state === 'paused' },
   video_ended: { name: 'Video Program terminato', type: 'boolean', defaultStyle: { bgcolor: 0x9b1d20, color: 0xffffff }, options: [], callback: () => this.values.video_state === 'ended' },
   last_error: { name: "L'ultimo comando ha prodotto un errore", type: 'boolean', defaultStyle: { bgcolor: 0x9b1d20, color: 0xffffff }, options: [], callback: () => Boolean(this.errorText) },
  })
  await this.configUpdated(config)
 }
 getConfigFields() {
  return [
   { id: 'help', type: 'static-text', label: 'NDIPlayer', value: 'In NDIPlayer abilita Comandi Companion… (porta predefinita 51235). GO richiede Program avviato; PLAY e PAUSE agiscono sul video in Program. ENTER prepara solo Preview.', width: 12 },
   { id: 'host', type: 'textinput', label: 'IP o nome del PC con NDIPlayer', default: '127.0.0.1', width: 8 },
   { id: 'port', type: 'number', label: 'Porta TCP', default: 51235, min: 1, max: 65535, width: 4 },
   { id: 'timeout', type: 'number', label: 'Attesa risposta (secondi)', default: 20, min: 16, max: 60, width: 6 },
  ]
 }
 async configUpdated(config) {
  clearInterval(this.pollTimer); this.client?.dispose(); this.client = null; this.connected = false; this.errorText = ''; this.recallBlocked = false
  const { presets, structure } = buildPresets(this.label); this.setPresetDefinitions(structure, presets)
  this.assetStatusSupported = null
  this.values = { connection: 'Scollegato', number: '?', last_response: '', last_command: '', last_error: '', last_selected_number: '?', last_block: '?', video_state: 'unknown', video_countdown: '?', video_remaining_seconds: '?', preview_asset_title: '?', program_asset_title: '?', preview_asset_number: '?', program_asset_number: '?' }
  this.setVariableValues(this.values); this.checkAllFeedbacks()
  const host = String(config.host ?? '127.0.0.1').trim(); const port = Number(config.port ?? 51235); const seconds = Number(config.timeout ?? 20)
  if (!host || /[\s\r\n]/.test(host) || !Number.isInteger(port) || port < 1 || port > 65535 || !Number.isFinite(seconds) || seconds < 16 || seconds > 60) {
   this.updateStatus(InstanceStatus.BadConfig, 'Indirizzo, porta o attesa non validi'); return
  }
  const client = new NDIPlayerClient({ host, port, timeout: seconds * 1000 }); this.client = client
  client.on('state', (connected, message) => {
   if (this.client !== client) return
   this.connected = connected
   if (connected) this.assetStatusSupported = null
   this.values.connection = connected ? 'Collegato' : 'Scollegato'
   if (!connected) { this.values.number = '?'; this.values.last_selected_number = '?'; this.values.last_block = '?'; this.resetVideo(); this.resetAssets() }
   this.setVariableValues(this.values); this.checkFeedbacks('connected')
   this.updateStatus(connected ? InstanceStatus.Ok : message === 'Collegamento in corso' ? InstanceStatus.Connecting : InstanceStatus.ConnectionFailure, message)
  })
  client.on('response', response => { if (this.client === client) this.applyResponse(response) })
  client.start()
  this.pollTimer = setInterval(() => {
   if (this.client !== client || !client.connected || client.pending || client.queue.length) return
   const pollVideo = () => { if (this.client === client && client.connected) return client.send('VIDEO_STATUS') }
   if (this.assetStatusSupported === false) { pollVideo()?.catch(() => {}); return }
   client.send('ASSET_STATUS').then(pollVideo).catch(() => {})
  }, 500)
 }
 resetVideo() { this.values.video_state = 'unknown'; this.values.video_countdown = '?'; this.values.video_remaining_seconds = '?'; this.checkFeedbacks('video_playing', 'video_paused', 'video_ended') }
 resetAssets() { this.values.preview_asset_title = '?'; this.values.program_asset_title = '?'; this.values.preview_asset_number = '?'; this.values.program_asset_number = '?' }
 applyResponse({ ok, line, command }) {
  if (command === 'ASSET_STATUS') {
   if (!ok) {
    this.resetAssets()
    if (line === 'ERR Comando sconosciuto o argomenti non validi') this.assetStatusSupported = false
    else this.log('warn', 'Richiesta ASSET_STATUS fallita: ' + line)
   } else {
    const match = /^OK ASSET_STATUS ([A-Za-z0-9+/]+={0,2})$/.exec(line)
    if (!match) {
     this.assetStatusSupported = false
     this.resetAssets()
     this.log('warn', 'Risposta ASSET_STATUS non valida: ' + line)
    } else {
     try {
      const state = JSON.parse(Buffer.from(match[1], 'base64').toString('utf8'))
      const validAsset = asset => asset === null || (asset && typeof asset.title === 'string' && typeof asset.number === 'string')
      if (!state || !validAsset(state.preview) || !Array.isArray(state.program) || !state.program.every(asset => asset && validAsset(asset))) throw new Error('struttura dati non valida')
      this.assetStatusSupported = true
      this.values.preview_asset_title = state.preview?.title ?? ''
      this.values.preview_asset_number = state.preview?.number ?? ''
      this.values.program_asset_title = state.program.map(asset => asset.title).join(' + ')
      this.values.program_asset_number = state.program.length > 1 ? state.program.map(asset => asset.number || '—').join(' + ') : (state.program[0]?.number ?? '')
     } catch (error) {
      this.assetStatusSupported = false
      this.resetAssets()
      this.log('warn', 'Risposta ASSET_STATUS non valida: ' + error.message)
     }
    }
   }
   this.setVariableValues(this.values); return
  }
  if (command === 'VIDEO_STATUS') {
   const match = ok && /^OK VIDEO_STATUS (playing|paused|ended|none|unavailable) (\?|[0-9]+(?:\.[0-9]+)?)$/.exec(line)
   if (!match) this.resetVideo()
   else { this.values.video_state = match[1]; this.values.video_remaining_seconds = match[2]; const seconds = Number(match[2]); this.values.video_countdown = match[2] === '?' ? '?' : (Math.floor(seconds / 3600) ? String(Math.floor(seconds / 3600)).padStart(2, '0') + ':' : '') + String(Math.floor(seconds / 60) % 60).padStart(2, '0') + ':' + (seconds % 60).toFixed(1).padStart(4, '0'); this.checkFeedbacks('video_playing', 'video_paused', 'video_ended') }
   this.setVariableValues(this.values); return
  }
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
 async destroy() { clearInterval(this.pollTimer); this.client?.dispose(); this.client = null; this.connected = false }
}
