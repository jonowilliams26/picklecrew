import assert from 'node:assert/strict'
import test from 'node:test'
import { validateCrewDeletionConfirmation } from './crew-deletion.ts'

test('accepts the exact crew name as deletion confirmation', () => {
  assert.deepEqual(
    validateCrewDeletionConfirmation({
      confirmation: 'Kitchen Legends',
      crewName: 'Kitchen Legends',
    }),
    { ok: true, value: undefined },
  )
})

test('rejects deletion confirmation with different casing or whitespace', () => {
  assert.equal(
    validateCrewDeletionConfirmation({
      confirmation: 'kitchen legends',
      crewName: 'Kitchen Legends',
    }).ok,
    false,
  )
  assert.equal(
    validateCrewDeletionConfirmation({
      confirmation: ' Kitchen Legends ',
      crewName: 'Kitchen Legends',
    }).ok,
    false,
  )
})
