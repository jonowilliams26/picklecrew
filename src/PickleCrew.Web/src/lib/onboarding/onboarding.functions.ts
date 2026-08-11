import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { createOnboardingCrew } from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import { createWorkOsClient } from '~/lib/workos/workos.server'

export const getActiveMemberships = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .handler(async ({ context }) => {
    const { auth } = context
    const workos = createWorkOsClient()
    const memberships = await workos.userManagement.listOrganizationMemberships({
      userId: auth.user.id,
      statuses: ['active'],
      limit: 100,
    })

    return {
      currentOrganizationId: auth.organizationId,
      memberships: memberships.data.map((membership) => ({
        organizationId: membership.organizationId,
      })),
    }
  })

export const createCrew = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { name: string }) => data)
  .handler(async ({ context, data }) => {
    const client = createApiClient({
      accessToken: context.auth.accessToken,
    })
    const result = await createOnboardingCrew({
      client,
      body: { name: data.name },
    })

    if (result.error) {
      const validationErrors = 'errors' in result.error ? result.error.errors : undefined
      const message = validationErrors
        ? Object.values(validationErrors).flat().join(' ')
        : result.error.detail ?? result.error.title ?? 'Crew creation failed.'
      throw new Error(message)
    }

    return result.data
  })
