import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import { sendCrewInvitations as sendCrewInvitationsRequest } from '~/lib/api/generated/sdk.gen'
import { getProblemDetailsMessage } from '~/lib/api/problem-details'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { CrewInvitationSubmissionResult } from '~/lib/crews/invitations'
import { failure, success } from '~/lib/results'

export const sendCrewInvitations = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: { emailAddresses: Array<string> }) => data)
  .handler(async ({ context, data }): Promise<CrewInvitationSubmissionResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await sendCrewInvitationsRequest({
        client,
        body: { emailAddresses: data.emailAddresses },
      })

      if (!result.error) {
        return success(result.data)
      }

      const message = getProblemDetailsMessage({
        problemDetails: result.error,
        fallbackMessage: 'Could not send invitation.',
      })
      const status = result.response?.status

      if (status === 400) {
        return failure({
          type: 'crew-invitation-validation-error',
          message,
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'crew-invitation-forbidden-error',
          message: 'You no longer have permission to invite friends to this crew.',
        })
      }

      return failure({
        type: 'crew-invitation-unavailable-error',
        message: 'Invitations are unavailable right now. Try again.',
      })
    } catch {
      return failure({
        type: 'crew-invitation-unavailable-error',
        message: 'Invitations are unavailable right now. Try again.',
      })
    }
  })
