import { createRouter } from '@tanstack/react-router'
import { PageLoadingSkeleton } from './components/page-loading-skeleton'
import { routeTree } from './routeTree.gen'

export function getRouter() {
  return createRouter({
    routeTree,
    defaultGcTime: 0,
    defaultPreload: 'intent',
    defaultPreloadStaleTime: 30_000,
    defaultPendingComponent: PageLoadingSkeleton,
    defaultPendingMs: 150,
    defaultPendingMinMs: 200,
    defaultStaleReloadMode: 'blocking',
    scrollRestoration: true,
  })
}
