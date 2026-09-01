import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { removeCrewMember as removeCrewMemberRequest } from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewMemberRemovalResult } from '~/lib/crews/member-removal'
import { failure, success } from '~/lib/results'

export const removeCrewMember = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { userId: string }) => data)
  .handler(async ({ context, data }): Promise<CrewMemberRemovalResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await removeCrewMemberRequest({
        client,
        path: { userId: data.userId },
      })

      if (!result.error) {
        return success(undefined)
      }

      const status = result.response?.status

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-member-removal-forbidden-error',
          message: 'You no longer have permission to remove crew members.',
        })
      }

      if (status === 404) {
        return failure({
          type: 'crew-member-removal-not-found-error',
          message: 'This person is no longer an active crew member.',
        })
      }

      return failure({
        type: 'crew-member-removal-unavailable-error',
        message: 'This crew member could not be removed. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-member-removal-unavailable-error',
        message: 'This crew member could not be removed. Try again.',
      })
    }
  })
