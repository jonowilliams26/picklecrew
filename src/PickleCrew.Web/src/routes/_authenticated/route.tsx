import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import {
  getAuth,
  switchToOrganization,
  type NoUserInfo,
  type UserInfo,
} from '@workos/authkit-tanstack-react-start'
import {
  AuthKitProvider,
  type AuthKitProviderProps,
} from '@workos/authkit-tanstack-react-start/client'
import { getCrewMembership } from '~/lib/crews/membership.functions'
import { decideCrewMembershipRoute } from '~/lib/crews/membership-routing'

function createInitialAuth(
  auth: UserInfo | NoUserInfo,
): NonNullable<AuthKitProviderProps['initialAuth']> {
  if (!auth.user) {
    return { user: null }
  }

  return {
    user: auth.user,
    sessionId: auth.sessionId,
    organizationId: auth.organizationId,
    role: auth.role,
    roles: auth.roles,
    permissions: auth.permissions,
    entitlements: auth.entitlements,
    featureFlags: auth.featureFlags,
    impersonator: auth.impersonator,
  }
}

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

    const activeAuth =
      resolution.type === 'switch-required'
        ? await switchToOrganization({
            data: { organizationId: resolution.crew.organizationId },
          })
        : auth

    if (decision.type === 'redirect') {
      throw redirect({ to: decision.destination, replace: true })
    }

    const activeCrew =
      resolution.type === 'active-crew' ||
      resolution.type === 'switch-required'
        ? resolution.crew
        : undefined

    return {
      activeCrew,
      initialAuth: createInitialAuth(activeAuth),
    }
  },
  shouldReload: false,
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  const { initialAuth } = Route.useLoaderData()

  return (
    <AuthKitProvider initialAuth={initialAuth}>
      <Outlet />
    </AuthKitProvider>
  )
}
