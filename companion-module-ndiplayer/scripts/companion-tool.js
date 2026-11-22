import { copyFileSync, unlinkSync, constants } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const moduleDir = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const operation = process.argv[2]
if (!['build', 'check'].includes(operation)) throw new Error('Expected build or check')
const help = resolve(moduleDir, 'companion/HELP.md')
const source = resolve(moduleDir, '../docs/companion-module-ndiplayer/companion/HELP.md')
copyFileSync(source, help, constants.COPYFILE_EXCL)
try {
  const tool = resolve(moduleDir, `node_modules/@companion-module/tools/dist/scripts/${operation}-connection.js`)
  const result = spawnSync(process.execPath, [tool, ...process.argv.slice(3)], { cwd: moduleDir, stdio: 'inherit' })
  if (result.error) throw result.error
  process.exitCode = result.status ?? 1
} finally {
  unlinkSync(help)
}
