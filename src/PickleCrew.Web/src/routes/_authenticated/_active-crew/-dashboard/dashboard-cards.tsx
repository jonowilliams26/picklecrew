import { cva, type VariantProps } from 'class-variance-authority'
import {
  Crown,
  Flame,
  Hammer,
  Handshake,
  Heart,
  Medal,
  Skull,
  Swords,
  Target,
  TrendingUp,
  Trophy,
  UserRound,
  UsersRound,
  type LucideIcon,
} from 'lucide-react'
import type { ComponentProps, ReactNode } from 'react'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Badge } from '~/components/ui/badge'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import type {
  CrewLeaderboardEntryHttpResponse,
  DoublesPairingPlayerHttpResponse,
  GetBestDoublesPairingHttpResponse,
  GetBestPartnerHttpResponse,
  GetBiggestRivalsHttpResponse,
  GetChiefPaddlerHttpResponse,
  GetCrewLeaderboardHttpResponse,
  GetCrewLongestWinStreakHttpResponse,
  GetFriendsOrLoversHttpResponse,
  GetNemesisHttpResponse,
  GetPersonalStatisticsHttpResponse,
  GetRecentGamesHttpResponse,
} from '~/lib/api/generated/types.gen'
import {
  formatCount,
  formatPercentage,
  formatRecord,
  formatSignedNumber,
  formatStreakPeriod,
  getBestPartnerPresentation,
  getCurrentStreakDetail,
  getFriendsOrLoversCardDetails,
  getInitials,
} from '~/lib/dashboard/dashboard'
import { cn } from '~/lib/utils'

const avatarStyles = [
  'bg-orange text-ink',
  'bg-lime text-ink',
  'bg-aqua text-ink',
  'bg-purple text-white',
  'bg-paper text-ink',
] as const

const statisticCardVariants = cva(
  'dashboard-stat-card relative overflow-hidden border-2 border-ink',
  {
    variants: {
      tone: {
        paper: 'bg-paper text-ink shadow-[7px_8px_0_var(--ink)]',
        cream: 'bg-cream text-ink shadow-[7px_8px_0_var(--ink)]',
        lime: 'bg-lime text-ink shadow-[7px_8px_0_var(--ink)]',
        orange: 'bg-orange text-ink shadow-[7px_8px_0_var(--ink)]',
        aqua: 'bg-aqua text-ink shadow-[7px_8px_0_var(--ink)]',
        purple: 'bg-purple text-white shadow-[7px_8px_0_var(--ink)]',
        ink: 'bg-ink text-cream shadow-[7px_8px_0_var(--orange)]',
      },
    },
    defaultVariants: {
      tone: 'paper',
    },
  },
)

const statisticCardDescriptionVariants = cva(
  'font-heading text-[11px]',
  {
    variants: {
      tone: {
        muted: 'text-ink-soft',
        light: 'text-white/70',
        cream: 'text-cream/70',
        current: 'text-current opacity-75',
      },
    },
    defaultVariants: {
      tone: 'muted',
    },
  },
)

const statisticCardIconVariants = cva(
  'grid size-11 rotate-[6deg] place-items-center rounded-full border-2 [&_svg]:size-5',
  {
    variants: {
      tone: {
        cream:
          'border-ink bg-cream text-ink shadow-[3px_4px_0_var(--ink)]',
        orange:
          'border-ink bg-orange text-ink shadow-[3px_4px_0_var(--ink)]',
        lime: 'border-ink bg-lime text-ink shadow-[3px_4px_0_var(--ink)]',
        aqua: 'border-ink bg-aqua text-ink shadow-[3px_4px_0_var(--ink)]',
        purple:
          'border-ink bg-purple text-white shadow-[3px_4px_0_var(--ink)]',
        'aqua-inverse':
          'border-white bg-aqua text-ink shadow-[3px_4px_0_var(--ink)]',
        'orange-inverse':
          'border-cream bg-orange text-ink shadow-[3px_4px_0_var(--cream)]',
      },
    },
    defaultVariants: {
      tone: 'cream',
    },
  },
)

type StatisticCardTone = NonNullable<
  VariantProps<typeof statisticCardVariants>['tone']
