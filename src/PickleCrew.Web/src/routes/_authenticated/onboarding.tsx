import { useForm } from '@tanstack/react-form'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import {
  ArrowRight,
  Check,
  CircleAlertIcon,
  Mail,
  Send,
  Sparkles,
  Users,
} from 'lucide-react'
import { useRef, useState } from 'react'
import { LogoMark, PickleBall } from '~/components/picklecrew-brand'
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
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '~/components/ui/field'
import { Input } from '~/components/ui/input'
import { Spinner } from '~/components/ui/spinner'
import type {
  CreateCrewHttpResponse,
  SendCrewInvitationHttpResponse,
} from '~/lib/api/generated/types.gen'
import { createCrew } from '~/lib/crews/crews.functions'
import { sendCrewInvitation } from '~/lib/crews/invitations.functions'
import {
  getCrewInvitationOutcomeMessage,
  initialOnboardingState,
  isCrewInvitationOutcomeTerminal,
  transitionToInvitationStep,
  validateCrewInvitationEmail,
  type OnboardingState,
} from '~/lib/crews/invitations'

export const Route = createFileRoute('/_authenticated/onboarding')({
  component: OnboardingPage,
})

function OnboardingPage() {
  const router = useRouter()
  const [onboardingState, setOnboardingState] = useState<OnboardingState>(
    initialOnboardingState,
  )

  return (
    <main className="onboarding-shell relative isolate min-h-screen overflow-hidden">
      <PickleBall className="pickleball onboarding-ball onboarding-ball-one" />
      <PickleBall className="pickleball onboarding-ball onboarding-ball-two" />

      <div className="absolute top-7 left-8 z-10 inline-flex items-center gap-2.5 font-heading text-[21px] font-[850] tracking-[-.045em] max-[680px]:top-5 max-[680px]:left-5">
        <LogoMark />
        <span>PickleCrew</span>
      </div>

      <section
        className="relative z-2 mx-auto flex min-h-screen w-[calc(100%-40px)] max-w-[900px] flex-col items-center justify-center gap-12 py-28 text-center max-[680px]:w-[calc(100%-28px)] max-[680px]:gap-7 max-[680px]:pt-20 max-[680px]:pb-12"
        aria-labelledby="onboarding-heading"
      >
        <OnboardingIntro state={onboardingState} />

        <div className="relative mx-auto w-full max-w-[560px] text-left">
          <OnboardingTag state={onboardingState} />
          <Card className="onboarding-form-card border-2 border-ink bg-cream shadow-none ring-0 max-[680px]:[--card-spacing:--spacing(4)]">
            {onboardingState.type === 'create-crew' ? (
              <CreateCrewStep
                onComplete={(crew) => {
                  setOnboardingState((state) =>
                    transitionToInvitationStep({ state, crew }),
                  )
                }}
              />
            ) : null}
            {onboardingState.type === 'invite-friends' ? (
              <InviteFriendStep
                crewName={onboardingState.crew.name}
                onContinue={() =>
                  router.invalidate({ sync: true, forcePending: true })
                }
              />
            ) : null}
          </Card>
        </div>
      </section>
    </main>
  )
}

function OnboardingIntro({ state }: { state: OnboardingState }) {
  if (state.type === 'create-crew') {
    return (
      <div className="onboarding-intro relative mx-auto max-w-[780px]">
        <OnboardingBadge>
          <Sparkles aria-hidden="true" />
          Welcome to the club
        </OnboardingBadge>
        <h1
          className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:mt-4 max-[680px]:mb-3 max-[680px]:text-[clamp(2.75rem,13vw,3.5rem)] max-[680px]:leading-[.86]"
          id="onboarding-heading"
        >
          Name your{' '}
          <span className="onboarding-heading-highlight">chaos.</span>
        </h1>
        <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft max-[680px]:text-[.95rem] max-[680px]:leading-[1.4]">
          Every legendary rivalry needs a home. Give your pickleball crew a name worthy of the
          group chat.
        </p>
      </div>
    )
  }

  return (
    <div className="onboarding-intro relative mx-auto max-w-[780px]">
      <OnboardingBadge>
        <Check aria-hidden="true" />
        Crew created
      </OnboardingBadge>
      <h1
        className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:mt-4 max-[680px]:mb-3 max-[680px]:text-[clamp(2.75rem,13vw,3.5rem)] max-[680px]:leading-[.86]"
        id="onboarding-heading"
      >
        Bring the{' '}
        <span className="onboarding-heading-highlight">chaos.</span>
      </h1>
      <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft max-[680px]:text-[.95rem] max-[680px]:leading-[1.4]">
        {state.crew.name} is official. Invite friends one at a time, then head to your dashboard.
      </p>
    </div>
  )
}

