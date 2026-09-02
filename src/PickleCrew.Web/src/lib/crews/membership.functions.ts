import { createServerFn } from '@tanstack/react-start'
import { getAuth } from '@workos/authkit-tanstack-react-start'
import { createApiClient } from '~/lib/api/api.server'
import { changeCrewMemberRole as changeCrewMemberRoleRequest } from '~/lib/api/generated/sdk.gen'
import type { CrewMemberRole } from '~/lib/api/generated/types.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewMemberRoleChangeResult } from '~/lib/crews/member-role-change'
import { resolveCrewMembership } from '~/lib/crews/membership-routing'
import { failure, success } from '~/lib/results'
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
    const membershipPages =
      await workos.userManagement.listOrganizationMemberships({
        userId: auth.user.id,
        statuses: ['active'],
      })
    const memberships = await membershipPages.autoPagination()

    return resolveCrewMembership({
      isSignedIn: true,
      currentOrganizationId: auth.organizationId,
      memberships: memberships.map((membership) => ({
        organizationId: membership.organizationId,
        organizationName: membership.organizationName,
        status: membership.status,
      })),
    })
  },
)

export const changeCrewMemberRole = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { userId: string; role: CrewMemberRole }) => data)
  .handler(async ({ context, data }): Promise<CrewMemberRoleChangeResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await changeCrewMemberRoleRequest({
        client,
        path: { userId: data.userId },
        body: { role: data.role },
      })

      if (!result.error) {
        return success(undefined)
      }

      const status = result.response?.status

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-member-role-change-forbidden-error',
          message: 'You no longer have permission to change crew member roles.',
        })
      }

      if (status === 404) {
        return failure({
          type: 'crew-member-role-change-not-found-error',
          message: 'This person is no longer an active crew member.',
        })
      }

      return failure({
        type: 'crew-member-role-change-unavailable-error',
        message: 'This crew member role could not be changed. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-member-role-change-unavailable-error',
        message: 'This crew member role could not be changed. Try again.',
      })
    }
  })
