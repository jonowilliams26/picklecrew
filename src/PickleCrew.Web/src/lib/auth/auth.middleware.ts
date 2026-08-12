import { redirect } from '@tanstack/react-router'
import { createMiddleware } from '@tanstack/react-start'
import { getAuth } from '@workos/authkit-tanstack-react-start'

export const authenticatedMiddleware = createMiddleware().server(
  async ({ next }) => {
    const auth = await getAuth()

    if (!auth.user) {
      throw redirect({
        href: '/api/auth/sign-in?returnPathname=/dashboard',
      })
    }

    return next({ context: { auth } })
  },
)
