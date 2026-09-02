import type {
  ChangeCrewNameHttpResponse,
  CreateCrewHttpResponse,
} from '~/lib/api/generated/types.gen'
import { failure, success, type Result } from '../results.ts'

export type InvalidCrewNameError = Readonly<{
  type: 'invalid-crew-name'
  message: string
}>

export type CrewCreationValidationError = Readonly<{
  type: 'crew-creation-validation-error'
  message: string
}>

export type CrewCreationForbiddenError = Readonly<{
  type: 'crew-creation-forbidden-error'
  message: string
}>

export type CrewCreationUnavailableError = Readonly<{
  type: 'crew-creation-unavailable-error'
  message: string
}>

export type CrewCreationSubmissionError =
  | CrewCreationValidationError
  | CrewCreationForbiddenError
  | CrewCreationUnavailableError

export type CrewCreationSubmissionResult = Result<
  CreateCrewHttpResponse,
  CrewCreationSubmissionError
>

export type CrewNameChangeValidationError = Readonly<{
  type: 'crew-name-change-validation-error'
  message: string
}>

export type CrewNameChangeForbiddenError = Readonly<{
  type: 'crew-name-change-forbidden-error'
  message: string
}>

export type CrewNameChangeUnavailableError = Readonly<{
  type: 'crew-name-change-unavailable-error'
  message: string
}>

export type CrewNameChangeError =
  | CrewNameChangeValidationError
  | CrewNameChangeForbiddenError
  | CrewNameChangeUnavailableError

export type CrewNameChangeResult = Result<
  ChangeCrewNameHttpResponse,
  CrewNameChangeError
>

export function validateCrewName(
  name: string,
): Result<string, InvalidCrewNameError> {
  const normalizedName = name.trim()
  const isValidLength =
    normalizedName.length >= 1 && normalizedName.length <= 100

  if (!isValidLength) {
    return failure({
      type: 'invalid-crew-name',
      message: 'Crew name must contain between 1 and 100 characters.',
    })
  }

  return success(normalizedName)
}
