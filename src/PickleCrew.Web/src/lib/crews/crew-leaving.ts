import { failure, success, type Result } from '../results.ts'

export type InvalidCrewLeavingConfirmationError = Readonly<{
  type: 'invalid-crew-leaving-confirmation'
  message: string
}>

type CrewLeavingForbiddenError = Readonly<{
  type: 'crew-leaving-forbidden-error'
  message: string
}>

type CrewLeavingNotFoundError = Readonly<{
  type: 'crew-leaving-not-found-error'
  message: string
}>

type CrewLeavingUnavailableError = Readonly<{
  type: 'crew-leaving-unavailable-error'
  message: string
}>

export type CrewLeavingResult = Result<
  void,
  | InvalidCrewLeavingConfirmationError
  | CrewLeavingForbiddenError
  | CrewLeavingNotFoundError
  | CrewLeavingUnavailableError
>

export function validateCrewLeavingConfirmation({
  confirmation,
  crewName,
}: {
  confirmation: string
  crewName: string
}): Result<void, InvalidCrewLeavingConfirmationError> {
  if (confirmation !== crewName) {
    return failure({
      type: 'invalid-crew-leaving-confirmation',
      message: `Enter ${crewName} exactly to confirm.`,
    })
  }

  return success(undefined)
}

export function formatMembershipDuration({
  joinedAt,
  now,
}: {
  joinedAt: string
  now: Date
}): string {
  const joinedDate = new Date(joinedAt)
  const elapsedMilliseconds = Math.max(0, now.getTime() - joinedDate.getTime())
  const elapsedDays = Math.floor(elapsedMilliseconds / (24 * 60 * 60 * 1000))

  if (!Number.isFinite(elapsedDays) || elapsedDays === 0) {
    return 'Less than a day'
  }

  if (elapsedDays < 30) {
    return `${elapsedDays} ${elapsedDays === 1 ? 'day' : 'days'}`
  }

  if (elapsedDays < 365) {
    const elapsedMonths = Math.floor(elapsedDays / 30)
    return `${elapsedMonths} ${elapsedMonths === 1 ? 'month' : 'months'}`
  }

  const elapsedYears = Math.floor(elapsedDays / 365)
  return `${elapsedYears} ${elapsedYears === 1 ? 'year' : 'years'}`
}