>
type StatisticCardDescriptionTone = NonNullable<
  VariantProps<typeof statisticCardDescriptionVariants>['tone']
>
type StatisticCardIconTone = NonNullable<
  VariantProps<typeof statisticCardIconVariants>['tone']
>

export function WinPercentageCard({
  statistics,
}: {
  statistics: GetPersonalStatisticsHttpResponse
}) {
  const winPercentage = formatPercentage({ value: statistics.winPercentage })
  const progressPercentage = Math.min(100, Math.max(0, winPercentage))

  return (
    <StatisticCard
      className="dashboard-stat-card-lime min-h-[310px] sm:col-span-2 xl:col-span-5 xl:row-span-2"
      tone="lime"
    >
      <StatisticCardHeader
        title="Your win percentage"
        description={`${formatCount({ count: statistics.wins, singular: 'win' })} from ${formatCount({ count: statistics.gamesPlayed, singular: 'game' })}`}
        icon={Target}
        iconTone="cream"
      />
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
        <StatisticCardDetail>
          {formatRecord({ wins: statistics.wins, losses: statistics.losses })}{' '}
          all time
        </StatisticCardDetail>
      </CardFooter>
    </StatisticCard>
  )
}

export function CrewRankCard({
  statistics,
}: {
  statistics: GetPersonalStatisticsHttpResponse
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-3"
      tone="ink"
      title="Crew rank"
      description="Your place on the crew leaderboard"
      value={`#${statistics.crewRank}`}
      detail={`of ${formatCount({ count: statistics.activeCrewMemberCount, singular: 'player' })}`}
      icon={Medal}
      iconTone="orange"
      descriptionTone="cream"
    />
  )
}

export function CurrentStreakCard({
  statistics,
}: {
  statistics: GetPersonalStatisticsHttpResponse
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-4"
      tone="orange"
      title="Current streak"
      description="Consecutive wins through your latest game"
      value={
        statistics.currentWinStreak === 0
          ? 'Your move'
          : `${statistics.currentWinStreak} W`
      }
      detail={getCurrentStreakDetail({
        currentWinStreak: statistics.currentWinStreak,
        longestWinStreak: statistics.longestWinStreak,
      })}
      icon={Flame}
      iconTone="lime"
    />
  )
}

export function PointDifferentialCard({
  statistics,
}: {
  statistics: GetPersonalStatisticsHttpResponse
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-3"
      tone="aqua"
      title="Point differential"
      description="Points scored minus points conceded"
      value={formatSignedNumber({ value: statistics.pointDifferential })}
      detail={`${statistics.pointsScored.toLocaleString('en-AU')} for · ${statistics.pointsConceded.toLocaleString('en-AU')} against`}
      icon={TrendingUp}
      iconTone="cream"
    />
  )
}

export function PaddlinCard({
  statistics,
}: {
  statistics: GetPersonalStatisticsHttpResponse
}) {
  return (
    <PersonalMetricCard
      className="xl:col-span-4"
      tone="purple"
      title="That's a Paddlin"
      description="11–0 wins. Zero points. Zero mercy."
      descriptionTone="light"
      value={statistics.thatsAPaddlin.toLocaleString('en-AU')}
      emptyState={
        statistics.thatsAPaddlin === 0
          ? "No paddlins yet. Court's waiting. Go obliterate somebody."
          : undefined
      }
      icon={Hammer}
      iconTone="lime"
    />
  )
}

export function RecentGamesCard({
  recentGames,
}: {
  recentGames: GetRecentGamesHttpResponse
}) {
  const recentGameCount = recentGames.games.length
  const recentRecord = formatRecord({
    wins: recentGames.wins,
    losses: recentGames.losses,
  })

  return (
    <StatisticCard
      className="sm:col-span-2 xl:col-span-4"
      tone="cream"
    >
      <StatisticCardHeader
        title="Last 10 games"
        description={
          recentGameCount === 0
            ? 'No recent games'
            : `${recentRecord} · last ${recentGameCount}`
        }
        action={
          recentGames.winStreak > 1 ? (
            <Badge className="bg-lime text-ink">
              <Flame data-icon="inline-start" />
              {recentGames.winStreak}-game win streak
            </Badge>
          ) : undefined
        }
      />
      <CardContent>
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
          <p className="m-0 text-sm text-muted-foreground">
            Record a game to start your recent form.
          </p>
        )}
      </CardContent>
      <CardFooter>
        <StatisticCardDetail className="text-muted-foreground">
          Newest game first
        </StatisticCardDetail>
      </CardFooter>
    </StatisticCard>
  )
}

