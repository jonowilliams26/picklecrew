import assert from 'node:assert/strict'
import test from 'node:test'
import { decideMembershipRoute } from './membership-routing.ts'

test('routes a user with no active memberships to onboarding', () => {
  assert.deepEqual(decideMembershipRoute([]), {
    destination: '/onboarding',
  })
})

test('routes a user with one active membership to the dashboard and activates it', () => {
  assert.deepEqual(decideMembershipRoute([{ organizationId: 'org_123' }]), {
    destination: '/dashboard',
    organizationIdToActivate: 'org_123',
  })
})

test('keeps an already active membership selected', () => {
  assert.deepEqual(
    decideMembershipRoute([{ organizationId: 'org_123' }], 'org_123'),
    { destination: '/dashboard' },
  )
})
