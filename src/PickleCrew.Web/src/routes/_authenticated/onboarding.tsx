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
import { useState } from 'react'
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
  SendCrewInvitationsHttpResponse,
} from '~/lib/api/generated/types.gen'
import { createCrew } from '~/lib/crews/crews.functions'
import { sendCrewInvitations } from '~/lib/crews/invitations.functions'
import {
  initialOnboardingState,
  transitionToInvitationConfirmation,
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
        className="relative z-2 mx-auto flex min-h-screen w-[calc(100%-40px)] max-w-[900px] flex-col items-center justify-center gap-12 py-28 text-center max-[680px]:w-[calc(100%-28px)] max-[680px]:gap-10 max-[680px]:pt-28 max-[680px]:pb-16"
        aria-labelledby="onboarding-heading"
      >
        <OnboardingIntro state={onboardingState} />

        <div className="relative mx-auto w-full max-w-[560px] text-left">
          <OnboardingTag state={onboardingState} />
          <Card className="onboarding-form-card border-2 border-ink bg-cream shadow-none ring-0">
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
                onComplete={(result) => {
                  setOnboardingState((state) =>
                    transitionToInvitationConfirmation({ state, result }),
                  )
                }}
              />
            ) : null}
            {onboardingState.type === 'invitation-confirmation' ? (
              <InvitationConfirmationStep
                state={onboardingState}
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
          className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:text-[clamp(3.7rem,18vw,5.5rem)]"
          id="onboarding-heading"
        >
          Name your{' '}
          <span className="onboarding-heading-highlight">chaos.</span>
        </h1>
        <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft">
          Every legendary rivalry needs a home. Give your pickleball crew a name worthy of the
          group chat.
        </p>
      </div>
    )
  }

  if (state.type === 'invite-friends') {
    return (
      <div className="onboarding-intro relative mx-auto max-w-[780px]">
        <OnboardingBadge>
          <Check aria-hidden="true" />
          Crew created
        </OnboardingBadge>
        <h1
          className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:text-[clamp(3.7rem,18vw,5.5rem)]"
          id="onboarding-heading"
        >
          Bring the{' '}
          <span className="onboarding-heading-highlight">chaos.</span>
        </h1>
        <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft">
          {state.crew.name} is official. Invite a friend and give the leaderboard someone to judge.
        </p>
      </div>
    )
  }

  return (
    <div className="onboarding-intro relative mx-auto max-w-[780px]">
      <OnboardingBadge>
        <Send aria-hidden="true" />
        Invitation sent
      </OnboardingBadge>
      <h1
        className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:text-[clamp(3.7rem,18vw,5.5rem)]"
        id="onboarding-heading"
      >
        Game{' '}
        <span className="onboarding-heading-highlight">on.</span>
      </h1>
      <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft">
        Your first invite is on its way. Continue when you're ready.
      </p>
    </div>
  )
}

function OnboardingBadge({ children }: { children: React.ReactNode }) {
  return (
    <div className="inline-flex rotate-[-1deg] items-center gap-2 rounded-full border border-ink bg-lime px-3 py-2 text-[10px] font-extrabold tracking-[.12em] uppercase shadow-[3px_3px_0_var(--ink)]">
      {children}
    </div>
  )
}

