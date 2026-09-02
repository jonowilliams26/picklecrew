import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import {
  changeCrewName as changeCrewNameRequest,
  createCrew as createCrewRequest,
  deleteCrew as deleteCrewRequest,
} from '~/lib/api/generated/sdk.gen'
import { getProblemDetailsMessage } from '~/lib/api/problem-details'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewDeletionResult } from '~/lib/crews/crew-deletion'
import type {
  CrewCreationSubmissionResult,
  CrewNameChangeResult,
} from '~/lib/crews/crews'
import { failure, success } from '~/lib/results'

export const changeCrewName = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { name: string }) => data)
  .handler(async ({ context, data }): Promise<CrewNameChangeResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await changeCrewNameRequest({
        client,
        body: { name: data.name },
      })

      if (!result.error) {
        return success(result.data)
      }

      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'crew-name-change-validation-error',
          message: getProblemDetailsMessage({
            problemDetails: result.error,
            fallbackMessage: 'Enter a valid crew name.',
          }),
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-name-change-forbidden-error',
          message: 'You no longer have permission to change this crew name.',
        })
      }

      return failure({
        type: 'crew-name-change-unavailable-error',
        message: 'This crew name could not be changed. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-name-change-unavailable-error',
        message: 'This crew name could not be changed. Try again.',
      })
    }
  })

export const deleteCrew = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { crewName: string }) => data)
  .handler(async ({ context, data }): Promise<CrewDeletionResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await deleteCrewRequest({
        client,
        body: { crewName: data.crewName },
      })

      if (!result.error) {
        return success(undefined)
      }

      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'invalid-crew-deletion-confirmation',
          message: 'Enter the crew name exactly to confirm.',
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-deletion-forbidden-error',
          message: 'You no longer have permission to delete this crew.',
        })
      }

      return failure({
        type: 'crew-deletion-unavailable-error',
        message: 'This crew could not be deleted. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-deletion-unavailable-error',
        message: 'This crew could not be deleted. Try again.',
      })
    }
  })

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
