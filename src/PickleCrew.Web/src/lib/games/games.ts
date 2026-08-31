import { z } from 'zod'
import type {
  GameFormat,
  GetGameRecordingOptionsHttpResponse,
  PaginatedHttpResponseOfGameHistoryItemHttpResponse,
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

export type GameHistoryForbiddenError = Readonly<{
  type: 'game-history-forbidden-error'
  message: string
}>

export type GameHistoryUnavailableError = Readonly<{
  type: 'game-history-unavailable-error'
  message: string
}>

export type GameHistoryResult = Result<
  PaginatedHttpResponseOfGameHistoryItemHttpResponse,
  GameHistoryForbiddenError | GameHistoryUnavailableError
>

export function createInitialGameFormValues(): GameFormValues {
  return {
    format: 'singles',
    sides: [createSide(), createSide()],
  }
}

export function createRematchGameFormValues({
  values,
}: {
  values: GameFormValues
}): GameFormValues {
  return {
    format: values.format,
    sides: [
      { ...values.sides[0], score: '' },
      { ...values.sides[1], score: '' },
    ],
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
  const result = gameFormSchema.safeParse(values)

  if (result.success) {
    return success(result.data)
  }

  const fieldErrors: Record<string, string> = {}

  for (const issue of result.error.issues) {
    const key = issue.path.map(String).join('.')

    if (key && fieldErrors[key] === undefined) {
      fieldErrors[key] = issue.message
    }
  }

  return failure({
    type: 'invalid-game',
    message:
      fieldErrors.participants ??
      'Correct the highlighted fields and try again.',
    fieldErrors,
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

const invalidScoreMessage = 'Enter a non-negative whole number.'
const scoreSchema = z
  .string()
  .trim()
  .regex(/^\d+$/, invalidScoreMessage)
  .refine((score) => Number.isSafeInteger(Number(score)), invalidScoreMessage)

function createParticipantSchema({ sideIndex }: { sideIndex: number }) {
  return z.discriminatedUnion('kind', [
    z.object({
      kind: z.literal('member'),
      userId: z
        .string()
        .min(1, `Select an active crew member for side ${sideIndex + 1}.`),
      guestName: z.string(),
    }),
    z.object({
      kind: z.literal('guest'),
      userId: z.string(),
      guestName: z
        .string()
        .transform(normalizeGuestName)
        .pipe(
          z
            .string()
            .min(1, `Enter a guest name for side ${sideIndex + 1}.`),
        ),
    }),
  ])
}

function createSideSchema({ sideIndex }: { sideIndex: number }) {
  return z.object({
    score: scoreSchema,
    participants: z.array(createParticipantSchema({ sideIndex })),
  })
}

const gameFormSchema = z
  .discriminatedUnion('format', [
    z.object({
      format: z.literal('singles'),
      sides: z.tuple([
        createSideSchema({ sideIndex: 0 }),
        createSideSchema({ sideIndex: 1 }),
      ]),
    }),
    z.object({
      format: z.literal('doubles'),
      sides: z.tuple([
        createSideSchema({ sideIndex: 0 }),
        createSideSchema({ sideIndex: 1 }),
      ]),
    }),
  ])
  .superRefine((values, context) => {
    const expectedParticipantCount = values.format === 'doubles' ? 2 : 1
    const participantCountLabel = `${expectedParticipantCount} participant${expectedParticipantCount === 1 ? '' : 's'}`

    if (
      values.sides.some(
        (side) => side.participants.length !== expectedParticipantCount,
      )
    ) {
      context.addIssue({
        code: 'custom',
        path: ['participants'],
        message: `Each side must have exactly ${participantCountLabel} for this format.`,
      })
    }

    const memberLocations = new Map<
      string,
      Array<ReadonlyArray<string | number>>
    >()
    const guestLocations = new Map<
      string,
      Array<ReadonlyArray<string | number>>
    >()

    for (const [sideIndex, side] of values.sides.entries()) {
      for (const [participantIndex, participant] of side.participants.entries()) {
        const participantPath = [
          'sides',
          sideIndex,
          'participants',
          participantIndex,
        ] as const

        if (participant.kind === 'member') {
          if (participant.userId) {
            addLocation({
              locations: memberLocations,
              value: participant.userId,
              path: [...participantPath, 'userId'],
            })
          }
          continue
        }

        if (participant.guestName) {
          addLocation({
            locations: guestLocations,
            value: participant.guestName.toLowerCase(),
            path: [...participantPath, 'guestName'],
          })
        }
      }
    }

    if (memberLocations.size === 0) {
      context.addIssue({
        code: 'custom',
        path: ['participants'],
        message:
          'At least one participant must be an active member of the current crew.',
      })
    }

    addDuplicateIssues({
      context,
      locations: memberLocations,
      message: 'A crew member may participate only once in a game.',
    })
    addDuplicateIssues({
      context,
      locations: guestLocations,
      message: 'Guest names must be distinct within a game.',
    })

    const [firstSide, secondSide] = values.sides
    const firstScore = parseScore(firstSide.score)
    const secondScore = parseScore(secondSide.score)

    if (
      firstScore !== undefined &&
      secondScore !== undefined &&
      !isValidFinalScore({ firstScore, secondScore })
    ) {
      const message = 'Enter a completed score played to 11 and won by two.'
      context.addIssue({
        code: 'custom',
        path: ['sides', 0, 'score'],
        message,
      })
      context.addIssue({
        code: 'custom',
        path: ['sides', 1, 'score'],
        message,
      })
    }
  })
  .transform(
    (values): RecordGameHttpRequest => ({
      format: values.format,
      sides: values.sides.map((side) => ({
        score: Number(side.score),
        participants: side.participants.map((participant) =>
          participant.kind === 'member'
            ? { userId: participant.userId }
            : { guestName: participant.guestName },
        ),
      })),
    }),
  )

function addLocation({
  locations,
  value,
  path,
}: {
  locations: Map<string, Array<ReadonlyArray<string | number>>>
  value: string
  path: ReadonlyArray<string | number>
}): void {
  const paths = locations.get(value) ?? []
  paths.push(path)
  locations.set(value, paths)
}

function parseScore(score: string): number | undefined {
  const parsedScore = Number(score)

  return /^\d+$/.test(score) && Number.isSafeInteger(parsedScore)
    ? parsedScore
    : undefined
}

function addDuplicateIssues({
  context,
  locations,
  message,
}: {
  context: z.RefinementCtx
  locations: ReadonlyMap<string, ReadonlyArray<ReadonlyArray<string | number>>>
  message: string
}): void {
  for (const paths of locations.values()) {
    if (paths.length < 2) {
      continue
    }

    for (const path of paths) {
      context.addIssue({ code: 'custom', path: [...path], message })
    }
  }
}