function OnboardingBadge({ children }: { children: React.ReactNode }) {
  return (
    <div className="inline-flex rotate-[-1deg] items-center gap-2 rounded-full border border-ink bg-lime px-3 py-2 text-[10px] font-extrabold tracking-[.12em] uppercase shadow-[3px_3px_0_var(--ink)] max-[680px]:py-1.5">
      {children}
    </div>
  )
}

function OnboardingTag({ state }: { state: OnboardingState }) {
  return (
    <div className="onboarding-form-tag" aria-hidden="true">
      {state.type === 'create-crew' ? <Users /> : <Mail />}
      {state.type === 'create-crew'
        ? 'Crew assembly point'
        : 'Friend assembly point'}
    </div>
  )
}

function CreateCrewStep({
  onComplete,
}: {
  onComplete: (crew: CreateCrewHttpResponse) => void
}) {
  const createCrewServerFn = useServerFn(createCrew)
  const switchToOrganizationServerFn = useServerFn(switchToOrganization)
  const [submissionError, setSubmissionError] = useState<string>()
  const form = useForm({
    defaultValues: {
      name: '',
    },
    onSubmit: async ({ value }) => {
      setSubmissionError(undefined)

      try {
        const crew = await createCrewServerFn({
          data: { name: value.name.trim() },
        })
        await switchToOrganizationServerFn({
          data: { organizationId: crew.organizationId },
        })
        onComplete(crew)
      } catch (caughtError) {
        setSubmissionError(
          caughtError instanceof Error
            ? caughtError.message
            : 'Crew creation failed. Try again.',
        )
      }
    },
  })

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(event) => {
        event.preventDefault()
        void form.handleSubmit()
      }}
    >
      <form.Subscribe selector={(state) => state.isSubmitting}>
        {(isSubmitting) => (
          <>
            <CardHeader className="gap-3 pt-2">
              <span className="text-[9px] font-black tracking-[.14em] text-orange uppercase">
                One tiny step
              </span>
              <CardTitle className="font-heading text-[clamp(2.5rem,4vw,3.5rem)] leading-[.9] font-black tracking-[-.055em] max-[680px]:text-[2rem]">
                Plant your flag.
              </CardTitle>
              <CardDescription className="font-heading text-base leading-[1.5] text-ink-soft max-[680px]:text-sm">
                This is the name your wins, streaks, and highly disputed rankings will live under.
              </CardDescription>
            </CardHeader>

            <CardContent>
              <FieldGroup>
                <form.Field
                  name="name"
                  validators={{
                    onChange: ({ value }) => {
                      const length = value.trim().length
                      return length === 0 || length > 100
                        ? 'Crew name must contain between 1 and 100 characters.'
                        : undefined
                    },
                  }}
                >
                  {(field) => {
                    const error = field.state.meta.errors[0]
                    const isInvalid = error !== undefined

                    return (
                      <Field
                        data-invalid={isInvalid || undefined}
                        data-disabled={isSubmitting || undefined}
                      >
                        <FieldLabel
                          className="font-heading text-sm font-extrabold"
                          htmlFor="crew-name"
                        >
                          Crew name
                        </FieldLabel>
                        <Input
                          id="crew-name"
                          name={field.name}
                          value={field.state.value}
                          variant="crew"
                          placeholder="e.g. Kitchen Nightmares"
                          onBlur={field.handleBlur}
                          onChange={(event) => field.handleChange(event.target.value)}
                          maxLength={100}
                          autoComplete="organization"
                          autoFocus
                          aria-describedby="crew-name-help"
                          aria-invalid={isInvalid || undefined}
                          disabled={isSubmitting}
                        />
                        <FieldDescription
                          className="flex items-start justify-between gap-4"
                          id="crew-name-help"
                        >
                          <span>You can invite friends next.</span>
                          <span className="shrink-0 font-mono text-[10px]">
                            {field.state.value.length}/100
                          </span>
                        </FieldDescription>
                        <FieldError>{error}</FieldError>
                      </Field>
                    )
                  }}
                </form.Field>
              </FieldGroup>

              {submissionError ? (
                <Alert variant="destructive">
                  <CircleAlertIcon />
                  <AlertTitle>Could not create crew</AlertTitle>
                  <AlertDescription>{submissionError}</AlertDescription>
                </Alert>
              ) : null}
            </CardContent>

            <CardFooter className="flex-col items-stretch gap-4">
              <Button
                className="w-full"
                variant="crew"
                size="xl"
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? <Spinner data-icon="inline-start" /> : null}
                {isSubmitting ? 'Making it official…' : 'Make it official'}
                {isSubmitting ? null : <ArrowRight data-icon="inline-end" />}
              </Button>
              <p className="m-0 text-center text-[8px] font-bold tracking-[.08em] text-ink-soft uppercase">
                No spreadsheets. No fuzzy memories. Just receipts.
              </p>
            </CardFooter>
          </>
        )}
      </form.Subscribe>
    </form>
  )
}

