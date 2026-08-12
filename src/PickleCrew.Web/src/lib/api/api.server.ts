import { createClient } from '~/lib/api/generated/client'

export function createApiClient({
  accessToken,
}: {
  accessToken: string
}) {
  const baseUrl = process.env.API_BASE_URL
  if (!baseUrl) {
    throw new Error('API_BASE_URL is not configured.')
  }

  return createClient({
    baseUrl,
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}
