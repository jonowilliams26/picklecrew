import type {
  GameFormat,
  GetGameRecordingOptionsHttpResponse,
  RecordGameHttpRequest,
  RecordGameHttpResponse,
} from '~/lib/api/generated/types.gen'
import { failure, success, type Result } from '../results.ts'

export type ParticipantKind = 'member' | 'guest'

export type GameParticipantFormValues = Readonly<{
  kind: ParticipantKind
  userId: string
  guestName: string
}>

export type GameSideFormValues = Readonly<{
  score: string
  participants: ReadonlyArray<GameParticipantFormValues>
}>

export type GameFormValues = Readonly<{
  format: GameFormat
  sides: readonly [GameSideFormValues, GameSideFormValues]
}>

export type InvalidGameError = Readonly<{
  type: 'invalid-game'
  message: string
  fieldErrors: Readonly<Record<string, string>>
}>

export type GameRecordingOptionsForbiddenError = Readonly<{
  type: 'game-recording-options-forbidden-error'
  message: string
}>

export type GameRecordingOptionsUnavailableError = Readonly<{
  type: 'game-recording-options-unavailable-error'
  message: string
}>

export type GameRecordingOptionsResult = Result<
  GetGameRecordingOptionsHttpResponse,
  GameRecordingOptionsForbiddenError | GameRecordingOptionsUnavailableError
>

export type GameRecordingValidationError = Readonly<{
  type: 'game-recording-validation-error'
  message: string
  fieldErrors: Readonly<Record<string, ReadonlyArray<string>>>
}>

export type GameRecordingForbiddenError = Readonly<{
  type: 'game-recording-forbidden-error'
  message: string
}>

export type GameRecordingNetworkError = Readonly<{
  type: 'game-recording-network-error'
  message: string
}>

export type GameRecordingUnavailableError = Readonly<{
  type: 'game-recording-unavailable-error'
  message: string
}>

export type GameRecordingSubmissionResult = Result<
  RecordGameHttpResponse,
  | GameRecordingValidationError
  | GameRecordingForbiddenError
  | GameRecordingNetworkError
  | GameRecordingUnavailableError
>

export function createInitialGameFormValues(): GameFormValues {
  return {
    format: 'singles',
    sides: [createSide(), createSide()],
  }
}

export function changeGameFormat({
  values,
  format,
}: {
  values: GameFormValues
  format: GameFormat
}): GameFormValues {
  const participantCount = format === 'doubles' ? 2 : 1
  const updateSide = (side: GameSideFormValues): GameSideFormValues => ({
    ...side,
    participants: Array.from(
      { length: participantCount },
      (_, participantIndex) =>
        side.participants[participantIndex] ?? createParticipant(),
    ),
  })
  const sides: readonly [GameSideFormValues, GameSideFormValues] = [
    updateSide(values.sides[0]),
    updateSide(values.sides[1]),
  ]

  return { ...values, format, sides }
}

export function changeParticipantKind({
  values,
  sideIndex,
  participantIndex,
  kind,
}: {
  values: GameFormValues
  sideIndex: number
  participantIndex: number
  kind: ParticipantKind
}): GameFormValues {
  return updateParticipant({
    values,
    sideIndex,
    participantIndex,
    participant: { ...createParticipant(), kind },
  })
}

export function changeParticipantValue({
  values,
  sideIndex,
  participantIndex,
  value,
}: {
  values: GameFormValues
  sideIndex: number
  participantIndex: number
  value: string
}): GameFormValues {
  const participant = getParticipant({ values, sideIndex, participantIndex })
  const updatedParticipant =
    participant.kind === 'member'
      ? { ...participant, userId: value }
      : { ...participant, guestName: value }

  return updateParticipant({
    values,
    sideIndex,
    participantIndex,
    participant: updatedParticipant,
  })
}

export function changeSideScore({
  values,
  sideIndex,
  score,
}: {
  values: GameFormValues
  sideIndex: number
  score: string
}): GameFormValues {
  const sides = [...values.sides] as [GameSideFormValues, GameSideFormValues]
  const side = sides[sideIndex]

  if (!side) {
    throw new Error('A Game form must contain exactly two Sides.')
  }

  sides[sideIndex] = { ...side, score }
  return { ...values, sides }
}

export function isValidFinalScore({
  firstScore,
  secondScore,
}: {
  firstScore: number
  secondScore: number
}): boolean {
  if (firstScore < 0 || secondScore < 0 || firstScore === secondScore) {
    return false
  }

  const winningScore = Math.max(firstScore, secondScore)
  const losingScore = Math.min(firstScore, secondScore)

  if (winningScore === 11) {
    return losingScore <= 9
  }

  return losingScore >= 10 && winningScore - losingScore === 2
}

