import type {
  CreateCrewHttpResponse,
  SendCrewInvitationsHttpResponse,
} from '~/lib/api/generated/types.gen'
import { failure, success, type Result } from '../results.ts'

export type InvalidCrewInvitationEmailError = Readonly<{
  type: 'invalid-crew-invitation-email'
  message: string
}>

export type CrewInvitationValidationError = Readonly<{
  type: 'crew-invitation-validation-error'
  message: string
}>

export type CrewInvitationForbiddenError = Readonly<{
  type: 'crew-invitation-forbidden-error'
  message: string
}>

export type CrewInvitationUnavailableError = Readonly<{
  type: 'crew-invitation-unavailable-error'
  message: string
}>

export type CrewInvitationSubmissionError =
  | CrewInvitationValidationError
  | CrewInvitationForbiddenError
  | CrewInvitationUnavailableError

export type CrewInvitationSubmissionResult = Result<
  SendCrewInvitationsHttpResponse,
  CrewInvitationSubmissionError
>

export type OnboardingState =
  | Readonly<{ type: 'create-crew' }>
  | Readonly<{
      type: 'invite-friends'
      crew: CreateCrewHttpResponse
    }>
  | Readonly<{
      type: 'invitation-confirmation'
      crew: CreateCrewHttpResponse
      result: SendCrewInvitationsHttpResponse
    }>

export const initialOnboardingState: OnboardingState = {
  type: 'create-crew',
}

export function validateCrewInvitationEmail(
  emailAddress: string,
): Result<string, InvalidCrewInvitationEmailError> {
  const normalizedEmailAddress = emailAddress.trim()
  const isValidEmailAddress = /^[^\s@]+@[^\s@]+$/.test(
    normalizedEmailAddress,
  )

  if (!isValidEmailAddress) {
    return failure({
      type: 'invalid-crew-invitation-email',
      message: 'Enter a valid email address.',
    })
  }

  return success(normalizedEmailAddress)
}

export function transitionToInvitationStep({
  state,
  crew,
}: {
  state: OnboardingState
  crew: CreateCrewHttpResponse
}): OnboardingState {
  if (state.type !== 'create-crew') {
    throw new Error('Crew creation can only complete from the create crew step.')
  }

  return { type: 'invite-friends', crew }
}

export function transitionToInvitationConfirmation({
  state,
  result,
}: {
  state: OnboardingState
  result: SendCrewInvitationsHttpResponse
}): OnboardingState {
  if (state.type !== 'invite-friends') {
    throw new Error(
      'Invitation submission can only complete from the invite friends step.',
    )
  }

  return {
    type: 'invitation-confirmation',
    crew: state.crew,
    result,
  }
}
