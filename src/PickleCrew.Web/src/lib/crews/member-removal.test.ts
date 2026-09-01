import assert from 'node:assert/strict'
import test from 'node:test'
import { validateMemberRemovalConfirmation } from './member-removal.ts'

test('accepts the member display name exactly', () => {
  assert.deepEqual(
    validateMemberRemovalConfirmation({
      confirmation: 'Amy Dink',
      displayName: 'Amy Dink',
    }),
    { ok: true, value: undefined },
  )
})

test('rejects a confirmation with different casing or whitespace', () => {
  assert.deepEqual(
    validateMemberRemovalConfirmation({
      confirmation: ' amy dink ',
      displayName: 'Amy Dink',
    }),
    {
      ok: false,
      error: {
        type: 'invalid-member-removal-confirmation',
        message: 'Enter Amy Dink exactly to confirm.',
      },
    },
  )
})
