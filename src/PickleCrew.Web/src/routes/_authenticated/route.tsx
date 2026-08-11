import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { getAuth } from '@workos/authkit-tanstack-react-start'

export const Route = createFileRoute('/_authenticated')({
  loader: async () => {
    const auth = await getAuth()

    if (!auth.user) {
      throw redirect({
        href: '/api/auth/sign-in?returnPathname=/dashboard',
      })
    }

    return auth
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  return <Outlet />
}
