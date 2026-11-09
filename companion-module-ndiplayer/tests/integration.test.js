import test from 'node:test'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { EventEmitter, once } from 'node:events'
import { createInterface } from 'node:readline'
import { setTimeout as sleep } from 'node:timers/promises'
import NDIPlayerInstance from '../src/main.js'
import { context, waitFor } from './helpers.js'

async function startApp(t) {
 const child = spawn(process.env.NDIPLAYER_TEST_HOST, [], { stdio: ['pipe', 'pipe', 'pipe'], windowsHide: true }); let stderr = '', port
 child.stderr.on('data', data => { stderr += data.toString() }); const events = new EventEmitter()
 createInterface({ input: child.stdout }).on('line', line => { if (line.startsWith('READY ')) port = Number(line.slice(6)); else if (line.startsWith('STATE ')) events.emit('state', JSON.parse(line.slice(6))) })
 t.after(async () => { if (child.exitCode === null) { child.stdin.write('QUIT\n'); await Promise.race([once(child, 'exit'), sleep(3000)]); if (child.exitCode === null) child.kill() } })
 await waitFor(() => port || child.exitCode !== null, 7000); assert.ok(port, stderr || 'Fixture NDIPlayer non avviata')
 async function state() { const result = once(events, 'state', { signal: AbortSignal.timeout(3000) }); child.stdin.write('STATE\n'); return (await result)[0] }
 async function until(predicate) { for (let i = 0; i < 100; i++) { const value = await state(); if (predicate(value)) return value; await sleep(10) } throw Error('Stato NDIPlayer non raggiunto') }
 return { port, state, until }
}
for (const variant of ['source', 'package']) {
 test('modulo ' + variant + ' contro ricevitore e Program reali di NDIPlayer (mittente video simulato)', { skip: !process.env.NDIPLAYER_TEST_HOST }, async t => {
  const app = await startApp(t); const { ctx, data } = context()
  const Type = variant === 'source' ? NDIPlayerInstance : (await import('../pkg/ndiplayer/main.js')).default
  const module = new Type(ctx); t.after(() => module.destroy()); await module.init({ host: '127.0.0.1', port: app.port }); await waitFor(() => module.connected)
  const action = (id, options = {}) => data.actions[id].callback({ options })
  await app.until(s => s.program.includes('A.png'))
  await action('next'); assert.equal((await app.state()).selected, 'B.png'); assert.deepEqual((await app.state()).program, ['A.png'])
  await action('prev'); assert.equal((await app.state()).selected, 'A.png')
  await action('next_block'); assert.equal((await app.state()).block, 'Intervallo')
  await action('prev_block'); assert.equal((await app.state()).block, 'Apertura')
  await action('digit', { digit: '1' }); await action('digit', { digit: '2' }); assert.equal(data.variables.number, '12')
  await action('enter'); assert.equal((await app.state()).selected, 'B.png'); assert.deepEqual((await app.state()).program, ['A.png'])
  await action('go'); await app.until(s => s.program.includes('B.png'))
  await action('select', { number: '999' }); await action('go'); assert.ok(data.variables.last_response.startsWith('ERR')); assert.deepEqual((await app.state()).program, ['B.png'])
  await action('cancel'); await action('next_block'); await action('select', { number: '12' }); assert.equal((await app.state()).selected, 'A.png')
  await action('go'); await app.until(s => s.program.includes('A.png'))
  await action('digit', { digit: '9' }); await action('digit', { digit: '8' }); await action('backspace'); assert.equal(data.variables.number, '9'); await action('cancel'); assert.equal(data.variables.number, '')
  await action('clear_fade'); assert.equal((await app.state()).clearing, true)
  await action('clear_cut'); const clear = await app.until(s => s.program.length === 0); assert.equal(clear.enabled, true); assert.equal(clear.selected, 'A.png')
  assert.equal(data.variables.last_response, 'OK CLEAR_CUT')
 })
}