export function BestPartnerCard({
  response,
}: {
  response: GetBestPartnerHttpResponse
}) {
  const bestPartner = getBestPartnerPresentation({
    bestPartner: response.bestPartner,
  })

  return (
    <StatisticCard
      className="min-h-[260px] sm:col-span-2 xl:col-span-4"
      tone="paper"
    >
      <StatisticCardHeader
        title="Best partner"
        description="You two have a special bond. Apparently it wins Games."
        icon={Handshake}
        iconTone="purple"
      />
      {bestPartner.available ? (
        <CardContent className="relative z-1 gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex min-w-0 items-center gap-5">
            <DashboardAvatar
              className="size-24 shrink-0 rotate-[-5deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
              fallbackClassName="bg-aqua font-heading text-3xl text-ink"
              person={bestPartner.partner}
            />
            <div className="flex min-w-0 flex-col gap-2">
              <StatisticCardDetail>Your court accomplice</StatisticCardDetail>
              <strong className="truncate font-heading text-3xl font-black sm:text-4xl">
                {bestPartner.partner.displayName}
              </strong>
              <Badge className="w-fit" variant="outline">
                {formatCount({
                  count: bestPartner.partner.wins,
                  singular: 'win',
                })}{' '}
                -{' '}
                {formatCount({
                  count: bestPartner.partner.losses,
                  singular: 'loss',
                  plural: 'losses',
                })}
              </Badge>
            </div>
          </div>
          <div className="flex shrink-0 items-baseline gap-2 lg:flex-col lg:items-end lg:gap-0">
            <strong className="font-heading text-7xl leading-[.75] font-black tracking-[-.08em]">
              {bestPartner.winPercentage}%
            </strong>
            <StatisticCardDetail>win rate</StatisticCardDetail>
          </div>
        </CardContent>
      ) : (
        <StatisticCardEmptyState
          title={bestPartner.title}
          description={bestPartner.description}
        />
      )}
      <StatisticCardDecoration icon={Handshake} rotation="counterclockwise" />
    </StatisticCard>
  )
}

export function NemesisCard({ response }: { response: GetNemesisHttpResponse }) {
  const nemesis = response.nemesis

  return (
    <StatisticCard
      className="min-h-[190px] sm:col-span-2 xl:col-span-4"
      tone="orange"
    >
      <StatisticCardHeader
        title="Your nemesis"
        description="The opponent who's handed you the most losses"
        icon={Skull}
        iconTone="cream"
      />
      {nemesis ? (
        <>
          <CardContent className="relative z-1">
            <div>
              <strong className="block font-heading text-6xl leading-[.72] font-black tracking-[-.08em]">
                {nemesis.losses.toLocaleString('en-AU')}
              </strong>
              <span className="whitespace-nowrap font-heading text-base font-black">
                {nemesis.losses === 1 ? 'loss' : 'losses'} to them
              </span>
            </div>
          </CardContent>
          <CardFooter className="relative z-1 mt-auto items-center gap-3">
            <DashboardAvatar
              className="size-14 rotate-[-5deg] border-2 border-ink shadow-[3px_4px_0_var(--ink)]"
              fallbackClassName="bg-purple font-heading text-lg text-white"
              person={nemesis}
            />
            <div className="flex min-w-0 flex-col gap-1">
              <strong className="max-w-full truncate font-heading text-base">
                {nemesis.displayName}
              </strong>
              <StatisticCardDetail>
                They&apos;ve got your number
              </StatisticCardDetail>
            </div>
          </CardFooter>
        </>
      ) : (
        <StatisticCardEmptyState
          align="start"
          title="Nobody owns you yet"
          titleSize="compact"
          description="Play at least three games against an active Crew member to reveal your Nemesis."
        />
      )}
      <StatisticCardDecoration icon={Skull} />
    </StatisticCard>
  )
}

