import {
  createFileRoute,
  getRouteApi,
  Link,
  useRouter,
} from '@tanstack/react-router'
import { useServerFn } from '@tanstack/react-start'
import {
  ChevronLeft,
  ChevronRight,
  CircleAlertIcon,
  Clock3,
  Crown,
  MailPlus,
  Send,
  UsersRound,
} from 'lucide-react'
import { useState } from 'react'
import { z } from 'zod'
import { Alert, AlertDescription, AlertTitle } from '~/components/ui/alert'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Badge } from '~/components/ui/badge'
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
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '~/components/ui/dialog'
import { Field, FieldError, FieldGroup, FieldLabel } from '~/components/ui/field'
import { Input } from '~/components/ui/input'
import { Spinner } from '~/components/ui/spinner'
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '~/components/ui/table'
import { toast } from '~/components/ui/toast'
import type { CrewMemberHttpResponse } from '~/lib/api/generated/types.gen'
import { getCrewMembers } from '~/lib/crews/crew-members.functions'
import { sendCrewInvitation } from '~/lib/crews/invitations.functions'
import {
  getCrewInvitationOutcomeMessage,
  validateCrewInvitationEmail,
} from '~/lib/crews/invitations'
import { cn } from '~/lib/utils'

const crewSearchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
})

const fallbackAvatarStyles = [
  'bg-orange text-ink',
  'bg-lime text-ink',
  'bg-aqua text-ink',
  'bg-purple text-white',
] as const

export const Route = createFileRoute('/_authenticated/_active-crew/crew')({
  validateSearch: crewSearchSchema,
  loaderDeps: ({ search }) => ({ page: search.page }),
  loader: ({ deps }) => getCrewMembers({ data: { page: deps.page } }),
  component: CrewPage,
})

const authenticatedRoute = getRouteApi('/_authenticated')

function getInitials({ entry }: { entry: CrewMemberHttpResponse }): string {
  const words = entry.displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
  const initials = words.map((word) => word.at(0)?.toUpperCase()).join('')

  return initials || entry.emailAddress.at(0)?.toUpperCase() || '?'
}

function getFallbackAvatarStyle({
  entry,
}: {
  entry: CrewMemberHttpResponse
}): string {
  const colorIndex = [...entry.id].reduce(
    (hash, character) => hash + character.charCodeAt(0),
    0,
  )

  return (
    fallbackAvatarStyles[colorIndex % fallbackAvatarStyles.length] ??
    fallbackAvatarStyles[0]
  )
}

function formatRole({ role }: { role: string }): string {
  return role
    .replace(/^org:/, '')
    .split(/[-_]/)
    .filter(Boolean)
    .map((word) => `${word.at(0)?.toUpperCase()}${word.slice(1)}`)
    .join(' ')
}

