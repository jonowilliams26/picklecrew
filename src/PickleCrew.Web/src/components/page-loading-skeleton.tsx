import { Skeleton } from '~/components/ui/skeleton'

export function PageLoadingSkeleton() {
  return (
    <main
      className="flex min-h-[60vh] flex-1 flex-col p-5 md:p-8 xl:p-10"
      role="status"
      aria-busy="true"
      aria-live="polite"
    >
      <span className="sr-only">Loading page</span>
      <div
        className="mx-auto flex w-full max-w-[1480px] flex-col gap-8"
        aria-hidden="true"
      >
        <header className="flex flex-col gap-3">
          <Skeleton className="h-3 w-24" />
          <Skeleton className="h-12 w-full max-w-sm" />
          <Skeleton className="h-4 w-full max-w-xl" />
        </header>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-64 w-full md:col-span-2 xl:col-span-1" />
        </div>
      </div>
    </main>
  )
}
