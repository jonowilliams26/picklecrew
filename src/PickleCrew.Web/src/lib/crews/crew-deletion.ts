import { failure, success, type Result } from '../results.ts'

export type InvalidCrewDeletionConfirmationError = Readonly<{
  type: 'invalid-crew-deletion-confirmation'
  message: string
}>

type CrewDeletionForbiddenError = Readonly<{
  type: 'crew-deletion-forbidden-error'
  message: string
}>

type CrewDeletionUnavailableError = Readonly<{
  type: 'crew-deletion-unavailable-error'
  message: string
}>

export type CrewDeletionResult = Result<
  void,
  | InvalidCrewDeletionConfirmationError
  | CrewDeletionForbiddenError
  | CrewDeletionUnavailableError
>

export function validateCrewDeletionConfirmation({
  confirmation,
  crewName,
}: {
  confirmation: string
  crewName: string
}): Result<void, InvalidCrewDeletionConfirmationError> {
  if (confirmation !== crewName) {
    return failure({
      type: 'invalid-crew-deletion-confirmation',
      message: `Enter ${crewName} exactly to confirm.`,
    })
  }

  return success(undefined)
}
