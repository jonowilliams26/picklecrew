import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import { getActiveMemberships } from '~/lib/onboarding/onboarding.functions'
import { decideMembershipRoute } from '~/lib/onboarding/membership-routing'

export const Route = createFileRoute('/_authenticated/_active-crew')({
  loader: async () => {
    const context = await getActiveMemberships()
    const decision = decideMembershipRoute(
      context.memberships,
      context.currentOrganizationId,
    )

    if (decision.destination === '/onboarding') {
      throw redirect({ to: '/onboarding' })
    }

    if (decision.organizationIdToActivate) {
      await switchToOrganization({
        data: { organizationId: decision.organizationIdToActivate },
      })
    }
  },
  component: ActiveCrewLayout,
})

function ActiveCrewLayout() {
  return <Outlet />
}
