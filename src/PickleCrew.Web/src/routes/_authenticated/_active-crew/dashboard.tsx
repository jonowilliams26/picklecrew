import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import { CircleAlert } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import { Separator } from '~/components/ui/separator'
import { getDashboard } from '~/lib/dashboard/dashboard.functions'
import {
  BestDoublesPairingCard,
  BestPartnerCard,
  BiggestRivalsCard,
  ChiefPaddlerCard,
  CrewLeaderboardCard,
  CrewRankCard,
  CurrentStreakCard,
  FriendsOrLoversCard,
  LongestCrewWinStreakCard,
  NemesisCard,
  PaddlinCard,
  PersonalStatisticsEmptyCard,
  PointDifferentialCard,
  RecentGamesCard,
  WinPercentageCard,
} from './-dashboard/dashboard-cards'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/dashboard',
)({
  loader: () => getDashboard(),
  component: DashboardPage,
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
    nemesis,
    recentGames,
    leaderboard,
    longestWinStreak,
    bestDoublesPairing,
    bestPartner,
    biggestRivals,
    chiefPaddler,
    friendsOrLovers,
  } = result.value

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
              <WinPercentageCard statistics={personalStatistics} />
              <CrewRankCard statistics={personalStatistics} />
              <CurrentStreakCard statistics={personalStatistics} />
              <PointDifferentialCard statistics={personalStatistics} />
              <PaddlinCard statistics={personalStatistics} />
              <RecentGamesCard recentGames={recentGames} />
              <BestPartnerCard response={bestPartner} />
              <NemesisCard response={nemesis} />
            </div>
          ) : (
            <PersonalStatisticsEmptyCard />
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
              leaderboard={leaderboard}
            />
            <BestDoublesPairingCard response={bestDoublesPairing} />
            <FriendsOrLoversCard response={friendsOrLovers} />
            <BiggestRivalsCard response={biggestRivals} />
            <LongestCrewWinStreakCard
              longestWinStreak={longestWinStreak}
            />
            <ChiefPaddlerCard response={chiefPaddler} />
          </div>
        </section>
      </div>
    </main>
  )
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
