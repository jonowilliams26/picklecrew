import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import {
  Crown,
  Flame,
  Medal,
  Target,
  Trophy,
  TrendingUp,
  UsersRound,
} from 'lucide-react'
import { Avatar, AvatarFallback } from '~/components/ui/avatar'
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
import { Separator } from '~/components/ui/separator'
import { cn } from '~/lib/utils'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/dashboard',
)({
  component: DashboardPage,
})

const recentForm = ['L', 'W', 'L', 'W', 'W', 'L', 'W', 'W', 'W', 'W'] as const

const crewMembers = [
  {
    name: 'Maya Chen',
    initials: 'MC',
    record: '21–8',
    score: '2,480',
    isCurrentUser: false,
  },
  {
    name: '',
    initials: '',
    record: '17–8',
    score: '2,310',
    isCurrentUser: true,
  },
  {
    name: 'Dev Patel',
    initials: 'DP',
    record: '15–11',
    score: '2,190',
    isCurrentUser: false,
  },
  {
    name: 'Jordan Lee',
    initials: 'JL',
    record: '14–13',
    score: '2,020',
    isCurrentUser: false,
  },
  {
    name: 'Sam Rivera',
    initials: 'SR',
    record: '10–16',
    score: '1,740',
    isCurrentUser: false,
  },
] as const

const avatarStyles = [
  'bg-orange text-ink',
  'bg-lime text-ink',
  'bg-aqua text-ink',
  'bg-purple text-white',
  'bg-paper text-ink',
] as const

function getFirstName({
  firstName,
  email,
}: {
  firstName: string | null | undefined
  email: string
}): string {
  return firstName?.trim() || email.split('@')[0] || 'Player'
}

function getInitials({
  firstName,
  lastName,
  email,
}: {
  firstName: string | null | undefined
  lastName: string | null | undefined
  email: string
}): string {
  const initials = [firstName, lastName]
    .filter(Boolean)
    .map((name) => name?.at(0)?.toUpperCase())
    .join('')

  return initials || email.at(0)?.toUpperCase() || '?'
}