function OnboardingTag({ state }: { state: OnboardingState }) {
  return (
    <div className="onboarding-form-tag" aria-hidden="true">
      {state.type === 'create-crew' ? <Users /> : null}
      {state.type === 'invite-friends' ? <Mail /> : null}
      {state.type === 'invitation-confirmation' ? <Check /> : null}
      {state.type === 'create-crew' ? 'Crew assembly point' : null}
      {state.type === 'invite-friends' ? 'Friend assembly point' : null}
      {state.type === 'invitation-confirmation' ? 'Ready to rally' : null}
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
              <CardTitle className="font-heading text-[clamp(2.5rem,4vw,3.5rem)] leading-[.9] font-black tracking-[-.055em]">
                Plant your flag.
              </CardTitle>
              <CardDescription className="font-heading text-base leading-[1.5] text-ink-soft">
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
  onComplete,
}: {
  crewName: string
  onComplete: (result: SendCrewInvitationsHttpResponse) => void
}) {
  const sendCrewInvitationsServerFn = useServerFn(sendCrewInvitations)
  const [emailValidationError, setEmailValidationError] = useState<string>()
  const [submissionError, setSubmissionError] = useState<string>()
  const form = useForm({
    defaultValues: {
      emailAddress: '',
    },
    onSubmit: async ({ value }) => {
      setEmailValidationError(undefined)
      setSubmissionError(undefined)
      const validation = validateCrewInvitationEmail(value.emailAddress)

      if (!validation.ok) {
        setEmailValidationError(validation.error.message)
        return
      }

      const result = await sendCrewInvitationsServerFn({
        data: { emailAddresses: [validation.value] },
      })

      if (!result.ok) {
        setSubmissionError(result.error.message)
        return
      }

      onComplete(result.value)
    },
  })

  return (
    <form
      className="flex flex-col gap-6"
      noValidate
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
                Step 2 of 2
              </span>
              <CardTitle className="font-heading text-[clamp(2.5rem,4vw,3.5rem)] leading-[.9] font-black tracking-[-.055em]">
                Add a rival.
              </CardTitle>
              <CardDescription className="font-heading text-base leading-[1.5] text-ink-soft">
                Invite one friend to join {crewName}.
              </CardDescription>
            </CardHeader>

            <CardContent>
              <FieldGroup>
                <form.Field name="emailAddress">
                  {(field) => {
                    const isInvalid = emailValidationError !== undefined

                    return (
                      <Field
                        data-invalid={isInvalid || undefined}
                        data-disabled={isSubmitting || undefined}
                      >
                        <FieldLabel
                          className="font-heading text-sm font-extrabold"
                          htmlFor="friend-email"
                        >
                          Friend's email
                        </FieldLabel>
                        <Input
                          id="friend-email"
                          name={field.name}
                          type="email"
                          value={field.state.value}
                          variant="crew"
                          placeholder="friend@example.com"
                          onBlur={field.handleBlur}
                          onChange={(event) => {
                            field.handleChange(event.target.value)
                            setEmailValidationError(undefined)
                          }}
                          autoComplete="email"
                          autoFocus
                          aria-invalid={isInvalid || undefined}
                          disabled={isSubmitting}
                        />
                        <FieldError>{emailValidationError}</FieldError>
                      </Field>
                    )
                  }}
                </form.Field>
              </FieldGroup>

              {submissionError ? (
                <Alert variant="destructive">
                  <CircleAlertIcon />
                  <AlertTitle>Could not send invitation</AlertTitle>
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
                {isSubmitting ? 'Sending invitation…' : 'Send invitation'}
                {isSubmitting ? null : <Send data-icon="inline-end" />}
              </Button>
              <p className="m-0 text-center text-[8px] font-bold tracking-[.08em] text-ink-soft uppercase">
                Friendly competition sold separately.
              </p>
            </CardFooter>
          </>
        )}
      </form.Subscribe>
    </form>
  )
}

function InvitationConfirmationStep({
  state,
  onContinue,
}: {
  state: Extract<OnboardingState, { type: 'invitation-confirmation' }>
  onContinue: () => Promise<void>
}) {
  const recipient = state.result.outcomes[0]?.emailAddress
  const [isNavigating, setIsNavigating] = useState(false)
  const [navigationError, setNavigationError] = useState<string>()

  const continueToDashboard = async () => {
    setIsNavigating(true)
    setNavigationError(undefined)

    try {
      await onContinue()
    } catch {
      setNavigationError('Could not open the dashboard. Try again.')
      setIsNavigating(false)
    }
  }

  return (
    <>
      <CardHeader className="gap-3 pt-2">
        <span className="text-[9px] font-black tracking-[.14em] text-orange uppercase">
          All set
        </span>
        <CardTitle className="font-heading text-[clamp(2.5rem,4vw,3.5rem)] leading-[.9] font-black tracking-[-.055em]">
          Invite sent.
        </CardTitle>
        <CardDescription className="font-heading text-base leading-[1.5] text-ink-soft">
          {recipient
            ? `${recipient} has been invited to ${state.crew.name}.`
            : `Your invitation to ${state.crew.name} was sent.`}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Alert aria-live="polite">
          <Check />
          <AlertTitle>Invitation sent</AlertTitle>
          <AlertDescription>
            Continue to your dashboard when you're ready.
          </AlertDescription>
        </Alert>
        {navigationError ? (
          <Alert variant="destructive">
            <CircleAlertIcon />
            <AlertTitle>Could not open dashboard</AlertTitle>
            <AlertDescription>{navigationError}</AlertDescription>
          </Alert>
        ) : null}
      </CardContent>
      <CardFooter>
        <Button
          className="w-full"
          variant="crew"
          size="xl"
          type="button"
          disabled={isNavigating}
          onClick={() => void continueToDashboard()}
        >
          {isNavigating ? <Spinner data-icon="inline-start" /> : null}
          {isNavigating ? 'Opening dashboard…' : 'Continue to dashboard'}
          {isNavigating ? null : <ArrowRight data-icon="inline-end" />}
        </Button>
      </CardFooter>
    </>
  )
}
