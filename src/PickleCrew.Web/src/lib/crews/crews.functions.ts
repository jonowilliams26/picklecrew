import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { createCrew as createCrewRequest } from '~/lib/api/generated/sdk.gen'
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
      const validationErrors =
        'errors' in result.error ? result.error.errors : undefined
      const message = validationErrors
        ? Object.values(validationErrors).flat().join(' ')
        : result.error.detail ?? result.error.title ?? 'Crew creation failed.'
      throw new Error(message)
    }

    return result.data
  })