export function createRecordGameRequest(
  values: GameFormValues,
): Result<RecordGameHttpRequest, InvalidGameError> {
  const fieldErrors: Record<string, string> = {}
  const memberLocations = new Map<string, string>()
  const guestLocations = new Map<string, string>()
  const participants: RecordGameHttpRequest['sides'][number]['participants'][] =
    []
  let memberCount = 0

  for (const [sideIndex, side] of values.sides.entries()) {
    const sideParticipants: RecordGameHttpRequest['sides'][number]['participants'] =
      []
    const expectedParticipantCount = values.format === 'doubles' ? 2 : 1

    if (side.participants.length !== expectedParticipantCount) {
      fieldErrors.participants = `Each Side must have exactly ${expectedParticipantCount} participant${expectedParticipantCount === 1 ? '' : 's'} for this format.`
    }

    for (const [participantIndex, participant] of side.participants.entries()) {
      if (participant.kind === 'member') {
        const key = participantFieldName({
          sideIndex,
          participantIndex,
          field: 'userId',
        })

        if (!participant.userId) {
          fieldErrors[key] = `Select an active Crew member for Side ${sideIndex + 1}.`
        } else {
          memberCount++
          addDuplicateError({
            value: participant.userId,
            key,
            locations: memberLocations,
            fieldErrors,
            message: 'A Crew member may participate only once in a Game.',
          })
        }

        sideParticipants.push({ userId: participant.userId })
        continue
      }

      const key = participantFieldName({
        sideIndex,
        participantIndex,
        field: 'guestName',
      })
      const guestName = normalizeGuestName(participant.guestName)

      if (!guestName) {
        fieldErrors[key] = `Enter a Guest name for Side ${sideIndex + 1}.`
      } else {
        addDuplicateError({
          value: guestName.toLowerCase(),
          key,
          locations: guestLocations,
          fieldErrors,
          message: 'Guest names must be distinct within a Game.',
        })
      }

      sideParticipants.push({ guestName })
    }

    participants.push(sideParticipants)
  }

  if (memberCount === 0) {
    fieldErrors.participants =
      'At least one participant must be an active member of the current Crew.'
  }

  const firstScore = parseScore(values.sides[0].score)
  const secondScore = parseScore(values.sides[1].score)

  if (firstScore === undefined) {
    fieldErrors[scoreFieldName({ sideIndex: 0 })] =
      'Enter a non-negative whole number.'
  }

  if (secondScore === undefined) {
    fieldErrors[scoreFieldName({ sideIndex: 1 })] =
      'Enter a non-negative whole number.'
  }

  const scoresArePresent = firstScore !== undefined && secondScore !== undefined

  if (scoresArePresent && !isValidFinalScore({ firstScore, secondScore })) {
    const message = 'Enter a completed score played to 11 and won by two.'
    fieldErrors[scoreFieldName({ sideIndex: 0 })] = message
    fieldErrors[scoreFieldName({ sideIndex: 1 })] = message
  }

  if (Object.keys(fieldErrors).length > 0) {
    return failure({
      type: 'invalid-game',
      message:
        fieldErrors.participants ??
        'Correct the highlighted fields and try again.',
      fieldErrors,
    })
  }

  if (firstScore === undefined || secondScore === undefined) {
    throw new Error('Validated Game scores must be present.')
  }

  return success({
    format: values.format,
    sides: [
      { score: firstScore, participants: participants[0] ?? [] },
      { score: secondScore, participants: participants[1] ?? [] },
    ],
  })
}

export function participantFieldName({
  sideIndex,
  participantIndex,
  field,
}: {
  sideIndex: number
  participantIndex: number
  field: 'userId' | 'guestName'
}): string {
  return `sides.${sideIndex}.participants.${participantIndex}.${field}`
}

export function scoreFieldName({ sideIndex }: { sideIndex: number }): string {
  return `sides.${sideIndex}.score`
}

export function normalizeGuestName(guestName: string): string {
  return guestName.trim()
}

function createSide(): GameSideFormValues {
  return { score: '', participants: [createParticipant()] }
}

function createParticipant(): GameParticipantFormValues {
  return { kind: 'member', userId: '', guestName: '' }
}

function getParticipant({
  values,
  sideIndex,
  participantIndex,
}: {
  values: GameFormValues
  sideIndex: number
  participantIndex: number
}): GameParticipantFormValues {
  const participant = values.sides[sideIndex]?.participants[participantIndex]

  if (!participant) {
    throw new Error('The requested Game participant slot does not exist.')
  }

  return participant
}

function updateParticipant({
  values,
  sideIndex,
  participantIndex,
  participant,
}: {
  values: GameFormValues
  sideIndex: number
  participantIndex: number
  participant: GameParticipantFormValues
}): GameFormValues {
  const sides = [...values.sides] as [GameSideFormValues, GameSideFormValues]
  const side = sides[sideIndex]

  if (!side || !side.participants[participantIndex]) {
    throw new Error('The requested Game participant slot does not exist.')
  }

  const participants = [...side.participants]
  participants[participantIndex] = participant
  sides[sideIndex] = { ...side, participants }

  return { ...values, sides }
}

function addDuplicateError({
  value,
  key,
  locations,
  fieldErrors,
  message,
}: {
  value: string
  key: string
  locations: Map<string, string>
  fieldErrors: Record<string, string>
  message: string
}): void {
  const existingKey = locations.get(value)

  if (existingKey) {
    fieldErrors[existingKey] = message
    fieldErrors[key] = message
    return
  }

  locations.set(value, key)
}

function parseScore(score: string): number | undefined {
  const normalizedScore = score.trim()

  if (!/^\d+$/.test(normalizedScore)) {
    return undefined
  }

  const parsedScore = Number(normalizedScore)

  return Number.isSafeInteger(parsedScore) ? parsedScore : undefined
}
