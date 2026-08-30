import {
  createFileRoute,
  Link,
  redirect,
  useRouter,
} from '@tanstack/react-router'
import {
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  ClipboardList,
  Crown,
  Plus,
  UserRound,
  UsersRound,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { z } from 'zod'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from '~/components/ui/avatar'
import { Badge } from '~/components/ui/badge'
import { Button, buttonVariants } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '~/components/ui/empty'
import {
  Pagination,
  PaginationContent,
  PaginationItem,
} from '~/components/ui/pagination'
import { Separator } from '~/components/ui/separator'
import type {
  GameHistoryItemHttpResponse,
  GameHistoryParticipantHttpResponse,
  GameHistorySideHttpResponse,
} from '~/lib/api/generated/types.gen'
import { getGames } from '~/lib/games/games.functions'
import { cn } from '~/lib/utils'

const gamesSearchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
})

export const Route = createFileRoute(
  '/_authenticated/_active-crew/games',
)({
  validateSearch: gamesSearchSchema,
  loaderDeps: ({ search }) => ({ page: search.page }),
  loader: async ({ deps }) => {
    const result = await getGames({ data: deps })

    if (
      result.ok &&
      result.value.totalPages > 0 &&
      deps.page > result.value.totalPages
    ) {
      throw redirect({
        to: '/games',
        search: { page: result.value.totalPages },
        replace: true,
      })
    }

    return result
  },
  component: GamesPage,
})

