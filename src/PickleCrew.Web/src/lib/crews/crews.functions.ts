import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { createCrew as createCrewRequest } from '~/lib/api/generated/sdk.gen'
import { getProblemDetailsMessage } from '~/lib/api/problem-details'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewCreationSubmissionResult } from '~/lib/crews/crews'
import { failure, success } from '~/lib/results'

export const createCrew = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { requestId: string; name: string }) => data)
  .handler(async ({ context, data }): Promise<CrewCreationSubmissionResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await createCrewRequest({
        client,
        body: {
          requestId: data.requestId,
          name: data.name,
        },
      })

      if (!result.error) {
        return success(result.data)
      }

      const message = getProblemDetailsMessage({
        problemDetails: result.error,
        fallbackMessage: 'Crew creation failed.',
      })
      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'crew-creation-validation-error',
          message,
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-creation-forbidden-error',
          message: 'You no longer have permission to create a crew. Sign in and try again.',
        })
      }

      return failure({
        type: 'crew-creation-unavailable-error',
        message: 'Crew creation is unavailable right now. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-creation-unavailable-error',
        message: 'Crew creation is unavailable right now. Try again.',
      })
    }
  })
