import { useForm } from '@tanstack/react-form'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import {
  ArrowRight,
  CircleAlertIcon,
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
import { createCrew } from '~/lib/crews/crews.functions'

export const Route = createFileRoute('/_authenticated/onboarding')({
  component: OnboardingPage,
})

function OnboardingPage() {
  const router = useRouter()
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
        await router.invalidate({ sync: true, forcePending: true })
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
        <div className="onboarding-intro relative mx-auto max-w-[780px]">
          <div className="inline-flex rotate-[-1deg] items-center gap-2 rounded-full border border-ink bg-lime px-3 py-2 text-[10px] font-extrabold tracking-[.12em] uppercase shadow-[3px_3px_0_var(--ink)]">
            <Sparkles aria-hidden="true" />
            Welcome to the club
          </div>
          <h1
            className="mt-7 mb-6 font-heading text-[clamp(4rem,7vw,7rem)] font-black leading-[.82] tracking-[-.075em] max-[680px]:text-[clamp(3.7rem,18vw,5.5rem)]"
            id="onboarding-heading"
          >
            Name your
            <span className="onboarding-heading-highlight"> chaos.</span>
          </h1>
          <p className="mx-auto my-0 max-w-[620px] font-heading text-[clamp(1.05rem,1.7vw,1.25rem)] leading-[1.55] font-[540] text-ink-soft">
            Every legendary rivalry needs a home. Give your pickleball crew a name worthy of the
            group chat.
          </p>
        </div>

        <div className="relative mx-auto w-full max-w-[560px] text-left">
          <div className="onboarding-form-tag" aria-hidden="true">
            <Users />
            Crew assembly point
          </div>
          <Card className="onboarding-form-card border-2 border-ink bg-cream shadow-none ring-0">
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
                        This is the name your wins, streaks, and highly disputed rankings will live
                        under.
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
                                  <span>You can invite friends later.</span>
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
          </Card>
        </div>
      </section>
    </main>
  )
}
