import { failure, success, type Result } from '../results.ts'

export type InvalidMemberRemovalConfirmationError = Readonly<{
  type: 'invalid-member-removal-confirmation'
  message: string
}>

type CrewMemberRemovalForbiddenError = Readonly<{
  type: 'crew-member-removal-forbidden-error'
  message: string
}>

type CrewMemberRemovalNotFoundError = Readonly<{
  type: 'crew-member-removal-not-found-error'
  message: string
}>

type CrewMemberRemovalUnavailableError = Readonly<{
  type: 'crew-member-removal-unavailable-error'
  message: string
}>

export type CrewMemberRemovalResult = Result<
  void,
  | CrewMemberRemovalForbiddenError
  | CrewMemberRemovalNotFoundError
  | CrewMemberRemovalUnavailableError
>

export function validateMemberRemovalConfirmation({
  confirmation,
  displayName,
}: {
  confirmation: string
  displayName: string
}): Result<void, InvalidMemberRemovalConfirmationError> {
  if (confirmation !== displayName) {
    return failure({
      type: 'invalid-member-removal-confirmation',
      message: `Enter ${displayName} exactly to confirm.`,
    })
  }

  return success(undefined)
}
