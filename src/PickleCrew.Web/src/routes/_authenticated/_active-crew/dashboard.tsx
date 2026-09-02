import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import { cva, type VariantProps } from 'class-variance-authority'
import type { ComponentProps, ReactNode } from 'react'
import {
  BrickWall,
  CircleAlert,
  Crown,
  Flame,
  Hammer,
  Hand,
  Handshake,
  Heart,
  History,
  Medal,
  Skull,
  Snowflake,
  Swords,
  Target,
  Trophy,
  TrendingDown,
  TrendingUp,
  UsersRound,
  Zap,
  type LucideIcon,
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
  RivalPlayerHttpResponse,
} from '~/lib/api/generated/types.gen'
import { getDashboard } from '~/lib/dashboard/dashboard.functions'
import {
  formatAveragePoints,
  formatAveragePointsConceded,
  formatCount,
  formatPercentage,
  formatRecord,
  formatSignedNumber,
  formatStreakPeriod,
  getBestPartnerPresentation,
  getCurrentStreakDetail,
  getFriendsOrLoversCardDetails,
  getInitials,
  type BestPartnerPresentation,
  type DashboardData,
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

const dashboardStatCardVariants = cva(
  'dashboard-stat-card relative border-2 border-ink',
  {
    variants: {
      tone: {
        aqua: 'bg-aqua text-ink shadow-[7px_8px_0_var(--ink)]',
        cream: 'bg-cream text-ink shadow-[7px_8px_0_var(--ink)]',
        ink: 'bg-ink text-cream shadow-[7px_8px_0_var(--orange)]',
        lime: 'bg-lime text-ink shadow-[7px_8px_0_var(--ink)]',
        orange: 'bg-orange text-ink shadow-[7px_8px_0_var(--ink)]',
        paper: 'bg-paper text-ink shadow-[7px_8px_0_var(--ink)]',
        purple: 'bg-purple text-white shadow-[7px_8px_0_var(--ink)]',
      },
    },
    defaultVariants: {
      tone: 'cream',
    },
  },
)

const dashboardCardIconVariants = cva(
  'grid place-items-center rounded-full border-2 [&_svg]:size-5',
  {
    variants: {
      tone: {
        aqua:
          'border-ink bg-aqua text-ink shadow-[3px_4px_0_var(--ink)]',
        'aqua-on-dark':
          'border-white bg-aqua text-ink shadow-[3px_4px_0_var(--ink)]',
        cream:
          'border-ink bg-cream text-ink shadow-[3px_4px_0_var(--ink)]',
        lime: 'border-ink bg-lime text-ink shadow-[3px_4px_0_var(--ink)]',
        orange:
          'border-ink bg-orange text-ink shadow-[3px_4px_0_var(--ink)]',
        'orange-on-dark':
          'border-cream bg-orange text-ink shadow-[3px_4px_0_var(--cream)]',
        purple:
          'border-ink bg-purple text-white shadow-[3px_4px_0_var(--ink)]',
      },
      size: {
        default: 'size-11',
        large: 'size-12 [&_svg]:size-6',
        small: 'size-10 shadow-[2px_3px_0_var(--ink)]',
      },
      tilt: {
        clockwise: 'rotate-[6deg]',
        counterclockwise: 'rotate-[-6deg]',
        none: '',
      },
    },
    defaultVariants: {
      size: 'default',
      tilt: 'clockwise',
    },
  },
)

const dashboardDecorativeIconVariants = cva(
  'pointer-events-none absolute -right-8 -bottom-10 rotate-[14deg] opacity-10',
  {
    variants: {
      direction: {
        clockwise: '',
        counterclockwise: 'rotate-[-14deg]',
      },
      layer: {
        background: '',
        raised: 'z-2',
      },
      size: {
        large: 'size-36 sm:size-48',
        small: 'size-36',
      },
    },
    defaultVariants: {
      direction: 'clockwise',
      layer: 'background',
      size: 'large',
    },
  },
)

const dashboardCardTextVariants = cva('', {
  variants: {
    role: {
      body: 'text-sm',
      description: 'font-heading text-[11px] text-current opacity-70',
      emptyTitle: 'font-heading text-3xl font-black',
      heading:
        'font-heading text-3xl font-black tracking-[-.045em] sm:text-4xl',
      heroMetric:
        'block font-heading text-[clamp(6.5rem,13vw,10rem)] leading-[.68] font-black tracking-[-.1em]',
      label: 'font-heading text-base font-black',
      metadata: 'text-[9px] font-black tracking-[.08em] uppercase',
      metric:
        'block font-heading text-6xl leading-[.72] font-black tracking-[-.08em]',
      title: 'text-xs font-black tracking-[.14em] uppercase',
    },
  },
})

const dashboardAvatarFallbackVariants = cva('font-heading font-black', {
  variants: {
    size: {
      default: 'text-lg',
      small: 'text-sm',
    },
  },
  defaultVariants: {
    size: 'default',
  },
})

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
              Statistics for the current crew appear here.
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
    nemesis: nemesisResponse,
    recentGames,
    leaderboard,
    longestWinStreak: longestWinStreakResponse,
    bestDoublesPairing,
    bestPartner: bestPartnerResponse,
    biggestRivals,
    brickWall: brickWallResponse,
    chiefPaddler: chiefPaddlerResponse,
    iceMan: iceManResponse,
    friendsOrLovers,
    limpWristSyndrome: limpWristSyndromeResponse,
    pointMachine: pointMachineResponse,
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
  const rivalry = biggestRivals.rivalry
  const firstRival = rivalry?.players[0]
  const secondRival = rivalry?.players[1]
  const brickWall = brickWallResponse.brickWall
  const chiefPaddler = chiefPaddlerResponse.chiefPaddler
  const iceMan = iceManResponse.iceMan
  const longestWinStreak = longestWinStreakResponse.longestWinStreak
  const bestPartner = getBestPartnerPresentation({
    bestPartner: bestPartnerResponse.bestPartner,
  })
  const friendsOrLoversDetails = getFriendsOrLoversCardDetails({
    friendsOrLovers,
  })
  const nemesis = nemesisResponse.nemesis
  const limpWristSyndrome = limpWristSyndromeResponse.limpWristSyndrome
  const pointMachine = pointMachineResponse.pointMachine

  if (
    pairing &&
    (pairing.players.length !== 2 ||
      !firstPairingPlayer ||
      !secondPairingPlayer)
  ) {
    throw new Error('A doubles pairing must contain exactly two players.')
  }

  if (
    rivalry &&
    (rivalry.players.length !== 2 || !firstRival || !secondRival)
  ) {
    throw new Error('A rivalry must contain exactly two players.')
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
              <WinPercentageCard
                personalRecord={personalRecord}
                personalStatistics={personalStatistics}
                progressPercentage={progressPercentage}
                winPercentage={winPercentage}
              />

              <CrewRankCard statistics={personalStatistics} />
              <CurrentStreakCard statistics={personalStatistics} />
              <PointDifferentialCard statistics={personalStatistics} />

              <RecentGamesCard
                recentGameCount={recentGameCount}
                recentGames={recentGames}
                recentRecord={recentRecord}
              />

              <NemesisCard nemesis={nemesis} />

              <ThatsAPaddlinCard statistics={personalStatistics} />

              <BestPartnerCard bestPartner={bestPartner} />
            </div>
          ) : (
            <Card className="relative min-h-[310px] overflow-hidden border-2 border-dashed border-ink/30 bg-cream">
              <CardContent className="relative z-1 my-auto items-center justify-center gap-2 text-center">
                <strong
                  className={dashboardCardTextVariants({ role: 'emptyTitle' })}
                >
                  No personal receipts yet
                </strong>
                <p
                  className={cn(
                    dashboardCardTextVariants({ role: 'body' }),
                    'm-0 max-w-sm text-ink-soft',
                  )}
                >
                  Get on the court, play a game, and start building a record
                  worth bragging about.
                </p>
              </CardContent>
              <DashboardDecorativeIcon icon={Target} />
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
            <CrewLeaderboardCard
              currentUserId={user.id}
              hasLeaderboardGames={hasLeaderboardGames}
              leaderboard={leaderboard}
            />

            <BestDoublesPairingCard
              firstPairingPlayer={firstPairingPlayer}
              pairing={pairing}
              secondPairingPlayer={secondPairingPlayer}
            />

            <FriendsOrLoversCard details={friendsOrLoversDetails} />

            <BiggestRivalsCard
              firstRival={firstRival}
              rivalry={rivalry}
              secondRival={secondRival}
            />

            <PointMachineCard pointMachine={pointMachine} />

            <LimpWristSyndromeCard
              limpWristSyndrome={limpWristSyndrome}
            />

            <BrickWallCard brickWall={brickWall} />

            <IceManCard iceMan={iceMan} />

            <LongestWinStreakCard longestWinStreak={longestWinStreak} />

            <ChiefPaddlerCard chiefPaddler={chiefPaddler} />
          </div>
        </section>
      </div>
    </main>
  )
}

