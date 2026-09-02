import assert from 'node:assert/strict'
import test from 'node:test'
import {
  formatMembershipDuration,
  getCrewLeavingDisabledReason,
  validateCrewLeavingConfirmation,
} from './crew-leaving.ts'

test('accepts the exact crew name as confirmation', () => {
  assert.deepEqual(
    validateCrewLeavingConfirmation({
      confirmation: 'Kitchen Legends',
      crewName: 'Kitchen Legends',
    }),
    { ok: true, value: undefined },
  )
})

test('rejects confirmation with different casing or whitespace', () => {
  assert.equal(
    validateCrewLeavingConfirmation({
      confirmation: 'kitchen legends',
      crewName: 'Kitchen Legends',
    }).ok,
    false,
  )
  assert.equal(
    validateCrewLeavingConfirmation({
      confirmation: ' Kitchen Legends ',
      crewName: 'Kitchen Legends',
    }).ok,
    false,
  )
})

test('explains why a sole administrator cannot leave', () => {
  assert.equal(
    getCrewLeavingDisabledReason({ canLeave: false }),
    'Assign another crew member as an administrator before leaving this crew.',
  )
  assert.equal(getCrewLeavingDisabledReason({ canLeave: true }), undefined)
})

test('formats membership duration using its largest useful unit', () => {
  const now = new Date('2026-03-16T12:00:00.000Z')

  assert.equal(
    formatMembershipDuration({
      joinedAt: '2026-03-16T06:00:00.000Z',
      now,
    }),
    'Less than a day',
  )
  assert.equal(
    formatMembershipDuration({
      joinedAt: '2026-03-15T12:00:00.000Z',
      now,
    }),
    '1 day',
  )
  assert.equal(
    formatMembershipDuration({
      joinedAt: '2026-01-15T12:00:00.000Z',
      now,
    }),
    '2 months',
  )
  assert.equal(
    formatMembershipDuration({
      joinedAt: '2024-03-16T12:00:00.000Z',
      now,
    }),
    '2 years',
  )
})