function CrewPage() {
  const membersResult = Route.useLoaderData()
  const { activeCrew } = authenticatedRoute.useLoaderData()
  const router = useRouter()

  if (!activeCrew) {
    throw new Error('Crew page rendered without an active crew.')
  }

  if (!membersResult.ok) {
    return (
      <main className="flex flex-1 items-start justify-center p-4 md:p-10">
        <Card className="w-full max-w-2xl border-2 border-border shadow-[6px_7px_0_var(--ink)]">
          <CardHeader>
            <CardTitle>Crew</CardTitle>
            <CardDescription>
              Active Crew membership is required to view this roster.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Alert variant="destructive">
              <CircleAlertIcon />
              <AlertTitle>Crew unavailable</AlertTitle>
              <AlertDescription>{membersResult.error.message}</AlertDescription>
            </Alert>
          </CardContent>
          <CardFooter>
            <Button
              type="button"
              onClick={() => void router.invalidate({ forcePending: true })}
            >
              Try again
            </Button>
          </CardFooter>
        </Card>
      </main>
    )
  }

  const roster = membersResult.value
  const firstEntryNumber = (roster.page - 1) * roster.pageSize + 1
  const lastEntryNumber = Math.min(
    roster.page * roster.pageSize,
    roster.totalCount,
  )

  return (
    <main className="relative flex flex-1 flex-col overflow-hidden p-5 md:p-8 md:pb-10 xl:p-10">
      <div className="mx-auto flex w-full max-w-[1480px] flex-col gap-7">
        <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex flex-col gap-2">
            <span className="text-[10px] font-black tracking-[.15em] text-orange uppercase">
              {activeCrew.name}
            </span>
            <h1 className="m-0 font-heading text-[clamp(2.5rem,5vw,4.5rem)] leading-[.9] font-black tracking-[-.055em]">
              The crew
            </h1>
            <p className="m-0 max-w-xl text-sm leading-relaxed text-ink-soft">
              The people you play with, compete with, and brag to.
            </p>
          </div>
          {activeCrew.role === 'admin' ? <InviteCrewMemberDialog /> : null}
        </header>

        <Card className="gap-0 border-2 border-ink bg-cream pb-0 text-ink shadow-[7px_8px_0_var(--ink)]">
          <CardHeader className="flex flex-row items-center justify-between gap-3 pb-3">
            <CardTitle className="font-heading text-2xl font-black tracking-[-.035em]">
              Crew roster
            </CardTitle>
            <Badge variant="secondary">
              <UsersRound data-icon="inline-start" />
              {roster.totalCount} {roster.totalCount === 1 ? 'person' : 'people'}
            </Badge>
          </CardHeader>
          <CardContent className="px-0">
            <Table>
              <TableCaption className="sr-only">
                Active crew members and pending crew invitation recipients.
              </TableCaption>
              <TableHeader>
                <TableRow className="border-line hover:bg-transparent">
                  <TableHead className="h-12 px-4 text-[10px] font-black tracking-[.12em] uppercase md:px-6">
                    Person
                  </TableHead>
                  <TableHead className="hidden h-12 px-6 text-right text-[10px] font-black tracking-[.12em] uppercase md:table-cell">
                    Role
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {roster.items.map((entry) => (
                  <CrewRosterRow entry={entry} key={entry.id} />
                ))}
              </TableBody>
            </Table>
          </CardContent>
          <CardFooter className="flex-col gap-3 border-t-2 border-line py-3 sm:flex-row sm:justify-between">
            <p className="m-0 text-xs text-muted-foreground">
              Showing {firstEntryNumber}–{lastEntryNumber} of {roster.totalCount}
            </p>
            <div className="flex items-center gap-2">
              {roster.page > 1 ? (
                <Button
                  variant="outline"
                  size="sm"
                  render={<Link to="/crew" search={{ page: roster.page - 1 }} />}
                >
                  <ChevronLeft data-icon="inline-start" />
                  Previous
                </Button>
              ) : (
                <Button variant="outline" size="sm" disabled>
                  <ChevronLeft data-icon="inline-start" />
                  Previous
                </Button>
              )}
              <span className="min-w-20 text-center font-mono text-xs font-bold">
                Page {roster.page} of {roster.totalPages}
              </span>
              {roster.page < roster.totalPages ? (
                <Button
                  variant="outline"
                  size="sm"
                  render={<Link to="/crew" search={{ page: roster.page + 1 }} />}
                >
                  Next
                  <ChevronRight data-icon="inline-end" />
                </Button>
              ) : (
                <Button variant="outline" size="sm" disabled>
                  Next
                  <ChevronRight data-icon="inline-end" />
                </Button>
              )}
            </div>
          </CardFooter>
        </Card>
      </div>
    </main>
  )
}