function DashboardStatCard({
  className,
  tone,
  ...props
}: ComponentProps<typeof Card> &
  VariantProps<typeof dashboardStatCardVariants>) {
  return (
    <Card
      className={cn(dashboardStatCardVariants({ tone }), className)}
      {...props}
    />
  )
}

function DashboardCardHeader({
  action,
  className,
  description,
  icon: Icon,
  iconSize,
  iconTone,
  title,
  titleVariant = 'label',
  tilt,
}: {
  action?: ReactNode
  className?: string
  description: ReactNode
  icon?: LucideIcon
  iconSize?: VariantProps<typeof dashboardCardIconVariants>['size']
  iconTone?: VariantProps<typeof dashboardCardIconVariants>['tone']
  title: ReactNode
  titleVariant?: 'heading' | 'label'
  tilt?: VariantProps<typeof dashboardCardIconVariants>['tilt']
}) {
  if (Icon && !iconTone) {
    throw new Error('A dashboard card icon must specify an icon tone.')
  }

  return (
    <CardHeader className={className}>
      <CardTitle
        className={dashboardCardTextVariants({
          role: titleVariant === 'label' ? 'title' : 'heading',
        })}
      >
        {title}
      </CardTitle>
      <CardDescription
        className={dashboardCardTextVariants({ role: 'description' })}
      >
        {description}
      </CardDescription>
      {Icon && iconTone ? (
        <CardAction
          className={dashboardCardIconVariants({
            size: iconSize,
            tilt,
            tone: iconTone,
          })}
        >
          <Icon aria-hidden="true" />
        </CardAction>
      ) : action ? (
        <CardAction>{action}</CardAction>
      ) : null}
    </CardHeader>
  )
}