function InviteFriendStep({
  crewName,
  onContinue,
}: {
  crewName: string
  onContinue: () => Promise<void>
}) {
  const sendCrewInvitationServerFn = useServerFn(sendCrewInvitation)
  const emailInputRef = useRef<HTMLInputElement>(null)
  const [emailAddress, setEmailAddress] = useState('')
  const [emailValidationError, setEmailValidationError] = useState<string>()
  const [submissionError, setSubmissionError] = useState<string>()
  const [outcome, setOutcome] = useState<SendCrewInvitationHttpResponse>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isNavigating, setIsNavigating] = useState(false)
  const [navigationError, setNavigationError] = useState<string>()
  const isBusy = isSubmitting || isNavigating

  const submitInvitation = async (normalizedEmailAddress: string) => {
    setSubmissionError(undefined)
    setOutcome(undefined)
    setIsSubmitting(true)

    try {
      const result = await sendCrewInvitationServerFn({
        data: { emailAddress: normalizedEmailAddress },
      })

      if (!result.ok) {
        setSubmissionError(result.error.message)
        return
      }

      setOutcome(result.value)

      if (isCrewInvitationOutcomeTerminal(result.value)) {
        setEmailAddress('')
        emailInputRef.current?.focus()
      }
    } catch {
      setSubmissionError('Invitations are unavailable right now. Try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const continueToDashboard = async () => {
    setNavigationError(undefined)
    setIsNavigating(true)

    try {
      await onContinue()
    } catch {
      setNavigationError('Could not open the dashboard. Try again.')
      setIsNavigating(false)
    }
  }

  return (
    <form
      className="flex flex-col gap-6"
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        setEmailValidationError(undefined)
        setSubmissionError(undefined)
        const validation = validateCrewInvitationEmail(emailAddress)

        if (!validation.ok) {
          setEmailValidationError(validation.error.message)
          return
        }

        void submitInvitation(validation.value)
      }}
    >
      <CardHeader className="gap-3 pt-2">
        <span className="text-[9px] font-black tracking-[.14em] text-orange uppercase">
          Step 2 of 2
        </span>
        <CardTitle className="font-heading text-[clamp(2.5rem,4vw,3.5rem)] leading-[.9] font-black tracking-[-.055em] max-[680px]:text-[2rem]">
          Add a rival.
        </CardTitle>
        <CardDescription className="font-heading text-base leading-[1.5] text-ink-soft max-[680px]:text-sm">
          Invite friends to {crewName} one at a time. Continue whenever you're ready.
        </CardDescription>
      </CardHeader>

      <CardContent className="flex flex-col gap-5">
        <FieldGroup>
          <Field
            data-invalid={emailValidationError !== undefined || undefined}
            data-disabled={isBusy || undefined}
          >
            <FieldLabel
              className="font-heading text-sm font-extrabold"
              htmlFor="friend-email"
            >
              Friend's email
            </FieldLabel>
            <Input
              ref={emailInputRef}
              id="friend-email"
              name="emailAddress"
              type="email"
              value={emailAddress}
              variant="crew"
              placeholder="friend@example.com"
              onChange={(event) => {
                setEmailAddress(event.target.value)
                setEmailValidationError(undefined)
                setOutcome(undefined)
              }}
              autoComplete="email"
              autoFocus
              aria-invalid={emailValidationError !== undefined || undefined}
              disabled={isBusy}
            />
            <FieldError>{emailValidationError}</FieldError>
          </Field>
        </FieldGroup>

        {outcome ? <CrewInvitationOutcomeAlert outcome={outcome} /> : null}

        {submissionError ? (
          <Alert variant="destructive">
            <CircleAlertIcon />
            <AlertTitle>Could not send invitation</AlertTitle>
            <AlertDescription>{submissionError}</AlertDescription>
          </Alert>
        ) : null}

        {navigationError ? (
          <Alert variant="destructive">
            <CircleAlertIcon />
            <AlertTitle>Could not open dashboard</AlertTitle>
            <AlertDescription>{navigationError}</AlertDescription>
          </Alert>
        ) : null}
      </CardContent>

      <CardFooter className="flex-col items-stretch gap-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <Button
            className="w-full"
            variant="crew"
            size="xl"
            type="submit"
            disabled={isBusy}
          >
            {isSubmitting ? <Spinner data-icon="inline-start" /> : null}
            {isSubmitting ? 'Sending invite…' : 'Send invite'}
            {isSubmitting ? null : <Send data-icon="inline-end" />}
          </Button>
          <Button
            className="w-full"
            variant="paper"
            size="xl"
            type="button"
            disabled={isBusy}
            onClick={() => void continueToDashboard()}
          >
            {isNavigating ? <Spinner data-icon="inline-start" /> : null}
            {isNavigating ? 'Opening dashboard…' : 'Continue to dashboard'}
            {isNavigating ? null : <ArrowRight data-icon="inline-end" />}
          </Button>
        </div>
        <p className="m-0 text-center text-[8px] font-bold tracking-[.08em] text-ink-soft uppercase">
          Friendly competition sold separately.
        </p>
      </CardFooter>
    </form>
  )
}

function CrewInvitationOutcomeAlert({
  outcome,
}: {
  outcome: SendCrewInvitationHttpResponse
}) {
  const title = getCrewInvitationOutcomeTitle(outcome)
  const isFailure = outcome.status === 'failed'

  return (
    <Alert
      variant={isFailure ? 'destructive' : 'default'}
      role="status"
      aria-live="polite"
    >
      {isFailure ? <CircleAlertIcon /> : <Check />}
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>
        {getCrewInvitationOutcomeMessage(outcome)}
      </AlertDescription>
    </Alert>
  )
}

function getCrewInvitationOutcomeTitle(
  outcome: SendCrewInvitationHttpResponse,
): string {
  switch (outcome.status) {
    case 'sent':
      return 'Invitation sent'
    case 'already-active-member':
      return 'Already in the crew'
    case 'already-pending':
      return 'Invitation already pending'
    case 'failed':
      return 'Could not send invitation'
  }
}
