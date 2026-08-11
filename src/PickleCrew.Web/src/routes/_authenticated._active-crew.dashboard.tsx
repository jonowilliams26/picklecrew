import { createFileRoute } from '@tanstack/react-router'
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

function DashboardPage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-xl border-2 border-ink shadow-[6px_7px_0_var(--ink)]">
        <CardHeader>
          <CardTitle className="font-heading text-3xl">Crew dashboard</CardTitle>
          <CardDescription>
            Your crew is active. Game tracking arrives in the next checkpoint.
          </CardDescription>
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
