import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { Button } from '~/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '~/components/ui/card'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/dashboard',
)({
  component: DashboardPage,
})

const authenticatedRoute = getRouteApi('/_authenticated')

function DashboardPage() {
  const { activeCrew } = authenticatedRoute.useLoaderData()

  if (!activeCrew) {
    throw new Error('Dashboard rendered without an active crew.')
  }

  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-xl border-2 border-ink shadow-[6px_7px_0_var(--ink)]">
        <CardHeader>
          <CardTitle className="font-heading text-3xl">Crew dashboard</CardTitle>
          <CardDescription>{activeCrew.name}</CardDescription>
        </CardHeader>
        <CardContent>
          <Button variant="paper" render={<a href="/api/auth/sign-out" />}>
            Sign out
          </Button>
        </CardContent>
      </Card>
    </main>
  )
}
