import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import type { User } from '@workos/authkit-tanstack-react-start'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import {
  CalendarDays,
  CircleAlertIcon,
  LogOut,
  UsersRound,
  type LucideIcon,
} from 'lucide-react'
import { useState } from 'react'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Badge } from '~/components/ui/badge'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '~/components/ui/dialog'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '~/components/ui/field'
import { Input } from '~/components/ui/input'
import { Separator } from '~/components/ui/separator'
import { Spinner } from '~/components/ui/spinner'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '~/components/ui/tooltip'
import {
  formatMembershipDuration,
  getCrewLeavingDisabledReason,
  validateCrewLeavingConfirmation,
} from '~/lib/crews/crew-leaving'
import {
  getCrewLeavingEligibility,
  leaveCrew,
} from '~/lib/crews/membership.functions'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/settings',
)({
  loader: async () => ({
    now: new Date().toISOString(),
    crewLeavingEligibility: await getCrewLeavingEligibility(),
  }),
  component: SettingsPage,
})

const authenticatedRoute = getRouteApi('/_authenticated')

function getDisplayName({ user }: { user: User }): string {
  const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ')
  return fullName || user.email
}

function getInitials({ user }: { user: User }): string {
  const initials = [user.firstName, user.lastName]
    .filter(Boolean)
    .map((name) => name?.at(0))
    .join('')

  return initials || user.email.at(0)?.toUpperCase() || '?'
}

function formatRole({ role }: { role: string }): string {
  return role
    .replace(/^org:/, '')
    .split(/[-_]/)
    .filter(Boolean)
    .map((word) => `${word.at(0)?.toUpperCase()}${word.slice(1)}`)
    .join(' ')
}

function formatJoinedDate({ joinedAt }: { joinedAt: string }): string {
  return new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(joinedAt))
}

function SettingsPage() {
  const { activeCrew } = authenticatedRoute.useLoaderData()
  const { now, crewLeavingEligibility } = Route.useLoaderData()
  const { user } = useAuth()

  if (!activeCrew || !user) {
    throw new Error('Settings page rendered without an active crew or user.')
  }

  const displayName = getDisplayName({ user })
  const role = formatRole({ role: activeCrew.role })
  const membershipDuration = formatMembershipDuration({
    joinedAt: activeCrew.joinedAt,
    now: new Date(now),
  })
  const crewLeavingDisabledReason = crewLeavingEligibility.ok
    ? getCrewLeavingDisabledReason({
        canLeave: crewLeavingEligibility.value.canLeave,
      })
    : crewLeavingEligibility.error.message

  return (
    <main className="relative flex flex-1 flex-col overflow-hidden p-5 md:p-8 md:pb-10 xl:p-10">
      <div className="mx-auto flex w-full max-w-4xl flex-col gap-7">
        <header className="flex flex-col gap-2">
          <span className="text-[10px] font-black tracking-[.15em] text-orange uppercase">
            {activeCrew.name}
          </span>
          <h1 className="m-0 font-heading text-[clamp(2.5rem,5vw,4.5rem)] leading-[.9] font-black tracking-[-.055em]">
            Settings
          </h1>
          <p className="m-0 max-w-xl text-sm leading-relaxed text-ink-soft">
            View your profile and active crew membership.
          </p>
        </header>

        <Card>
          <CardHeader>
            <CardTitle>Your profile</CardTitle>
            <CardDescription>
              Your account and membership details for this crew.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-6">
            <div className="flex items-center gap-4">
              <Avatar className="size-14" size="lg">
                {user.profilePictureUrl ? (
                  <AvatarImage src={user.profilePictureUrl} alt={displayName} />
                ) : null}
                <AvatarFallback>{getInitials({ user })}</AvatarFallback>
              </Avatar>
              <div className="flex min-w-0 flex-col gap-1">
                <div className="flex min-w-0 flex-wrap items-center gap-2">
                  <strong className="min-w-0 truncate text-lg">
                    {displayName}
                  </strong>
                  <Badge variant="secondary">{role}</Badge>
                </div>
                <span className="truncate text-sm text-muted-foreground">
                  {user.email}
                </span>
              </div>
            </div>

            <div className="flex flex-col">
              <MembershipDetail
                icon={UsersRound}
                label="Crew"
                value={activeCrew.name}
              />
              <Separator />
              <MembershipDetail
                icon={CalendarDays}
                label="Member for"
                value={membershipDuration}
                supportingText={`Joined ${formatJoinedDate({ joinedAt: activeCrew.joinedAt })}`}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Danger zone</CardTitle>
            <CardDescription>
              Actions here affect your access to {activeCrew.name}.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col">
            <div className="flex flex-col gap-4 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:gap-6">
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <h3 className="font-semibold">Leave this crew</h3>
                <p className="text-sm text-muted-foreground">
                  {crewLeavingDisabledReason ?? (
                    <>
                      You will be signed out and lose access to the crew. Your
                      historical games and statistics will remain.
                    </>
                  )}
                </p>
              </div>
              <div className="self-end sm:ml-auto">
                <LeaveCrewAction
                  crewName={activeCrew.name}
                  disabledReason={crewLeavingDisabledReason}
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </main>
  )
}