function DashboardDecorativeIcon({
  direction,
  icon: Icon,
  layer,
  size,
}: {
  direction?: VariantProps<typeof dashboardDecorativeIconVariants>['direction']
  icon: LucideIcon
  layer?: VariantProps<typeof dashboardDecorativeIconVariants>['layer']
  size?: VariantProps<typeof dashboardDecorativeIconVariants>['size']
}) {
  return (
    <Icon
      className={dashboardDecorativeIconVariants({ direction, layer, size })}
      aria-hidden="true"
    />
  )
}

function DashboardCardEmptyState({
  className,
  description,
  title,
}: {
  className?: string
  description: ReactNode
  title: ReactNode
}) {
  return (
    <CardContent
      className={cn(
        'relative z-1 my-auto items-center justify-center gap-2 text-center',
        className,
      )}
    >
      <strong className={dashboardCardTextVariants({ role: 'emptyTitle' })}>
        {title}
      </strong>
      <p
        className={cn(
          dashboardCardTextVariants({ role: 'body' }),
          'm-0 max-w-xs text-current opacity-70',
        )}
      >
        {description}
      </p>
    </CardContent>
  )
}

function DashboardPersonMetric({
  avatarFallbackClassName,
  metadata,
  metric,
  metricLabel,
  person,
}: {
  avatarFallbackClassName: string
  metadata: ReactNode
  metric: ReactNode
  metricLabel: ReactNode
  person: Pick<
    DoublesPairingPlayerHttpResponse,
    'userId' | 'displayName' | 'profilePictureUrl'
  >
}) {
  return (
    <>
      <CardContent className="relative z-3">
        <div>
          <strong className={dashboardCardTextVariants({ role: 'metric' })}>
            {metric}
          </strong>
          <span
            className={cn(
              dashboardCardTextVariants({ role: 'label' }),
              'whitespace-nowrap',
            )}
          >
            {metricLabel}
          </span>
        </div>
      </CardContent>
      <CardFooter className="relative z-3 mt-auto items-center gap-3">
        <DashboardAvatar
          className="size-20 rotate-[-5deg] border-2 border-ink shadow-[3px_4px_0_var(--ink)]"
          fallbackClassName={avatarFallbackClassName}
          person={person}
        />
        <div className="flex min-w-0 flex-col gap-1">
          <strong
            className={cn(
              dashboardCardTextVariants({ role: 'label' }),
              'max-w-full truncate',
            )}
          >
            {person.displayName}
          </strong>
          <span className={dashboardCardTextVariants({ role: 'metadata' })}>
            {metadata}
          </span>
        </div>
      </CardFooter>
    </>
  )
}

function WinPercentageCard({
  personalRecord,
  personalStatistics,
  progressPercentage,
  winPercentage,
}: {
  personalRecord: string
  personalStatistics: DashboardData['personalStatistics']
  progressPercentage: number
  winPercentage: number
}) {
  return (
    <DashboardStatCard
      className="dashboard-stat-card-lime min-h-[310px] sm:col-span-2 xl:col-span-5 xl:row-span-2"
      tone="lime"
    >
      <DashboardCardHeader
        description={
          <>
            {formatCount({
              count: personalStatistics.wins,
              singular: 'win',
            })}{' '}
            from{' '}
            {formatCount({
              count: personalStatistics.gamesPlayed,
              singular: 'game',
            })}
          </>
        }
        icon={Target}
        iconSize="large"
        iconTone="cream"
        title="Your win percentage"
      />
      <CardContent className="relative z-1 mt-auto gap-5">
        <strong
          className={dashboardCardTextVariants({ role: 'heroMetric' })}
        >
          {winPercentage}
          <span className="ml-2 text-[.25em] tracking-[-.03em]">%</span>
        </strong>
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
      <CardFooter className="relative z-1 justify-end">
        <span
          className={cn(
            dashboardCardTextVariants({ role: 'metadata' }),
            'text-right',
          )}
        >
          {personalRecord} all time
        </span>
      </CardFooter>
      <DashboardDecorativeIcon icon={Target} />
    </DashboardStatCard>
  )
}

