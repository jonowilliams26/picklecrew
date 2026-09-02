import { createServerFn } from '@tanstack/react-start'
import { getAuth } from '@workos/authkit-tanstack-react-start'
import { resolveCrewMembership } from './membership-routing'
import { createApiClient } from '~/lib/api/api.server'
import {
  getCrewLeavingEligibility as getCrewLeavingEligibilityRequest,
  leaveCrew as leaveCrewRequest,
} from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type {
  CrewLeavingEligibilityResult,
  CrewLeavingResult,
} from '~/lib/crews/crew-leaving'
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
        role: membership.role.slug,
        joinedAt: membership.createdAt,
      })),
    })
  },
)

export const getCrewLeavingEligibility = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .handler(async ({ context }): Promise<CrewLeavingEligibilityResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await getCrewLeavingEligibilityRequest({ client })

      if (!result.error) {
        return success(result.data)
      }

      const status = result.response?.status

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-leaving-forbidden-error',
          message:
            'Your active crew membership could not be verified. Refresh your crew and try again.',
        })
      }

      if (status === 404) {
        return failure({
          type: 'crew-leaving-not-found-error',
          message: 'You are no longer an active member of this crew.',
        })
      }

      return failure({
        type: 'crew-leaving-unavailable-error',
        message: 'Crew leaving availability could not be loaded. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-leaving-unavailable-error',
        message: 'Crew leaving availability could not be loaded. Try again.',
      })
    }
  })

export const leaveCrew = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { crewName: string }) => data)
  .handler(async ({ context, data }): Promise<CrewLeavingResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await leaveCrewRequest({
        client,
        body: { crewName: data.crewName },
      })

      if (!result.error) {
        return success(undefined)
      }

      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'invalid-crew-leaving-confirmation',
          message: 'Enter the crew name exactly to confirm.',
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-leaving-forbidden-error',
          message:
            'Your active crew membership could not be verified. Refresh your crew and try again.',
        })
      }

      if (status === 404) {
        return failure({
          type: 'crew-leaving-not-found-error',
          message: 'You are no longer an active member of this crew.',
        })
      }

      if (status === 409) {
        return failure({
          type: 'crew-leaving-sole-administrator-error',
          message:
            'Assign another crew member as an administrator before leaving this crew.',
        })
      }

      return failure({
        type: 'crew-leaving-unavailable-error',
        message: 'This crew could not be left. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-leaving-unavailable-error',
        message: 'This crew could not be left. Try again.',
      })
    }
  })