function MembershipDetail({
  icon: Icon,
  label,
  value,
  supportingText,
}: {
  icon: LucideIcon
  label: string
  value: string
  supportingText?: string
}) {
  return (
    <div className="flex items-center gap-4 py-4 first:pt-0 last:pb-0">
      <Icon className="size-5 shrink-0 text-muted-foreground" aria-hidden="true" />
      <div className="grid min-w-0 flex-1 gap-1 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <span className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">
          {label}
        </span>
        <span className="flex min-w-0 flex-col sm:items-end sm:text-right">
          <span className="truncate font-semibold">{value}</span>
          {supportingText ? (
            <span className="text-xs text-muted-foreground">
              {supportingText}
            </span>
          ) : null}
        </span>
      </div>
    </div>
  )
}

function LeaveCrewAction({
  crewName,
  disabledReason,
}: {
  crewName: string
  disabledReason?: string
}) {
  if (!disabledReason) {
    return <LeaveCrewDialog crewName={crewName} />
  }

  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <span
            className="inline-flex"
            tabIndex={0}
            aria-label={disabledReason}
          />
        }
      >
        <Button type="button" variant="destructive" disabled>
          <LogOut data-icon="inline-start" />
          Leave crew
        </Button>
      </TooltipTrigger>
      <TooltipContent>
        <p>{disabledReason}</p>
      </TooltipContent>
    </Tooltip>
  )
}

function LeaveCrewDialog({ crewName }: { crewName: string }) {
  const leaveCrewServerFn = useServerFn(leaveCrew)
  const [isOpen, setIsOpen] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const [confirmationError, setConfirmationError] = useState<string>()
  const [submissionError, setSubmissionError] = useState<string>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const hasValidConfirmation = confirmation === crewName

  const resetForm = () => {
    setConfirmation('')
    setConfirmationError(undefined)
    setSubmissionError(undefined)
    setIsSubmitting(false)
  }

  const setDialogOpen = (nextOpen: boolean) => {
    if (!nextOpen && isSubmitting) {
      return
    }

    setIsOpen(nextOpen)

    if (!nextOpen) {
      resetForm()
    }
  }

  const submitCrewLeaving = async () => {
    setSubmissionError(undefined)
    setIsSubmitting(true)

    try {
      const result = await leaveCrewServerFn({
        data: { crewName: confirmation },
      })

      if (!result.ok) {
        if (result.error.type === 'invalid-crew-leaving-confirmation') {
          setConfirmationError(result.error.message)
        } else {
          setSubmissionError(result.error.message)
        }
        setIsSubmitting(false)
        return
      }

      window.location.assign('/api/auth/sign-out')
    } catch {
      setSubmissionError('This crew could not be left. Try again.')
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setDialogOpen}>
      <DialogTrigger render={<Button type="button" variant="destructive" />}>
        <LogOut data-icon="inline-start" />
        Leave crew
      </DialogTrigger>
      <DialogContent>
        <form
          className="flex flex-col gap-6"
          noValidate
          onSubmit={(event) => {
            event.preventDefault()

            if (isSubmitting) {
              return
            }

            setConfirmationError(undefined)
            setSubmissionError(undefined)
            const validation = validateCrewLeavingConfirmation({
              confirmation,
              crewName,
            })

            if (!validation.ok) {
              setConfirmationError(validation.error.message)
              return
            }

            void submitCrewLeaving()
          }}
        >
          <DialogHeader>
            <DialogTitle>Leave {crewName}?</DialogTitle>
            <DialogDescription>
              You will be signed out and lose access to the crew. Your
              historical games and statistics will remain.
            </DialogDescription>
          </DialogHeader>

          <FieldGroup>
            <Field
              data-invalid={confirmationError !== undefined || undefined}
              data-disabled={isSubmitting || undefined}
            >
              <FieldLabel
                className="gap-0.5"
                htmlFor="leave-crew-confirmation"
              >
                Enter <strong>{crewName}</strong> to confirm
              </FieldLabel>
              <Input
                id="leave-crew-confirmation"
                name="confirmation"
                value={confirmation}
                onChange={(event) => {
                  setConfirmation(event.target.value)
                  setConfirmationError(undefined)
                  setSubmissionError(undefined)
                }}
                autoComplete="off"
                autoFocus
                required
                aria-invalid={confirmationError !== undefined || undefined}
                disabled={isSubmitting}
              />
              <FieldError>{confirmationError}</FieldError>
            </Field>
          </FieldGroup>

          {submissionError ? (
            <Alert variant="destructive">
              <CircleAlertIcon />
              <AlertTitle>Crew not left</AlertTitle>
              <AlertDescription>{submissionError}</AlertDescription>
            </Alert>
          ) : null}

          <DialogFooter>
            <DialogClose
              render={<Button type="button" variant="outline" />}
              disabled={isSubmitting}
            >
              Cancel
            </DialogClose>
            <Button
              type="submit"
              variant="destructive"
              disabled={isSubmitting || !hasValidConfirmation}
            >
              {isSubmitting ? (
                <Spinner data-icon="inline-start" />
              ) : (
                <LogOut data-icon="inline-start" />
              )}
              {isSubmitting ? 'Leaving…' : 'Leave crew'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
