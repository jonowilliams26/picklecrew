import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import {
  Check,
  CircleAlertIcon,
  RotateCcw,
  Save,
  UserRound,
  UserRoundCheck,
  UserRoundPlus,
  UsersRound,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from '~/components/ui/field'
import { Input } from '~/components/ui/input'
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '~/components/ui/select'
import {
  ToggleGroup,
  ToggleGroupItem,
} from '~/components/ui/toggle-group'
import type {
  GameFormat,
  GameRecordingMemberHttpResponse,
  GetGameRecordingOptionsHttpResponse,
  RecordedGameParticipantHttpResponse,
  RecordGameHttpResponse,
} from '~/lib/api/generated/types.gen'
import {
  getGameRecordingOptions,
  recordGame,
} from '~/lib/games/games.functions'
import {
  changeGameFormat,
  changeParticipantKind,
  changeParticipantValue,
  changeSideScore,
  createInitialGameFormValues,
  createRecordGameRequest,
  participantFieldName,
  scoreFieldName,
  type GameFormValues,
  type GameParticipantFormValues,
  type ParticipantKind,
} from '~/lib/games/games'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/record-game',
)({
  loader: () => getGameRecordingOptions(),
  component: RecordGamePage,
})

function RecordGamePage() {
  const optionsResult = Route.useLoaderData()
  const router = useRouter()

  if (!optionsResult.ok) {
    return (
      <main className="flex flex-1 items-start justify-center p-4 md:p-10">
        <Card className="w-full max-w-2xl border-2 border-border shadow-[6px_7px_0_var(--ink)]">
          <CardHeader>
            <CardTitle>Record a Game</CardTitle>
            <CardDescription>
              Active Crew membership is required before a Game can be recorded.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Alert variant="destructive">
              <CircleAlertIcon />
              <AlertTitle>Recording unavailable</AlertTitle>
              <AlertDescription>{optionsResult.error.message}</AlertDescription>
            </Alert>
          </CardContent>
          <CardFooter>
            <Button
              type="button"
              onClick={() => void router.invalidate({ forcePending: true })}
            >
              Try again
            </Button>
          </CardFooter>
        </Card>
      </main>
    )
  }

  return <GameRecordingForm options={optionsResult.value} />
}

