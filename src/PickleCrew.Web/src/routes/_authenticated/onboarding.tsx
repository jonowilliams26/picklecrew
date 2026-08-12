import { useForm } from '@tanstack/react-form'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import { CircleAlertIcon } from 'lucide-react'
import { useState } from 'react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
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
import { createCrew } from '~/lib/onboarding/onboarding.functions'

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
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-lg border-2 border-ink shadow-[6px_7px_0_var(--orange)]">
        <CardHeader>
          <CardTitle className="font-heading text-3xl">Create your crew</CardTitle>
          <CardDescription>
            Give your pickleball group a name. You can invite friends later.
          </CardDescription>
        </CardHeader>
        <CardContent>
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
                            <FieldLabel htmlFor="crew-name">
                              Crew name
                            </FieldLabel>
                            <Input
                              id="crew-name"
                              name={field.name}
                              value={field.state.value}
                              onBlur={field.handleBlur}
                              onChange={(event) =>
                                field.handleChange(event.target.value)
                              }
                              maxLength={100}
                              autoComplete="organization"
                              aria-invalid={isInvalid || undefined}
                              disabled={isSubmitting}
                            />
                            <FieldDescription>
                              Between 1 and 100 characters.
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

                  <Button
                    className="w-full"
                    variant="crew"
                    size="lg"
                    type="submit"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? (
                      <Spinner data-icon="inline-start" />
                    ) : null}
                    {isSubmitting ? 'Creating crew…' : 'Create crew'}
                  </Button>
                </>
              )}
            </form.Subscribe>
          </form>
        </CardContent>
      </Card>
    </main>
  )
}
