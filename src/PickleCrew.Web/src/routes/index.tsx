import { createFileRoute, redirect } from '@tanstack/react-router'
import { getAuth } from '@workos/authkit-tanstack-react-start'
import { useEffect } from 'react'
import {
  ArrowDownRight,
  ArrowRight,
  Award,
  Clock3,
  Crown,
  Flame,
  Medal,
  RefreshCcw,
  Sparkles,
  Trophy,
  Users,
  Zap,
} from 'lucide-react'
import { LogoMark, PickleBall } from '~/components/picklecrew-brand'
import { buttonVariants } from '~/components/ui/button'
import { decideGuestHomepageRoute } from '~/lib/auth/guest-routing'
import { cn } from '~/lib/utils'

export const Route = createFileRoute('/')({
  component: HomePage,
  ssr: true,
  loader: async () => {
    const { user } = await getAuth()
    const decision = decideGuestHomepageRoute({ isSignedIn: user !== null })

    if (decision.type === 'redirect') {
      throw redirect({ to: decision.destination, replace: true })
    }
  },
})

const leaderboard = [
  { rank: '01', initials: 'M', name: 'Maya', record: '21–8', score: '2,480' },
  { rank: '02', initials: 'J', name: 'Jordan', record: '18–10', score: '2,210' },
  { rank: '03', initials: 'D', name: 'Dev', record: '15–12', score: '1,960' },
  { rank: '04', initials: 'S', name: 'Sam', record: '11–16', score: '1,540' },
] as const

const tickerItems = [
  'Maya is on a 7 game heater',
  'Jordan demands a recount',
  'Maya + Dev are the duo to beat',
  'Friday rematch: 7:00 PM',
] as const

const tickerGroupItems = [...tickerItems, ...tickerItems] as const

const playerAvatarStyles = ['bg-orange', 'bg-aqua', 'bg-purple text-white', 'bg-paper'] as const

const displayHeadingClasses =
  'm-0 font-heading text-[clamp(3rem,5.6vw,5.5rem)] font-black leading-[.95] tracking-[-.065em] max-[680px]:text-[clamp(3rem,14vw,4.5rem)]'

const trophyCardStateStyles = {
  progress: '[&_.trophy-card-icon]:bg-lime',
  earned: 'bg-aqua [&_.trophy-card-icon_svg]:fill-lime',
  locked: 'border-cream bg-ink text-cream [&_.trophy-card-icon]:bg-purple',
} as const

const trophyProgressStyles = {
  'two-thirds': 'w-2/3',
  'four-fifths': 'w-4/5',
} as const

const achievements = [
  {
    title: 'On a Heater',
    description: 'Win 3 games in a row before someone checks your paddle.',
    badge: '2 / 3 wins',
    footer: 'One more. Stay spicy.',
    state: 'progress',
    progress: 'two-thirds',
    icon: Flame,
  },
  {
    title: 'Crown Snatcher',
    description: 'Beat the current crew leader and borrow the throne.',
    badge: 'Unlocked · Jordan',
    footer: 'Receipt verified.',
    state: 'earned',
    progress: null,
    icon: Crown,
  },
  {
    title: 'Deuce Ex Machina',
    description: 'Win 12–10 or deeper after everyone thought it was over.',
    badge: 'Locked',
    footer: 'Not dramatic enough yet.',
    state: 'locked',
    progress: null,
    icon: Clock3,
  },
  {
    title: 'Social Butterfly',
    description: 'Win with 3 different doubles partners. Networking!',
    badge: '2 / 3 partners',
    footer: 'Make one more friend.',
    state: 'progress',
    progress: 'two-thirds',
    icon: Users,
  },
  {
    title: 'Reverse Pickle',
    description: 'Lose, demand the immediate rematch, then win it.',
    badge: 'Unlocked · Dev',
    footer: 'The comeback was personal.',
    state: 'earned',
    progress: null,
    icon: RefreshCcw,
  },
  {
    title: 'Court Goblin',
    description: 'Play 5 games in one day and refuse to go home.',
    badge: '4 / 5 games',
    footer: 'Hydration is for quitters.',
    state: 'progress',
    progress: 'four-fifths',
    icon: Zap,
  },
] as const

function useScrollReveals() {
  useEffect(() => {
    const root = document.documentElement
    const elements = document.querySelectorAll('[data-reveal]')

    root.classList.add('reveal-ready')

    if (!('IntersectionObserver' in window)) {
      elements.forEach((element) => element.setAttribute('data-revealed', 'true'))

      return () => root.classList.remove('reveal-ready')
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) {
            return
          }

          entry.target.setAttribute('data-revealed', 'true')
          observer.unobserve(entry.target)
        })
      },
      {
        rootMargin: '0px 0px -8% 0px',
        threshold: 0.14,
      },
    )

    elements.forEach((element) => observer.observe(element))

    return () => {
      observer.disconnect()
      root.classList.remove('reveal-ready')
    }
  }, [])
}