function GameRecordingForm({
  options,
}: {
  options: GetGameRecordingOptionsHttpResponse
}) {
  const recordGameServerFn = useServerFn(recordGame)
  const [values, setValues] = useState<GameFormValues>(() =>
    createInitialGameFormValues(),
  )
  const [fieldErrors, setFieldErrors] = useState<
    Readonly<Record<string, string>>
  >({})
  const [submissionError, setSubmissionError] = useState<string>()
  const [savedGame, setSavedGame] = useState<RecordGameHttpResponse>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const memberByUserId = useMemo(
    () => new Map(options.members.map((member) => [member.userId, member])),
    [options.members],
  )
  const selectedUserIds = useMemo(
    () =>
      new Set(
        values.sides.flatMap((side) =>
          side.participants
            .filter((participant) => participant.kind === 'member')
            .map((participant) => participant.userId)
            .filter(Boolean),
        ),
      ),
    [values.sides],
  )

  const clearErrors = ({ prefixes }: { prefixes: ReadonlyArray<string> }) => {
    setFieldErrors((currentErrors) => {
      const nextErrors = Object.fromEntries(
        Object.entries(currentErrors).filter(
          ([key]) => !prefixes.some((prefix) => key.startsWith(prefix)),
        ),
      )
      return nextErrors
    })
    setSubmissionError(undefined)
  }

  const updateFormat = ({ format }: { format: GameFormat }) => {
    setValues((currentValues) =>
      changeGameFormat({ values: currentValues, format }),
    )
    clearErrors({ prefixes: ['sides.', 'participants'] })
  }

  const updateParticipantKind = ({
    sideIndex,
    participantIndex,
    kind,
  }: {
    sideIndex: number
    participantIndex: number
    kind: ParticipantKind
  }) => {
    setValues((currentValues) =>
      changeParticipantKind({
        values: currentValues,
        sideIndex,
        participantIndex,
        kind,
      }),
    )
    clearErrors({
      prefixes: [
        `sides.${sideIndex}.participants.${participantIndex}`,
        'participants',
      ],
    })
  }

  const updateParticipantValue = ({
    sideIndex,
    participantIndex,
    value,
  }: {
    sideIndex: number
    participantIndex: number
    value: string
  }) => {
    setValues((currentValues) =>
      changeParticipantValue({
        values: currentValues,
        sideIndex,
        participantIndex,
        value,
      }),
    )
    clearErrors({
      prefixes: [
        `sides.${sideIndex}.participants.${participantIndex}`,
        'participants',
      ],
    })
  }

  const updateScore = ({
    sideIndex,
    score,
  }: {
    sideIndex: number
    score: string
  }) => {
    setValues((currentValues) =>
      changeSideScore({ values: currentValues, sideIndex, score }),
    )
    clearErrors({ prefixes: [scoreFieldName({ sideIndex })] })
  }

  const submit = async () => {
    setSubmissionError(undefined)
    const request = createRecordGameRequest(values)

    if (!request.ok) {
      setFieldErrors(request.error.fieldErrors)
      setSubmissionError(request.error.message)
      return
    }

    setFieldErrors({})
    setIsSubmitting(true)

    try {
      const result = await recordGameServerFn({ data: request.value })

      if (!result.ok) {
        setSubmissionError(result.error.message)

        if (result.error.type === 'game-recording-validation-error') {
          setFieldErrors(
            mapServerFieldErrors({
              serverErrors: result.error.fieldErrors,
              values,
            }),
          )
        }

        return
      }

      setSavedGame(result.value)
    } catch {
      setSubmissionError(
        'The server could not be reached. Check your connection and try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  const reset = () => {
    setValues(createInitialGameFormValues())
    setFieldErrors({})
    setSubmissionError(undefined)
    setSavedGame(undefined)
  }

  if (savedGame) {
    return (
      <GameSavedConfirmation
        game={savedGame}
        memberByUserId={memberByUserId}
        onRecordAnother={reset}
      />
    )
  }

  return (
    <main className="flex flex-1 items-start justify-center p-4 pb-24 md:p-10">
      <Card className="w-full max-w-3xl border-2 border-border shadow-[6px_7px_0_var(--ink)]">
        <form
          className="flex flex-col gap-6"
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            void submit()
          }}
        >
          <CardHeader>
            <CardTitle className="text-2xl font-black tracking-[-.04em] md:text-3xl">
              Record a Game
            </CardTitle>
            <CardDescription>Enter the completed Game result.</CardDescription>
          </CardHeader>

          <CardContent className="flex flex-col gap-6">
            <FieldSet disabled={isSubmitting}>
              <FieldLegend>Game format</FieldLegend>
              <Field
                data-invalid={fieldErrors.participants !== undefined || undefined}
              >
                <ToggleGroup
                  className="grid w-full grid-cols-2"
                  variant="outline"
                  size="lg"
                  value={[values.format]}
                  disabled={isSubmitting}
                  aria-label="Game format"
                  onValueChange={(formats) => {
                    const format = formats[0]

                    if (format === 'singles' || format === 'doubles') {
                      updateFormat({ format })
                    }
                  }}
                >
                  <ToggleGroupItem className="w-full" value="singles">
                    <UserRound data-icon="inline-start" />
                    Singles
                  </ToggleGroupItem>
                  <ToggleGroupItem className="w-full" value="doubles">
                    <UsersRound data-icon="inline-start" />
                    Doubles
                  </ToggleGroupItem>
                </ToggleGroup>
                <FieldError>{fieldErrors.participants}</FieldError>
              </Field>
            </FieldSet>

            <FieldGroup>
              {values.sides.map((side, sideIndex) => (
                <GameSideFields
                  key={sideIndex}
                  sideIndex={sideIndex}
                  side={side}
                  members={options.members}
                  selectedUserIds={selectedUserIds}
                  fieldErrors={fieldErrors}
                  isSubmitting={isSubmitting}
                  onParticipantKindChange={updateParticipantKind}
                  onParticipantValueChange={updateParticipantValue}
                  onScoreChange={updateScore}
                />
              ))}
            </FieldGroup>

            {submissionError ? (
              <Alert variant="destructive" aria-live="assertive">
                <CircleAlertIcon />
                <AlertTitle>Game not saved</AlertTitle>
                <AlertDescription>{submissionError}</AlertDescription>
              </Alert>
            ) : null}
          </CardContent>

          <CardFooter className="flex-col items-stretch gap-3">
            <Button
              className="w-full"
              variant="crew"
              size="xl"
              type="submit"
              disabled={isSubmitting}
            >
              Save game
              <Save data-icon="inline-end" />
            </Button>
          </CardFooter>
        </form>
      </Card>
    </main>
  )
}

