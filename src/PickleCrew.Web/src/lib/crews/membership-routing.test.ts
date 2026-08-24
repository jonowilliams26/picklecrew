import assert from 'node:assert/strict'
import test from 'node:test'
import {
  decideCrewMembershipRoute,
  resolveCrewMembership,
  type CrewMembership,
  type MembershipResolution,
} from './membership-routing.ts'

const firstMembership: CrewMembership = {
  organizationId: 'org_first',
  organizationName: 'First Crew',
  status: 'active',
  role: 'admin',
}

const secondMembership: CrewMembership = {
  organizationId: 'org_second',
  organizationName: 'Second Crew',
  status: 'active',
  role: 'member',
}

function resolve({
  isSignedIn = true,
  memberships = [],
  currentOrganizationId,
}: {
  isSignedIn?: boolean
  memberships?: ReadonlyArray<CrewMembership>
  currentOrganizationId?: string
} = {}) {
  return resolveCrewMembership({
    isSignedIn,
    memberships,
    currentOrganizationId,
  })
}

test('resolves a signed-out user before considering memberships', () => {
  assert.deepEqual(
    resolve({ isSignedIn: false, memberships: [firstMembership] }),
    { type: 'signed-out' },
  )
})

test('resolves no active crew when memberships are empty', () => {
  assert.deepEqual(resolve(), { type: 'no-active-crew' })
})

test('filters inactive and pending memberships', () => {
  assert.deepEqual(
    resolve({
      memberships: [
        { ...firstMembership, status: 'inactive' },
        { ...secondMembership, status: 'pending' },
      ],
    }),
    { type: 'no-active-crew' },
  )
})

test('preserves the current organization when its membership is active', () => {
  assert.deepEqual(
    resolve({
      memberships: [firstMembership, secondMembership],
      currentOrganizationId: 'org_second',
    }),
    {
      type: 'active-crew',
      crew: {
        organizationId: 'org_second',
        name: 'Second Crew',
        role: 'member',
      },
    },
  )
})

test('selects the first active membership when the current organization is missing', () => {
  assert.deepEqual(
    resolve({ memberships: [firstMembership, secondMembership] }),
    {
      type: 'switch-required',
      crew: {
        organizationId: 'org_first',
        name: 'First Crew',
        role: 'admin',
      },
    },
  )
})

test('selects the first active membership when the current organization is inactive', () => {
  assert.deepEqual(
    resolve({
      memberships: [
        firstMembership,
        { ...secondMembership, status: 'inactive' },
      ],
      currentOrganizationId: 'org_second',
    }),
    {
      type: 'switch-required',
      crew: {
        organizationId: 'org_first',
        name: 'First Crew',
        role: 'admin',
      },
    },
  )
})

const signedOut: MembershipResolution = { type: 'signed-out' }
const noActiveCrew: MembershipResolution = { type: 'no-active-crew' }
const activeCrew: MembershipResolution = {
  type: 'active-crew',
  crew: { organizationId: 'org_first', name: 'First Crew', role: 'admin' },
}
const switchRequired: MembershipResolution = {
  type: 'switch-required',
  crew: { organizationId: 'org_first', name: 'First Crew', role: 'admin' },
}

test('routes signed-out protected requests to sign-in', () => {
  assert.deepEqual(
    decideCrewMembershipRoute({ pathname: '/dashboard', resolution: signedOut }),
    { type: 'sign-in', returnPathname: '/dashboard' },
  )
})

test('renders onboarding only when there is no active crew', () => {
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/onboarding',
      resolution: noActiveCrew,
    }),
    { type: 'render' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/onboarding/',
      resolution: noActiveCrew,
    }),
    { type: 'render' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/dashboard',
      resolution: noActiveCrew,
    }),
    { type: 'redirect', destination: '/onboarding' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/future-protected-route',
      resolution: noActiveCrew,
    }),
    { type: 'redirect', destination: '/onboarding' },
  )
})

test('renders protected content for an active crew', () => {
  assert.deepEqual(
    decideCrewMembershipRoute({ pathname: '/dashboard', resolution: activeCrew }),
    { type: 'render' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/dashboard',
      resolution: switchRequired,
    }),
    { type: 'render' },
  )
})

test('prevents an active member from reopening onboarding', () => {
  assert.deepEqual(
    decideCrewMembershipRoute({ pathname: '/onboarding', resolution: activeCrew }),
    { type: 'redirect', destination: '/dashboard' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({ pathname: '/onboarding/', resolution: activeCrew }),
    { type: 'redirect', destination: '/dashboard' },
  )
  assert.deepEqual(
    decideCrewMembershipRoute({
      pathname: '/onboarding',
      resolution: switchRequired,
    }),
    { type: 'redirect', destination: '/dashboard' },
  )
})
