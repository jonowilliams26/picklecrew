import { failure, success, type Result } from '../results.ts'

export const statisticsResetConfirmationPhrase = 'reset statistics'

export type InvalidStatisticsResetConfirmationError = Readonly<{
  type: 'invalid-statistics-reset-confirmation'
  message: string
}>

type StatisticsResetForbiddenError = Readonly<{
  type: 'statistics-reset-forbidden-error'
  message: string
}>

type StatisticsResetUnavailableError = Readonly<{
  type: 'statistics-reset-unavailable-error'
  message: string
}>

export type StatisticsResetResult = Result<
  void,
  | InvalidStatisticsResetConfirmationError
  | StatisticsResetForbiddenError
  | StatisticsResetUnavailableError
>

export function validateStatisticsResetConfirmation({
  confirmation,
}: {
  confirmation: string
}): Result<void, InvalidStatisticsResetConfirmationError> {
  if (confirmation !== statisticsResetConfirmationPhrase) {
    return failure({
      type: 'invalid-statistics-reset-confirmation',
      message: `Enter ${statisticsResetConfirmationPhrase} exactly to confirm.`,
    })
  }

  return success(undefined)
}