function GameSideFields({
  sideIndex,
  side,
  members,
  selectedUserIds,
  fieldErrors,
  isSubmitting,
  onParticipantKindChange,
  onParticipantValueChange,
  onScoreChange,
}: {
  sideIndex: number
  side: GameFormValues['sides'][number]
  members: ReadonlyArray<GameRecordingMemberHttpResponse>
  selectedUserIds: ReadonlySet<string>
  fieldErrors: Readonly<Record<string, string>>
  isSubmitting: boolean
  onParticipantKindChange: (change: {
    sideIndex: number
    participantIndex: number
    kind: ParticipantKind
  }) => void
  onParticipantValueChange: (change: {
    sideIndex: number
    participantIndex: number
    value: string
  }) => void
  onScoreChange: (change: { sideIndex: number; score: string }) => void
}) {
  const sideNumber = sideIndex + 1
  const scoreKey = scoreFieldName({ sideIndex })
  const scoreInputId = `side-${sideNumber}-score`

  return (
    <FieldSet
      className="rounded-xl border-2 border-border p-4 md:p-5"
      disabled={isSubmitting}
    >
      <FieldLegend className="font-heading text-lg font-extrabold">
        Side {sideNumber}
      </FieldLegend>
      <FieldGroup className="gap-5">
        {side.participants.map((participant, participantIndex) => (
          <ParticipantFields
            key={participantIndex}
            sideIndex={sideIndex}
            participantIndex={participantIndex}
            participant={participant}
            members={members}
            selectedUserIds={selectedUserIds}
            fieldErrors={fieldErrors}
            isSubmitting={isSubmitting}
            onKindChange={onParticipantKindChange}
            onValueChange={onParticipantValueChange}
          />
        ))}

        <Field
          data-invalid={fieldErrors[scoreKey] !== undefined || undefined}
          data-disabled={isSubmitting || undefined}
        >
          <FieldLabel htmlFor={scoreInputId}>Final score</FieldLabel>
          <Input
            id={scoreInputId}
            name={scoreKey}
            type="number"
            min={0}
            step={1}
            inputMode="numeric"
            value={side.score}
            placeholder="0"
            onChange={(event) =>
              onScoreChange({ sideIndex, score: event.target.value })
            }
            aria-invalid={fieldErrors[scoreKey] !== undefined || undefined}
            disabled={isSubmitting}
          />
          <FieldError>{fieldErrors[scoreKey]}</FieldError>
        </Field>
      </FieldGroup>
    </FieldSet>
  )
}