export function PersonalStatisticsEmptyCard() {
  return (
    <Card className="min-h-[310px] border-2 border-dashed border-ink/30 bg-cream">
      <StatisticCardEmptyState
        title="No personal receipts yet"
        description="Get on the court, play a Game, and start building a record worth bragging about."
      />
    </Card>
  )
}

export function CrewLeaderboardCard({
  leaderboard,
  currentUserId,
}: {
  leaderboard: GetCrewLeaderboardHttpResponse
  currentUserId: string
}) {
  const hasGames = leaderboard.entries.some(
    (member) => member.wins + member.losses > 0,
  )

  return (
    <StatisticCard
      className="xl:col-span-7 xl:row-span-2 xl:row-start-1"
      tone="cream"
    >
      <StatisticCardHeader
        className="border-b-2 border-line pb-8!"
        title="Crew leaderboard"
        description="Ranked by totally indisputable crew score."
        icon={Trophy}
        iconTone="orange"
      />
      {hasGames ? (
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
                currentUserId={currentUserId}
                member={member}
                key={member.userId}
              />
            ))}
          </ol>
        </CardContent>
      ) : (
        <StatisticCardEmptyState
          title="No leaderboard drama yet"
          description="Get on the court, record a Game, and give the Crew something to argue about."
        />
      )}
    </StatisticCard>
  )
}

export function BestDoublesPairingCard({
  response,
}: {
  response: GetBestDoublesPairingHttpResponse
}) {
  const pairing = response.pairing
  const firstPlayer = pairing?.players[0]
  const secondPlayer = pairing?.players[1]

  if (
    pairing &&
    (pairing.players.length !== 2 || !firstPlayer || !secondPlayer)
  ) {
    throw new Error('A doubles pairing must contain exactly two players.')
  }

  return (
    <StatisticCard
      className="dashboard-pair-card min-h-[330px] xl:col-span-6 xl:row-start-3"
      tone="purple"
    >
      <StatisticCardHeader
        title="Best doubles pairing"
        description="The duo nobody wants across the net"
        descriptionTone="light"
        icon={UsersRound}
        iconTone="aqua-inverse"
      />
      {pairing && firstPlayer && secondPlayer ? (
        <>
          <CardContent className="relative z-1 items-center gap-3 text-center">
            <DashboardPlayerPair
              firstPlayer={firstPlayer}
              secondPlayer={secondPlayer}
              separator="+"
            />
            <CardTitle className="font-heading text-3xl font-black">
              {firstPlayer.displayName} + {secondPlayer.displayName}
            </CardTitle>
            <div className="flex items-baseline gap-2">
              <strong className="font-heading text-5xl font-black text-lime">
                {formatPercentage({ value: pairing.winPercentage })}%
              </strong>
              <StatisticCardDetail>win rate</StatisticCardDetail>
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
              · {formatCount({ count: pairing.gamesPlayed, singular: 'game' })}
            </Badge>
          </CardFooter>
        </>
      ) : (
        <StatisticCardEmptyState
          title="No doubles record yet"
          description="Grab a partner, hit the court, and start proving which duo runs this Crew."
          descriptionTone="light"
        />
      )}
    </StatisticCard>
  )
}

export function FriendsOrLoversCard({
  response,
}: {
  response: GetFriendsOrLoversHttpResponse
}) {
  const details = getFriendsOrLoversCardDetails({ friendsOrLovers: response })

  return (
    <StatisticCard
      className="min-h-[330px] xl:col-span-6 xl:row-start-3"
      tone="lime"
    >
      <StatisticCardHeader
        title="Friends or lovers?"
        description="The pair that keeps choosing each other"
        icon={Heart}
        iconTone="orange"
      />
      {details.type === 'pairing' ? (
        <CardContent className="relative z-1 items-center gap-4 text-center">
          <DashboardPlayerPair
            firstPlayer={details.firstPlayer}
            secondPlayer={details.secondPlayer}
            separator={<Heart className="size-5 fill-orange text-orange" />}
          />
          <CardTitle className="font-heading text-3xl font-black">
            {details.firstPlayer.displayName} +{' '}
            {details.secondPlayer.displayName}
          </CardTitle>
          <div className="flex flex-col items-center gap-1">
            <strong className="font-heading text-6xl leading-none font-black tracking-[-.065em]">
              {details.gamesPlayed.toLocaleString('en-AU')}
            </strong>
            <StatisticCardDetail>
              {details.gamesTogetherLabel}
            </StatisticCardDetail>
          </div>
        </CardContent>
      ) : (
        <StatisticCardEmptyState
          title="No doubles partners yet"
          description="Play at least three doubles Games with another Crew member to start tracking who keeps showing up together."
        />
      )}
      <StatisticCardDecoration icon={Heart} />
    </StatisticCard>
  )
}

