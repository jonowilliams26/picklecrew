import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import {
  getAuth,
  switchToOrganization,
} from '@workos/authkit-tanstack-react-start'
import { LoadingScreen } from '~/components/loading-screen'
import { getCrewMembership } from '~/lib/crews/membership.functions'
import { decideCrewMembershipRoute } from '~/lib/crews/membership-routing'

export const Route = createFileRoute('/_authenticated')({
  loader: async ({ location }) => {
    const [auth, resolution] = await Promise.all([
      getAuth(),
      getCrewMembership(),
    ])
    const decision = decideCrewMembershipRoute({
      pathname: location.pathname,
      resolution,
    })

    if (decision.type === 'sign-in') {
      throw redirect({
        href: `/api/auth/sign-in?returnPathname=${encodeURIComponent(decision.returnPathname)}`,
      })
    }

    if (resolution.type === 'switch-required') {
      await switchToOrganization({
        data: { organizationId: resolution.crew.organizationId },
      })
    }

    if (decision.type === 'redirect') {
      throw redirect({ to: decision.destination, replace: true })
    }

    const activeCrew =
      resolution.type === 'active-crew' ||
      resolution.type === 'switch-required'
        ? resolution.crew
        : undefined

    return { activeCrew, user: auth.user ?? undefined }
  },
  shouldReload: true,
  pendingComponent: MembershipPending,
  pendingMs: 0,
  pendingMinMs: 200,
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  return <Outlet />
}

function MembershipPending() {
  return <LoadingScreen />
}
