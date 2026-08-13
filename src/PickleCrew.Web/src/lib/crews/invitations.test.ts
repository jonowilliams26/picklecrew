import assert from 'node:assert/strict'
import test from 'node:test'
import type {
  CreateCrewHttpResponse,
  SendCrewInvitationsHttpResponse,
} from '~/lib/api/generated/types.gen.ts'
import {
  initialOnboardingState,
  transitionToInvitationConfirmation,
  transitionToInvitationStep,
  validateCrewInvitationEmail,
} from './invitations.ts'

const crew: CreateCrewHttpResponse = {
  crewId: '4aeb9f09-8f8e-42a1-a31c-86e3120f2810',
  name: 'Kitchen Legends',
  organizationId: 'org_123',
}

const invitationResult: SendCrewInvitationsHttpResponse = {
  outcomes: [
    {
      emailAddress: 'friend@example.com',
      status: 'Sent',
    },
  ],
}

test('validates and normalizes one invitation email address', () => {
  assert.deepEqual(validateCrewInvitationEmail(' friend@example.com '), {
    ok: true,
    value: 'friend@example.com',
  })
})

test('rejects an invalid invitation email address', () => {
  assert.deepEqual(validateCrewInvitationEmail('not-an-email'), {
    ok: false,
    error: {
      type: 'invalid-crew-invitation-email',
      message: 'Enter a valid email address.',
    },
  })
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

test('transitions from invitations to confirmation while preserving the crew', () => {
  const invitationStep = transitionToInvitationStep({
    state: initialOnboardingState,
    crew,
  })

  assert.deepEqual(
    transitionToInvitationConfirmation({
      state: invitationStep,
      result: invitationResult,
    }),
    {
      type: 'invitation-confirmation',
      crew,
      result: invitationResult,
    },
  )
})
