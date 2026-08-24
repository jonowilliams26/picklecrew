import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import type { ReactNode } from 'react'

export function Authorized({
  role,
  children,
}: {
  role: string
  children: ReactNode
}) {
  const { role: activeRole } = useAuth()

  if (activeRole !== role) {
    return null
  }

  return children
}