function RecentGamesCard({
  recentGameCount,
  recentGames,
  recentRecord,
}: {
  recentGameCount: number
  recentGames: DashboardData['recentGames']
  recentRecord: string
}) {
  return (
    <DashboardStatCard className="sm:col-span-2 xl:col-span-4" tone="cream">
      <DashboardCardHeader
        description={
          recentGameCount === 0
            ? 'No recent games'
            : `${recentRecord} · last ${recentGameCount}`
        }
        icon={History}
        iconSize="small"
        iconTone="orange"
        tilt="counterclockwise"
        title="Last 10 games"
      />
      <CardContent className="relative z-1">
        {recentGames.winStreak > 1 ? (
          <Badge className="w-fit bg-lime text-ink">
            <Flame data-icon="inline-start" />
            {recentGames.winStreak}-game win streak
          </Badge>
        ) : null}
        {recentGameCount > 0 ? (
          <div
            className="flex flex-wrap gap-2"
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
          <p
            className={cn(
              dashboardCardTextVariants({ role: 'body' }),
              'm-0 text-muted-foreground',
            )}
          >
            Record a game to start your recent form.
          </p>
        )}
      </CardContent>
      <CardFooter className="relative z-1">
        <span
          className={cn(
            dashboardCardTextVariants({ role: 'metadata' }),
            'text-muted-foreground',
          )}
        >
          Newest game first
        </span>
      </CardFooter>
      <DashboardDecorativeIcon icon={History} size="small" />
    </DashboardStatCard>
  )
}

function NemesisCard({
  nemesis,
}: {
  nemesis: DashboardData['nemesis']['nemesis']
}) {
  return (
    <DashboardStatCard
      className="min-h-[190px] overflow-hidden sm:col-span-2 xl:col-span-4"
      tone="orange"
    >
      <DashboardCardHeader
        description="The opponent who's handed you the most losses"
        icon={Skull}
        iconSize="small"
        iconTone="cream"
        tilt="counterclockwise"
        title="Your nemesis"
      />
      {nemesis ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-purple text-white"
          metadata="They've got your number"
          metric={nemesis.losses.toLocaleString('en-AU')}
          metricLabel={`${nemesis.losses === 1 ? 'loss' : 'losses'} to them`}
          person={nemesis}
        />
      ) : (
        <DashboardCardEmptyState
          description="Play at least three games against an active crew member to reveal your nemesis."
          title="Nobody owns you yet"
        />
      )}
      <DashboardDecorativeIcon icon={Skull} size="small" />
    </DashboardStatCard>
  )
}

function CrewLeaderboardCard({
  currentUserId,
  hasLeaderboardGames,
  leaderboard,
}: {
  currentUserId: string
  hasLeaderboardGames: boolean
  leaderboard: DashboardData['leaderboard']
}) {
  return (
    <DashboardStatCard
      className="xl:col-span-7 xl:row-span-2 xl:row-start-1"
      tone="cream"
    >
      <DashboardCardHeader
        className="border-b-2 border-line pb-8!"
        description="Ranked by totally indisputable crew score."
        icon={Trophy}
        iconSize="large"
        iconTone="orange"
        title="Crew leaderboard"
        titleVariant="heading"
      />
      {hasLeaderboardGames ? (
        <CardContent className="relative z-1 gap-0">
          <div className="grid grid-cols-[34px_1fr_auto] gap-3 px-3 pb-2 text-[8px] font-black tracking-[.12em] text-muted-foreground uppercase sm:grid-cols-[42px_1fr_78px_72px]">
            <span>Rank</span>
            <span>Player</span>
            <span className="hidden sm:block">Record</span>
            <span className="text-right">Score</span>
          </div>
          <ol className="m-0 flex list-none flex-col gap-2 p-0">
            {leaderboard.entries.map((member) => (
              <LeaderboardEntry
                currentUserId={currentUserId}
                member={member}
                key={member.userId}
              />
            ))}
          </ol>
        </CardContent>
      ) : (
        <DashboardCardEmptyState
          className="[&_p]:max-w-sm"
          description="Get on the court, record a game, and give the crew something to argue about."
          title="No leaderboard drama yet"
        />
      )}
      <DashboardDecorativeIcon icon={Trophy} />
    </DashboardStatCard>
  )
}