function CrewRosterRow({ entry }: { entry: CrewMemberHttpResponse }) {
  const isPending = entry.status === 'pending'

  return (
    <TableRow className="border-line even:bg-muted/40">
      <TableCell className="px-4 py-4 whitespace-normal md:min-w-72 md:px-6">
        <div className="flex items-center gap-3">
          <Avatar className="size-11 after:border-0" size="lg">
            {!isPending && entry.profilePictureUrl ? (
              <AvatarImage
                src={entry.profilePictureUrl}
                alt={entry.displayName}
              />
            ) : null}
            <AvatarFallback
              className={cn(
                'font-heading font-semibold',
                getFallbackAvatarStyle({ entry }),
              )}
            >
              {isPending ? <Clock3 /> : getInitials({ entry })}
            </AvatarFallback>
          </Avatar>
          <span className="flex min-w-0 flex-1 flex-col gap-0.5">
            <span className="flex min-w-0 items-center gap-1.5">
              <strong className="min-w-0 truncate font-heading text-sm sm:text-base">
                {entry.displayName}
              </strong>
              {isPending ? (
                <Badge variant="secondary" className="md:hidden">
                  <Clock3 data-icon="inline-start" />
                  Pending
                </Badge>
              ) : null}
              {entry.role === 'admin' ? (
                <Badge className="md:hidden">
                  <Crown data-icon="inline-start" />
                  Admin
                </Badge>
              ) : null}
            </span>
            <span className="truncate text-xs text-muted-foreground">
              {isPending ? 'Invitation awaiting acceptance' : entry.emailAddress}
            </span>
          </span>
        </div>
      </TableCell>
      <TableCell className="hidden px-6 py-4 text-right md:table-cell">
        {isPending ? (
          <Badge variant="secondary">
            <Clock3 data-icon="inline-start" />
            Pending
          </Badge>
        ) : entry.role === 'member' ? (
          <span className="sr-only">Member</span>
        ) : entry.role ? (
          <Badge variant={entry.role === 'admin' ? 'default' : 'secondary'}>
            {entry.role === 'admin' ? <Crown data-icon="inline-start" /> : null}
            {formatRole({ role: entry.role })}
          </Badge>
        ) : null}
      </TableCell>
    </TableRow>
  )
}

function InviteCrewMemberDialog() {
  const sendCrewInvitationServerFn = useServerFn(sendCrewInvitation)
  const router = useRouter()
  const [isOpen, setIsOpen] = useState(false)
  const [emailAddress, setEmailAddress] = useState('')
  const [emailValidationError, setEmailValidationError] = useState<string>()
  const [submissionError, setSubmissionError] = useState<string>()
  const [isSubmitting, setIsSubmitting] = useState(false)

  const resetForm = () => {
    setEmailAddress('')
    setEmailValidationError(undefined)
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

  const closeAfterSuccess = () => {
    setIsOpen(false)
    resetForm()
  }

  const submitInvitation = async (normalizedEmailAddress: string) => {
    setSubmissionError(undefined)
    setIsSubmitting(true)

    try {
      const result = await sendCrewInvitationServerFn({
        data: { emailAddress: normalizedEmailAddress },
      })

      if (!result.ok) {
        setSubmissionError(result.error.message)
        return
      }

      if (result.value.status !== 'sent') {
        setSubmissionError(getCrewInvitationOutcomeMessage(result.value))
        return
      }

      const invitedEmailAddress = result.value.emailAddress
      closeAfterSuccess()
      toast.add({
        type: 'success',
        title: 'Invitation sent',
        description: `${invitedEmailAddress} has been invited.`,
      })
      void router.invalidate({ forcePending: true }).catch(() => {
        toast.add({
          type: 'error',
          title: 'Roster not refreshed',
          description: 'Refresh the page to see the pending invitation.',
        })
      })
    } catch {
      setSubmissionError('Invitations are unavailable right now. Try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setDialogOpen}>
      <DialogTrigger render={<Button type="button" />}>
        <MailPlus data-icon="inline-start" />
        Invite
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
          <DialogHeader>
            <DialogTitle>Invite a crew member</DialogTitle>
            <DialogDescription>
              Send one invitation at a time. New members join with the standard
              member role.
            </DialogDescription>
          </DialogHeader>

          <FieldGroup>
            <Field
              data-invalid={emailValidationError !== undefined || undefined}
              data-disabled={isSubmitting || undefined}
            >
              <FieldLabel htmlFor="crew-invitation-email">
                Email address
              </FieldLabel>
              <Input
                id="crew-invitation-email"
                name="emailAddress"
                type="email"
                value={emailAddress}
                placeholder="friend@example.com"
                onChange={(event) => {
                  setEmailAddress(event.target.value)
                  setEmailValidationError(undefined)
                  setSubmissionError(undefined)
                }}
                autoComplete="email"
                autoFocus
                required
                aria-invalid={emailValidationError !== undefined || undefined}
                disabled={isSubmitting}
              />
              <FieldError>{emailValidationError}</FieldError>
            </Field>
          </FieldGroup>

          {submissionError ? (
            <Alert variant="destructive">
              <CircleAlertIcon />
              <AlertTitle>Invitation not sent</AlertTitle>
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
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Spinner data-icon="inline-start" /> : null}
              {isSubmitting ? 'Sending invite…' : 'Send invite'}
              {isSubmitting ? null : <Send data-icon="inline-end" />}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