function ParticipantFields({
  sideIndex,
  participantIndex,
  participant,
  members,
  selectedUserIds,
  fieldErrors,
  isSubmitting,
  onKindChange,
  onValueChange,
}: {
  sideIndex: number
  participantIndex: number
  participant: GameParticipantFormValues
  members: ReadonlyArray<GameRecordingMemberHttpResponse>
  selectedUserIds: ReadonlySet<string>
  fieldErrors: Readonly<Record<string, string>>
  isSubmitting: boolean
  onKindChange: (change: {
    sideIndex: number
    participantIndex: number
    kind: ParticipantKind
  }) => void
  onValueChange: (change: {
    sideIndex: number
    participantIndex: number
    value: string
  }) => void
}) {
  const participantNumber = participantIndex + 1
  const field = participant.kind === 'member' ? 'userId' : 'guestName'
  const fieldKey = participantFieldName({ sideIndex, participantIndex, field })
  const controlId = `side-${sideIndex + 1}-participant-${participantNumber}`
  const selectItems = members.map((member) => ({
    label: member.displayName,
    value: member.userId,
  }))

  return (
    <FieldSet>
      <FieldLegend variant="label">Participant {participantNumber}</FieldLegend>
      <FieldGroup className="gap-3">
        <ToggleGroup
          className="grid w-full grid-cols-2"
          variant="outline"
          value={[participant.kind]}
          disabled={isSubmitting}
          aria-label={`Side ${sideIndex + 1} participant ${participantNumber} type`}
          onValueChange={(kinds) => {
            const kind = kinds[0]

            if (kind === 'member' || kind === 'guest') {
              onKindChange({ sideIndex, participantIndex, kind })
            }
          }}
        >
          <ToggleGroupItem className="w-full" value="member">
            <UserRoundCheck data-icon="inline-start" />
            Crew member
          </ToggleGroupItem>
          <ToggleGroupItem className="w-full" value="guest">
            <UserRoundPlus data-icon="inline-start" />
            Guest
          </ToggleGroupItem>
        </ToggleGroup>

        <Field
          data-invalid={fieldErrors[fieldKey] !== undefined || undefined}
          data-disabled={isSubmitting || undefined}
        >
          {participant.kind === 'member' ? (
            <>
              <FieldLabel htmlFor={controlId}>Crew member</FieldLabel>
              <Select
                items={selectItems}
                value={participant.userId || null}
                disabled={isSubmitting}
                onValueChange={(value) =>
                  onValueChange({
                    sideIndex,
                    participantIndex,
                    value: value ?? '',
                  })
                }
              >
                <SelectTrigger
                  className="h-12 w-full"
                  id={controlId}
                  aria-invalid={fieldErrors[fieldKey] !== undefined || undefined}
                >
                  <SelectValue placeholder="Select a Crew member" />
                </SelectTrigger>
                <SelectContent alignItemWithTrigger={false}>
                  <SelectGroup>
                    {members.map((member) => (
                      <SelectItem
                        key={member.userId}
                        value={member.userId}
                        disabled={
                          member.userId !== participant.userId &&
                          selectedUserIds.has(member.userId)
                        }
                      >
                        <MemberLabel member={member} />
                      </SelectItem>
                    ))}
                  </SelectGroup>
                </SelectContent>
              </Select>
            </>
          ) : (
            <>
              <FieldLabel htmlFor={controlId}>Guest name</FieldLabel>
              <Input
                id={controlId}
                name={fieldKey}
                value={participant.guestName}
                placeholder="Enter Guest name"
                autoComplete="off"
                aria-invalid={fieldErrors[fieldKey] !== undefined || undefined}
                disabled={isSubmitting}
                onChange={(event) =>
                  onValueChange({
                    sideIndex,
                    participantIndex,
                    value: event.target.value,
                  })
                }
              />
            </>
          )}
          <FieldError>{fieldErrors[fieldKey]}</FieldError>
        </Field>
      </FieldGroup>
    </FieldSet>
  )
}

function MemberLabel({
  member,
}: {
  member: GameRecordingMemberHttpResponse
}) {
  return (
    <span className="flex min-w-0 items-center gap-2">
      <Avatar size="sm">
        {member.profilePictureUrl ? (
          <AvatarImage src={member.profilePictureUrl} alt="" />
        ) : null}
        <AvatarFallback>{getInitials(member.displayName)}</AvatarFallback>
      </Avatar>
      <span className="truncate">{member.displayName}</span>
    </span>
  )
}

