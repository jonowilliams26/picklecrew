import {
  createFileRoute,
  getRouteApi,
  Link,
  Outlet,
  useRouterState,
} from '@tanstack/react-router'
import type { User } from '@workos/authkit-tanstack-react-start'
import {
  ChartNoAxesColumnIncreasing,
  ChevronUp,
  ClipboardList,
  LayoutDashboard,
  LogOut,
  Plus,
  UsersRound,
} from 'lucide-react'
import { LogoMark } from '~/components/picklecrew-brand'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { Button } from '~/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '~/components/ui/dropdown-menu'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarSeparator,
  SidebarTrigger,
} from '~/components/ui/sidebar'

export const Route = createFileRoute('/_authenticated/_active-crew')({
  component: ActiveCrewLayout,
})

const authenticatedRoute = getRouteApi('/_authenticated')

const navigationItems = [
  { label: 'Dashboard', icon: LayoutDashboard, isActive: true },
  { label: 'Games', icon: ClipboardList, isActive: false },
  {
    label: 'Leaderboard',
    icon: ChartNoAxesColumnIncreasing,
    isActive: false,
  },
  { label: 'Crew', icon: UsersRound, isActive: false },
] as const

function getDisplayName({ user }: { user: User }): string {
  const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ')
  return fullName || user.email
}

function getInitials({ user }: { user: User }): string {
  const initials = [user.firstName, user.lastName]
    .filter(Boolean)
    .map((name) => name?.at(0))
    .join('')

  return initials || user.email.at(0)?.toUpperCase() || '?'
}

function ActiveCrewLayout() {
  const { activeCrew, user } = authenticatedRoute.useLoaderData()
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const isRecordingGame = pathname === '/record-game'

  if (!activeCrew || !user) {
    throw new Error('Active crew layout rendered without an active crew or user.')
  }

  return (
    <SidebarProvider>
      <ActiveCrewSidebar crewName={activeCrew.name} user={user} />

      <SidebarInset className="dashboard-shell min-h-svh overflow-hidden font-heading">
        <header className="flex h-18 shrink-0 items-center justify-between border-b-2 border-border bg-background/90 px-5 backdrop-blur-sm md:hidden">
          <div className="flex items-center gap-2">
            <SidebarTrigger aria-label="Open navigation" />
            <a
              className="inline-flex items-center gap-2.5 font-heading text-lg font-black tracking-[-.045em]"
              href="/dashboard"
              aria-label="PickleCrew dashboard"
            >
              <LogoMark />
              <span>PickleCrew</span>
            </a>
          </div>
          {!isRecordingGame ? (
            <Button
              size="sm"
              variant="crew"
              render={<Link to="/record-game" />}
            >
              <Plus data-icon="inline-start" />
              New Game
            </Button>
          ) : null}
        </header>

        <Outlet />
      </SidebarInset>
    </SidebarProvider>
  )
}

function ActiveCrewSidebar({
  crewName,
  user,
}: {
  crewName: string
  user: User
}) {
  const displayName = getDisplayName({ user })

  return (
    <Sidebar className="border-r-2" collapsible="offcanvas">
      <SidebarHeader className="gap-5 p-5">
        <a
          className="inline-flex items-center gap-2.5 font-heading text-xl font-black tracking-[-.045em]"
          href="/dashboard"
          aria-label="PickleCrew dashboard"
        >
          <LogoMark />
          <span>PickleCrew</span>
        </a>

        <div className="flex min-w-0 items-stretch gap-3 px-2">
          <span
            className="w-0.5 shrink-0 rounded-full bg-gradient-to-b from-orange/70 to-lime/70"
            aria-hidden="true"
          />
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="text-[9px] font-extrabold tracking-[.12em] text-muted-foreground uppercase">
              Current crew
            </span>
            <strong className="truncate font-heading text-base">
              {crewName}
            </strong>
          </div>
        </div>

        <Button
          className="w-full"
          size="lg"
          variant="crew"
          render={<Link to="/record-game" />}
        >
          <Plus data-icon="inline-start" />
          Record game
        </Button>
      </SidebarHeader>

      <SidebarSeparator />

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Menu</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu className="gap-0.5">
              {navigationItems.map((item) => {
                const Icon = item.icon

                return (
                  <SidebarMenuItem key={item.label}>
                    <SidebarMenuButton
                      className="dashboard-nav-link h-9 rounded-lg px-3 font-normal text-muted-foreground hover:bg-transparent hover:text-foreground data-active:bg-transparent data-active:font-bold data-active:text-foreground"
                      render={
                        item.isActive ? <a href="/dashboard" /> : undefined
                      }
                      isActive={item.isActive}
                    >
                      <Icon aria-hidden="true" />
                      <span>{item.label}</span>
                    </SidebarMenuButton>
                    {!item.isActive ? (
                      <SidebarMenuBadge>Soon</SidebarMenuBadge>
                    ) : null}
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="p-4">
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger
                render={
                  <SidebarMenuButton
                    className="h-auto rounded-xl border border-sidebar-border bg-background/60 p-2.5 hover:bg-background data-open:bg-background"
                    size="lg"
                  />
                }
              >
                <Avatar size="lg">
                  {user.profilePictureUrl ? (
                    <AvatarImage
                      src={user.profilePictureUrl}
                      alt={displayName}
                    />
                  ) : null}
                  <AvatarFallback>{getInitials({ user })}</AvatarFallback>
                </Avatar>
                <span className="flex min-w-0 flex-1 flex-col items-start gap-0.5">
                  <strong className="w-full truncate font-heading text-sm">
                    {displayName}
                  </strong>
                  <span className="w-full truncate text-[10px] text-muted-foreground">
                    {user.email}
                  </span>
                </span>
                <ChevronUp aria-hidden="true" />
              </DropdownMenuTrigger>
              <DropdownMenuContent
                className="min-w-56"
                align="start"
                side="top"
                sideOffset={8}
              >
                <DropdownMenuGroup>
                  <DropdownMenuLabel className="flex flex-col gap-1">
                    <span className="font-heading text-sm text-foreground">
                      {displayName}
                    </span>
                    <span className="truncate font-normal">{user.email}</span>
                  </DropdownMenuLabel>
                </DropdownMenuGroup>
                <DropdownMenuSeparator />
                <DropdownMenuGroup>
                  <DropdownMenuItem render={<a href="/api/auth/sign-out" />}>
                    <LogOut aria-hidden="true" />
                    Sign out
                  </DropdownMenuItem>
                </DropdownMenuGroup>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  )
}
