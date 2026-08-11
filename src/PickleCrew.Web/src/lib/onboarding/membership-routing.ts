export type ActiveMembership = {
  organizationId: string
}

export type MembershipRouteDecision =
  | { destination: '/onboarding' }
  | {
      destination: '/dashboard'
      organizationIdToActivate?: string
    }

export function decideMembershipRoute(
  memberships: ReadonlyArray<ActiveMembership>,
  currentOrganizationId?: string,
): MembershipRouteDecision {
  if (memberships.length === 0) {
    return { destination: '/onboarding' }
  }

  const currentMembership = memberships.find(
    (membership) => membership.organizationId === currentOrganizationId,
  )
  const organizationId =
    currentMembership?.organizationId ?? memberships[0]?.organizationId

  if (organizationId === undefined) {
    return { destination: '/onboarding' }
  }

  return organizationId === currentOrganizationId
    ? { destination: '/dashboard' }
    : {
        destination: '/dashboard',
        organizationIdToActivate: organizationId,
      }
}