function GameSavedConfirmation({
  game,
  memberByUserId,
  onRecordAnother,
}: {
  game: RecordGameHttpResponse
  memberByUserId: ReadonlyMap<string, GameRecordingMemberHttpResponse>
  onRecordAnother: () => void
}) {
  const firstSide = game.sides[0]
  const secondSide = game.sides[1]

  if (!firstSide || !secondSide) {
    throw new Error('A saved Game must contain exactly two Sides.')
  }

  const firstNames = firstSide.participants
    .map((participant) => getParticipantName({ participant, memberByUserId }))
    .join(' + ')
  const secondNames = secondSide.participants
    .map((participant) => getParticipantName({ participant, memberByUserId }))
    .join(' + ')
  const formatName = game.format === 'singles' ? 'Singles' : 'Doubles'

  return (
    <main className="flex flex-1 items-start justify-center p-4 pb-24 md:p-10">
      <Card className="w-full max-w-2xl border-2 border-border shadow-[6px_7px_0_var(--ink)]">
        <CardHeader>
          <span className="text-sm font-bold text-accent">Game recorded</span>
          <CardTitle className="text-2xl font-black tracking-[-.04em] md:text-3xl">
            Result saved
          </CardTitle>
          <CardDescription>
            {new Intl.DateTimeFormat(undefined, {
              dateStyle: 'medium',
              timeStyle: 'short',
            }).format(new Date(game.playedAtUtc))}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Alert role="status" aria-live="polite">
            <Check />
            <AlertTitle>{formatName} Game complete</AlertTitle>
            <AlertDescription>
              {firstNames} {firstSide.score}–{secondSide.score} {secondNames}
            </AlertDescription>
          </Alert>
        </CardContent>
        <CardFooter>
          <Button
            className="w-full"
            variant="crew"
            size="xl"
            type="button"
            onClick={onRecordAnother}
          >
            <RotateCcw data-icon="inline-start" />
            Record another Game
          </Button>
        </CardFooter>
      </Card>
    </main>
  )
}

function getParticipantName({
  participant,
  memberByUserId,
}: {
  participant: RecordedGameParticipantHttpResponse
  memberByUserId: ReadonlyMap<string, GameRecordingMemberHttpResponse>
}): string {
  if (participant.guestName) {
    return participant.guestName
  }

  if (participant.userId) {
    return memberByUserId.get(participant.userId)?.displayName ?? 'Crew member'
  }

  throw new Error('A saved participant must be a Crew member or Guest.')
}

function getInitials(displayName: string): string {
  const initials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.at(0)?.toUpperCase())
    .join('')

  return initials || '?'
}

function mapServerFieldErrors({
  serverErrors,
  values,
}: {
  serverErrors: Readonly<Record<string, ReadonlyArray<string>>>
  values: GameFormValues
}): Readonly<Record<string, string>> {
  const fieldErrors: Record<string, string> = {}

  for (const [key, messages] of Object.entries(serverErrors)) {
    const message = messages[0]

    if (!message) {
      continue
    }

    const normalizedKey = key.toLowerCase()
    const participantMatch = normalizedKey.match(
      /^sides\[(\d+)]\.participants\[(\d+)](?:\.(userid|guestname))?$/,
    )

    if (participantMatch) {
      const sideIndex = Number(participantMatch[1])
      const participantIndex = Number(participantMatch[2])
      const serverField = participantMatch[3]
      const participant = values.sides[sideIndex]?.participants[participantIndex]
      const field =
        serverField === 'userid' || serverField === 'guestname'
          ? serverField === 'userid'
            ? 'userId'
            : 'guestName'
          : participant?.kind === 'guest'
            ? 'guestName'
            : 'userId'
      fieldErrors[
        participantFieldName({ sideIndex, participantIndex, field })
      ] = message
      continue
    }

    if (normalizedKey.includes('.participants')) {
      fieldErrors.participants = message
    } else if (normalizedKey === 'sides') {
      if (message.toLowerCase().includes('score')) {
        fieldErrors[scoreFieldName({ sideIndex: 0 })] = message
        fieldErrors[scoreFieldName({ sideIndex: 1 })] = message
      } else {
        fieldErrors.participants = message
      }
    }
  }

  return fieldErrors
}
