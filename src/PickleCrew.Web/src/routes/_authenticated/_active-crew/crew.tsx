import {
  createFileRoute,
  getRouteApi,
  Link,
  useRouter,
} from '@tanstack/react-router'
import {
  ChevronLeft,
  ChevronRight,
  CircleAlertIcon,
  Crown,
  UsersRound,
} from 'lucide-react'
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
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '~/components/ui/table'
import type { CrewMemberHttpResponse } from '~/lib/api/generated/types.gen'
import { getCrewMembers } from '~/lib/crews/crew-members.functions'
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

function getInitials({ member }: { member: CrewMemberHttpResponse }): string {
  const words = member.displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
  const initials = words.map((word) => word.at(0)?.toUpperCase()).join('')

  return initials || member.emailAddress.at(0)?.toUpperCase() || '?'
}

function getFallbackAvatarStyle({
  member,
}: {
  member: CrewMemberHttpResponse
}): string {
  const colorIndex = [...member.userId].reduce(
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
  const firstMemberNumber = (roster.page - 1) * roster.pageSize + 1
  const lastMemberNumber = Math.min(
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
          <Badge>
            <UsersRound data-icon="inline-start" />
            {roster.totalCount} {roster.totalCount === 1 ? 'member' : 'members'}
          </Badge>
        </header>

        <Card className="gap-0 border-2 border-ink bg-cream pb-0 text-ink shadow-[7px_8px_0_var(--ink)]">
          <CardHeader className="pb-3">
            <CardTitle className="font-heading text-2xl font-black tracking-[-.035em]">
              Crew members
            </CardTitle>
          </CardHeader>
          <CardContent className="px-0">
            <Table>
              <TableCaption className="sr-only">
                Active Crew members and their WorkOS organization roles.
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
                {roster.items.map((member) => (
                  <TableRow
                    className="border-line even:bg-muted/40"
                    key={member.userId}
                  >
                    <TableCell className="px-4 py-4 whitespace-normal md:min-w-72 md:px-6">
                      <div className="flex items-center gap-3">
                        <Avatar className="size-11 after:border-0" size="lg">
                          {member.profilePictureUrl ? (
                            <AvatarImage
                              src={member.profilePictureUrl}
                              alt={member.displayName}
                            />
                          ) : null}
                          <AvatarFallback
                            className={cn(
                              'font-heading font-semibold',
                              getFallbackAvatarStyle({ member }),
                            )}
                          >
                            {getInitials({ member })}
                          </AvatarFallback>
                        </Avatar>
                        <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                          <span className="flex min-w-0 items-center gap-1.5">
                            <strong className="min-w-0 truncate font-heading text-sm sm:text-base">
                              {member.displayName}
                            </strong>
                            {member.role === 'admin' ? (
                              <Badge className="md:hidden">
                                <Crown data-icon="inline-start" />
                                Admin
                              </Badge>
                            ) : null}
                          </span>
                          <span className="truncate text-xs text-muted-foreground">
                            {member.emailAddress}
                          </span>
                        </span>
                      </div>
                    </TableCell>
                    <TableCell className="hidden px-6 py-4 text-right md:table-cell">
                      {member.role === 'member' ? (
                        <span className="sr-only">Member</span>
                      ) : (
                        <Badge
                          variant={
                            member.role === 'admin' ? 'default' : 'secondary'
                          }
                        >
                          {member.role === 'admin' ? (
                            <Crown data-icon="inline-start" />
                          ) : null}
                          {formatRole({ role: member.role })}
                        </Badge>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
          <CardFooter className="flex-col gap-3 border-t-2 border-line py-3 sm:flex-row sm:justify-between">
            <p className="m-0 text-xs text-muted-foreground">
              Showing {firstMemberNumber}–{lastMemberNumber} of {roster.totalCount}
            </p>
            <div className="flex items-center gap-2">
              {roster.page > 1 ? (
                <Button
                  variant="outline"
                  size="sm"
                  render={
                    <Link
                      to="/crew"
                      search={{ page: roster.page - 1 }}
                    />
                  }
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
                  render={
                    <Link
                      to="/crew"
                      search={{ page: roster.page + 1 }}
                    />
                  }
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
