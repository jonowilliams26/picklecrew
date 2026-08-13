import assert from 'node:assert/strict'
import test from 'node:test'
import type {
  CreateCrewHttpResponse,
  SendCrewInvitationHttpResponse,
} from '~/lib/api/generated/types.gen.ts'
import {
  getCrewInvitationOutcomeMessage,
  initialOnboardingState,
  isCrewInvitationOutcomeTerminal,
  transitionToInvitationStep,
  validateCrewInvitationEmail,
} from './invitations.ts'

const crew: CreateCrewHttpResponse = {
  crewId: '4aeb9f09-8f8e-42a1-a31c-86e3120f2810',
  name: 'Kitchen Legends',
  organizationId: 'org_123',
}

test('validates and normalizes an invitation email address', () => {
  assert.deepEqual(validateCrewInvitationEmail(' friend@example.com '), {
    ok: true,
    value: 'friend@example.com',
  })
})

test('rejects blank and malformed invitation email addresses', () => {
  assert.equal(validateCrewInvitationEmail(' ').ok, false)
  assert.equal(validateCrewInvitationEmail('not-an-email').ok, false)
})

test('describes each invitation outcome for its recipient', () => {
  const outcomes: SendCrewInvitationHttpResponse[] = [
    { emailAddress: 'sent@example.com', status: 'sent' },
    {
      emailAddress: 'member@example.com',
      status: 'already-active-member',
    },
    {
      emailAddress: 'pending@example.com',
      status: 'already-pending',
    },
    {
      emailAddress: 'failed@example.com',
      status: 'failed',
      message: 'Try again.',
    },
  ]

  assert.deepEqual(outcomes.map(getCrewInvitationOutcomeMessage), [
    'sent@example.com has been invited.',
    'member@example.com is already a crew member.',
    'pending@example.com already has a pending invitation.',
    'Try again.',
  ])
})

test('treats sent, active member, and pending outcomes as terminal', () => {
  assert.equal(
    isCrewInvitationOutcomeTerminal({
      emailAddress: 'sent@example.com',
      status: 'sent',
    }),
    true,
  )
  assert.equal(
    isCrewInvitationOutcomeTerminal({
      emailAddress: 'member@example.com',
      status: 'already-active-member',
    }),
    true,
  )
  assert.equal(
    isCrewInvitationOutcomeTerminal({
      emailAddress: 'pending@example.com',
      status: 'already-pending',
    }),
    true,
  )
  assert.equal(
    isCrewInvitationOutcomeTerminal({
      emailAddress: 'failed@example.com',
      status: 'failed',
    }),
    false,
  )
})

test('transitions from crew creation to the invitation step', () => {
  assert.deepEqual(
    transitionToInvitationStep({
      state: initialOnboardingState,
      crew,
    }),
    {
      type: 'invite-friends',
      crew,
    },
  )
})
