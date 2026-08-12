export type GuestHomepageRouteDecision =
  | Readonly<{ type: 'render' }>
  | Readonly<{ type: 'redirect'; destination: '/dashboard' }>

export function decideGuestHomepageRoute({
  isSignedIn,
}: {
  isSignedIn: boolean
}): GuestHomepageRouteDecision {
  return isSignedIn
    ? { type: 'redirect', destination: '/dashboard' }
    : { type: 'render' }
}
