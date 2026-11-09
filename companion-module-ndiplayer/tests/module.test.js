import test from 'node:test'
import assert from 'node:assert/strict'
import { setTimeout as sleep } from 'node:timers/promises'
import { NDIPlayerClient, validCommand } from '../src/client.js'
import NDIPlayerInstance from '../src/main.js'
import { buildActions, buildPresets } from '../src/definitions.js'
import { fixture, waitFor, makeClient, context } from './helpers.js'

test('tutti i comandi validi; nessun ritorno a capo o numero errato', () => {
 for (const command of ['NEXT', 'PREV', 'NEXT_BLOCK', 'PREV_BLOCK', 'GO', 'CLEAR_FADE', 'CLEAR_CUT', 'ENTER', 'BACKSPACE', 'CANCEL', 'SELECT 0012', 'DIGIT 9']) assert.ok(validCommand(command))
 for (const command of ['SELECT -1', 'SELECT 1234567890', 'DIGIT 12', 'GO\nCLEAR_CUT', 'INVALID']) assert.equal(validCommand(command), false)
})
test('invio disconnesso rifiutato senza coda offline', async () => {
 const client = new NDIPlayerClient({ host: '127.0.0.1', port: 1 })
 await assert.rejects(client.send('GO'), /non collegato/); assert.equal(client.queue.length, 0); client.dispose()
})
test('comandi in ordine, ogni risposta associata al suo comando', async t => {
 const f = await fixture(t, (command, socket) => setTimeout(() => socket.write('OK ' + command + '\n'), 10))
 const client = makeClient(t, f.port); await waitFor(() => client.connected)
 const result = await Promise.all(['SELECT 12', 'GO', 'CLEAR_CUT'].map(command => client.send(command)))
 assert.deepEqual(f.commands, ['SELECT 12', 'GO', 'CLEAR_CUT']); assert.deepEqual(result.map(r => r.command), f.commands); assert.ok(result.every(r => r.ok))
})
test('risposta UTF-8 frammentata e CRLF', async t => {
 const reply = Buffer.from('OK BLOCK Aperturà\r\n')
 const f = await fixture(t, (_, socket) => { socket.write(reply.subarray(0, reply.length - 4)); setTimeout(() => socket.write(reply.subarray(reply.length - 4)), 10) })
 const client = makeClient(t, f.port); await waitFor(() => client.connected)
 assert.equal((await client.send('NEXT_BLOCK')).line, 'OK BLOCK Aperturà')
})
test('ERR applicativo mantiene il collegamento', async t => {
 const f = await fixture(t, (_, socket) => socket.write('ERR Numero assente nel blocco\n'))
 const client = makeClient(t, f.port); await waitFor(() => client.connected)
 assert.equal((await client.send('SELECT 999')).ok, false); assert.equal(client.connected, true)
})
test('interruzione annulla coda, nessuna ripetizione dopo riconnessione', async t => {
 let connections = 0
 const f = await fixture(t, (_, socket) => socket.destroy()); f.server.on('connection', () => connections++)
 const client = makeClient(t, f.port); await waitFor(() => client.connected)
 const results = await Promise.allSettled([client.send('GO'), client.send('CLEAR_CUT')])
 assert.ok(results.every(r => r.status === 'rejected')); await waitFor(() => connections >= 2 && client.connected); await sleep(50)
 assert.deepEqual(f.commands, ['GO'])
})
test('timeout senza ritrasmissione', async t => {
 const f = await fixture(t, () => {}); const client = makeClient(t, f.port, { timeout: 40 })
 await waitFor(() => client.connected); await assert.rejects(client.send('GO'), /Risposta scaduta/); await sleep(80); assert.deepEqual(f.commands, ['GO'])
})
test('coda limitata a 32 comandi', async t => {
 const f = await fixture(t, () => {}); const client = makeClient(t, f.port); await waitFor(() => client.connected)
 const all = Promise.allSettled(Array.from({ length: 32 }, () => client.send('DIGIT 1')))
 await assert.rejects(client.send('GO'), /piena/); client.dispose(); assert.ok((await all).every(r => r.status === 'rejected'))
})
test('risposta inattesa rifiutata', async t => {
 const f = await fixture(t, (_, socket) => socket.write('PONG\n'))
 const client = makeClient(t, f.port); await waitFor(() => client.connected); await assert.rejects(client.send('NEXT'), /inattesa/)
})
test('23 preset, 12 azioni, SELECT non esegue GO', async () => {
 const sent = []; const actions = buildActions(async command => { sent.push(command) }); const { presets, structure } = buildPresets('Regia')
 assert.equal(Object.keys(actions).length, 12); assert.equal(Object.keys(presets).length, 23); assert.equal(structure.flatMap(s => s.definitions).length, 23)
 for (const preset of Object.values(presets)) for (const step of preset.steps) for (const action of step.down) assert.ok(actions[action.actionId])
 await actions.select.callback({ options: { number: '0012' } }); assert.deepEqual(sent, ['SELECT 0012'])
 assert.ok(presets.number_display.style.text.includes('$(Regia:number)')); assert.deepEqual(presets.go.steps[0].down, [{ actionId: 'go', options: {} }])
})
test('API Companion reale: definizioni, feedback e variabili da ACK', async t => {
 const f = await fixture(t, (command, socket) => socket.write(command === 'DIGIT 1' ? 'OK NUMBER 1\n' : command === 'SELECT 999' ? 'ERR Numero assente nel blocco\n' : command === 'SELECT 12' ? 'OK SELECT 12\n' : 'OK BLOCK Apertura\n'))
 const { ctx, data } = context(); const instance = new NDIPlayerInstance(ctx); t.after(() => instance.destroy()); await instance.init({ host: '127.0.0.1', port: f.port })
 await waitFor(() => instance.connected); assert.equal(data.variables.connection, 'Collegato'); assert.equal(data.feedbacks.connected.callback(), true)
 await data.actions.digit.callback({ options: { digit: '1' } }); assert.equal(data.variables.number, '1')
 await data.actions.select.callback({ options: { number: '999' } }); assert.equal(data.feedbacks.last_error.callback(), true); assert.equal(data.variables.number, '999')
 await data.actions.select.callback({ options: { number: '12' } }); assert.equal(data.variables.number, ''); assert.equal(data.variables.last_selected_number, '12'); assert.equal(data.variables.last_error, '')
 await data.actions.next_block.callback({ options: {} }); assert.equal(data.variables.last_block, 'Apertura'); assert.equal(data.variables.last_selected_number, '?'); assert.equal(Object.keys(data.presets).length, 23)
})
test('configurazione nuova invalida chiude la vecchia connessione', async t => {
 const f = await fixture(t, (_, socket) => socket.write('OK NUMBER 1\n')); const { ctx, data } = context(); const instance = new NDIPlayerInstance(ctx)
 t.after(() => instance.destroy()); await instance.init({ host: '127.0.0.1', port: f.port }); await waitFor(() => instance.connected)
 await instance.sendCommand('DIGIT 1'); await instance.configUpdated({ host: '', port: 51235 }); assert.equal(instance.connected, false); assert.equal(data.variables.number, '?'); assert.equal(data.statuses.at(-1).status, 'bad_config')
})

test('numero non valido localmente blocca GO sulla vecchia selezione', async t => {
 const f = await fixture(t, (command, socket) => socket.write('OK ' + command + '\n')); const { ctx, data } = context(); const instance = new NDIPlayerInstance(ctx)
 t.after(() => instance.destroy()); await instance.init({ host: '127.0.0.1', port: f.port }); await waitFor(() => instance.connected)
 await data.actions.select.callback({ options: { number: 'abc' } }); await data.actions.go.callback({ options: {} }); assert.deepEqual(f.commands, []); assert.match(data.variables.last_error, /Richiamo non valido/)
 await data.actions.next.callback({ options: {} }); await data.actions.go.callback({ options: {} }); assert.deepEqual(f.commands, ['NEXT', 'GO'])
})
