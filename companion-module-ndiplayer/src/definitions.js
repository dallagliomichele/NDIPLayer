export const simpleCommands = [
 ['prev', 'PREV', 'File precedente · Preview'],
 ['next', 'NEXT', 'File successivo · Preview'],
 ['prev_block', 'PREV_BLOCK', 'Blocco precedente · Preview'],
 ['next_block', 'NEXT_BLOCK', 'Blocco successivo · Preview'],
 ['enter', 'ENTER', 'Conferma numero · seleziona in Preview'],
 ['backspace', 'BACKSPACE', 'Cancella ultima cifra'],
 ['cancel', 'CANCEL', 'Azzera numero composto'],
 ['go', 'GO', 'GO · manda la selezione in Program'],
 ['clear_fade', 'CLEAR_FADE', 'Clear Program · dissolvenza'],
 ['clear_cut', 'CLEAR_CUT', 'Clear Program · cut'],
]
export function buildActions(send) {
 const actions = Object.fromEntries(simpleCommands.map(([id, command, name]) => [id, {
  name, options: [], callback: async () => { await send(command) },
 }]))
 actions.select = {
  name: 'Richiama numero nel blocco attivo · solo Preview',
  options: [{ id: 'number', type: 'textinput', label: 'Numero assegnato (1–9 cifre)', default: '1', regex: '/^[0-9]{1,9}$/', useVariables: true }],
  callback: async event => {
   const number = String(event.options.number ?? '').trim()
   if (!/^[0-9]{1,9}$/.test(number)) { await send('INVALID'); return }
   await send('SELECT ' + number)
  },
 }
 actions.digit = {
  name: 'Tastierino · aggiungi cifra',
  options: [{ id: 'digit', type: 'dropdown', label: 'Cifra', default: '0', choices: Array.from({ length: 10 }, (_, i) => ({ id: String(i), label: String(i) })) }],
  callback: async event => {
   const digit = String(event.options.digit ?? '')
   if (!/^[0-9]$/.test(digit)) { await send('INVALID'); return }
   await send('DIGIT ' + digit)
  },
 }
 return actions
}
export function buildPresets(label) {
 const presets = {}; const groups = { nav: [], play: [], keys: [], info: [] }
 const disconnected = { feedbackId: 'connected', options: {}, isInverted: true, style: { bgcolor: 0x404040, color: 0xcccccc } }
 function button(id, name, text, color, actionId, options = {}, group = 'play') {
  presets[id] = { type: 'simple', name, style: { text, size: 'auto', color: 0xffffff, bgcolor: color },
   steps: [{ down: actionId ? [{ actionId, options }] : [], up: [] }], feedbacks: [disconnected] }
  groups[group].push(id)
 }
 button('prev', 'File precedente', 'PREV', 0x17436b, 'prev', {}, 'nav')
 button('next', 'File successivo', 'NEXT', 0x17436b, 'next', {}, 'nav')
 button('prev_block', 'Blocco precedente', 'BLOCCO\nPREV', 0x50357b, 'prev_block', {}, 'nav')
 button('next_block', 'Blocco successivo', 'BLOCCO\nNEXT', 0x50357b, 'next_block', {}, 'nav')
 button('go', 'Manda in onda la selezione', 'GO', 0x126b36, 'go')
 button('clear_fade', 'Clear in dissolvenza', 'CLEAR\nFADE', 0x9c5400, 'clear_fade')
 button('clear_cut', 'Clear immediato in cut', 'CLEAR\nCUT', 0x9b1d20, 'clear_cut')
 button('select', "Richiamo dedicato: modifica Numero nell'azione", 'SELECT\nNUMERO', 0x17436b, 'select', { number: '1' }, 'nav')
 for (const i of [7, 8, 9, 4, 5, 6, 1, 2, 3, 0]) button('digit_' + i, 'Cifra ' + i, String(i), 0x1e293b, 'digit', { digit: String(i) }, 'keys')
 button('enter', 'Conferma numero in Preview', 'ENTER\nPREVIEW', 0x17436b, 'enter', {}, 'keys')
 button('backspace', 'Cancella ultima cifra', 'BACKSPACE', 0x584124, 'backspace', {}, 'keys')
 button('cancel', 'Azzera numero composto', 'CANCEL', 0x67343c, 'cancel', {}, 'keys')
 button('number_display', 'Numero composto · ultima risposta TCP', 'NUMERO\n$(' + label + ':number)', 0x152737, null, {}, 'info')
 button('connection_display', 'Collegamento TCP', 'TCP\n$(' + label + ':connection)', 0x65302e, null, {}, 'info')
 presets.connection_display.feedbacks = [
  { feedbackId: 'connected', options: {}, style: { bgcolor: 0x126b36, color: 0xffffff } },
  { feedbackId: 'last_error', options: {}, style: { bgcolor: 0x9b1d20, color: 0xffffff } },
 ]
 return { presets, structure: [
  { id: 'navigation', name: 'Navigazione · solo Preview', definitions: groups.nav },
  { id: 'program', name: 'Program', definitions: groups.play },
  { id: 'keypad', name: 'Tastierino numerico', definitions: groups.keys },
  { id: 'information', name: 'Informazioni TCP', definitions: groups.info },
 ] }
}
