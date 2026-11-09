import assert from 'node:assert/strict'
import net from 'node:net'
import { once } from 'node:events'
import { setTimeout as sleep } from 'node:timers/promises'
import { NDIPlayerClient } from '../src/client.js'
export async function fixture(t, handler) {
 const sockets = new Set(), commands = []
 const server = net.createServer(socket => {
  sockets.add(socket); socket.on('close', () => sockets.delete(socket)); let buffer = ''
  socket.on('data', data => { buffer += data.toString('ascii'); let at
   while ((at = buffer.indexOf('\n')) !== -1) { const command = buffer.slice(0, at); buffer = buffer.slice(at + 1); commands.push(command); handler(command, socket) }
  })
 })
 server.listen(0, '127.0.0.1'); await once(server, 'listening')
 t.after(() => new Promise(resolve => { for (const socket of sockets) socket.destroy(); server.close(resolve) }))
 return { port: server.address().port, commands, server }
}
export async function waitFor(predicate, timeout = 3000) {
 const until = Date.now() + timeout
 while (!predicate() && Date.now() < until) await sleep(5)
 assert.ok(predicate(), 'Condizione non raggiunta entro il timeout')
}
export function makeClient(t, port, options = {}) {
 const client = new NDIPlayerClient({ host: '127.0.0.1', port, reconnectDelay: 30, ...options })
 t.after(() => client.dispose()); client.start(); return client
}
export function context() {
 const data = { variables: {}, statuses: [] }
 const ctx = {
  _isInstanceContext: true, id: 'ndiplayer-test', label: 'NDIPlayer',
  setActionDefinitions: actions => { data.actions = actions }, setFeedbackDefinitions: feedbacks => { data.feedbacks = feedbacks },
  setPresetDefinitions: (structure, presets) => { data.structure = structure; data.presets = presets },
  setVariableDefinitions: variables => { data.definitions = variables }, setVariableValues: values => Object.assign(data.variables, values),
  checkAllFeedbacks: () => {}, checkFeedbacks: () => {}, updateStatus: (status, message) => data.statuses.push({ status, message }),
 }
 return { ctx, data }
}
