import { WorkOS } from '@workos-inc/node'

export function createWorkOsClient() {
  const apiKey = process.env.WORKOS_API_KEY
  if (!apiKey) {
    throw new Error('WORKOS_API_KEY is not configured.')
  }

  return new WorkOS(apiKey)
}
