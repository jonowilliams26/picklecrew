import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import { Spinner } from '~/components/ui/spinner'
import { getCrewMembership } from '~/lib/crews/membership-resolution.functions'
import { decideCrewMembershipRoute } from '~/lib/crews/membership-routing'

export const Route = createFileRoute('/_authenticated')({
  loader: async ({ location }) => {
    const resolution = await getCrewMembership()
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

    return { activeCrew }
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
  return (
    <main
      className="flex min-h-screen items-center justify-center gap-3 p-6 text-sm text-ink-soft"
      aria-live="polite"
      aria-busy="true"
    >
      <Spinner />
      <span>Loading…</span>
    </main>
  )
}
