export type MembershipStatus = 'active' | 'inactive' | 'pending'

export type CrewMembership = Readonly<{
  organizationId: string
  organizationName: string
  status: MembershipStatus
}>

export type Crew = Readonly<{
  organizationId: string
  name: string
}>

export type MembershipResolution =
  | Readonly<{ type: 'signed-out' }>
  | Readonly<{ type: 'no-active-crew' }>
  | Readonly<{
      type: 'active-crew'
      crew: Crew
    }>
  | Readonly<{
      type: 'switch-required'
      crew: Crew
    }>

export type MembershipRouteDecision =
  | Readonly<{ type: 'render' }>
  | Readonly<{
      type: 'sign-in'
      returnPathname: string
    }>
  | Readonly<{
      type: 'redirect'
      destination: '/onboarding' | '/dashboard'
    }>

export function resolveCrewMembership({
  isSignedIn,
  memberships,
  currentOrganizationId,
}: {
  isSignedIn: boolean
  memberships: ReadonlyArray<CrewMembership>
  currentOrganizationId?: string
}): MembershipResolution {
  if (!isSignedIn) {
    return { type: 'signed-out' }
  }

  const activeMemberships = memberships.filter(
    (membership) => membership.status === 'active',
  )

  if (activeMemberships.length === 0) {
    return { type: 'no-active-crew' }
  }

  const currentMembership = activeMemberships.find(
    (membership) => membership.organizationId === currentOrganizationId,
  )
  const selectedMembership = currentMembership ?? activeMemberships[0]

  if (selectedMembership === undefined) {
    return { type: 'no-active-crew' }
  }

  const crew = {
    organizationId: selectedMembership.organizationId,
    name: selectedMembership.organizationName,
  }

  return selectedMembership.organizationId === currentOrganizationId
    ? { type: 'active-crew', crew }
    : { type: 'switch-required', crew }
}

export function decideCrewMembershipRoute({
  pathname,
  resolution,
}: {
  pathname: string
  resolution: MembershipResolution
}): MembershipRouteDecision {
  if (resolution.type === 'signed-out') {
    return { type: 'sign-in', returnPathname: pathname }
  }

  const isOnboarding =
    pathname === '/onboarding' || pathname === '/onboarding/'

  if (resolution.type === 'no-active-crew') {
    return isOnboarding
      ? { type: 'render' }
      : { type: 'redirect', destination: '/onboarding' }
  }

  return isOnboarding
    ? { type: 'redirect', destination: '/dashboard' }
    : { type: 'render' }
}
