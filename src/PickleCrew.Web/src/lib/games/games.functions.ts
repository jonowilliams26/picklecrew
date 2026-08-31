import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import {
  getGameRecordingOptions as getGameRecordingOptionsRequest,
  getGames as getGamesRequest,
  recordGame as recordGameRequest,
} from '~/lib/api/generated/sdk.gen'
import type { RecordGameHttpRequest } from '~/lib/api/generated/types.gen'
import { getProblemDetailsMessage } from '~/lib/api/problem-details'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type {
  GameHistoryResult,
  GameRecordingOptionsResult,
  GameRecordingSubmissionResult,
} from '~/lib/games/games'
import { failure, success } from '~/lib/results'

export const getGames = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .validator((data: { page: number }) => data)
  .handler(async ({ context, data }): Promise<GameHistoryResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await getGamesRequest({
        client,
        query: { Page: data.page },
      })

      if (!result.error) {
        return success(result.data)
      }

      if (result.response?.status === 401 || result.response?.status === 403) {
        return failure({
          type: 'game-history-forbidden-error',
          message:
            'Your active crew membership could not be verified. Refresh your crew and try again.',
        })
      }

      return failure({
        type: 'game-history-unavailable-error',
        message: 'Game history could not be loaded. Try again.',
      })
    } catch {
      return failure({
        type: 'game-history-unavailable-error',
        message: 'Game history could not be loaded. Try again.',
      })
    }
  })

export const getGameRecordingOptions = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .handler(async ({ context }): Promise<GameRecordingOptionsResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await getGameRecordingOptionsRequest({ client })

      if (!result.error) {
        return success(result.data)
      }

      if (result.response?.status === 401 || result.response?.status === 403) {
        return failure({
          type: 'game-recording-options-forbidden-error',
          message:
            'Your active crew membership could not be verified. Refresh your crew and try again.',
        })
      }

      return failure({
        type: 'game-recording-options-unavailable-error',
        message: 'Crew members could not be loaded. Try again.',
      })
    } catch {
      return failure({
        type: 'game-recording-options-unavailable-error',
        message: 'Crew members could not be loaded. Try again.',
      })
    }
  })

export const recordGame = createServerFn({ method: 'POST' })
  .middleware([authenticatedMiddleware])
  .validator((data: RecordGameHttpRequest) => data)
  .handler(async ({ context, data }): Promise<GameRecordingSubmissionResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const result = await recordGameRequest({
        client,
        body: data,
      })

      if (!result.error) {
        return success(result.data)
      }

      const status = result.response?.status
      const message = getProblemDetailsMessage({
        problemDetails: result.error,
        fallbackMessage: 'The game could not be saved.',
      })

      if (status === 400) {
        return failure({
          type: 'game-recording-validation-error',
          message,
          fieldErrors:
            'errors' in result.error && result.error.errors
              ? result.error.errors
              : {},
        })
      }

      if (status === 401 || status === 403) {
        return failure({
          type: 'game-recording-forbidden-error',
          message:
            'Your active crew membership could not be verified. Refresh your crew and try again.',
        })
      }

      return failure({
        type: 'game-recording-unavailable-error',
        message: 'The game could not be saved because the service is unavailable. Try again.',
      })
    } catch {
      return failure({
        type: 'game-recording-network-error',
        message: 'The server could not be reached. Check your connection and try again.',
      })
    }
  })
