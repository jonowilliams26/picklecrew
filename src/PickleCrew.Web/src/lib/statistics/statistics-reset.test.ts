import assert from 'node:assert/strict'
import test from 'node:test'
import {
  statisticsResetConfirmationPhrase,
  validateStatisticsResetConfirmation,
} from './statistics-reset.ts'

test('accepts the exact statistics reset confirmation phrase', () => {
  assert.deepEqual(
    validateStatisticsResetConfirmation({
      confirmation: statisticsResetConfirmationPhrase,
    }),
    { ok: true, value: undefined },
  )
})

test('rejects confirmation with different casing or whitespace', () => {
  assert.deepEqual(
    validateStatisticsResetConfirmation({
      confirmation: ' Reset statistics ',
    }),
    {
      ok: false,
      error: {
        type: 'invalid-statistics-reset-confirmation',
        message: 'Enter reset statistics exactly to confirm.',
      },
    },
  )
})
