import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { getCrewMembers as getCrewMembersRequest } from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewMembersResult } from '~/lib/crews/crew-members'
import { failure, success } from '~/lib/results'

const pageSize = 10

export const getCrewMembers = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .validator((data: { page: number }) => data)
  .handler(async ({ context, data }): Promise<CrewMembersResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await getCrewMembersRequest({
        client,
        query: {
          Page: data.page,
          PageSize: pageSize,
        },
      })

      if (!result.error) {
        return success(result.data)
      }

      if (result.response?.status === 401 || result.response?.status === 403) {
        return failure({
          type: 'crew-members-forbidden-error',
          message:
            'Your active Crew membership could not be verified. Refresh your Crew and try again.',
        })
      }

      return failure({
        type: 'crew-members-unavailable-error',
        message: 'Crew members could not be loaded. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-members-unavailable-error',
        message: 'Crew members could not be loaded. Try again.',
      })
    }
  })
