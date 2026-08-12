import { createFileRoute, Outlet } from '@tanstack/react-router'

export const Route = createFileRoute('/_authenticated/_active-crew')({
  component: ActiveCrewLayout,
})

function ActiveCrewLayout() {
  return <Outlet />
}
