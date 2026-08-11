import { createServerFn } from '@tanstack/react-start'
import { WorkOS } from '@workos-inc/node'
import { getAuth } from '@workos/authkit-tanstack-react-start'
import { createClient } from '~/lib/api/generated/client'
import { createOnboardingCrew } from '~/lib/api/generated/sdk.gen'

export const getActiveMemberships = createServerFn({ method: 'GET' }).handler(
  async () => {
    const auth = await getAuth()
    if (!auth.user) {
      throw new Error('Authentication is required.')
    }

    const apiKey = process.env.WORKOS_API_KEY
    if (!apiKey) {
      throw new Error('WORKOS_API_KEY is not configured.')
    }

    const workos = new WorkOS(apiKey)
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
  },
)

export const createCrew = createServerFn({ method: 'POST' })
  .validator((data: { name: string }) => data)
  .handler(async ({ data }) => {
    const auth = await getAuth()
    if (!auth.user) {
      throw new Error('Authentication is required.')
    }

    const baseUrl = process.env.API_BASE_URL
    if (!baseUrl) {
      throw new Error('API_BASE_URL is not configured.')
    }

    const client = createClient({ baseUrl })
    const result = await createOnboardingCrew({
      client,
      body: { name: data.name },
      headers: { Authorization: `Bearer ${auth.accessToken}` },
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