function BestDoublesPairingCard({
  firstPairingPlayer,
  pairing,
  secondPairingPlayer,
}: {
  firstPairingPlayer: DoublesPairingPlayerHttpResponse | undefined
  pairing: DashboardData['bestDoublesPairing']['pairing']
  secondPairingPlayer: DoublesPairingPlayerHttpResponse | undefined
}) {
  return (
    <DashboardStatCard
      className="dashboard-pair-card min-h-[330px] xl:col-span-6 xl:row-start-3"
      tone="purple"
    >
      <DashboardCardHeader
        description="The duo nobody wants across the net"
        icon={UsersRound}
        iconTone="aqua-on-dark"
        tilt="none"
        title="Best doubles pairing"
      />
      {pairing && firstPairingPlayer && secondPairingPlayer ? (
        <>
          <CardContent className="relative z-1 items-center gap-3 text-center">
            <div className="flex items-center justify-center">
              <DashboardAvatar
                className="z-1 size-20 rotate-[-7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                person={firstPairingPlayer}
              />
              <span
                className={cn(
                  dashboardCardTextVariants({ role: 'label' }),
                  'z-2 -mx-2 grid size-10 rotate-[6deg] place-items-center rounded-full border-2 border-ink bg-cream text-ink',
                )}
                aria-hidden="true"
              >
                +
              </span>
              <DashboardAvatar
                className="z-1 size-20 rotate-[7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                person={secondPairingPlayer}
              />
            </div>
            <strong className={dashboardCardTextVariants({ role: 'label' })}>
              {firstPairingPlayer.displayName} +{' '}
              {secondPairingPlayer.displayName}
            </strong>
            <div className="flex items-baseline gap-2">
              <strong
                className={cn(
                  dashboardCardTextVariants({ role: 'metric' }),
                  'text-lime',
                )}
              >
                {formatPercentage({ value: pairing.winPercentage })}%
              </strong>
              <span
                className={dashboardCardTextVariants({ role: 'metadata' })}
              >
                win rate
              </span>
            </div>
          </CardContent>
          <CardFooter className="relative z-1 mt-auto justify-center">
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
        <DashboardCardEmptyState
          description="Grab a partner, hit the court, and start proving which duo runs this crew."
          title="No doubles record yet"
        />
      )}
      <DashboardDecorativeIcon icon={UsersRound} />
    </DashboardStatCard>
  )
}

function FriendsOrLoversCard({
  details: friendsOrLoversDetails,
}: {
  details: ReturnType<typeof getFriendsOrLoversCardDetails>
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:row-start-3"
      tone="lime"
    >
      <DashboardCardHeader
        className="relative z-1"
        description="The pair that keeps choosing each other"
        icon={Heart}
        iconTone="orange"
        tilt="counterclockwise"
        title="Friends or lovers?"
      />
      {friendsOrLoversDetails.type === 'pairing' ? (
        <>
          <CardContent className="relative z-1 items-center gap-4 text-center">
            <div className="flex items-center justify-center">
              <DashboardAvatar
                className="z-1 size-20 rotate-[-7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                person={friendsOrLoversDetails.firstPlayer}
              />
              <Heart
                className="z-2 -mx-2 size-10 rotate-[6deg] rounded-full border-2 border-ink bg-cream p-2 fill-orange text-orange"
                aria-hidden="true"
              />
              <DashboardAvatar
                className="z-1 size-20 rotate-[7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                person={friendsOrLoversDetails.secondPlayer}
              />
            </div>
            <strong className={dashboardCardTextVariants({ role: 'label' })}>
              {friendsOrLoversDetails.firstPlayer.displayName} +{' '}
              {friendsOrLoversDetails.secondPlayer.displayName}
            </strong>
            <div className="flex flex-col items-center gap-1">
              <strong className={dashboardCardTextVariants({ role: 'metric' })}>
                {friendsOrLoversDetails.gamesPlayed.toLocaleString(
                  'en-AU',
                )}
              </strong>
              <span
                className={dashboardCardTextVariants({ role: 'metadata' })}
              >
                {friendsOrLoversDetails.gamesTogetherLabel}
              </span>
            </div>
          </CardContent>
        </>
      ) : (
        <DashboardCardEmptyState
          description="Play at least three doubles games with another crew member to start tracking who keeps showing up together."
          title="No doubles partners yet"
        />
      )}
      <DashboardDecorativeIcon icon={Heart} />
    </DashboardStatCard>
  )
}

