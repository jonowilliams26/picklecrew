import { LogoMark, PickleBall } from '~/components/picklecrew-brand'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import { Spinner } from '~/components/ui/spinner'

export function LoadingScreen() {
  return (
    <main
      className="loading-shell relative isolate flex min-h-dvh items-center justify-center overflow-hidden px-5 py-28 font-heading"
      aria-busy="true"
      aria-live="polite"
    >
      <PickleBall className="pickleball loading-ball loading-ball-one" />
      <PickleBall className="pickleball loading-ball loading-ball-two" />
      <PickleBall className="pickleball loading-ball loading-ball-three" />
      <PickleBall className="pickleball loading-ball loading-ball-four" />
      <PickleBall className="pickleball loading-ball loading-ball-five" />
      <PickleBall className="pickleball loading-ball loading-ball-six" />
      <PickleBall className="pickleball loading-ball loading-ball-seven" />
      <PickleBall className="pickleball loading-ball loading-ball-eight" />
      <PickleBall className="pickleball loading-ball loading-ball-nine" />
      <PickleBall className="pickleball loading-ball loading-ball-ten" />

      <div className="absolute top-5 left-5 z-10 inline-flex items-center gap-2.5 text-xl font-black tracking-[-.045em] sm:top-7 sm:left-8">
        <LogoMark />
        <span>PickleCrew</span>
      </div>

      <section
        className="relative z-2 w-full max-w-xl"
        aria-labelledby="loading-heading"
      >
        <div className="loading-card-tag">
          <Spinner />
          <span>Rally in progress</span>
        </div>

        <Card
          className="loading-card gap-5 overflow-visible border-2 border-ink bg-cream py-7 text-ink ring-0 sm:gap-6 sm:py-9"
        >
          <CardHeader className="items-center gap-4 px-6 text-center sm:px-10">
            <div className="loading-court" aria-hidden="true">
              <div className="loading-court-ball">
                <LogoMark />
              </div>
            </div>

            <span className="font-mono text-[9px] font-extrabold tracking-[.15em] text-orange uppercase">
              Getting things ready
            </span>
            <CardTitle
              className="max-w-md text-[clamp(2.75rem,8vw,4.5rem)] leading-[.85] font-black tracking-[-.065em]"
              id="loading-heading"
            >
              Warming up the court.
            </CardTitle>
            <CardDescription className="max-w-md font-heading text-base leading-relaxed text-ink-soft sm:text-lg">
              Checking the lineup and getting the scoreboard ready for the next
              rally.
            </CardDescription>
          </CardHeader>

          <CardContent className="px-6 sm:px-10">
            <div className="loading-progress" aria-hidden="true">
              <span />
            </div>
          </CardContent>

          <CardFooter className="justify-center gap-3 px-6 text-[10px] font-extrabold tracking-[.11em] text-ink-soft uppercase sm:px-10">
            <span className="loading-dots" aria-hidden="true">
              <i />
              <i />
              <i />
            </span>
            Loading your crew
          </CardFooter>
        </Card>
      </section>
    </main>
  )
}
