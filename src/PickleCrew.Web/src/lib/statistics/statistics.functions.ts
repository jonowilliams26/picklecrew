import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { resetCrewStatistics as resetCrewStatisticsRequest } from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import { failure, success } from '~/lib/results'
import type { StatisticsResetResult } from '~/lib/statistics/statistics-reset'

export const resetCrewStatistics = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { confirmation: string }) => data)
  .handler(async ({ context, data }): Promise<StatisticsResetResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await resetCrewStatisticsRequest({
        client,
        body: { confirmation: data.confirmation },
      })

      if (!result.error) {
        return success(undefined)
      }

      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'invalid-statistics-reset-confirmation',
          message: 'Enter reset statistics exactly to confirm.',
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'statistics-reset-forbidden-error',
          message: 'You no longer have permission to reset crew statistics.',
        })
      }

      return failure({
        type: 'statistics-reset-unavailable-error',
        message: 'Crew statistics could not be reset. Try again.',
      })
    } catch {
      return failure({
        type: 'statistics-reset-unavailable-error',
        message: 'Crew statistics could not be reset. Try again.',
      })
    }
  })
