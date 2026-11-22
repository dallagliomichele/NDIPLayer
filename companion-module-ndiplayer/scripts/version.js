import { readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { resolve, dirname } from 'node:path'
const dir = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const path = resolve(dir, 'VERSION')
let version = readFileSync(path, 'utf8').trim()
if (!/^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(version)) throw new Error('VERSION deve contenere major.minor.patch')
const increment = process.argv[2]
if (increment) {
 const parts = version.split('.').map(Number); const index = ['major', 'minor', 'patch'].indexOf(increment)
 if (index < 0) throw new Error('Usare major, minor o patch')
 parts[index]++; for (let i = index + 1; i < 3; i++) parts[i] = 0
 version = parts.join('.'); writeFileSync(path, version + '\n')
}
for (const name of ['package.json', 'package-lock.json', 'companion/manifest.json']) {
 const file = resolve(dir, name); const obj = JSON.parse(readFileSync(file, 'utf8')); obj.version = version
 if (name === 'package-lock.json') obj.packages[''].version = version
 writeFileSync(file, JSON.stringify(obj, null, 2) + '\n')
}
console.log('Versione modulo Companion: ' + version)
