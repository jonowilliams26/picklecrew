import { createServerFn } from '@tanstack/react-start'
import { getAuth } from '@workos/authkit-tanstack-react-start'
import { resolveCrewMembership } from './membership-routing'
import { createWorkOsClient } from '~/lib/workos/workos.server'

export const getCrewMembership = createServerFn({ method: 'GET' }).handler(
  async () => {
    const auth = await getAuth()

    if (!auth.user) {
      return resolveCrewMembership({
        isSignedIn: false,
        memberships: [],
      })
    }

    const workos = createWorkOsClient()
    const memberships =
      await workos.userManagement.listOrganizationMemberships({
        userId: auth.user.id,
        statuses: ['active'],
        limit: 100,
      })

    return resolveCrewMembership({
      isSignedIn: true,
      currentOrganizationId: auth.organizationId,
      memberships: memberships.data.map((membership) => ({
        organizationId: membership.organizationId,
        organizationName: membership.organizationName,
        status: membership.status,
        role: membership.role.slug,
      })),
    })
  },
)