function DashboardPage() {
  const { user } = useAuth()

  if (!user) {
    throw new Error('Dashboard rendered without an authenticated user.')
  }

  const firstName = getFirstName({ firstName: user.firstName, email: user.email })
  const initials = getInitials({
    firstName: user.firstName,
    lastName: user.lastName,
    email: user.email,
  })

  return (
    <main className="relative flex flex-1 flex-col overflow-hidden p-5 md:p-8 md:pb-10 xl:p-10">
      <div className="mx-auto flex w-full max-w-[1480px] flex-col gap-12">
        <section className="flex flex-col gap-6" aria-labelledby="personal-stats-heading">
          <SectionHeading
            eyebrow="01 · Your game"
            title="Personal receipts"
            description="Your numbers inside this crew. No selective memory allowed."
            headingId="personal-stats-heading"
          />

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-12">
            <Card className="dashboard-stat-card dashboard-stat-card-lime min-h-[310px] border-2 border-ink bg-lime shadow-[7px_8px_0_var(--ink)] sm:col-span-2 xl:col-span-5 xl:row-span-2">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Your win percentage
                </CardTitle>
                <CardDescription className="font-heading text-ink-soft">
                  17 wins from 25 games
                </CardDescription>
                <CardAction className="grid size-12 rotate-[5deg] place-items-center rounded-full border-2 border-ink bg-cream shadow-[3px_4px_0_var(--ink)] [&_svg]:size-6">
                  <Target aria-hidden="true" />
                </CardAction>
              </CardHeader>
              <CardContent className="mt-auto gap-5">
                <div className="font-heading text-[clamp(6.5rem,13vw,10rem)] leading-[.68] font-black tracking-[-.1em]">
                  68<span className="ml-2 text-[.25em] tracking-[-.03em]">%</span>
                </div>
                <div
                  className="h-4 overflow-hidden rounded-full border-2 border-ink bg-cream"
                  role="progressbar"
                  aria-label="Win percentage"
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={68}
                >
                  <span className="block h-full w-[68%] border-r-2 border-ink bg-orange" />
                </div>
              </CardContent>
              <CardFooter className="justify-end">
                <span className="text-right text-[9px] font-black tracking-[.08em] uppercase">
                  Certified nuisance
                </span>
              </CardFooter>
            </Card>

            <PersonalMetricCard
              className="bg-ink text-cream shadow-[6px_7px_0_var(--orange)] xl:col-span-3"
              eyebrow="Crew rank"
              value="#2"
              detail="of 8 players"
              icon={Medal}
              iconClassName="bg-orange text-ink"
            />
            <PersonalMetricCard
              className="bg-orange text-ink shadow-[6px_7px_0_var(--ink)] xl:col-span-4"
              eyebrow="Current streak"
              value="4 W"
              detail="one away from your best"
              icon={Flame}
              iconClassName="bg-lime text-ink"
            />
            <PersonalMetricCard
              className="bg-aqua text-ink shadow-[6px_7px_0_var(--ink)] xl:col-span-3"
              eyebrow="Point differential"
              value="+72"
              detail="312 for · 240 against"
              icon={TrendingUp}
              iconClassName="bg-cream text-ink"
            />

            <Card className="dashboard-stat-card border-2 border-ink bg-cream shadow-[6px_7px_0_var(--ink)] sm:col-span-2 xl:col-span-4">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Last 10 games
                </CardTitle>
                <CardDescription className="font-heading">7–3 · currently spicy</CardDescription>
                <CardAction>
                  <Badge className="bg-lime text-ink">
                    <Flame data-icon="inline-start" />
                    4-game win streak
                  </Badge>
                </CardAction>
              </CardHeader>
              <CardContent>
                <div className="grid w-fit grid-cols-5 gap-2" aria-label="Seven wins and three losses in the last ten games">
                  {recentForm.map((result, index) => (
                    <span
                      className={cn(
                        'grid size-9 place-items-center rounded-full border-2 border-ink font-mono text-xs font-black shadow-[2px_3px_0_var(--ink)]',
                        result === 'W' ? 'bg-lime' : 'bg-orange',
                      )}
                      key={`${result}-${index}`}
                    >
                      {result}
                    </span>
                  ))}
                </div>
              </CardContent>
              <CardFooter>
                <span className="text-[9px] font-bold tracking-[.08em] text-muted-foreground uppercase">
                  Losses build character. Allegedly.
                </span>
              </CardFooter>
            </Card>
          </div>
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
              <CardContent className="gap-0">
                <div className="grid grid-cols-[34px_1fr_auto] gap-3 px-3 pb-2 text-[8px] font-black tracking-[.12em] text-muted-foreground uppercase sm:grid-cols-[42px_1fr_78px_72px]">
                  <span>Rank</span>
                  <span>Player</span>
                  <span className="hidden sm:block">Record</span>
                  <span className="text-right">Score</span>
                </div>
                <ol className="m-0 flex list-none flex-col gap-2 p-0">
                  {crewMembers.map((member, index) => {
                    const name = member.isCurrentUser ? firstName : member.name
                    const memberInitials = member.isCurrentUser ? initials : member.initials

                    return (
                      <li
                        className={cn(
                          'grid min-h-16 grid-cols-[34px_1fr_auto] items-center gap-3 rounded-xl border-2 border-ink px-3 py-2 sm:grid-cols-[42px_1fr_78px_72px]',
                          member.isCurrentUser ? 'bg-lime shadow-[3px_4px_0_var(--ink)]' : 'bg-paper',
                        )}
                        key={name}
                      >
                        <span className="font-mono text-sm font-black">0{index + 1}</span>
                        <span className="flex min-w-0 items-center gap-3">
                          <Avatar className="size-10 border-2 border-ink" size="lg">
                            <AvatarFallback className={cn('font-black', avatarStyles[index])}>
                              {memberInitials}
                            </AvatarFallback>
                          </Avatar>
                          <strong className="truncate font-heading text-sm sm:text-base">
                            {name}
                          </strong>
                          {index === 0 ? (
                            <Crown
                              className="hidden size-5 rotate-[-12deg] fill-orange sm:block"
                              aria-label="Crew leader"
                            />
                          ) : null}
                        </span>
                        <span className="hidden font-mono text-xs text-ink-soft sm:block">
                          {member.record}
                        </span>
                        <strong className="text-right font-mono text-sm">{member.score}</strong>
                      </li>
                    )
                  })}
                </ol>
              </CardContent>
            </Card>

            <Card className="dashboard-pair-card relative min-h-[330px] border-2 border-ink bg-purple text-white shadow-[7px_8px_0_var(--ink)] xl:col-span-5">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Best doubles pairing
                </CardTitle>
                <CardDescription className="font-heading text-white/70">
                  Minimum 5 games together
                </CardDescription>
                <CardAction className="grid size-11 place-items-center rounded-full border-2 border-white bg-aqua text-ink [&_svg]:size-5">
                  <UsersRound aria-hidden="true" />
                </CardAction>
              </CardHeader>
              <CardContent className="items-center gap-3 text-center">
                <div className="flex items-center justify-center" aria-hidden="true">
                  <Avatar className="z-1 size-20 rotate-[-7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]">
                    <AvatarFallback className="bg-orange font-heading text-2xl font-black text-ink">MC</AvatarFallback>
                  </Avatar>
                  <span className="z-2 -mx-2 grid size-10 rotate-[6deg] place-items-center rounded-full border-2 border-ink bg-cream font-heading text-xl font-black text-ink">
                    +
                  </span>
                  <Avatar className="z-1 size-20 rotate-[7deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]">
                    <AvatarFallback className="bg-aqua font-heading text-2xl font-black text-ink">DP</AvatarFallback>
                  </Avatar>
                </div>
                <CardTitle className="font-heading text-3xl font-black">Maya + Dev</CardTitle>
                <div className="flex items-baseline gap-2">
                  <strong className="font-heading text-5xl font-black text-lime">82%</strong>
                  <span className="text-[9px] font-black tracking-[.08em] uppercase">win rate</span>
                </div>
              </CardContent>
              <CardFooter className="mt-auto justify-center">
                <Badge className="rotate-[-2deg] border border-white bg-transparent px-3 text-white" variant="outline">
                  9 wins · 2 losses · deeply annoying
                </Badge>
              </CardFooter>
            </Card>

            <Card className="dashboard-streak-card relative min-h-[290px] border-2 border-ink bg-orange text-ink shadow-[7px_8px_0_var(--ink)] xl:col-span-5">
              <CardHeader>
                <CardTitle className="text-[10px] font-black tracking-[.14em] uppercase">
                  Longest crew win streak
                </CardTitle>
                <CardDescription className="font-heading text-ink-soft">The record to chase</CardDescription>
                <CardAction className="grid size-11 rotate-[7deg] place-items-center rounded-full border-2 border-ink bg-lime shadow-[3px_4px_0_var(--ink)] [&_svg]:size-5">
                  <Flame aria-hidden="true" />
                </CardAction>
              </CardHeader>
              <CardContent className="relative z-1 flex-row items-end justify-between gap-5">
                <div>
                  <strong className="block font-heading text-8xl leading-[.72] font-black tracking-[-.08em]">9</strong>
                  <span className="font-heading text-xl font-black">straight wins</span>
                </div>
                <Avatar className="size-16 rotate-[-5deg] border-2 border-ink shadow-[4px_5px_0_var(--ink)]">
                  <AvatarFallback className="bg-cream font-heading text-xl font-black text-ink">MC</AvatarFallback>
                </Avatar>
              </CardContent>
              <CardFooter className="relative z-1 mt-auto flex-col items-start gap-1">
                <strong className="font-heading text-base">Maya Chen</strong>
                <span className="text-[9px] font-black tracking-[.08em] uppercase">Ended by Dev · 11–9 · 2 June</span>
              </CardFooter>
            </Card>

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
    <Card className={cn('dashboard-stat-card min-h-[190px] border-2 border-ink', className)}>
      <CardHeader>
        <CardTitle className="text-[9px] font-black tracking-[.14em] uppercase">{eyebrow}</CardTitle>
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
        <strong className="font-heading text-6xl leading-none font-black tracking-[-.065em]">{value}</strong>
        <span className="text-[10px] font-bold tracking-[.05em] uppercase opacity-75">{detail}</span>
      </CardContent>
    </Card>
  )
}