export function BiggestRivalsCard({
  response,
}: {
  response: GetBiggestRivalsHttpResponse
}) {
  const rivalry = response.rivalry
  const firstRival = rivalry?.players[0]
  const secondRival = rivalry?.players[1]

  if (
    rivalry &&
    (rivalry.players.length !== 2 || !firstRival || !secondRival)
  ) {
    throw new Error('A rivalry must contain exactly two players.')
  }

  return (
    <StatisticCard
      className="min-h-[330px] xl:col-span-6 xl:row-start-4"
      tone="ink"
    >
      <StatisticCardHeader
        title="Biggest rivals"
        description="The closest all-time head-to-head in the Crew"
        descriptionTone="cream"
        icon={Swords}
        iconTone="orange-inverse"
      />
      {rivalry && firstRival && secondRival ? (
        <>
          <CardContent className="relative z-1 items-center gap-3 text-center">
            <DashboardPlayerPair
              avatarClassName="border-cream shadow-[4px_5px_0_var(--orange)]"
              firstPlayer={firstRival}
              secondPlayer={secondRival}
              separator="VS"
              separatorClassName="size-11 border-cream bg-lime text-sm"
            />
            <CardTitle className="font-heading text-3xl font-black">
              {firstRival.displayName} vs {secondRival.displayName}
            </CardTitle>
            <strong className="font-heading text-6xl leading-none font-black tracking-[-.06em] text-orange">
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
        <StatisticCardEmptyState
          title="No rivalry yet"
          description="Record a Game between Crew members and let the scorekeeping begin."
          descriptionTone="cream"
        />
      )}
      <StatisticCardDecoration icon={Swords} />
    </StatisticCard>
  )
}

