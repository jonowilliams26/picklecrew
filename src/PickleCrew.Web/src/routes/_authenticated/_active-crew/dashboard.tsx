import { createFileRoute } from '@tanstack/react-router'
import { Construction } from 'lucide-react'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '~/components/ui/empty'

export const Route = createFileRoute(
  '/_authenticated/_active-crew/dashboard',
)({
  component: DashboardPage,
})

function DashboardPage() {
  return (
    <div className="relative flex flex-1 items-center justify-center p-5 pb-28 md:p-10">
      <Empty className="dashboard-empty max-w-2xl border-2 border-border bg-card px-6 py-16 shadow-[8px_9px_0_var(--ink)] md:px-12">
        <EmptyHeader className="max-w-lg gap-3">
          <EmptyMedia
            className="size-14 rotate-[-4deg] rounded-full border-2 border-foreground bg-secondary shadow-[3px_4px_0_var(--ink)]"
            variant="icon"
          >
            <Construction aria-hidden="true" />
          </EmptyMedia>
          <span className="text-[9px] font-extrabold tracking-[.14em] text-accent uppercase">
            Court under construction
          </span>
          <EmptyTitle className="font-heading text-3xl font-black tracking-[-.045em] md:text-5xl">
            The dashboard is warming up.
          </EmptyTitle>
          <EmptyDescription className="max-w-md font-heading text-base">
            Crew stats, standings, and recent games will land here soon. For
            now, the application shell is ready for the next rally.
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    </div>
  )
}
