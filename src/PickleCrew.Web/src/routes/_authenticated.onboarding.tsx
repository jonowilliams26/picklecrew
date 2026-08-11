import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import { switchToOrganization } from '@workos/authkit-tanstack-react-start'
import { CircleAlertIcon } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import { Field, FieldDescription, FieldLabel } from '~/components/ui/field'
import { Input } from '~/components/ui/input'
import { Spinner } from '~/components/ui/spinner'
import {
  createCrew,
  getActiveMemberships,
} from '~/lib/onboarding/onboarding-functions'
import { decideMembershipRoute } from '~/lib/onboarding/membership-routing'

export const Route = createFileRoute('/_authenticated/onboarding')({
  loader: async () => {
    const context = await getActiveMemberships()
    const decision = decideMembershipRoute(
      context.memberships,
      context.currentOrganizationId,
    )

    if (decision.destination === '/dashboard') {
      if (decision.organizationIdToActivate) {
        await switchToOrganization({
          data: { organizationId: decision.organizationIdToActivate },
        })
      }

      throw redirect({ to: '/dashboard' })
    }
  },
  component: OnboardingPage,
})

function OnboardingPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string>()
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(undefined)

    const formData = new FormData(event.currentTarget)
    const name = String(formData.get('name') ?? '').trim()

    if (name.length === 0 || name.length > 100) {
      setError('Crew name must contain between 1 and 100 characters.')
      return
    }

    setIsSubmitting(true)

    try {
      const crew = await createCrew({ data: { name } })
      await switchToOrganization({
        data: { organizationId: crew.organizationId },
      })
      await navigate({ to: '/dashboard' })
    } catch (caughtError) {
      setError(
        caughtError instanceof Error
          ? caughtError.message
          : 'Crew creation failed. Try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

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
          <form className="space-y-6" onSubmit={handleSubmit}>
            <Field data-invalid={error ? true : undefined}>
              <FieldLabel htmlFor="crew-name">Crew name</FieldLabel>
              <Input
                id="crew-name"
                name="name"
                maxLength={100}
                autoComplete="organization"
                aria-invalid={error ? true : undefined}
                disabled={isSubmitting}
                required
              />
              <FieldDescription>Between 1 and 100 characters.</FieldDescription>
            </Field>

            {error ? (
              <Alert variant="destructive">
                <CircleAlertIcon />
                <AlertTitle>Could not create crew</AlertTitle>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            ) : null}

            <Button
              className="w-full"
              variant="crew"
              size="lg"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner /> : null}
              {isSubmitting ? 'Creating crew…' : 'Create crew'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  )
}
