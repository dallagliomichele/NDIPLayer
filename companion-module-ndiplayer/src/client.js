import net from 'node:net'
import { EventEmitter } from 'node:events'
import { StringDecoder } from 'node:string_decoder'

export function validCommand(command) {
 return /^(NEXT|PREV|NEXT_BLOCK|PREV_BLOCK|GO|CLEAR_FADE|CLEAR_CUT|BACKSPACE|CANCEL|ENTER|SELECT [0-9]{1,9}|DIGIT [0-9])$/.test(command)
}

// One acknowledged command at a time. Reconnection never replays commands.
export class NDIPlayerClient extends EventEmitter {
 constructor({ host, port, timeout = 20000, reconnectDelay = 2000 }) {
  super(); this.host = host; this.port = port; this.timeout = timeout; this.reconnectDelay = reconnectDelay
  this.queue = []; this.pending = null; this.connected = false; this.closed = false
 }
 start() {
  if (this.closed || this.socket) return
  const socket = net.createConnection({ host: this.host, port: this.port })
  this.socket = socket; this.lastFailure = null; this.buffer = ''; this.decoder = new StringDecoder('utf8')
  this.emit('state', false, 'Collegamento in corso')
  socket.setNoDelay(true); socket.setKeepAlive(true, 10000)
  this.connectTimer = setTimeout(() => socket.destroy(new Error('Connessione scaduta')), 5000)
  socket.on('connect', () => {
   if (this.socket !== socket || this.closed) return
   clearTimeout(this.connectTimer); this.connected = true; this.emit('state', true, 'Collegato'); this.pump()
  })
  socket.on('data', data => {
   if (this.socket !== socket || this.closed) return
   this.buffer += this.decoder.write(data)
   if (this.buffer.length > 16384) { socket.destroy(new Error('Risposta TCP troppo lunga')); return }
   let at
   while ((at = this.buffer.indexOf('\n')) !== -1) {
    const line = this.buffer.slice(0, at).replace(/\r$/, ''); this.buffer = this.buffer.slice(at + 1)
    if (line.length === 0) continue
    if (!/^(OK|ERR)( |$)/.test(line) || !this.pending) { socket.destroy(new Error('Risposta TCP inattesa')); return }
    const entry = this.pending; this.pending = null; clearTimeout(this.replyTimer)
    const response = { ok: /^OK( |$)/.test(line), line, command: entry.command }
    this.emit('response', response); entry.resolve(response)
    // Drain only after the complete data chunk has been parsed, preventing late lines
    // in the same packet from being associated with the next command.
    queueMicrotask(() => this.pump())
   }
  })
  socket.on('error', error => { if (this.socket === socket && !this.closed) { this.lastFailure = error; this.emit('state', false, error.message) } })
  socket.on('close', () => {
   if (this.socket !== socket) return
   clearTimeout(this.connectTimer); this.socket = null; this.connected = false
   const reason = this.lastFailure?.message ?? 'Collegamento interrotto'; this.failQueue(new Error(reason + '; comandi non ripetuti'))
   if (!this.closed) { this.emit('state', false, reason); this.reconnectTimer = setTimeout(() => this.start(), this.reconnectDelay) }
  })
 }
 send(command) {
  if (!validCommand(command)) return Promise.reject(new Error('Comando o numero non valido'))
  if (!this.connected || this.closed) return Promise.reject(new Error('NDIPlayer non collegato: comando non inviato'))
  if (this.queue.length + (this.pending ? 1 : 0) >= 32) return Promise.reject(new Error('Coda comandi piena'))
  return new Promise((resolve, reject) => { this.queue.push({ command, resolve, reject }); this.pump() })
 }
 pump() {
  if (this.closed || !this.connected || this.pending || !this.queue.length) return
  this.pending = this.queue.shift(); const socket = this.socket
  this.replyTimer = setTimeout(() => socket.destroy(new Error('Risposta scaduta: verificare esito in NDIPlayer')), this.timeout)
  socket.write(this.pending.command + '\n', 'ascii', error => { if (error) socket.destroy(error) })
 }
 failQueue(error) {
  clearTimeout(this.replyTimer)
  if (this.pending) { this.pending.reject(error); this.pending = null }
  for (const entry of this.queue.splice(0)) entry.reject(error)
 }
 dispose() {
  if (this.closed) return
  this.closed = true; this.connected = false; clearTimeout(this.reconnectTimer); clearTimeout(this.connectTimer)
  const socket = this.socket; this.socket = null; socket?.destroy(); this.failQueue(new Error('Connessione chiusa'))
 }
}
