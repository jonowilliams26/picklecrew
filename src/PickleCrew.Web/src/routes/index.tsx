import { createFileRoute } from '@tanstack/react-router'
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
import { buttonVariants } from '~/components/ui/button'

export const Route = createFileRoute('/')({
  component: HomePage,
  ssr: true,
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

function LogoMark() {
  return (
    <span className="logo-mark" aria-hidden="true">
      <svg viewBox="0 0 64 64" shapeRendering="geometricPrecision">
        <circle className="logo-ball" cx="32" cy="32" r="28" />
        <circle className="logo-hole" cx="20" cy="15" r="4" />
        <circle className="logo-hole" cx="43" cy="19" r="4" />
        <circle className="logo-hole" cx="31" cy="30" r="4" />
        <circle className="logo-hole" cx="15" cy="38" r="4" />
        <circle className="logo-hole" cx="48" cy="40" r="4" />
        <circle className="logo-hole" cx="29" cy="49" r="4" />
      </svg>
    </span>
  )
}

function PickleBall({ className }: { className: string }) {
  return (
    <span className={className} aria-hidden="true">
      <i />
      <i />
      <i />
      <i />
      <i />
    </span>
  )
}

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
    <div className="landing-shell">
      <header className="landing-header">
        <a className="brand" href="#top" aria-label="PickleCrew home">
          <LogoMark />
          <span>PickleCrew</span>
        </a>
        <nav aria-label="Main navigation">
          <a href="#stats">The receipts</a>
          <a href="#how-it-works">How it works</a>
        </nav>
        <a
          className={buttonVariants({ variant: 'crew', size: 'lg' })}
          href="#join-the-crew"
        >
          Start a crew
          <ArrowRight data-icon="inline-end" />
        </a>
      </header>

      <main id="top">
        <section className="hero-section" aria-labelledby="hero-heading">
          <div className="hero-copy">
            <div className="eyebrow">
              <Zap aria-hidden="true" />
              <span>Stats for competitive friendships</span>
            </div>
            <h1 id="hero-heading">
              Own the
              <span className="hero-highlight"> bragging rights.</span>
            </h1>
            <p className="hero-lede">
              Your crew already keeps score. PickleCrew keeps the receipts — every
              win, streak, rivalry, and suspiciously unbeatable doubles pairing.
            </p>
            <div className="hero-actions">
              <a
                className={buttonVariants({ variant: 'crew', size: 'xl' })}
                href="#stats"
              >
                See who&apos;s really the best
                <ArrowDownRight data-icon="inline-end" />
              </a>
              <a className="text-link" href="#how-it-works">
                How the chaos works
                <ArrowRight aria-hidden="true" />
              </a>
            </div>
            <div className="crew-proof" aria-label="Made for every kind of crew">
              <div className="mini-faces" aria-hidden="true">
                <span>MJ</span>
                <span>DK</span>
                <span>SR</span>
                <span>+8</span>
              </div>
              <p>
                Built for friends who say
                <strong> “we&apos;re not that competitive.”</strong>
              </p>
            </div>
          </div>

          <div className="hero-stage" aria-label="Example crew leaderboard">
            <div className="court-lines" aria-hidden="true">
              <span className="court-net" />
              <span className="court-center" />
              <span className="court-kitchen court-kitchen-left" />
              <span className="court-kitchen court-kitchen-right" />
            </div>
            <PickleBall className="pickleball pickleball-one" />
            <PickleBall className="pickleball pickleball-two" />
            <article className="scoreboard-card">
              <header className="scoreboard-header">
                <div>
                  <span className="micro-label">CREW #014</span>
                  <h2>Dink Dynasty</h2>
                </div>
                <span className="live-pill"><i /> LIVE</span>
              </header>
              <div className="scoreboard-subhead">
                <span>This week</span>
                <span>Crew score</span>
              </div>
              <ol className="leader-list">
                {leaderboard.map((player, index) => (
                  <li className={index === 0 ? 'is-leading' : undefined} key={player.name}>
                    <span className="player-rank">{player.rank}</span>
                    <span className={`player-avatar player-avatar-${index + 1}`}>
                      {player.initials}
                    </span>
                    <span className="player-name">
                      {player.name}
                      {index === 0 && <Crown aria-label="Crew leader" />}
                    </span>
                    <span className="player-record">{player.record}</span>
                    <strong className="player-score">
                      {player.score}
                      <small>pts</small>
                    </strong>
                  </li>
                ))}
              </ol>
              <footer className="scoreboard-footer">
                <div className="tiny-trophy" aria-hidden="true"><Trophy /></div>
                <div>
                  <span>Current crew boss</span>
                  <strong>Maya “No Mercy” Chen</strong>
                </div>
                <span className="trend">↑ 2</span>
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

        <section className="stats-section" id="stats" aria-labelledby="stats-heading">
          <div className="section-kicker" data-reveal="slide-left">
            <span>THE RECEIPTS</span>
            <i />
          </div>
          <div className="section-heading-row" data-reveal="rise">
            <h2 id="stats-heading">Every game adds fuel to the rivalry.</h2>
            <p>
              No spreadsheets. No fuzzy memories. Just gloriously undeniable proof
              of who showed up and who got pickled.
            </p>
          </div>

          <div className="stats-grid">
            <div data-reveal="tilt-left">
              <article className="feature-card win-card">
              <header>
                <span>WIN PERCENTAGE</span>
                <Award aria-hidden="true" />
              </header>
              <div className="win-number">68<span>%</span></div>
              <div className="win-meter" aria-label="68 percent win rate">
                <span />
              </div>
              <footer>
                <strong>+12%</strong>
                <span>since the new paddle arrived</span>
              </footer>
              </article>
            </div>

            <div data-reveal="tilt-right">
              <article className="feature-card streak-card">
              <header>
                <span>LONGEST STREAK</span>
                <Flame aria-hidden="true" />
              </header>
              <div className="streak-number">7</div>
              <p>straight wins</p>
              <div className="streak-dots" aria-hidden="true">
                {Array.from({ length: 7 }, (_, index) => <i key={index}>W</i>)}
              </div>
              <footer>ENDED BY JORDAN · 11–9</footer>
              </article>
            </div>

            <div data-reveal="slide-left">
              <article className="feature-card ranking-card">
              <header>
                <div>
                  <span>CREW LEADERBOARD · TOTAL POINTS</span>
                  <h3>The ladder never lies.</h3>
                </div>
                <Medal aria-hidden="true" />
              </header>
              <ol>
                {leaderboard.slice(0, 3).map((player, index) => (
                  <li key={player.name}>
                    <span className="ranking-place">{index + 1}</span>
                    <span className={`player-avatar player-avatar-${index + 1}`}>
                      {player.initials}
                    </span>
                    <strong className="ranking-name">
                      {player.name}
                      {index === 0 && <Crown aria-label="Crew leader" />}
                    </strong>
                    <span>{player.record}</span>
                    <b className="ranking-score">
                      {player.score}
                      <small>pts</small>
                    </b>
                  </li>
                ))}
              </ol>
              <p className="ranking-note">Every result moves the score. Excuses earn zero points.</p>
              </article>
            </div>

            <div data-reveal="slide-right">
              <article className="feature-card pairing-card">
              <header>
                <span>BEST DOUBLES PAIRING</span>
                <Users aria-hidden="true" />
              </header>
              <div className="pairing-visual" aria-hidden="true">
                <div className="pair-avatar pair-avatar-one">M</div>
                <span>+</span>
                <div className="pair-avatar pair-avatar-two">D</div>
              </div>
              <h3>Maya &amp; Dev</h3>
              <div className="pairing-score">
                <strong>82%</strong>
                <span>win rate together</span>
              </div>
              <footer>Someone split them up.</footer>
              </article>
            </div>
          </div>
        </section>

        <section
          className="trophies-section"
          id="trophies"
          aria-labelledby="trophies-heading"
        >
          <div className="trophies-inner">
            <div className="trophies-heading">
              <div data-reveal="slide-left">
                <div className="section-kicker section-kicker-light">
                  <span>TROPHY CABINET</span>
                  <i />
                </div>
                <h2 id="trophies-heading">
                  Your crew&apos;s trophy cabinet gets weird fast.
                </h2>
              </div>
              <p data-reveal="rise">
                Wins are nice. Hyper-specific proof of chaos is better. Every game
                can unlock another deeply serious honor.
              </p>
            </div>

            <div className="trophy-showcase">
              <article className="trophy-featured" data-reveal="tilt-left">
                <header>
                  <span>FRESHLY UNLOCKED</span>
                  <strong>RARE-ISH</strong>
                </header>
                <div className="trophy-emblem" aria-hidden="true">
                  <Trophy />
                  <Sparkles />
                </div>
                <div className="trophy-featured-copy">
                  <span>11–0 VICTORY</span>
                  <h3>That&apos;s a Paddlin&apos;</h3>
                  <p>
                    Win 11–0. No crumbs, no mercy, no alternate version of events.
                  </p>
                </div>
                <footer>
                  <span className="trophy-avatar" aria-hidden="true">M</span>
                  <div>
                    <small>UNLOCKED BY</small>
                    <strong>Maya “No Mercy” Chen</strong>
                  </div>
                  <b>11–0</b>
                </footer>
              </article>

              <div className="trophy-grid">
                {achievements.map((achievement) => {
                  const AchievementIcon = achievement.icon

                  return (
                    <article
                      className={`trophy-card trophy-card-${achievement.state}`}
                      data-reveal="rise"
                      key={achievement.title}
                    >
                      <header>
                        <span className="trophy-card-icon" aria-hidden="true">
                          <AchievementIcon />
                        </span>
                        <span className="trophy-card-badge">{achievement.badge}</span>
                      </header>
                      <h3>{achievement.title}</h3>
                      <p>{achievement.description}</p>
                      <footer>
                        {achievement.progress ? (
                          <div
                            className={`trophy-progress trophy-progress-${achievement.progress}`}
                            aria-hidden="true"
                          >
                            <span />
                          </div>
                        ) : (
                          <i aria-hidden="true" />
                        )}
                        <span>{achievement.footer}</span>
                      </footer>
                    </article>
                  )
                })}
              </div>
            </div>

            <p className="trophy-fine-print" data-reveal="rise">
              ACHIEVEMENTS ARE PERMANENT. CONTEXT IS OPTIONAL.
            </p>
          </div>
        </section>

        <section className="rally-section" id="how-it-works" aria-labelledby="rally-heading">
          <div className="rally-copy" data-reveal="slide-left">
            <div className="section-kicker section-kicker-light">
              <span>POST-GAME RITUAL</span>
              <i />
            </div>
            <h2 id="rally-heading">From final point to group chat chaos.</h2>
            <p>
              Record the result, watch the rankings move, then share the evidence
              before the losing side rewrites history.
            </p>
            <ol className="steps-list">
              <li data-reveal="rise">
                <span>01</span>
                <div><strong>Log the game</strong><small>Singles, doubles, scores. Done.</small></div>
              </li>
              <li data-reveal="rise">
                <span>02</span>
                <div><strong>Watch stats shuffle</strong><small>Streaks rise. Leaders wobble.</small></div>
              </li>
              <li data-reveal="rise">
                <span>03</span>
                <div><strong>Deploy bragging rights</strong><small>Bring screenshots to the group chat.</small></div>
              </li>
            </ol>
          </div>

          <div
            className="chat-stage"
            data-reveal="pop"
            aria-label="Example post-game group chat"
          >
            <PickleBall className="pickleball chat-ball" />
            <div className="phone-card">
              <header>
                <div className="chat-faces" aria-hidden="true">
                  <span>M</span><span>D</span><span>J</span>
                </div>
                <div><strong>Dink Dynasty</strong><small>4 members · 3 typing...</small></div>
              </header>
              <div className="chat-feed">
                <div className="chat-message chat-message-system">
                  <Trophy aria-hidden="true" />
                  <span><strong>Maya + Dev won 11–7</strong>New best doubles pairing: 82%</span>
                </div>
                <div className="chat-message chat-message-left">Wind was definitely a factor.</div>
                <div className="chat-message chat-message-right">We played indoors, Jordan.</div>
                <div className="typing-dots" aria-label="Someone is typing"><i /><i /><i /></div>
              </div>
              <footer><span>Send your excuse...</span><ArrowRight aria-hidden="true" /></footer>
            </div>
            <div className="chat-stamp" aria-hidden="true">RECEIPTS<br />ATTACHED</div>
          </div>
        </section>

        <section className="manifesto-section" aria-labelledby="manifesto-heading">
          <div data-reveal="rise">
            <span className="manifesto-label">THE CREW CODE</span>
            <h2 id="manifesto-heading">Play nice. Track everything.</h2>
          </div>
          <div className="manifesto-rules">
            <p data-reveal="tilt-left"><span>01</span>All heroic wins deserve documentation.</p>
            <p data-reveal="rise"><span>02</span>All “off days” still count.</p>
            <p data-reveal="tilt-right"><span>03</span>The leaderboard may be questioned, but never ignored.</p>
          </div>
        </section>

        <section className="final-cta" id="join-the-crew" aria-labelledby="final-heading">
          <div className="final-court" aria-hidden="true"><span /><span /><span /></div>
          <PickleBall className="pickleball final-ball" />
          <div className="final-copy" data-reveal="pop">
            <span className="micro-label">YOUR NEXT ERA STARTS HERE</span>
            <h2 id="final-heading">Ready to settle this on the court?</h2>
            <p>Create the crew. Log the games. Keep the crown moving.</p>
            <a
              className={buttonVariants({ variant: 'paper', size: 'xl' })}
              href="#top"
            >
              Start the rivalry
              <ArrowRight data-icon="inline-end" />
            </a>
          </div>
          <div className="crown-doodle" aria-hidden="true"><Crown /></div>
        </section>
      </main>

      <footer className="landing-footer">
        <a className="brand" href="#top" aria-label="PickleCrew home">
          <LogoMark />
          <span>PickleCrew</span>
        </a>
        <p>Made for the group chat, proven on the court.</p>
        <span>© PICKLECREW</span>
      </footer>
    </div>
  )
}
