import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { leaveCrew as leaveCrewRequest } from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewLeavingResult } from '~/lib/crews/crew-leaving'
import { failure, success } from '~/lib/results'

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