export function LongestCrewWinStreakCard({
  longestWinStreak,
}: {
  longestWinStreak: GetCrewLongestWinStreakHttpResponse
}) {
  const hasWinStreak = longestWinStreak.longestWinStreak > 0

  return (
    <StatisticCard
      className="dashboard-streak-card min-h-[290px] xl:col-span-5 xl:col-start-8 xl:row-start-1"
      tone="orange"
    >
      <StatisticCardHeader
        title="Longest crew win streak"
        description="The record to chase"
        icon={Flame}
        iconTone="lime"
      />
      {hasWinStreak ? (
        <>
          <CardContent className="relative z-1 flex-row items-end justify-between gap-5">
            <div>
              <strong className="block font-heading text-8xl leading-[.72] font-black tracking-[-.08em]">
                {longestWinStreak.longestWinStreak}
              </strong>
              <span className="font-heading text-xl font-black">
                straight{' '}
                {longestWinStreak.longestWinStreak === 1 ? 'win' : 'wins'}
              </span>
            </div>
            {longestWinStreak.userId && longestWinStreak.displayName ? (
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
            <StatisticCardDetail>
              {formatStreakPeriod({
                startedAtUtc:
                  longestWinStreak.longestWinStreakStartedAtUtc,
                endedAtUtc: longestWinStreak.longestWinStreakEndedAtUtc,
              })}
            </StatisticCardDetail>
          </CardFooter>
        </>
      ) : (
        <StatisticCardEmptyState
          title="No win streak yet"
          description="Get on the court and start proving who runs this Crew."
        />
      )}
    </StatisticCard>
  )
}

export function ChiefPaddlerCard({
  response,
}: {
  response: GetChiefPaddlerHttpResponse
}) {
  const chiefPaddler = response.chiefPaddler

  return (
    <StatisticCard
      className="min-h-[290px] xl:col-span-5 xl:col-start-8 xl:row-start-2"
      tone="aqua"
    >
      <StatisticCardHeader
        title="Chief Paddler"
        description="Most 11–0 wins in the Crew"
        icon={Hammer}
        iconTone="purple"
      />
      {chiefPaddler ? (
        <CardContent className="relative z-1 mt-auto gap-6 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex min-w-0 items-center gap-5">
            <div className="relative shrink-0">
              <DashboardAvatar
                className="size-24 rotate-[-5deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]"
                fallbackClassName="bg-lime font-heading text-3xl text-ink"
                person={chiefPaddler}
              />
              <ChiefBadge />
            </div>
            <div className="flex min-w-0 flex-col gap-1">
              <StatisticCardDetail>Reigning Chief</StatisticCardDetail>
              <strong className="truncate font-heading text-3xl font-black">
                {chiefPaddler.displayName}
              </strong>
            </div>
          </div>
          <div className="flex shrink-0 flex-col sm:items-end">
            <strong className="font-heading text-7xl leading-[.75] font-black tracking-[-.08em]">
              {chiefPaddler.thatsAPaddlin.toLocaleString('en-AU')}
            </strong>
            <StatisticCardDetail>paddlins delivered</StatisticCardDetail>
          </div>
        </CardContent>
      ) : (
        <CardContent className="relative z-1 mt-auto gap-6 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex min-w-0 items-center gap-5">
            <div className="relative shrink-0">
              <span className="grid size-24 rotate-[-5deg] place-items-center rounded-full border-2 border-dashed border-ink bg-cream shadow-[4px_5px_0_var(--ink)] [&_svg]:size-10">
                <UserRound aria-label="Chief Paddler unclaimed" />
              </span>
              <ChiefBadge claimed={false} />
            </div>
            <div className="flex min-w-0 flex-col gap-1">
              <StatisticCardDetail>Chief wanted</StatisticCardDetail>
              <strong className="font-heading text-3xl font-black">
                The throne&apos;s empty
              </strong>
            </div>
          </div>
          <p className="m-0 max-w-44 text-sm text-ink-soft sm:text-right">
            Hand out an 11–0 paddlin and claim it.
          </p>
        </CardContent>
      )}
      <StatisticCardDecoration icon={Crown} />
    </StatisticCard>
  )
}

function StatisticCard({
  tone,
  className,
  ...props
}: ComponentProps<typeof Card> & VariantProps<typeof statisticCardVariants>) {
  return (
    <Card
      className={cn(statisticCardVariants({ tone }), className)}
      {...props}
    />
  )
}

function StatisticCardHeader({
  title,
  description,
  descriptionTone,
  icon: Icon,
  iconTone,
  action,
  className,
}: {
  title: string
  description: string
  descriptionTone?: StatisticCardDescriptionTone
  icon?: LucideIcon
  iconTone?: StatisticCardIconTone
  action?: ReactNode
  className?: string
}) {
  return (
    <CardHeader className={cn('relative z-1', className)}>
      <CardTitle className="text-xs font-black tracking-[.14em] uppercase">
        {title}
      </CardTitle>
      <CardDescription
        className={statisticCardDescriptionVariants({
          tone: descriptionTone,
        })}
      >
        {description}
      </CardDescription>
      {Icon ? (
        <CardAction className={statisticCardIconVariants({ tone: iconTone })}>
          <Icon aria-hidden="true" />
        </CardAction>
      ) : action ? (
        <CardAction>{action}</CardAction>
      ) : null}
    </CardHeader>
  )
}

function StatisticCardDecoration({
  icon: Icon,
  rotation = 'clockwise',
}: {
  icon: LucideIcon
  rotation?: 'clockwise' | 'counterclockwise'
}) {
  return (
    <Icon
      className={cn(
        'pointer-events-none absolute -right-8 -bottom-10 size-36 opacity-10',
        rotation === 'clockwise' ? 'rotate-[14deg]' : 'rotate-[-14deg]',
      )}
      aria-hidden="true"
    />
  )
}

function StatisticCardEmptyState({
  title,
  description,
  align = 'center',
  titleSize = 'default',
  descriptionTone = 'muted',
}: {
  title: string
  description: string
  align?: 'start' | 'center'
  titleSize?: 'compact' | 'default'
  descriptionTone?: StatisticCardDescriptionTone
}) {
  return (
    <CardContent
      className={cn(
        'relative z-1 my-auto gap-2',
        align === 'center' ? 'items-center justify-center text-center' : 'gap-1',
      )}
    >
      <strong
        className={cn(
          'font-heading font-black',
          titleSize === 'default' ? 'text-3xl' : 'text-2xl',
        )}
      >
        {title}
      </strong>
      <p
        className={cn(
          'm-0 max-w-sm text-sm',
          statisticCardDescriptionVariants({ tone: descriptionTone }),
        )}
      >
        {description}
      </p>
    </CardContent>
  )
}

function StatisticCardDetail({
  className,
  children,
}: {
  className?: string
  children: ReactNode
}) {
  return (
    <span
      className={cn(
        'text-[9px] font-black tracking-[.08em] uppercase',
        className,
      )}
    >
      {children}
    </span>
  )
}

function PersonalMetricCard({
  className,
  tone,
  title,
  description,
  descriptionTone,
  value,
  detail,
  emptyState,
  icon,
  iconTone,
}: {
  className: string
  tone: StatisticCardTone
  title: string
  description: string
  descriptionTone?: StatisticCardDescriptionTone
  value: string
  detail?: string
  emptyState?: string
  icon: LucideIcon
  iconTone: StatisticCardIconTone
}) {
  return (
    <StatisticCard className={cn('min-h-[190px]', className)} tone={tone}>
      <StatisticCardHeader
        title={title}
        description={description}
        descriptionTone={descriptionTone}
        icon={icon}
        iconTone={iconTone}
      />
      <CardContent className="relative z-1 mt-auto gap-1">
        {emptyState ? (
          <p className="m-0 max-w-xs text-sm leading-relaxed font-medium opacity-80">
            {emptyState}
          </p>
        ) : (
          <>
            <strong className="font-heading text-6xl leading-none font-black tracking-[-.065em]">
              {value}
            </strong>
            {detail ? <StatisticCardDetail>{detail}</StatisticCardDetail> : null}
          </>
        )}
      </CardContent>
    </StatisticCard>
  )
}

function DashboardPlayerPair({
  firstPlayer,
  secondPlayer,
  separator,
  avatarClassName,
  separatorClassName,
}: {
  firstPlayer: Pick<
    DoublesPairingPlayerHttpResponse,
    'userId' | 'displayName' | 'profilePictureUrl'
  >
  secondPlayer: Pick<
    DoublesPairingPlayerHttpResponse,
    'userId' | 'displayName' | 'profilePictureUrl'
  >
  separator: ReactNode
  avatarClassName?: string
  separatorClassName?: string
}) {
  return (
    <div className="flex items-center justify-center">
      <DashboardAvatar
        className={cn(
          'relative z-1 size-20 rotate-[-7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]',
          avatarClassName,
        )}
        fallbackClassName="font-heading text-2xl"
        person={firstPlayer}
      />
      <span
        className={cn(
          'relative z-2 -mx-2 grid size-10 rotate-[6deg] place-items-center rounded-full border-2 border-ink bg-cream font-heading text-xl font-black text-ink',
          separatorClassName,
        )}
        aria-hidden="true"
      >
        {separator}
      </span>
      <DashboardAvatar
        className={cn(
          'relative z-1 size-20 rotate-[7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]',
          avatarClassName,
        )}
        fallbackClassName="font-heading text-2xl"
        person={secondPlayer}
      />
    </div>
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

function ChiefBadge({ claimed = true }: { claimed?: boolean }) {
  return (
    <span className="absolute -top-3 -right-3 grid size-10 rotate-[10deg] place-items-center rounded-full border-2 border-ink bg-orange shadow-[2px_3px_0_var(--ink)] [&_svg]:size-5">
      <Crown
        aria-label={claimed ? 'Chief Paddler' : undefined}
        aria-hidden={claimed ? undefined : 'true'}
      />
    </span>
  )
}

function getAvatarStyle({ userId }: { userId: string }): string {
  const colorIndex = [...userId].reduce(
    (hash, character) => hash + character.charCodeAt(0),
    0,
  )

  return avatarStyles[colorIndex % avatarStyles.length] ?? avatarStyles[0]
}
