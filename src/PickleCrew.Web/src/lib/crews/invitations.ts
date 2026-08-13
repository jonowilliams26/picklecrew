import type {
  CreateCrewHttpResponse,
  SendCrewInvitationHttpResponse,
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
  SendCrewInvitationHttpResponse,
  CrewInvitationSubmissionError
>

export type OnboardingState =
  | Readonly<{ type: 'create-crew' }>
  | Readonly<{
      type: 'invite-friends'
      crew: CreateCrewHttpResponse
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

export function getCrewInvitationOutcomeMessage(
  outcome: SendCrewInvitationHttpResponse,
): string {
  switch (outcome.status) {
    case 'sent':
      return `${outcome.emailAddress} has been invited.`
    case 'already-active-member':
      return `${outcome.emailAddress} is already a crew member.`
    case 'already-pending':
      return `${outcome.emailAddress} already has a pending invitation.`
    case 'failed':
      return outcome.message ?? 'Could not send this invitation. Try again.'
  }
}

export function isCrewInvitationOutcomeTerminal(
  outcome: SendCrewInvitationHttpResponse,
): boolean {
  return outcome.status !== 'failed'
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
