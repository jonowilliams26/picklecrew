import {
  createFileRoute,
  getRouteApi,
  Link,
  Outlet,
  useRouterState,
} from '@tanstack/react-router'
import type { User } from '@workos/authkit-tanstack-react-start'
import { useAuth } from '@workos/authkit-tanstack-react-start/client'
import {
  Check,
  ChevronsUpDown,
  ChevronUp,
  ClipboardList,
  LayoutDashboard,
  LogOut,
  Plus,
  UsersRound,
} from 'lucide-react'
import { LogoMark } from '~/components/picklecrew-brand'
import { Avatar, AvatarFallback, AvatarImage } from '~/components/ui/avatar'
import { buttonVariants } from '~/components/ui/button'
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
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarSeparator,
  SidebarTrigger,
  useSidebar,
} from '~/components/ui/sidebar'

export const Route = createFileRoute('/_authenticated/_active-crew')({
  component: ActiveCrewLayout,
})

const authenticatedRoute = getRouteApi('/_authenticated')

const navigationItems = [
  { label: 'Dashboard', icon: LayoutDashboard, to: '/dashboard' },
  { label: 'Games', icon: ClipboardList, to: '/games' },
  { label: 'Crew', icon: UsersRound, to: '/crew' },
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
  const { activeCrew } = authenticatedRoute.useLoaderData()
  const { user } = useAuth()
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const isRecordingGame = pathname === '/record-game'

  if (!activeCrew || !user) {
    throw new Error('Active crew layout rendered without an active crew or user.')
  }

  return (
    <SidebarProvider className="min-h-dvh bg-background">
      <ActiveCrewSidebar
        crewName={activeCrew.name}
        pathname={pathname}
        user={user}
      />

      <SidebarInset className="dashboard-shell min-h-dvh overflow-hidden font-heading">
        <header className="flex h-[calc(4.5rem+env(safe-area-inset-top))] shrink-0 items-center justify-between border-b-2 border-border bg-background px-5 pt-[env(safe-area-inset-top)] md:hidden">
          <div className="flex items-center gap-2">
            <SidebarTrigger aria-label="Open navigation" />
            <Link
              className="inline-flex items-center gap-2.5 font-heading text-lg font-black tracking-[-.045em]"
              to="/dashboard"
              aria-label="PickleCrew dashboard"
            >
              <LogoMark />
              <span>PickleCrew</span>
            </Link>
          </div>
          {!isRecordingGame ? (
            <Link
              className={buttonVariants({ size: 'sm', variant: 'crew' })}
              to="/record-game"
            >
              <Plus data-icon="inline-start" />
              New game
            </Link>
          ) : null}
        </header>

        <Outlet />
      </SidebarInset>
    </SidebarProvider>
  )
}

function CrewSwitcher({ crewName }: { crewName: string }) {
  const { isMobile, setOpenMobile } = useSidebar()
  const closeMobileNavigation = () => setOpenMobile(false)

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <SidebarMenuButton
                className="hover:bg-sidebar-foreground/5 active:bg-sidebar-foreground/10 data-open:bg-sidebar-foreground/10 data-open:hover:bg-sidebar-foreground/10"
                size="lg"
              />
            }
          >
            <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
              <UsersRound aria-hidden="true" />
            </span>
            <span className="flex min-w-0 flex-1 flex-col items-start gap-0.5">
              <strong className="w-full truncate font-heading text-sm">
                {crewName}
              </strong>
              <span className="w-full truncate text-[10px] text-muted-foreground">
                Current crew
              </span>
            </span>
            <ChevronsUpDown aria-hidden="true" />
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="min-w-56"
            align="start"
            side={isMobile ? 'bottom' : 'right'}
            sideOffset={8}
          >
            <DropdownMenuGroup>
              <DropdownMenuLabel>Crews</DropdownMenuLabel>
              <DropdownMenuItem>
                <UsersRound aria-hidden="true" />
                <span className="min-w-0 flex-1 truncate">{crewName}</span>
                <Check className="ml-auto" aria-hidden="true" />
                <span className="sr-only">Current crew</span>
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem
                render={
                  <Link
                    to="/create-crew"
                    onClick={closeMobileNavigation}
                  />
                }
              >
                <Plus aria-hidden="true" />
                Create new crew
              </DropdownMenuItem>
            </DropdownMenuGroup>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}

function ActiveCrewSidebar({
  crewName,
  pathname,
  user,
}: {
  crewName: string
  pathname: string
  user: User
}) {
  const displayName = getDisplayName({ user })
  const { setOpenMobile } = useSidebar()
  const closeMobileNavigation = () => setOpenMobile(false)

  return (
    <Sidebar className="border-r-2" collapsible="offcanvas">
      <SidebarHeader className="gap-5 p-5">
        <Link
          className="inline-flex items-center gap-2.5 font-heading text-xl font-black tracking-[-.045em]"
          to="/dashboard"
          aria-label="PickleCrew dashboard"
          onClick={closeMobileNavigation}
        >
          <LogoMark />
          <span>PickleCrew</span>
        </Link>

        <CrewSwitcher crewName={crewName} />

        <Link
          className={buttonVariants({
            className: 'w-full',
            variant: 'crew',
          })}
          to="/record-game"
          onClick={closeMobileNavigation}
        >
          <Plus data-icon="inline-start" />
          New game
        </Link>
      </SidebarHeader>

      <SidebarSeparator />

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Menu</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu className="gap-0.5">
              {navigationItems.map((item) => {
                const Icon = item.icon
                const isActive = item.to === pathname

                return (
                  <SidebarMenuItem key={item.label}>
                    <SidebarMenuButton
                      className="dashboard-nav-link h-9 rounded-lg px-3 font-normal text-muted-foreground hover:bg-transparent hover:text-foreground data-active:bg-transparent data-active:font-bold data-active:text-foreground"
                      render={
                        <Link
                          to={item.to}
                          onClick={closeMobileNavigation}
                        />
                      }
                      isActive={isActive}
                    >
                      <Icon aria-hidden="true" />
                      <span>{item.label}</span>
                    </SidebarMenuButton>
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
