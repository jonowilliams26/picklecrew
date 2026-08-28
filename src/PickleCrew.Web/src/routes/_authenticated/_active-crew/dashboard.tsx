import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import {
  CircleAlert,
  Crown,
  Flame,
  Medal,
  Target,
  Trophy,
  TrendingUp,
  UsersRound,
} from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Badge } from '~/components/ui/badge'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import { Separator } from '~/components/ui/separator'
import type {
  CrewLeaderboardEntryHttpResponse,
  DoublesPairingPlayerHttpResponse,
} from '~/lib/api/generated/types.gen'
import { getDashboard } from '~/lib/dashboard/dashboard.functions'
import {
  formatCount,
  formatPercentage,
  formatRecord,
  formatSignedNumber,
  formatStreakPeriod,
  getCurrentStreakDetail,
  getInitials,
} from '~/lib/dashboard/dashboard'
import { cn } from '~/lib/utils'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/dashboard',
)({
  loader: () => getDashboard(),
  component: DashboardPage,
})

const avatarStyles = [
  'bg-orange text-ink',
  'bg-lime text-ink',
  'bg-aqua text-ink',
  'bg-purple text-white',
  'bg-paper text-ink',
] as const

function DashboardPage() {
  const result = Route.useLoaderData()
  const { user } = useAuth()
  const router = useRouter()

  if (!user) {
    throw new Error('Dashboard rendered without an authenticated user.')
  }

  if (!result.ok) {
    return (
      <main className="flex flex-1 items-start justify-center p-5 md:p-10">
        <Card className="w-full max-w-2xl border-2 border-ink shadow-[6px_7px_0_var(--ink)]">
          <CardHeader>
            <CardTitle>Dashboard</CardTitle>
            <CardDescription>
              Statistics for the current Crew appear here.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Alert variant="destructive">
              <CircleAlert />
              <AlertTitle>Dashboard unavailable</AlertTitle>
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

  const {
    personalStatistics,
    recentGames,
    leaderboard,
    longestWinStreak,
    bestDoublesPairing,
  } = result.value
  const winPercentage = formatPercentage({
    value: personalStatistics.winPercentage,
  })
  const progressPercentage = Math.min(100, Math.max(0, winPercentage))
  const personalRecord = formatRecord({
    wins: personalStatistics.wins,
    losses: personalStatistics.losses,
  })
  const recentRecord = formatRecord({
    wins: recentGames.wins,
    losses: recentGames.losses,
  })
  const recentGameCount = recentGames.games.length
  const hasLeaderboardGames = leaderboard.entries.some(
    (member) => member.wins + member.losses > 0,
  )
  const pairing = bestDoublesPairing.pairing
  const firstPairingPlayer = pairing?.players[0]
  const secondPairingPlayer = pairing?.players[1]

  if (
    pairing &&
    (pairing.players.length !== 2 ||
      !firstPairingPlayer ||
      !secondPairingPlayer)
  ) {
    throw new Error('A doubles pairing must contain exactly two players.')
  }

  return (
    <main className="relative flex flex-1 flex-col overflow-hidden p-5 md:p-8 md:pb-10 xl:p-10">
      <div className="mx-auto flex w-full max-w-[1480px] flex-col gap-12">
        <section
          className="flex flex-col gap-6"
          aria-labelledby="personal-stats-heading"
        >
          <SectionHeading
            eyebrow="01 · Your game"
            title="Personal receipts"
            description="Your numbers inside this crew. No selective memory allowed."
            headingId="personal-stats-heading"
          />

          {personalStatistics.gamesPlayed > 0 ? (
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-12">
              <Card className="dashboard-stat-card dashboard-stat-card-lime min-h-[310px] border-2 border-ink bg-lime shadow-[7px_8px_0_var(--ink)] sm:col-span-2 xl:col-span-5 xl:row-span-2">
                <CardHeader>
                  <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                    Your win percentage
                  </CardTitle>
                  <CardDescription className="font-heading text-ink-soft">
                    {formatCount({
                      count: personalStatistics.wins,
                      singular: 'win',
                    })}{' '}
                    from{' '}
                    {formatCount({
                      count: personalStatistics.gamesPlayed,
                      singular: 'game',
                    })}
                  </CardDescription>
                  <CardAction className="grid size-12 rotate-[5deg] place-items-center rounded-full border-2 border-ink bg-cream shadow-[3px_4px_0_var(--ink)] [&_svg]:size-6">
                    <Target aria-hidden="true" />
                  </CardAction>
                </CardHeader>
                <CardContent className="mt-auto gap-5">
                  <div className="font-heading text-[clamp(6.5rem,13vw,10rem)] leading-[.68] font-black tracking-[-.1em]">
                    {winPercentage}
                    <span className="ml-2 text-[.25em] tracking-[-.03em]">%</span>
                  </div>
                  <div
                    className="h-4 overflow-hidden rounded-full border-2 border-ink bg-cream"
                    role="progressbar"
                    aria-label="Win percentage"
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-valuenow={progressPercentage}
                  >
                    <span
                      className={cn(
                        'block h-full bg-orange',
                        progressPercentage > 0 &&
                          progressPercentage < 100 &&
                          'border-r-2 border-ink',
                      )}
                      style={{ width: `${progressPercentage}%` }}
                    />
                  </div>
                </CardContent>
                <CardFooter className="justify-end">
                  <span className="text-right text-[9px] font-black tracking-[.08em] uppercase">
                    {personalRecord} all time
                  </span>
                </CardFooter>
              </Card>

              <PersonalMetricCard
                className="bg-ink text-cream shadow-[6px_7px_0_var(--orange)] xl:col-span-3"
                eyebrow="Crew rank"
                value={`#${personalStatistics.crewRank}`}
                detail={`of ${formatCount({
                  count: personalStatistics.activeCrewMemberCount,
                  singular: 'player',
                })}`}
                icon={Medal}
                iconClassName="bg-orange text-ink"
              />
              <PersonalMetricCard
                className="bg-orange text-ink shadow-[6px_7px_0_var(--ink)] xl:col-span-4"
                eyebrow="Current streak"
                value={
                  personalStatistics.currentWinStreak === 0
                    ? 'Your move'
                    : `${personalStatistics.currentWinStreak} W`
                }
                detail={getCurrentStreakDetail({
                  currentWinStreak: personalStatistics.currentWinStreak,
                  longestWinStreak: personalStatistics.longestWinStreak,
                })}
                icon={Flame}
                iconClassName="bg-lime text-ink"
              />
              <PersonalMetricCard
                className="bg-aqua text-ink shadow-[6px_7px_0_var(--ink)] xl:col-span-3"
                eyebrow="Point differential"
                value={formatSignedNumber({
                  value: personalStatistics.pointDifferential,
                })}
                detail={`${personalStatistics.pointsScored.toLocaleString('en-AU')} for · ${personalStatistics.pointsConceded.toLocaleString('en-AU')} against`}
                icon={TrendingUp}
                iconClassName="bg-cream text-ink"
              />

              <Card className="dashboard-stat-card border-2 border-ink bg-cream shadow-[6px_7px_0_var(--ink)] sm:col-span-2 xl:col-span-4">
                <CardHeader>
                  <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                    Last 10 games
                  </CardTitle>
                  <CardDescription className="font-heading">
                    {recentGameCount === 0
                      ? 'No recent games'
                      : `${recentRecord} · last ${recentGameCount}`}
                  </CardDescription>
                  {recentGames.winStreak > 1 ? (
                    <CardAction>
                      <Badge className="bg-lime text-ink">
                        <Flame data-icon="inline-start" />
                        {recentGames.winStreak}-game win streak
                      </Badge>
                    </CardAction>
                  ) : null}
                </CardHeader>
                <CardContent>
                  {recentGameCount > 0 ? (
                    <div
                      className="grid w-fit grid-cols-5 gap-2"
                      aria-label={`${formatCount({ count: recentGames.wins, singular: 'win' })} and ${formatCount({ count: recentGames.losses, singular: 'loss', plural: 'losses' })} in the last ${formatCount({ count: recentGameCount, singular: 'game' })}`}
                    >
                      {recentGames.games.map((game) => {
                        const resultLabel = game.result === 'win' ? 'W' : 'L'

                        return (
                          <span
                            className={cn(
                              'grid size-9 place-items-center rounded-full border-2 border-ink font-mono text-xs font-black shadow-[2px_3px_0_var(--ink)]',
                              game.result === 'win' ? 'bg-lime' : 'bg-orange',
                            )}
                            key={game.gameId}
                            title={`${game.result === 'win' ? 'Win' : 'Loss'} · ${game.format}`}
                          >
                            {resultLabel}
                          </span>
                        )
                      })}
                    </div>
                  ) : (
                    <p className="m-0 text-sm text-muted-foreground">
                      Record a game to start your recent form.
                    </p>
                  )}
                </CardContent>
                <CardFooter>
                  <span className="text-[9px] font-bold tracking-[.08em] text-muted-foreground uppercase">
                    Newest game first
                  </span>
                </CardFooter>
              </Card>
            </div>
          ) : (
            <Card className="min-h-[310px] border-2 border-dashed border-ink/30 bg-cream">
              <CardContent className="my-auto items-center justify-center gap-2 text-center">
                <strong className="font-heading text-3xl font-black">
                  No personal receipts yet
                </strong>
                <p className="m-0 max-w-sm text-sm text-ink-soft">
                  Get on the court, play a Game, and start building a record
                  worth bragging about.
                </p>
              </CardContent>
            </Card>
          )}
        </section>

        <Separator className="bg-ink/20" />

        <section
          className="flex flex-col gap-7"
          aria-labelledby="crew-stats-heading"
        >
          <SectionHeading
            eyebrow="02 · Crew chaos"
            title="The whole squad"
            description="The standings, streaks, pairings, and evidence currently driving the group chat."
            headingId="crew-stats-heading"
          />

          <div className="grid gap-5 xl:grid-cols-12">
            <Card className="border-2 border-ink bg-cream text-ink shadow-[7px_8px_0_var(--ink)] xl:col-span-7 xl:row-span-2">
              <CardHeader className="border-b-2 border-line pb-8!">
                <CardTitle className="font-heading text-3xl font-black tracking-[-.045em] sm:text-4xl">
                  Crew leaderboard
                </CardTitle>
                <CardDescription className="font-heading text-ink-soft">
                  Ranked by totally indisputable crew score.
                </CardDescription>
                <CardAction className="grid size-12 rotate-[6deg] place-items-center rounded-full border-2 border-ink bg-orange shadow-[3px_4px_0_var(--ink)] [&_svg]:size-6">
                  <Trophy aria-hidden="true" />
                </CardAction>
              </CardHeader>
              {hasLeaderboardGames ? (
                <CardContent className="gap-0">
                  <div className="grid grid-cols-[34px_1fr_auto] gap-3 px-3 pb-2 text-[8px] font-black tracking-[.12em] text-muted-foreground uppercase sm:grid-cols-[42px_1fr_78px_72px]">
                    <span>Rank</span>
                    <span>Player</span>
                    <span className="hidden sm:block">Record</span>
                    <span className="text-right">Score</span>
                  </div>
                  <ol className="m-0 flex list-none flex-col gap-2 p-0">
                    {leaderboard.entries.map((member) => (
                      <LeaderboardEntry
                        currentUserId={user.id}
                        member={member}
                        key={member.userId}
                      />
                    ))}
                  </ol>
                </CardContent>
              ) : (
                <CardContent className="my-auto items-center justify-center gap-2 text-center">
                  <strong className="font-heading text-3xl font-black">
                    No leaderboard drama yet
                  </strong>
                  <p className="m-0 max-w-sm text-sm text-ink-soft">
                    Get on the court, record a Game, and give the Crew something
                    to argue about.
                  </p>
                </CardContent>
              )}
            </Card>

            <Card className="dashboard-pair-card relative min-h-[330px] border-2 border-ink bg-purple text-white shadow-[7px_8px_0_var(--ink)] xl:col-span-5">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Best doubles pairing
                </CardTitle>
                <CardDescription className="font-heading text-white/70">
                  The duo nobody wants across the net
                </CardDescription>
                <CardAction className="grid size-11 place-items-center rounded-full border-2 border-white bg-aqua text-ink [&_svg]:size-5">
                  <UsersRound aria-hidden="true" />
                </CardAction>
              </CardHeader>
              {pairing && firstPairingPlayer && secondPairingPlayer ? (
                <>
                  <CardContent className="items-center gap-3 text-center">
                    <div className="flex items-center justify-center">
                      <DashboardAvatar
                        className="z-1 size-20 rotate-[-7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                        fallbackClassName="font-heading text-2xl"
                        person={firstPairingPlayer}
                      />
                      <span
                        className="z-2 -mx-2 grid size-10 rotate-[6deg] place-items-center rounded-full border-2 border-ink bg-cream font-heading text-xl font-black text-ink"
                        aria-hidden="true"
                      >
                        +
                      </span>
                      <DashboardAvatar
                        className="z-1 size-20 rotate-[7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                        fallbackClassName="font-heading text-2xl"
                        person={secondPairingPlayer}
                      />
                    </div>
                    <CardTitle className="font-heading text-3xl font-black">
                      {firstPairingPlayer.displayName} +{' '}
                      {secondPairingPlayer.displayName}
                    </CardTitle>
                    <div className="flex items-baseline gap-2">
                      <strong className="font-heading text-5xl font-black text-lime">
                        {formatPercentage({ value: pairing.winPercentage })}%
                      </strong>
                      <span className="text-[9px] font-black tracking-[.08em] uppercase">
                        win rate
                      </span>
                    </div>
                  </CardContent>
                  <CardFooter className="mt-auto justify-center">
                    <Badge
                      className="rotate-[-2deg] border border-white bg-transparent px-3 text-white"
                      variant="outline"
                    >
                      {formatCount({ count: pairing.wins, singular: 'win' })} ·{' '}
                      {formatCount({
                        count: pairing.losses,
                        singular: 'loss',
                        plural: 'losses',
                      })}{' '}
                      ·{' '}
                      {formatCount({
                        count: pairing.gamesPlayed,
                        singular: 'game',
                      })}
                    </Badge>
                  </CardFooter>
                </>
              ) : (
                <CardContent className="my-auto items-center justify-center gap-2 text-center">
                  <strong className="font-heading text-3xl font-black">
                    No doubles record yet
                  </strong>
                  <p className="m-0 max-w-xs text-sm text-white/70">
                    Grab a partner, hit the court, and start proving which duo
                    runs this Crew.
                  </p>
                </CardContent>
              )}
            </Card>

            <Card className="dashboard-streak-card relative min-h-[290px] border-2 border-ink bg-orange text-ink shadow-[7px_8px_0_var(--ink)] xl:col-span-5">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Longest crew win streak
                </CardTitle>
                <CardDescription className="font-heading text-ink-soft">
                  The record to chase
                </CardDescription>
                <CardAction className="grid size-11 rotate-[7deg] place-items-center rounded-full border-2 border-ink bg-lime shadow-[3px_4px_0_var(--ink)] [&_svg]:size-5">
                  <Flame aria-hidden="true" />
                </CardAction>
              </CardHeader>
              {longestWinStreak.longestWinStreak > 0 ? (
                <>
                  <CardContent className="relative z-1 flex-row items-end justify-between gap-5">
                    <div>
                      <strong className="block font-heading text-8xl leading-[.72] font-black tracking-[-.08em]">
                        {longestWinStreak.longestWinStreak}
                      </strong>
                      <span className="font-heading text-xl font-black">
                        straight{' '}
                        {longestWinStreak.longestWinStreak === 1
                          ? 'win'
                          : 'wins'}
                      </span>
                    </div>
                    {longestWinStreak.userId &&
                    longestWinStreak.displayName ? (
                      <DashboardAvatar
                        className="size-16 rotate-[-5deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                        fallbackClassName="bg-cream font-heading text-xl text-ink"
                        person={{
                          userId: longestWinStreak.userId,
                          displayName: longestWinStreak.displayName,
                          profilePictureUrl:
                            longestWinStreak.profilePictureUrl ?? null,
                        }}
                      />
                    ) : null}
                  </CardContent>
                  <CardFooter className="relative z-1 mt-auto flex-col items-start gap-1">
                    <strong className="font-heading text-base">
                      {longestWinStreak.displayName ?? 'No record holder yet'}
                    </strong>
                    <span className="text-[9px] font-black tracking-[.08em] uppercase">
                      {formatStreakPeriod({
                        startedAtUtc:
                          longestWinStreak.longestWinStreakStartedAtUtc,
                        endedAtUtc:
                          longestWinStreak.longestWinStreakEndedAtUtc,
                      })}
                    </span>
                  </CardFooter>
                </>
              ) : (
                <CardContent className="my-auto items-center justify-center gap-2 text-center">
                  <strong className="font-heading text-3xl font-black">
                    No win streak yet
                  </strong>
                  <p className="m-0 max-w-xs text-sm text-ink-soft">
                    Get on the court and start proving who runs this Crew.
                  </p>
                </CardContent>
              )}
            </Card>
          </div>
        </section>
      </div>
    </main>
  )
}

function LeaderboardEntry({
  member,
  currentUserId,
}: {
  member: CrewLeaderboardEntryHttpResponse
  currentUserId: string
}) {
  const isCurrentUser = member.userId === currentUserId

  return (
    <li
      className={cn(
        'grid min-h-16 grid-cols-[34px_1fr_auto] items-center gap-3 rounded-xl border-2 border-ink px-3 py-2 sm:grid-cols-[42px_1fr_78px_72px]',
        isCurrentUser ? 'bg-lime shadow-[3px_4px_0_var(--ink)]' : 'bg-paper',
      )}
    >
      <span className="font-mono text-sm font-black">
        {String(member.rank).padStart(2, '0')}
      </span>
      <span className="flex min-w-0 items-center gap-3">
        <DashboardAvatar
          className="size-10 border-2 border-ink"
          fallbackClassName="font-black"
          person={member}
        />
        <strong className="truncate font-heading text-sm sm:text-base">
          {member.displayName}
          {isCurrentUser ? <span className="sr-only"> (you)</span> : null}
        </strong>
        {member.rank === 1 ? (
          <Crown
            className="hidden size-5 rotate-[-12deg] fill-orange sm:block"
            aria-label="Crew leader"
          />
        ) : null}
      </span>
      <span className="hidden font-mono text-xs text-ink-soft sm:block">
        {formatRecord({ wins: member.wins, losses: member.losses })}
      </span>
      <strong className="text-right font-mono text-sm">
        {member.crewScore.toLocaleString('en-AU')}
      </strong>
    </li>
  )
}

function DashboardAvatar({
  person,
  className,
  fallbackClassName,
}: {
  person: Pick<
    DoublesPairingPlayerHttpResponse,
    'userId' | 'displayName' | 'profilePictureUrl'
  >
  className: string
  fallbackClassName: string
}) {
  return (
    <Avatar className={className}>
      {person.profilePictureUrl ? (
        <AvatarImage
          src={person.profilePictureUrl}
          alt={person.displayName}
        />
      ) : null}
      <AvatarFallback
        className={cn(
          'font-black',
          getAvatarStyle({ userId: person.userId }),
          fallbackClassName,
        )}
      >
        {getInitials({ displayName: person.displayName })}
      </AvatarFallback>
    </Avatar>
  )
}

function getAvatarStyle({ userId }: { userId: string }): string {
  const colorIndex = [...userId].reduce(
    (hash, character) => hash + character.charCodeAt(0),
    0,
  )

  return avatarStyles[colorIndex % avatarStyles.length] ?? avatarStyles[0]
}

function SectionHeading({
  eyebrow,
  title,
  description,
  headingId,
}: {
  eyebrow: string
  title: string
  description: string
  headingId: string
}) {
  return (
    <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
      <div className="flex flex-col gap-2">
        <span className="text-[10px] font-black tracking-[.15em] text-orange uppercase">
          {eyebrow}
        </span>
        <h2
          className="m-0 font-heading text-[clamp(2.3rem,4vw,4.2rem)] leading-[.9] font-black tracking-[-.055em]"
          id={headingId}
        >
          {title}
        </h2>
      </div>
      <p className="m-0 max-w-xl text-sm leading-relaxed text-ink-soft lg:text-right">
        {description}
      </p>
    </div>
  )
}

function PersonalMetricCard({
  className,
  eyebrow,
  value,
  detail,
  icon: Icon,
  iconClassName,
}: {
  className: string
  eyebrow: string
  value: string
  detail: string
  icon: typeof Medal
  iconClassName: string
}) {
  return (
    <Card
      className={cn(
        'dashboard-stat-card min-h-[190px] border-2 border-ink',
        className,
      )}
    >
      <CardHeader>
        <CardTitle className="text-[9px] font-black tracking-[.14em] uppercase">
          {eyebrow}
        </CardTitle>
        <CardAction
          className={cn(
            'grid size-10 rotate-[6deg] place-items-center rounded-full border-2 border-current [&_svg]:size-5',
            iconClassName,
          )}
        >
          <Icon aria-hidden="true" />
        </CardAction>
      </CardHeader>
      <CardContent className="mt-auto gap-1">
        <strong className="font-heading text-6xl leading-none font-black tracking-[-.065em]">
          {value}
        </strong>
        <span className="text-[10px] font-bold tracking-[.05em] uppercase opacity-75">
          {detail}
        </span>
      </CardContent>
    </Card>
  )
}
