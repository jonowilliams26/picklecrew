import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import {
  CircleAlertIcon,
  Crown,
  RotateCcw,
  Save,
  Sparkles,
  Swords,
  Trophy,
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
import { Separator } from '~/components/ui/separator'
import { Spinner } from '~/components/ui/spinner'
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
import { cn } from '~/lib/utils'
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

  const firstSide = values.sides[0]
  const secondSide = values.sides[1]

  if (!firstSide || !secondSide) {
    throw new Error('A Game form must contain exactly two Sides.')
  }

  return (
    <main className="relative flex flex-1 items-start justify-center p-4 pb-28 md:p-10">
      <Card className="w-full max-w-5xl border-2 border-ink bg-cream py-0 shadow-[6px_7px_0_var(--ink)]">
        <form
          className="flex flex-col gap-0"
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            void submit()
          }}
        >
          <CardHeader className="rounded-t-[10px] border-b-2 border-ink bg-ink p-6 text-cream md:p-8">
            <span className="flex items-center gap-2 text-[10px] font-black tracking-[.14em] text-lime uppercase">
              <Sparkles aria-hidden="true" />
              Record a Game
            </span>
            <CardTitle className="max-w-2xl text-3xl font-black leading-none tracking-[-.05em] md:text-5xl">
              Who owned the court?
            </CardTitle>
            <CardDescription className="max-w-xl font-heading text-base text-cream/70">
              Add the players and final score. We’ll handle the bragging rights.
            </CardDescription>
          </CardHeader>

          <CardContent className="flex flex-col gap-7 p-4 md:p-7">
            <FieldSet className="gap-3" disabled={isSubmitting}>
              <FieldLegend className="font-heading text-base font-black tracking-[-.025em]">
                Game format
              </FieldLegend>
              <Field
                data-invalid={fieldErrors.participants !== undefined || undefined}
              >
                <ToggleGroup
                  className="grid w-full grid-cols-2 gap-3"
                  variant="crew"
                  size="card"
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
                    <UserRound aria-hidden="true" />
                    <span className="flex flex-col items-center leading-none">
                      Singles
                      <span className="mt-1 text-[10px] font-bold tracking-[.08em] uppercase opacity-65">
                        One vs one
                      </span>
                    </span>
                  </ToggleGroupItem>
                  <ToggleGroupItem className="w-full" value="doubles">
                    <UsersRound aria-hidden="true" />
                    <span className="flex flex-col items-center leading-none">
                      Doubles
                      <span className="mt-1 text-[10px] font-bold tracking-[.08em] uppercase opacity-65">
                        Two vs two
                      </span>
                    </span>
                  </ToggleGroupItem>
                </ToggleGroup>
                <FieldError>{fieldErrors.participants}</FieldError>
              </Field>
            </FieldSet>

            <div className="flex flex-col items-center gap-1 text-center">
              <span className="flex items-center gap-2 text-[10px] font-black tracking-[.12em] text-accent uppercase">
                <Swords aria-hidden="true" />
                Build the matchup
              </span>
              <p className="font-mono text-[10px] text-muted-foreground uppercase">
                First to 11 · win by 2
              </p>
              <Separator className="mt-2 bg-ink/15" />
            </div>

            <FieldGroup className="items-stretch gap-6 lg:grid lg:grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)] lg:gap-8">
              <GameSideFields
                sideIndex={0}
                side={firstSide}
                members={options.members}
                selectedUserIds={selectedUserIds}
                fieldErrors={fieldErrors}
                isSubmitting={isSubmitting}
                onParticipantKindChange={updateParticipantKind}
                onParticipantValueChange={updateParticipantValue}
                onScoreChange={updateScore}
              />
              <Separator className="bg-ink/15 lg:hidden" />
              <Separator
                className="hidden bg-ink/15 lg:block"
                orientation="vertical"
              />
              <GameSideFields
                sideIndex={1}
                side={secondSide}
                members={options.members}
                selectedUserIds={selectedUserIds}
                fieldErrors={fieldErrors}
                isSubmitting={isSubmitting}
                onParticipantKindChange={updateParticipantKind}
                onParticipantValueChange={updateParticipantValue}
                onScoreChange={updateScore}
              />
            </FieldGroup>

            {submissionError ? (
              <Alert variant="destructive" aria-live="assertive">
                <CircleAlertIcon />
                <AlertTitle>Game not saved</AlertTitle>
                <AlertDescription>{submissionError}</AlertDescription>
              </Alert>
            ) : null}
          </CardContent>

          <CardFooter className="flex-col items-stretch gap-3 border-t border-ink/15 p-4 md:flex-row md:items-center md:justify-between md:p-6">
            <p className="hidden max-w-sm font-heading text-sm font-semibold text-muted-foreground md:block">
              Give the Sides one last look, then make it official.
            </p>
            <Button
              className="w-full md:w-auto md:min-w-64"
              variant="crew"
              size="xl"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <Spinner data-icon="inline-start" />
              ) : (
                <Save data-icon="inline-start" />
              )}
              {isSubmitting ? 'Saving Game…' : 'Lock in the result'}
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
    <FieldSet className="min-w-0 gap-5 px-1 py-2 md:px-3" disabled={isSubmitting}>
      <FieldLegend className="sr-only">Side {sideNumber}</FieldLegend>
      <FieldGroup className="gap-5">
        <Field
          className="mx-auto max-w-36 items-center"
          data-invalid={fieldErrors[scoreKey] !== undefined || undefined}
          data-disabled={isSubmitting || undefined}
        >
          <FieldLabel
            className="justify-center gap-2 text-center font-heading text-xl font-black tracking-[-.04em]"
            htmlFor={scoreInputId}
          >
            <span
              className={cn(
                'size-2 rounded-full',
                sideIndex === 0 ? 'bg-orange' : 'bg-aqua',
              )}
              aria-hidden="true"
            />
            Side {sideNumber}
          </FieldLabel>
          <Input
            id={scoreInputId}
            name={scoreKey}
            type="number"
            variant="score"
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
          <FieldError className="text-center">
            {fieldErrors[scoreKey]}
          </FieldError>
        </Field>

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
    <FieldSet className="gap-3 border-t border-ink/15 pt-4">
      <FieldLegend
        className="px-1 font-semibold tracking-[.08em] text-muted-foreground uppercase data-[variant=label]:text-[9px]"
        variant="label"
      >
        Player {participantNumber}
      </FieldLegend>
      <FieldGroup className="gap-4">
        <ToggleGroup
          className="grid w-full grid-cols-2 gap-2"
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
                  className="w-full data-[size=default]:h-12"
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
                className="h-12"
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

  const formatName = game.format === 'singles' ? 'Singles' : 'Doubles'
  const playedAt = new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(game.playedAtUtc))

  return (
    <main className="relative flex flex-1 items-start justify-center p-4 pb-28 md:p-10">
      <Card className="w-full max-w-3xl gap-0 border-2 border-ink bg-cream py-0 shadow-[6px_7px_0_var(--ink)]">
        <CardHeader className="items-center justify-items-center gap-3 rounded-t-[10px] border-b-2 border-ink bg-ink p-7 text-center text-cream md:p-9">
          <span className="grid size-14 place-items-center justify-self-center rounded-full bg-lime text-ink">
            <Trophy className="-rotate-6" aria-hidden="true" />
          </span>
          <span className="text-[10px] font-black tracking-[.14em] text-lime uppercase">
            Game recorded
          </span>
          <CardTitle className="max-w-xl text-3xl font-black leading-none tracking-[-.05em] md:text-5xl">
            Bragging rights secured.
          </CardTitle>
          <CardDescription className="font-mono text-[11px] text-cream/65 uppercase">
            {formatName} · {playedAt}
          </CardDescription>
        </CardHeader>
        <CardContent className="gap-6 p-5 md:p-8">
          <div
            className="grid grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)] items-stretch gap-3 md:gap-8"
            role="status"
            aria-live="polite"
          >
            <SavedResultSide
              participants={firstSide.participants}
              memberByUserId={memberByUserId}
              score={firstSide.score}
              isWinner={firstSide.score > secondSide.score}
              sideIndex={0}
            />
            <Separator className="h-full bg-ink/15" orientation="vertical" />
            <SavedResultSide
              participants={secondSide.participants}
              memberByUserId={memberByUserId}
              score={secondSide.score}
              isWinner={secondSide.score > firstSide.score}
              sideIndex={1}
            />
          </div>
          <p className="text-center font-heading text-sm font-semibold text-muted-foreground">
            The result is saved and ready for the next rally.
          </p>
        </CardContent>
        <CardFooter className="border-t border-ink/15 p-5 md:p-6">
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

function SavedResultSide({
  participants,
  memberByUserId,
  score,
  isWinner,
  sideIndex,
}: {
  participants: ReadonlyArray<RecordedGameParticipantHttpResponse>
  memberByUserId: ReadonlyMap<string, GameRecordingMemberHttpResponse>
  score: number
  isWinner: boolean
  sideIndex: number
}) {
  return (
    <div className="flex min-w-0 flex-col items-center justify-center gap-2 p-3 text-center md:p-5">
      <span className="flex min-h-4 items-center justify-center gap-1.5 font-mono text-[9px] font-black tracking-[.12em] text-muted-foreground uppercase">
        {isWinner ? (
          <>
            <Crown className="size-4" aria-hidden="true" />
            Winner
          </>
        ) : (
          `Side ${sideIndex + 1}`
        )}
      </span>
      <strong className="font-heading text-6xl font-black leading-none tracking-[-.06em] tabular-nums md:text-8xl">
        {score}
      </strong>
      <ul className="mt-2 flex w-full min-w-0 max-w-56 flex-col divide-y divide-ink/10 border-y border-ink/15">
        {participants.map((participant, index) => {
          const name = getParticipantName({ participant, memberByUserId })
          const member = participant.userId
            ? memberByUserId.get(participant.userId)
            : undefined

          return (
            <li
              className="flex min-w-0 items-center justify-start gap-2 px-1 py-2.5 text-left"
              key={`${index}-${name}`}
            >
              <Avatar size="sm">
                {member?.profilePictureUrl ? (
                  <AvatarImage src={member.profilePictureUrl} alt="" />
                ) : null}
                <AvatarFallback>{getInitials(name)}</AvatarFallback>
              </Avatar>
              <span
                className="min-w-0 truncate font-heading text-sm font-medium leading-tight"
                title={name}
              >
                {name}
              </span>
            </li>
          )
        })}
      </ul>
    </div>
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
