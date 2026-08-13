import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { createCrew as createCrewRequest } from '~/lib/api/generated/sdk.gen'
import { getProblemDetailsMessage } from '~/lib/api/problem-details'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'

export const createCrew = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { name: string }) => data)
  .handler(async ({ context, data }) => {
    const client = createApiClient({
      accessToken: context.auth.accessToken,
    })
    const result = await createCrewRequest({
      client,
      body: { name: data.name },
    })

    if (result.error) {
      const message = getProblemDetailsMessage({
        problemDetails: result.error,
        fallbackMessage: 'Crew creation failed.',
      })
      throw new Error(message)
    }

    return result.data
  })