function BiggestRivalsCard({
  firstRival,
  rivalry,
  secondRival,
}: {
  firstRival: RivalPlayerHttpResponse | undefined
  rivalry: DashboardData['biggestRivals']['rivalry']
  secondRival: RivalPlayerHttpResponse | undefined
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:row-start-4"
      tone="ink"
    >
      <DashboardCardHeader
        description="The closest all-time head-to-head in the crew"
        icon={Swords}
        iconTone="orange-on-dark"
        title="Biggest rivals"
      />
      {rivalry && firstRival && secondRival ? (
        <>
          <CardContent className="relative z-1 items-center gap-3 text-center">
            <div className="flex items-center justify-center">
              <DashboardAvatar
                className="relative z-1 size-20 rotate-[-7deg] border-2 border-cream shadow-[4px_5px_0_var(--orange)]"
                person={firstRival}
              />
              <span
                className={cn(
                  dashboardCardTextVariants({ role: 'label' }),
                  'relative z-2 -mx-2 grid size-11 rotate-[6deg] place-items-center rounded-full border-2 border-cream bg-lime text-ink',
                )}
                aria-hidden="true"
              >
                VS
              </span>
              <DashboardAvatar
                className="relative z-1 size-20 rotate-[7deg] border-2 border-cream shadow-[4px_5px_0_var(--orange)]"
                person={secondRival}
              />
            </div>
            <strong className={dashboardCardTextVariants({ role: 'label' })}>
              {firstRival.displayName} vs {secondRival.displayName}
            </strong>
            <strong
              className={cn(
                dashboardCardTextVariants({ role: 'metric' }),
                'text-orange',
              )}
            >
              {formatRecord({
                wins: firstRival.wins,
                losses: firstRival.losses,
              })}
            </strong>
          </CardContent>
          <CardFooter className="relative z-1 mt-auto justify-center">
            <Badge
              className="rotate-[-2deg] border border-cream bg-transparent px-3 text-cream"
              variant="outline"
            >
              {formatCount({
                count: rivalry.gamesPlayed,
                singular: 'game',
              })}{' '}
              head-to-head
            </Badge>
          </CardFooter>
        </>
      ) : (
        <DashboardCardEmptyState
          description="Record a game between crew members and let the scorekeeping begin."
          title="No rivalry yet"
        />
      )}
      <DashboardDecorativeIcon icon={Swords} />
    </DashboardStatCard>
  )
}

function PointMachineCard({
  pointMachine,
}: {
  pointMachine: DashboardData['pointMachine']['pointMachine']
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:col-start-7 xl:row-start-5"
      tone="orange"
    >
      <DashboardCardHeader
        description="Highest all-time average points scored per game"
        icon={Zap}
        iconTone="lime"
        tilt="counterclockwise"
        title="Point Machine"
      />
      {pointMachine ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-purple text-white"
          metadata={`${formatCount({ count: pointMachine.pointsScored, singular: 'point' })} across ${formatCount({ count: pointMachine.gamesPlayed, singular: 'game' })}`}
          metric={formatAveragePoints({
            value: pointMachine.averagePointsScored,
          })}
          metricLabel="points per game"
          person={pointMachine}
        />
      ) : (
        <DashboardCardEmptyState
          description="Play at least three games and somebody will set the pace."
          title="No Point Machine yet"
        />
      )}
      <DashboardDecorativeIcon icon={Zap} />
    </DashboardStatCard>
  )
}

function LimpWristSyndromeCard({
  limpWristSyndrome,
}: {
  limpWristSyndrome: DashboardData['limpWristSyndrome']['limpWristSyndrome']
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:col-start-7 xl:row-start-4"
      tone="paper"
    >
      <DashboardCardHeader
        description="Most all time losses in the crew. They’ve got a bad case of limp wrist syndrome."
        icon={Hand}
        iconTone="orange"
        tilt="counterclockwise"
        title="Limp wrist syndrome"
      />
      {limpWristSyndrome ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-aqua text-ink"
          metadata={`Point differential ${formatSignedNumber({ value: limpWristSyndrome.pointDifferential })}`}
          metric={limpWristSyndrome.losses.toLocaleString('en-AU')}
          metricLabel={limpWristSyndrome.losses === 1 ? 'loss' : 'losses'}
          person={limpWristSyndrome}
        />
      ) : (
        <DashboardCardEmptyState
          description="Record a game and somebody will claim it."
          title="Nobody has lost yet"
        />
      )}
      <DashboardDecorativeIcon
        direction="counterclockwise"
        icon={Hand}
      />
    </DashboardStatCard>
  )
}

function BrickWallCard({
  brickWall,
}: {
  brickWall: DashboardData['brickWall']['brickWall']
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:row-start-5"
      tone="aqua"
    >
      <DashboardCardHeader
        description="Lowest average points conceded per game"
        icon={BrickWall}
        iconTone="orange"
        title="Brick wall"
      />
      {brickWall ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-purple text-white"
          metadata={
            <>
              {formatCount({
                count: brickWall.pointsConceded,
                singular: 'point',
              })}{' '}
              conceded across{' '}
              {formatCount({
                count: brickWall.gamesPlayed,
                singular: 'game',
              })}
            </>
          }
          metric={formatAveragePointsConceded({
            value: brickWall.averagePointsConceded,
          })}
          metricLabel="points conceded per game"
          person={brickWall}
        />
      ) : (
        <DashboardCardEmptyState
          description="Play at least three games and see who gives away the fewest points."
          title="No wall yet"
        />
      )}
      <DashboardDecorativeIcon icon={BrickWall} />
    </DashboardStatCard>
  )
}

