import assert from 'node:assert/strict'
import test from 'node:test'
import { validateCrewName } from './crews.ts'

test('normalizes a valid crew name', () => {
  assert.deepEqual(validateCrewName('  Kitchen Legends  '), {
    ok: true,
    value: 'Kitchen Legends',
  })
})

test('rejects a blank crew name', () => {
  const result = validateCrewName('   ')

  assert.equal(result.ok, false)
  if (!result.ok) {
    assert.equal(result.error.type, 'invalid-crew-name')
  }
})

test('accepts crew names containing up to 100 trimmed characters', () => {
  assert.equal(validateCrewName(` ${'a'.repeat(100)} `).ok, true)
})

test('rejects crew names containing more than 100 trimmed characters', () => {
  assert.equal(validateCrewName(` ${'a'.repeat(101)} `).ok, false)
})