function GamesPage() {
  const result = Route.useLoaderData()
  const { page } = Route.useSearch()
  const router = useRouter()

  if (!result.ok) {
    return (
      <main className="flex flex-1 items-start justify-center p-5 md:p-10">
        <Card className="w-full max-w-2xl border-2 border-ink shadow-[6px_7px_0_var(--ink)]">
          <CardHeader>
            <CardTitle>Game history</CardTitle>
            <CardDescription>
              Completed Games for the current Crew appear here.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Alert variant="destructive">
              <ClipboardList />
              <AlertTitle>History unavailable</AlertTitle>
              <AlertDescription>{result.error.message}</AlertDescription>
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

  const history = result.value
  const startGame =
    history.totalCount === 0 ? 0 : (history.page - 1) * history.pageSize + 1
  const endGame = Math.min(
    history.page * history.pageSize,
    history.totalCount,
  )

  return (
    <main className="relative flex flex-1 flex-col p-5 pb-12 md:p-8 xl:p-10">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-8">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex flex-col gap-2">
            <span className="text-[10px] font-black tracking-[.15em] text-orange uppercase">
              The receipts
            </span>
            <h1 className="m-0 font-heading text-[clamp(2.8rem,6vw,5rem)] leading-[.9] font-black tracking-[-.06em]">
              Game history
            </h1>
            <p className="m-0 max-w-xl text-sm leading-relaxed text-ink-soft">
              Every final score, matchup, and date from this Crew.
            </p>
          </div>
          <Link
            className={buttonVariants({ variant: 'crew', size: 'lg' })}
            to="/record-game"
          >
            <Plus data-icon="inline-start" />
            New game
          </Link>
        </header>

        {history.items.length === 0 ? (
          <Card className="border-2 border-dashed border-ink/30 bg-cream">
            <CardContent>
              <Empty>
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <ClipboardList />
                  </EmptyMedia>
                  <EmptyTitle>No Games recorded yet</EmptyTitle>
                  <EmptyDescription>
                    Record the first result to start this Crew&apos;s history.
                  </EmptyDescription>
                </EmptyHeader>
                <EmptyContent>
                  <Link
                    className={buttonVariants({ variant: 'crew' })}
                    to="/record-game"
                  >
                    <Plus data-icon="inline-start" />
                    Record the first game
                  </Link>
                </EmptyContent>
              </Empty>
            </CardContent>
          </Card>
        ) : (
          <section className="flex flex-col gap-4" aria-label="Completed Games">
            {history.items.map((game) => (
              <GameResultCard game={game} key={game.gameId} />
            ))}
          </section>
        )}

        {history.totalPages > 1 ? (
          <div className="-mt-4 flex items-center justify-end gap-3">
            <p className="m-0 whitespace-nowrap font-mono text-[10px] tracking-[.08em] text-muted-foreground uppercase">
              Showing {startGame}–{endGame} of {history.totalCount} Games
            </p>
            <GamePagination page={page} totalPages={history.totalPages} />
          </div>
        ) : null}
      </div>
    </main>
  )
}

function GameResultCard({ game }: { game: GameHistoryItemHttpResponse }) {
  const firstSide = game.sides[0]
  const secondSide = game.sides[1]

  if (!firstSide || !secondSide) {
    throw new Error('A historical Game must contain exactly two Sides.')
  }

  const formatName = game.format === 'singles' ? 'Singles' : 'Doubles'
  const FormatIcon = game.format === 'singles' ? UserRound : UsersRound

  return (
    <Card className="gap-0 overflow-hidden border-2 border-ink bg-cream py-0 shadow-[4px_5px_0_var(--ink)]">
      <CardHeader className="flex flex-row items-center justify-between gap-3 border-b border-ink/15 px-4 py-3! md:px-5">
        <div className="flex items-center gap-2 text-xs leading-none font-normal text-muted-foreground">
          <CalendarDays className="size-3.5" aria-hidden="true" />
          <LocalPlayedAt playedAtUtc={game.playedAtUtc} />
        </div>
        <Badge variant="outline">
          <FormatIcon data-icon="inline-start" />
          {formatName}
        </Badge>
      </CardHeader>
      <CardContent className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-stretch gap-3 p-4 md:gap-7 md:p-6">
        <ResultSide
          side={firstSide}
          isWinner={firstSide.score > secondSide.score}
          align="right"
        />
        <Separator className="h-full bg-ink/15" orientation="vertical" />
        <ResultSide
          side={secondSide}
          isWinner={secondSide.score > firstSide.score}
          align="left"
        />
      </CardContent>
    </Card>
  )
}

function ResultSide({
  side,
  isWinner,
  align,
}: {
  side: GameHistorySideHttpResponse
  isWinner: boolean
  align: 'left' | 'right'
}) {
  return (
    <div
      className={cn(
        'flex min-w-0 flex-col gap-3',
        align === 'right' ? 'items-end text-right' : 'items-start text-left',
      )}
    >
      <span
        className={cn(
          'flex min-h-5 items-center gap-1.5 text-[9px] font-black tracking-[.1em] uppercase',
          isWinner ? 'text-orange' : 'text-muted-foreground',
        )}
      >
        {isWinner ? (
          <>
            <Crown className="size-3.5 -rotate-12" aria-hidden="true" />
            Winner
          </>
        ) : (
          `Side ${side.sideNumber}`
        )}
      </span>
      <strong className="font-heading text-5xl leading-none font-black tracking-[-.06em] tabular-nums md:text-7xl">
        {side.score}
      </strong>
      <ul
        className={cn(
          'm-0 flex min-w-0 list-none flex-col gap-2 p-0',
          align === 'right' && 'items-end',
        )}
      >
        {side.participants.map((participant, index) => (
          <Participant
            participant={participant}
            align={align}
            key={`${participant.displayName}-${index}`}
          />
        ))}
      </ul>
    </div>
  )
}

function Participant({
  participant,
  align,
}: {
  participant: GameHistoryParticipantHttpResponse
  align: 'left' | 'right'
}) {
  return (
    <li
      className={cn(
        'flex min-w-0 items-center gap-2',
        align === 'right' && 'flex-row-reverse',
      )}
    >
      <Avatar size="sm">
        {participant.profilePictureUrl ? (
          <AvatarImage
            src={participant.profilePictureUrl}
            alt={participant.displayName}
          />
        ) : null}
        <AvatarFallback>{getInitials(participant.displayName)}</AvatarFallback>
      </Avatar>
      <span className="min-w-0 truncate font-heading text-xs font-semibold sm:text-sm">
        {participant.displayName}
      </span>
    </li>
  )
}

function GamePagination({
  page,
  totalPages,
}: {
  page: number
  totalPages: number
}) {
  return (
    <Pagination className="mx-0 w-auto justify-end">
      <PaginationContent>
        <PaginationItem>
          {page > 1 ? (
            <Link
              className={buttonVariants({ variant: 'outline', size: 'sm' })}
              to="/games"
              search={{ page: page - 1 }}
              aria-label="Go to previous page"
            >
              <ChevronLeft data-icon="inline-start" />
              <span className="hidden sm:inline">Previous</span>
            </Link>
          ) : (
            <Button
              variant="outline"
              size="sm"
              disabled
              aria-label="Go to previous page"
            >
              <ChevronLeft data-icon="inline-start" />
              <span className="hidden sm:inline">Previous</span>
            </Button>
          )}
        </PaginationItem>
        <PaginationItem>
          {page < totalPages ? (
            <Link
              className={buttonVariants({ variant: 'outline', size: 'sm' })}
              to="/games"
              search={{ page: page + 1 }}
              aria-label="Go to next page"
            >
              <span className="hidden sm:inline">Next</span>
              <ChevronRight data-icon="inline-end" />
            </Link>
          ) : (
            <Button
              variant="outline"
              size="sm"
              disabled
              aria-label="Go to next page"
            >
              <span className="hidden sm:inline">Next</span>
              <ChevronRight data-icon="inline-end" />
            </Button>
          )}
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  )
}

function LocalPlayedAt({ playedAtUtc }: { playedAtUtc: string }) {
  const [formattedPlayedAt, setFormattedPlayedAt] = useState<string>()

  useEffect(() => {
    setFormattedPlayedAt(formatPlayedAt(new Date(playedAtUtc)))
  }, [playedAtUtc])

  return (
    <time className="inline-flex items-center leading-none" dateTime={playedAtUtc}>
      {formattedPlayedAt ?? '\u00a0'}
    </time>
  )
}

function formatPlayedAt(playedAt: Date): string {
  const parts = new Intl.DateTimeFormat('en-AU', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  }).formatToParts(playedAt)
  const valueByType = new Map(parts.map((part) => [part.type, part.value]))

  return [
    valueByType.get('day'),
    valueByType.get('month'),
    valueByType.get('year'),
    `${valueByType.get('hour')}:${valueByType.get('minute')}`,
    valueByType.get('dayPeriod')?.toLowerCase(),
  ].join(' ')
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