function IceManCard({
  iceMan,
}: {
  iceMan: DashboardData['iceMan']['iceMan']
}) {
  return (
    <DashboardStatCard
      className="min-h-[330px] overflow-hidden xl:col-span-6 xl:row-start-6"
      tone="purple"
    >
      <DashboardCardHeader
        description="The clutch win master. Most wins with the other side scoring 9 or more."
        icon={Snowflake}
        iconTone="aqua-on-dark"
        title="The Ice Man"
      />
      {iceMan ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-orange text-ink"
          metadata="Ice in their veins"
          metric={iceMan.clutchWins.toLocaleString('en-AU')}
          metricLabel={iceMan.clutchWins === 1 ? 'clutch win' : 'clutch wins'}
          person={iceMan}
        />
      ) : (
        <DashboardCardEmptyState
          description="Win a game after the other side reaches 9 points to claim it."
          title="No clutch wins yet"
        />
      )}
      <DashboardDecorativeIcon icon={Snowflake} />
    </DashboardStatCard>
  )
}

function LongestWinStreakCard({
  longestWinStreak,
}: {
  longestWinStreak: DashboardData['longestWinStreak']['longestWinStreak']
}) {
  return (
    <DashboardStatCard
      className="dashboard-streak-card min-h-[190px] overflow-hidden xl:col-span-5 xl:col-start-8 xl:row-start-1"
      tone="orange"
    >
      <DashboardCardHeader
        description="The record to chase"
        icon={Flame}
        iconSize="small"
        iconTone="lime"
        tilt="counterclockwise"
        title="Longest crew win streak"
      />
      {longestWinStreak ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-cream text-ink"
          metadata={formatStreakPeriod({
            startedAtUtc: longestWinStreak.startedAtUtc,
            endedAtUtc: longestWinStreak.endedAtUtc,
          })}
          metric={longestWinStreak.wins}
          metricLabel={`straight ${longestWinStreak.wins === 1 ? 'win' : 'wins'}`}
          person={{
            userId: longestWinStreak.userId,
            displayName: longestWinStreak.displayName,
            profilePictureUrl: longestWinStreak.profilePictureUrl ?? null,
          }}
        />
      ) : (
        <DashboardCardEmptyState
          description="Get on the court and start proving who runs this crew."
          title="No win streak yet"
        />
      )}
      <DashboardDecorativeIcon icon={Flame} layer="raised" />
    </DashboardStatCard>
  )
}

function ChiefPaddlerCard({
  chiefPaddler,
}: {
  chiefPaddler: DashboardData['chiefPaddler']['chiefPaddler']
}) {
  return (
    <DashboardStatCard
      className="min-h-[190px] overflow-hidden xl:col-span-5 xl:col-start-8 xl:row-start-2"
      tone="aqua"
    >
      <DashboardCardHeader
        description="Most 11–0 wins in the crew"
        icon={Crown}
        iconSize="small"
        iconTone="purple"
        tilt="counterclockwise"
        title="Chief Paddler"
      />
      {chiefPaddler ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-lime text-ink"
          metadata="Reigning Chief"
          metric={chiefPaddler.thatsAPaddlin.toLocaleString('en-AU')}
          metricLabel="paddlins delivered"
          person={{
            userId: chiefPaddler.userId,
            displayName: chiefPaddler.displayName,
            profilePictureUrl: chiefPaddler.profilePictureUrl ?? null,
          }}
        />
      ) : (
        <DashboardCardEmptyState
          description="Hand out an 11–0 paddlin and claim it."
          title="The throne's empty"
        />
      )}
      <DashboardDecorativeIcon icon={Crown} size="large" />
    </DashboardStatCard>
  )
}