function HomePage() {
  useScrollReveals()

  return (
    <div className="landing-shell min-h-dvh">
      <header className="mx-auto flex h-[calc(88px+env(safe-area-inset-top))] w-[calc(100%-40px)] max-w-[1240px] items-center justify-between border-b border-line pt-[env(safe-area-inset-top)] max-[680px]:h-[calc(74px+env(safe-area-inset-top))] max-[680px]:w-[calc(100%-28px)]">
        <a
          className="inline-flex items-center gap-2.5 font-heading text-[21px] font-[850] tracking-[-.045em]"
          href="#top"
          aria-label="PickleCrew home"
        >
          <LogoMark />
          <span>PickleCrew</span>
        </a>
        <nav
          className="flex items-center gap-[38px] text-xs font-bold tracking-[.08em] uppercase max-[680px]:hidden"
          aria-label="Main navigation"
        >
          <a className="landing-nav-link relative" href="#stats">
            The receipts
          </a>
          <a className="landing-nav-link relative" href="#how-it-works">
            How it works
          </a>
        </nav>
        <div className="flex items-center gap-4">
          <a
            className={buttonVariants({ variant: 'crew', size: 'lg' })}
            href="/api/auth/sign-in"
          >
            Sign in
            <ArrowRight data-icon="inline-end" />
          </a>
        </div>
      </header>

      <main id="top">
        <section
          className="mx-auto grid min-h-[720px] w-[calc(100%-40px)] max-w-[1240px] grid-cols-[minmax(0,.88fr)_minmax(500px,1.12fr)] items-center gap-12 pt-[72px] pb-[86px] max-[1051px]:grid-cols-1 max-[1051px]:pt-20 max-[680px]:min-h-0 max-[680px]:w-[calc(100%-28px)] max-[680px]:gap-[25px] max-[680px]:pt-[55px] max-[680px]:pb-[70px]"
          aria-labelledby="hero-heading"
        >
          <div className="relative z-2 max-[1051px]:max-w-[780px]">
            <div className="inline-flex rotate-[-1deg] items-center gap-[9px] rounded-full border border-ink bg-cream px-[13px] py-2 text-[11px] font-extrabold tracking-[.12em] uppercase shadow-[3px_3px_0_var(--ink)] [&_svg]:size-3.5 [&_svg]:fill-orange">
              <Zap aria-hidden="true" />
              <span>Stats for competitive friendships</span>
            </div>
            <h1
              className="mt-[30px] mb-[22px] max-w-[650px] font-heading text-[clamp(4rem,7.2vw,7.15rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:mt-[25px] max-[680px]:text-[clamp(3.8rem,19vw,5.6rem)]"
              id="hero-heading"
            >
              Own the
              <span className="hero-highlight"> bragging rights.</span>
            </h1>
            <p className="m-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.28rem)] leading-[1.55] font-[540] text-ink-soft max-[680px]:text-base">
              Your crew already keeps score. PickleCrew keeps the receipts — every win, streak,
              rivalry, and suspiciously unbeatable doubles pairing.
            </p>
            <div className="mt-[34px] flex items-center gap-[25px] max-[680px]:flex-col max-[680px]:text-center">
              <a
                className={buttonVariants({ variant: 'crew', size: 'xl' })}
                href="/api/auth/sign-up"
              >
                Start a crew
                <ArrowDownRight data-icon="inline-end" />
              </a>
              <a
                className="group inline-flex items-center gap-2 border-b border-ink text-xs font-extrabold tracking-[.04em] uppercase"
                href="#how-it-works"
              >
                How the chaos works
                <ArrowRight
                  className="size-[15px] transition-transform duration-[180ms] group-hover:translate-x-1"
                  aria-hidden="true"
                />
              </a>
            </div>
            <div
              className="mt-[38px] flex items-center gap-[15px] max-[680px]:flex-col max-[680px]:text-center"
              aria-label="Made for every kind of crew"
            >
              <div className="flex" aria-hidden="true">
                <span className="grid size-[34px] place-items-center rounded-full border-2 border-paper bg-lime text-[10px] font-black">
                  MJ
                </span>
                <span className="-ml-2 grid size-[34px] place-items-center rounded-full border-2 border-paper bg-aqua text-[10px] font-black">
                  DK
                </span>
                <span className="-ml-2 grid size-[34px] place-items-center rounded-full border-2 border-paper bg-purple text-[10px] font-black text-white">
                  SR
                </span>
                <span className="-ml-2 grid size-[34px] place-items-center rounded-full border-2 border-paper bg-orange text-[10px] font-black">
                  +8
                </span>
              </div>
              <p className="m-0 max-w-[280px] font-heading text-[13px] leading-[1.4] text-ink-soft">
                Built for friends who say
                <strong className="text-ink"> “we&apos;re not that competitive.”</strong>
              </p>
            </div>
          </div>

          <div
            className="hero-stage relative isolate grid min-h-[600px] place-items-center max-[1051px]:mx-auto max-[1051px]:-mt-2.5 max-[1051px]:w-full max-[1051px]:max-w-[700px] max-[680px]:min-h-[490px]"
            aria-label="Example crew leaderboard"
          >
            <div className="court-lines" aria-hidden="true">
              <span className="court-net" />
              <span className="court-center" />
              <span className="court-kitchen court-kitchen-left" />
              <span className="court-kitchen court-kitchen-right" />
            </div>
            <PickleBall className="pickleball pickleball-one" />
            <PickleBall className="pickleball pickleball-two" />
            <article className="scoreboard-card">
              <header className="flex items-center justify-between px-6 pt-[22px] pb-[18px] max-[680px]:p-[17px]">
                <div>
                  <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
                    CREW #014
                  </span>
                  <h2 className="mt-1 mb-0 font-heading text-[27px] font-[860] tracking-[-.045em] max-[680px]:text-[21px]">
                    Dink Dynasty
                  </h2>
                </div>
                <span className="inline-flex items-center gap-1.5 rounded-full border border-ink bg-lime px-[9px] py-1.5 text-[9px] font-[850] tracking-[.14em] uppercase">
                  <i className="live-dot size-1.5 rounded-full bg-ink" /> LIVE
                </span>
              </header>
              <div className="flex justify-between border-y border-line px-6 py-[9px] text-[9px] font-bold tracking-[.08em] text-ink-soft uppercase max-[680px]:px-[17px] max-[680px]:py-2">
                <span>This week</span>
                <span>Crew score</span>
              </div>
              <ol className="m-0 list-none p-0">
                {leaderboard.map((player, index) => (
                  <li
                    className={cn(
                      'grid min-h-[59px] grid-cols-[25px_34px_1fr_auto_64px] items-center gap-[9px] border-b border-line px-5 py-2 font-heading text-[13px] max-[680px]:min-h-[52px] max-[680px]:grid-cols-[20px_31px_1fr_58px] max-[680px]:px-[13px] max-[680px]:py-[7px]',
                      index === 0 && 'bg-lime',
                    )}
                    key={player.name}
                  >
                    <span className="font-mono text-[10px] text-ink-soft">{player.rank}</span>
                    <span
                      className={cn(
                        'grid size-[34px] place-items-center rounded-full border border-ink font-heading text-xs font-black max-[680px]:size-[30px]',
                        playerAvatarStyles[index],
                      )}
                    >
                      {player.initials}
                    </span>
                    <span className="flex items-center gap-1.5 font-[750]">
                      {player.name}
                      {index === 0 && (
                        <Crown className="size-3.5 fill-orange" aria-label="Crew leader" />
                      )}
                    </span>
                    <span className="font-mono text-[10px] text-ink-soft max-[680px]:hidden">
                      {player.record}
                    </span>
                    <strong className="flex flex-col items-end text-right text-[13px] leading-none">
                      {player.score}
                      <small className="mt-1 text-[6px] font-extrabold tracking-[.12em] uppercase">
                        pts
                      </small>
                    </strong>
                  </li>
                ))}
              </ol>
              <footer className="grid grid-cols-[37px_1fr_auto] items-center gap-2.5 bg-ink px-5 py-[15px] text-cream max-[680px]:px-3.5 max-[680px]:py-3">
                <div
                  className="grid size-[34px] place-items-center rounded-full bg-lime text-ink [&_svg]:size-[17px]"
                  aria-hidden="true"
                >
                  <Trophy />
                </div>
                <div className="flex flex-col">
                  <span className="text-[8px] tracking-[.08em] uppercase">Current crew boss</span>
                  <strong className="mt-[3px] font-heading text-xs">Maya “No Mercy” Chen</strong>
                </div>
                <span className="text-[13px] font-black text-lime uppercase">↑ 2</span>
              </footer>
            </article>

            <div className="stat-sticker stat-sticker-streak">
              <Flame aria-hidden="true" />
              <span>HOT STREAK</span>
              <strong>7 WINS</strong>
            </div>
            <div className="stat-sticker stat-sticker-duo">
              <Users aria-hidden="true" />
              <span>POWER PAIR</span>
              <strong>MAYA + DEV</strong>
              <small>82% WIN RATE</small>
            </div>
          </div>
        </section>

        <div className="news-ticker" aria-label="Latest fictional crew updates">
          <div className="ticker-track">
            {[0, 1].map((group) => (
              <div
                className="ticker-group"
                aria-hidden={group === 1 ? 'true' : undefined}
                key={group}
              >
                {tickerGroupItems.map((item, index) => (
                  <span key={`${item}-${index}`}>
                    <Sparkles aria-hidden="true" />
                    {item}
                  </span>
                ))}
              </div>
            ))}
          </div>
        </div>

        <section
          className="mx-auto w-[calc(100%-40px)] max-w-[1160px] pt-[145px] pb-[155px] max-[680px]:w-[calc(100%-28px)] max-[680px]:py-[100px]"
          id="stats"
          aria-labelledby="stats-heading"
        >
          <div
            className="inline-flex items-center gap-[9px] text-[11px] font-extrabold tracking-[.12em] text-ink-soft uppercase"
            data-reveal="slide-left"
          >
            <span>THE RECEIPTS</span>
            <i className="block h-px w-[70px] bg-current" />
          </div>
          <div
            className="mt-6 mb-[58px] grid grid-cols-[1.45fr_.55fr] items-end gap-20 max-[1051px]:grid-cols-1 max-[1051px]:gap-6 max-[680px]:mb-[38px]"
            data-reveal="rise"
          >
            <h2 className={displayHeadingClasses} id="stats-heading">
              Every game adds fuel to the rivalry.
            </h2>
            <p className="m-0 font-heading text-base leading-[1.55] text-ink-soft max-[1051px]:max-w-[620px]">
              No spreadsheets. No fuzzy memories. Just gloriously undeniable proof of who showed up
              and who got pickled.
            </p>
          </div>

          <div className="stats-grid grid grid-cols-2 gap-[18px] max-[680px]:grid-cols-[minmax(0,1fr)] [&>[data-reveal]]:flex [&>[data-reveal]]:min-w-0">
            <div data-reveal="tilt-left">
              <article className="feature-card relative min-h-[390px] w-full overflow-hidden rounded-[20px] border-2 border-ink bg-lime p-7 transition-[transform,box-shadow] duration-200 max-[680px]:min-h-[370px] max-[361px]:p-[22px]">
                <header className="flex items-start justify-between">
                  <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
                    WIN PERCENTAGE
                  </span>
                  <Award className="size-7 fill-cream" aria-hidden="true" />
                </header>
                <div className="my-[15px] mt-9 font-heading text-[clamp(6.5rem,13vw,10.5rem)] font-[950] leading-[.72] tracking-[-.09em]">
                  68<span className="ml-[3px] text-[.28em] tracking-[-.03em]">%</span>
                </div>
                <div
                  className="win-meter h-[15px] overflow-hidden rounded-full border-2 border-ink bg-cream"
                  aria-label="68 percent win rate"
                >
                  <span className="block h-full w-[68%] origin-left border-r-2 border-ink bg-orange" />
                </div>
                <footer className="mt-[19px] flex items-center gap-3 text-[10px] uppercase">
                  <strong className="rounded-full border border-ink bg-cream px-[7px] py-1">
                    +12%
                  </strong>
                  <span>since the new paddle arrived</span>
                </footer>
              </article>
            </div>

            <div data-reveal="tilt-right">
              <article className="feature-card streak-card relative min-h-[390px] w-full overflow-hidden rounded-[20px] border-2 border-ink bg-orange p-7 transition-[transform,box-shadow] duration-200 max-[680px]:min-h-[370px] max-[361px]:p-[22px]">
                <header className="flex items-start justify-between">
                  <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
                    LONGEST STREAK
                  </span>
                  <Flame className="size-7 fill-lime" aria-hidden="true" />
                </header>
                <div className="relative z-1 mt-[18px] font-heading text-[11rem] font-[950] leading-[.82] tracking-[-.1em]">
                  7
                </div>
                <p className="relative z-1 -mt-0.5 mr-0 mb-5 ml-2 font-heading text-[23px] font-[850]">
                  straight wins
                </p>
                <div
                  className="streak-dots relative z-1 flex gap-[5px] max-[361px]:justify-between max-[361px]:gap-[3px]"
                  aria-hidden="true"
                >
                  {Array.from({ length: 7 }, (_, index) => (
                    <i
                      className="grid size-[31px] place-items-center rounded-full border border-ink bg-cream text-[10px] font-black not-italic max-[361px]:size-[27px]"
                      key={index}
                    >
                      W
                    </i>
                  ))}
                </div>
                <footer className="absolute right-6 bottom-[22px] left-7 z-2 text-[9px] font-extrabold tracking-[.08em]">
                  ENDED BY JORDAN · 11–9
                </footer>
              </article>
            </div>

            <div data-reveal="slide-left">
              <article className="feature-card ranking-card relative min-h-[450px] w-full overflow-hidden rounded-[20px] border-2 border-ink bg-ink p-7 text-cream transition-[transform,box-shadow] duration-200 max-[680px]:min-h-[430px] max-[361px]:p-[22px]">
                <header className="flex items-start justify-between">
                  <div>
                    <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
                      CREW LEADERBOARD · TOTAL POINTS
                    </span>
                    <h3 className="mt-[5px] mb-5 font-heading text-[29px] tracking-[-.04em]">
                      The ladder never lies.
                    </h3>
                  </div>
                  <Medal className="size-7 text-lime" aria-hidden="true" />
                </header>
                <ol className="m-0 list-none p-0">
                  {leaderboard.slice(0, 3).map((player, index) => (
                    <li
                      className="grid min-h-[67px] grid-cols-[28px_42px_1fr_auto_62px] items-center gap-[11px] border-t border-white/18 first:bg-lime first:text-ink max-[361px]:grid-cols-[24px_32px_minmax(0,1fr)_52px] max-[361px]:gap-1.5"
                      key={player.name}
                    >
                      <span className="text-[10px]">{index + 1}</span>
                      <span
                        className={cn(
                          'grid size-[38px] place-items-center rounded-full border border-current font-heading text-xs font-black max-[361px]:size-[30px]',
                          playerAvatarStyles[index],
                        )}
                      >
                        {player.initials}
                      </span>
                      <strong className="flex items-center gap-[7px] font-heading text-[15px]">
                        {player.name}
                        {index === 0 && (
                          <Crown
                            className="size-[17px] rotate-[8deg] fill-orange stroke-[2.5]"
                            aria-label="Crew leader"
                          />
                        )}
                      </strong>
                      <span className="text-[10px] opacity-70 max-[361px]:hidden">
                        {player.record}
                      </span>
                      <b className="flex flex-col items-end text-right text-xs leading-none">
                        {player.score}
                        <small className="mt-1 text-[6px] font-extrabold tracking-[.12em] uppercase">
                          pts
                        </small>
                      </b>
                    </li>
                  ))}
                </ol>
                <p className="mt-[22px] mb-0 text-[9px] tracking-[.06em] text-white/55 uppercase">
                  Every result moves the score. Excuses earn zero points.
                </p>
              </article>
            </div>

            <div data-reveal="slide-right">
              <article className="feature-card relative flex min-h-[450px] w-full flex-col items-center overflow-hidden rounded-[20px] border-2 border-ink bg-purple p-7 text-white transition-[transform,box-shadow] duration-200 max-[680px]:min-h-[430px] max-[361px]:p-[22px]">
                <header className="flex w-full items-start justify-between">
                  <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
                    BEST DOUBLES PAIRING
                  </span>
                  <Users className="size-7 text-lime" aria-hidden="true" />
                </header>
                <div
                  className="my-[17px] mt-[30px] flex items-center justify-center"
                  aria-hidden="true"
                >
                  <div className="pair-avatar pair-avatar-one grid size-[105px] place-items-center rounded-full border-2 border-ink bg-orange font-heading text-[40px] font-[950] text-ink shadow-[5px_6px_0_var(--ink)] max-[361px]:size-[88px]">
                    M
                  </div>
                  <span className="relative z-2 -mx-[9px] grid size-[45px] rotate-[7deg] place-items-center rounded-full border-2 border-ink bg-lime font-heading text-2xl font-black text-ink max-[361px]:size-[39px]">
                    +
                  </span>
                  <div className="pair-avatar pair-avatar-two grid size-[105px] place-items-center rounded-full border-2 border-ink bg-aqua font-heading text-[40px] font-[950] text-ink shadow-[5px_6px_0_var(--ink)] max-[361px]:size-[88px]">
                    D
                  </div>
                </div>
                <h3 className="m-0 font-heading text-[29px] font-black">Maya &amp; Dev</h3>
                <div className="flex items-baseline gap-2">
                  <strong className="font-heading text-[38px] text-lime">82%</strong>
                  <span className="text-[9px] uppercase">win rate together</span>
                </div>
                <footer className="mt-auto rotate-[-2deg] border border-white px-3 py-[7px] text-[9px] font-[850] tracking-[.09em] uppercase">
                  Someone split them up.
                </footer>
              </article>
            </div>
          </div>
        </section>

        <section
          className="trophies-section relative overflow-hidden border-y-2 border-ink py-[135px] text-white max-[680px]:py-[95px]"
          id="trophies"
          aria-labelledby="trophies-heading"
        >
          <div className="relative z-1 mx-auto w-[calc(100%-40px)] max-w-[1160px] max-[680px]:w-[calc(100%-28px)]">
            <div className="mb-[58px] grid grid-cols-[1.35fr_.65fr] items-end gap-20 max-[1051px]:grid-cols-1 max-[1051px]:gap-6 max-[680px]:mb-[38px]">
              <div data-reveal="slide-left">
                <div className="inline-flex items-center gap-[9px] text-[11px] font-extrabold tracking-[.12em] text-lime uppercase">
                  <span>TROPHY CABINET</span>
                  <i className="block h-px w-[70px] bg-current" />
                </div>
                <h2
                  className="mt-6 mb-0 max-w-[800px] font-heading text-[clamp(3rem,5.6vw,5.5rem)] font-black leading-[.95] tracking-[-.065em] max-[680px]:text-[clamp(3rem,14vw,4.5rem)]"
                  id="trophies-heading"
                >
                  Your crew&apos;s trophy cabinet gets weird fast.
                </h2>
              </div>
              <p
                className="m-0 font-heading text-base leading-[1.55] text-white/72 max-[1051px]:max-w-[620px]"
                data-reveal="rise"
              >
                Wins are nice. Hyper-specific proof of chaos is better. Every game can unlock
                another deeply serious honor.
              </p>
            </div>

            <div className="grid grid-cols-[minmax(330px,.75fr)_minmax(0,1.4fr)] items-stretch gap-[22px] max-[1051px]:grid-cols-1">
              <article
                className="trophy-featured relative flex min-h-[540px] min-w-0 flex-col overflow-hidden rounded-3xl border-2 border-ink bg-lime p-[30px] text-ink max-[1051px]:min-h-[500px] max-[680px]:min-h-[510px] max-[680px]:p-6"
                data-reveal="tilt-left"
              >
                <header className="relative z-2 flex items-center justify-between text-[9px] font-black tracking-[.12em]">
                  <span>FRESHLY UNLOCKED</span>
                  <strong className="rounded-full border border-ink bg-orange px-2 py-1.5 text-[8px]">
                    RARE-ISH
                  </strong>
                </header>
                <div className="trophy-emblem" aria-hidden="true">
                  <Trophy />
                  <Sparkles />
                </div>
                <div className="relative z-2 text-center">
                  <span className="text-[8px] font-black tracking-[.14em]">11–0 VICTORY</span>
                  <h3 className="mt-[7px] mb-[9px] font-heading text-[clamp(2.5rem,4vw,3.8rem)] font-[950] leading-[.92] tracking-[-.055em]">
                    That&apos;s a Paddlin&apos;
                  </h3>
                  <p className="mx-auto my-0 max-w-[360px] font-heading text-sm leading-[1.45]">
                    Win 11–0. No crumbs, no mercy, no alternate version of events.
                  </p>
                </div>
                <footer className="relative z-2 mt-auto grid grid-cols-[42px_1fr_auto] items-center gap-2.5 border-t border-ink/28 pt-6">
                  <span
                    className="grid size-[38px] place-items-center rounded-full border border-ink bg-aqua font-heading font-[950]"
                    aria-hidden="true"
                  >
                    M
                  </span>
                  <div className="flex min-w-0 flex-col">
                    <small className="text-[7px] font-[850] tracking-[.1em]">UNLOCKED BY</small>
                    <strong className="truncate font-heading text-xs">Maya “No Mercy” Chen</strong>
                  </div>
                  <b className="font-heading text-[22px]">11–0</b>
                </footer>
              </article>

              <div className="trophy-grid grid min-w-0 grid-cols-3 gap-4 max-[1051px]:grid-cols-2 max-[680px]:grid-cols-[minmax(0,1fr)]">
                {achievements.map((achievement) => {
                  const AchievementIcon = achievement.icon

                  return (
                    <article
                      className={cn(
                        'trophy-card relative flex min-h-[250px] min-w-0 flex-col overflow-hidden rounded-[18px] border-2 border-ink bg-cream p-[21px] text-ink max-[680px]:min-h-[220px]',
                        `trophy-card-${achievement.state}`,
                        trophyCardStateStyles[achievement.state],
                      )}
                      data-reveal="rise"
                      key={achievement.title}
                    >
                      <header className="flex items-start justify-between gap-2.5">
                        <span
                          className="trophy-card-icon grid size-[49px] shrink-0 place-items-center rounded-full border-2 border-current bg-orange [&_svg]:size-[25px]"
                          aria-hidden="true"
                        >
                          <AchievementIcon />
                        </span>
                        <span className="max-w-[105px] rounded-full border border-current px-[7px] py-[5px] text-center text-[7px] leading-[1.2] font-black tracking-[.08em] uppercase">
                          {achievement.badge}
                        </span>
                      </header>
                      <h3 className="mt-[25px] mb-2 font-heading text-[25px] font-[920] leading-[.95] tracking-[-.045em]">
                        {achievement.title}
                      </h3>
                      <p className="m-0 font-heading text-xs leading-[1.42]">
                        {achievement.description}
                      </p>
                      <footer className="mt-auto flex flex-col gap-2 pt-5">
                        {achievement.progress ? (
                          <div
                            className={cn(
                              'trophy-progress h-[9px] overflow-hidden rounded-full border border-current bg-white/50',
                              `trophy-progress-${achievement.progress}`,
                            )}
                            aria-hidden="true"
                          >
                            <span
                              className={cn(
                                'block h-full origin-left border-r border-ink bg-orange',
                                trophyProgressStyles[achievement.progress],
                              )}
                            />
                          </div>
                        ) : (
                          <i className="block h-0.5 w-8 bg-current" aria-hidden="true" />
                        )}
                        <span className="text-[7px] font-[850] tracking-[.08em] uppercase">
                          {achievement.footer}
                        </span>
                      </footer>
                    </article>
                  )
                })}
              </div>
            </div>

            <p
              className="mt-[35px] mb-0 text-center text-[8px] font-extrabold tracking-[.12em] text-white/55"
              data-reveal="rise"
            >
              ACHIEVEMENTS ARE PERMANENT. CONTEXT IS OPTIONAL.
            </p>
          </div>
        </section>

        <section
          className="grid grid-cols-[minmax(0,.95fr)_minmax(460px,1.05fr)] items-center gap-20 overflow-hidden bg-ink px-[max(20px,calc((100vw-1160px)/2))] py-[130px] text-cream max-[1051px]:grid-cols-1 max-[680px]:grid-cols-[minmax(0,1fr)] max-[680px]:gap-[45px] max-[680px]:px-5 max-[680px]:py-[95px]"
          id="how-it-works"
          aria-labelledby="rally-heading"
        >
          <div className="max-[1051px]:max-w-[720px]" data-reveal="slide-left">
            <div className="inline-flex items-center gap-[9px] text-[11px] font-extrabold tracking-[.12em] text-lime uppercase">
              <span>POST-GAME RITUAL</span>
              <i className="block h-px w-[70px] bg-current" />
            </div>
            <h2 className={cn(displayHeadingClasses, 'mt-[25px]')} id="rally-heading">
              From final point to group chat chaos.
            </h2>
            <p className="mt-[26px] mb-[42px] max-w-[550px] font-heading text-[17px] leading-[1.6] text-white/66">
              Record the result, watch the rankings move, then share the evidence before the losing
              side rewrites history.
            </p>
            <ol className="steps-list m-0 list-none p-0">
              <li
                className="grid grid-cols-[52px_1fr] items-center gap-[15px] border-t border-white/18 py-[17px]"
                data-reveal="rise"
              >
                <span className="grid size-10 place-items-center rounded-full border border-lime text-[10px] text-lime">
                  01
                </span>
                <div className="flex flex-col gap-[3px]">
                  <strong className="font-heading text-[17px]">Log the game</strong>
                  <small className="text-[10px] text-white/50">
                    Singles, doubles, scores. Done.
                  </small>
                </div>
              </li>
              <li
                className="grid grid-cols-[52px_1fr] items-center gap-[15px] border-t border-white/18 py-[17px]"
                data-reveal="rise"
              >
                <span className="grid size-10 place-items-center rounded-full border border-lime text-[10px] text-lime">
                  02
                </span>
                <div className="flex flex-col gap-[3px]">
                  <strong className="font-heading text-[17px]">Watch stats shuffle</strong>
                  <small className="text-[10px] text-white/50">Streaks rise. Leaders wobble.</small>
                </div>
              </li>
              <li
                className="grid grid-cols-[52px_1fr] items-center gap-[15px] border-y border-white/18 py-[17px]"
                data-reveal="rise"
              >
                <span className="grid size-10 place-items-center rounded-full border border-lime text-[10px] text-lime">
                  03
                </span>
                <div className="flex flex-col gap-[3px]">
                  <strong className="font-heading text-[17px]">Deploy bragging rights</strong>
                  <small className="text-[10px] text-white/50">
                    Bring screenshots to the group chat.
                  </small>
                </div>
              </li>
            </ol>
          </div>

          <div
            className="chat-stage relative grid min-h-[650px] place-items-center max-[1051px]:mx-auto max-[1051px]:w-full max-[1051px]:max-w-[650px] max-[680px]:min-h-[540px]"
            data-reveal="pop"
            aria-label="Example post-game group chat"
          >
            <PickleBall className="pickleball chat-ball" />
            <div className="phone-card">
              <header className="flex items-center gap-3 border-b border-line bg-cream p-[18px]">
                <div className="flex" aria-hidden="true">
                  <span className="grid size-[29px] place-items-center rounded-full border border-ink bg-orange text-[9px] font-black">
                    M
                  </span>
                  <span className="-ml-2 grid size-[29px] place-items-center rounded-full border border-ink bg-aqua text-[9px] font-black">
                    D
                  </span>
                  <span className="-ml-2 grid size-[29px] place-items-center rounded-full border border-ink bg-lime text-[9px] font-black">
                    J
                  </span>
                </div>
                <div className="flex flex-col gap-px">
                  <strong className="font-heading text-[13px]">Dink Dynasty</strong>
                  <small className="text-[8px] text-ink-soft">4 members · 3 typing...</small>
                </div>
              </header>
              <div className="chat-feed flex min-h-[360px] flex-col gap-[13px] px-[17px] py-[22px] max-[680px]:min-h-[315px]">
                <div className="chat-message chat-message-system flex max-w-full items-center gap-3 rounded-[10px] border border-ink bg-lime px-[13px] py-2.5 font-heading text-xs leading-[1.35] shadow-[2px_3px_0_var(--ink)]">
                  <Trophy className="size-[27px] fill-orange" aria-hidden="true" />
                  <span className="flex flex-col text-[9px]">
                    <strong className="text-[11px]">Maya + Dev won 11–7</strong>
                    New best doubles pairing: 82%
                  </span>
                </div>
                <div className="chat-message chat-message-left max-w-[84%] self-start rounded-[14px] border border-ink bg-cream px-[13px] py-2.5 font-heading text-xs leading-[1.35] shadow-[2px_3px_0_var(--ink)]">
                  Wind was definitely a factor.
                </div>
                <div className="chat-message chat-message-right max-w-[84%] self-end rounded-[14px] border border-ink bg-aqua px-[13px] py-2.5 font-heading text-xs leading-[1.35] shadow-[2px_3px_0_var(--ink)]">
                  We played indoors, Jordan.
                </div>
                <div
                  className="typing-dots flex self-start rounded-[14px] border border-ink bg-cream px-[15px] py-3"
                  aria-label="Someone is typing"
                >
                  <i />
                  <i />
                  <i />
                </div>
              </div>
              <footer className="m-3 flex items-center justify-between rounded-full border border-line bg-cream px-3.5 py-[11px] text-[9px] text-ink-soft [&_svg]:size-4">
                <span>Send your excuse...</span>
                <ArrowRight aria-hidden="true" />
              </footer>
            </div>
            <div className="chat-stamp" aria-hidden="true">
              RECEIPTS
              <br />
              ATTACHED
            </div>
          </div>
        </section>

        <section
          className="mx-auto w-[calc(100%-40px)] max-w-[1160px] py-[140px] text-center max-[680px]:w-[calc(100%-28px)] max-[680px]:py-[100px]"
          aria-labelledby="manifesto-heading"
        >
          <div data-reveal="rise">
            <span className="mb-[22px] inline-block rotate-[-2deg] border border-ink bg-lime px-[11px] py-[7px] text-[9px] font-[850] tracking-[.12em]">
              THE CREW CODE
            </span>
            <h2
              className="m-0 font-heading text-[clamp(3.5rem,7vw,7rem)] font-black leading-[.95] tracking-[-.065em] max-[680px]:text-[clamp(3rem,14vw,4.5rem)]"
              id="manifesto-heading"
            >
              Play nice. Track everything.
            </h2>
          </div>
          <div className="manifesto-rules mt-[65px] grid grid-cols-3 border-y border-line max-[680px]:mt-[45px] max-[680px]:grid-cols-1">
            <p
              className="m-0 flex min-h-[145px] flex-col items-start justify-between border-r border-line p-7 text-left font-heading text-lg leading-[1.3] font-bold max-[680px]:min-h-[110px] max-[680px]:border-r-0 max-[680px]:border-b"
              data-reveal="tilt-left"
            >
              <span className="font-mono text-[10px] text-orange">01</span>
              All heroic wins deserve documentation.
            </p>
            <p
              className="m-0 flex min-h-[145px] flex-col items-start justify-between border-r border-line p-7 text-left font-heading text-lg leading-[1.3] font-bold max-[680px]:min-h-[110px] max-[680px]:border-r-0 max-[680px]:border-b"
              data-reveal="rise"
            >
              <span className="font-mono text-[10px] text-orange">02</span>
              All “off days” still count.
            </p>
            <p
              className="m-0 flex min-h-[145px] flex-col items-start justify-between p-7 text-left font-heading text-lg leading-[1.3] font-bold max-[680px]:min-h-[110px]"
              data-reveal="tilt-right"
            >
              <span className="font-mono text-[10px] text-orange">03</span>
              The leaderboard may be questioned, but never ignored.
            </p>
          </div>
        </section>

        <section
          className="final-cta relative mx-auto mb-[90px] grid min-h-[560px] w-[calc(100%-40px)] max-w-[1240px] place-items-center overflow-hidden rounded-[26px] border-2 border-ink bg-lime text-center max-[680px]:mb-[70px] max-[680px]:min-h-[590px] max-[680px]:w-[calc(100%-28px)]"
          id="join-the-crew"
          aria-labelledby="final-heading"
        >
          <div className="final-court" aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
          <PickleBall className="pickleball final-ball" />
          <div
            className="relative z-3 flex max-w-[780px] flex-col items-center px-[30px] py-[70px]"
            data-reveal="pop"
          >
            <span className="block text-[9px] font-[850] tracking-[.14em] uppercase">
              YOUR NEXT ERA STARTS HERE
            </span>
            <h2
              className="mt-[17px] mb-5 font-heading text-[clamp(3.5rem,7vw,7rem)] font-black leading-[.95] tracking-[-.065em] max-[680px]:text-[clamp(3rem,14vw,4.5rem)]"
              id="final-heading"
            >
              Ready to settle this on the court?
            </h2>
            <p className="mt-0 mb-[30px] font-heading text-[17px]">
              Create the crew. Log the games. Keep the crown moving.
            </p>
            <a
              className={buttonVariants({ variant: 'paper', size: 'xl' })}
              href="/api/auth/sign-up"
            >
              Start a crew
              <ArrowRight data-icon="inline-end" />
            </a>
          </div>
          <div className="crown-doodle" aria-hidden="true">
            <Crown />
          </div>
        </section>
      </main>

      <footer className="mx-auto grid w-[calc(100%-40px)] max-w-[1240px] grid-cols-[1fr_auto_1fr] items-center gap-[30px] border-t border-line pt-7 pb-[calc(2.5rem+env(safe-area-inset-bottom))] max-[680px]:w-[calc(100%-28px)] max-[680px]:grid-cols-1 max-[680px]:justify-items-start">
        <a
          className="inline-flex items-center gap-2.5 font-heading text-[21px] font-[850] tracking-[-.045em]"
          href="#top"
          aria-label="PickleCrew home"
        >
          <LogoMark />
          <span>PickleCrew</span>
        </a>
        <p className="m-0 text-[9px] tracking-[.08em] text-ink-soft uppercase">
          Made for the group chat, proven on the court.
        </p>
        <span className="justify-self-end text-[9px] tracking-[.08em] text-ink-soft uppercase max-[680px]:justify-self-start">
          © PICKLECREW
        </span>
      </footer>
    </div>
  )
}