function BestPartnerCard({
  bestPartner,
}: {
  bestPartner: BestPartnerPresentation
}) {
  return (
    <DashboardStatCard
      className="min-h-[190px] overflow-hidden sm:col-span-2 xl:col-span-4"
      tone="paper"
    >
      <DashboardCardHeader
        description="You two have a special bond. Apparently it wins games."
        icon={Handshake}
        iconSize="small"
        iconTone="purple"
        tilt="counterclockwise"
        title="Best partner"
      />
      {bestPartner.available ? (
        <DashboardPersonMetric
          avatarFallbackClassName="bg-aqua text-ink"
          metadata={
            <>
              {formatCount({
                count: bestPartner.partner.wins,
                singular: 'win',
              })}{' '}
              ·{' '}
              {formatCount({
                count: bestPartner.partner.losses,
                singular: 'loss',
                plural: 'losses',
              })}
            </>
          }
          metric={`${bestPartner.winPercentage}%`}
          metricLabel="win rate"
          person={bestPartner.partner}
        />
      ) : (
        <DashboardCardEmptyState
          description={bestPartner.description}
          title={bestPartner.title}
        />
      )}
      <DashboardDecorativeIcon
        direction="counterclockwise"
        icon={Handshake}
        size="small"
      />
    </DashboardStatCard>
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
          fallbackSize="small"
          person={member}
        />
        <span className="flex min-w-0 flex-col">
          <strong className="truncate font-heading text-sm sm:text-base">
            {member.displayName}
            {isCurrentUser ? <span className="sr-only"> (you)</span> : null}
          </strong>
          <span className="font-mono text-[10px] text-ink-soft sm:hidden">
            {formatCount({ count: member.wins, singular: 'win' })} -{' '}
            {formatCount({
              count: member.losses,
              singular: 'loss',
              plural: 'losses',
            })}
          </span>
        </span>
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
  fallbackSize,
}: {
  person: Pick<
    DoublesPairingPlayerHttpResponse,
    'userId' | 'displayName' | 'profilePictureUrl'
  >
  className: string
  fallbackClassName?: string
  fallbackSize?: VariantProps<typeof dashboardAvatarFallbackVariants>['size']
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
          dashboardAvatarFallbackVariants({ size: fallbackSize }),
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

function CrewRankCard({
  statistics,
}: {
  statistics: DashboardData['personalStatistics']
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-3"
      description="Your place on the crew leaderboard"
      detail={`of ${formatCount({
        count: statistics.activeCrewMemberCount,
        singular: 'player',
      })}`}
      eyebrow="Crew rank"
      icon={Medal}
      iconTone="orange"
      tone="ink"
      value={`#${statistics.crewRank}`}
    />
  )
}

function CurrentStreakCard({
  statistics,
}: {
  statistics: DashboardData['personalStatistics']
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-4"
      description="Consecutive wins through your latest game"
      detail={getCurrentStreakDetail({
        currentWinStreak: statistics.currentWinStreak,
        longestWinStreak: statistics.longestWinStreak,
      })}
      eyebrow="Current streak"
      icon={Flame}
      iconTone="lime"
      tone="orange"
      value={
        statistics.currentWinStreak === 0
          ? 'Your move'
          : `${statistics.currentWinStreak} W`
      }
    />
  )
}

function PointDifferentialCard({
  statistics,
}: {
  statistics: DashboardData['personalStatistics']
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-3"
      description="Points scored minus points conceded"
      detail={`${statistics.pointsScored.toLocaleString('en-AU')} for · ${statistics.pointsConceded.toLocaleString('en-AU')} against`}
      eyebrow="Point differential"
      icon={
        statistics.pointDifferential < 0 ? TrendingDown : TrendingUp
      }
      iconTone="cream"
      tone="aqua"
      value={formatSignedNumber({ value: statistics.pointDifferential })}
    />
  )
}

function ThatsAPaddlinCard({
  statistics,
}: {
  statistics: DashboardData['personalStatistics']
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-4"
      description="11–0 wins. Zero points. Zero mercy."
      emptyState={
        statistics.thatsAPaddlin === 0
          ? "No paddlins yet. Court's waiting. Go obliterate somebody."
          : undefined
      }
      eyebrow="That's a Paddlin"
      icon={Hammer}
      iconTone="lime"
      tone="purple"
      value={statistics.thatsAPaddlin.toLocaleString('en-AU')}
    />
  )
}

function PersonalMetricCard({
  className,
  eyebrow,
  description,
  value,
  detail,
  emptyState,
  icon,
  iconTone,
  tone,
}: {
  className: string
  eyebrow: string
  description: string
  value: string
  detail?: string
  emptyState?: string
  icon: LucideIcon
  iconTone: NonNullable<
    VariantProps<typeof dashboardCardIconVariants>['tone']
  >
  tone: NonNullable<VariantProps<typeof dashboardStatCardVariants>['tone']>
}) {
  return (
    <DashboardStatCard className={cn('min-h-[190px]', className)} tone={tone}>
      <DashboardCardHeader
        description={description}
        icon={icon}
        iconSize="small"
        iconTone={iconTone}
        title={eyebrow}
      />
      <CardContent className="relative z-1 mt-auto gap-1">
        {emptyState ? (
          <p
            className={cn(
              dashboardCardTextVariants({ role: 'body' }),
              'm-0 max-w-xs leading-relaxed font-medium opacity-80',
            )}
          >
            {emptyState}
          </p>
        ) : (
          <>
            <strong className={dashboardCardTextVariants({ role: 'metric' })}>
              {value}
            </strong>
            {detail ? (
              <span
                className={cn(
                  dashboardCardTextVariants({ role: 'metadata' }),
                  'opacity-75',
                )}
              >
                {detail}
              </span>
            ) : null}
          </>
        )}
      </CardContent>
      <DashboardDecorativeIcon icon={icon} size="small" />
    </DashboardStatCard>
  )
}
