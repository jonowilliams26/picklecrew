import type {
  GetBestDoublesPairingHttpResponse,
  GetChiefPaddlerHttpResponse,
  GetCrewLeaderboardHttpResponse,
  GetCrewLongestWinStreakHttpResponse,
  GetPersonalStatisticsHttpResponse,
  GetRecentGamesHttpResponse,
} from '~/lib/api/generated/types.gen'
import type { Result } from '~/lib/results'

export type DashboardData = Readonly<{
  personalStatistics: GetPersonalStatisticsHttpResponse
  recentGames: GetRecentGamesHttpResponse
  leaderboard: GetCrewLeaderboardHttpResponse
  longestWinStreak: GetCrewLongestWinStreakHttpResponse
  bestDoublesPairing: GetBestDoublesPairingHttpResponse
  chiefPaddler: GetChiefPaddlerHttpResponse
}>

type DashboardForbiddenError = Readonly<{
  type: 'dashboard-forbidden-error'
  message: string
}>

type DashboardUnavailableError = Readonly<{
  type: 'dashboard-unavailable-error'
  message: string
}>

export type DashboardResult = Result<
  DashboardData,
  DashboardForbiddenError | DashboardUnavailableError
>

export function formatPercentage({
  value,
}: {
  value: number | string
}): number {
  return Math.round(Number(value) * 100)
}

export function formatSignedNumber({ value }: { value: number }): string {
  return value > 0 ? `+${value.toLocaleString('en-AU')}` : value.toLocaleString('en-AU')
}

export function formatRecord({
  wins,
  losses,
}: {
  wins: number
  losses: number
}): string {
  return `${wins.toLocaleString('en-AU')}–${losses.toLocaleString('en-AU')}`
}

export function formatCount({
  count,
  singular,
  plural = `${singular}s`,
}: {
  count: number
  singular: string
  plural?: string
}): string {
  return `${count.toLocaleString('en-AU')} ${count === 1 ? singular : plural}`
}

export function getInitials({ displayName }: { displayName: string }): string {
  const initials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.at(0)?.toUpperCase())
    .join('')

  return initials || '?'
}

export function getCurrentStreakDetail({
  currentWinStreak,
  longestWinStreak,
}: {
  currentWinStreak: number
  longestWinStreak: number
}): string {
  if (currentWinStreak === 0) {
    return 'One win is all it takes to start some trouble'
  }

  const winsUntilBest = longestWinStreak - currentWinStreak

  if (winsUntilBest <= 0) {
    return 'You’ve got the hot paddle — personal best in progress'
  }

  return winsUntilBest === 1
    ? 'one away from your best'
    : `${winsUntilBest} away from your best`
}

export function formatStreakPeriod({
  startedAtUtc,
  endedAtUtc,
}: {
  startedAtUtc: string | null
  endedAtUtc: string | null
}): string {
  if (!startedAtUtc) {
    return 'No streak recorded yet'
  }

  if (!endedAtUtc) {
    return `Still cooking since ${formatDate({ dateTimeUtc: startedAtUtc })} — somebody hand them an L already`
  }

  return `${formatDate({ dateTimeUtc: startedAtUtc })}–${formatDate({ dateTimeUtc: endedAtUtc })}`
}

function formatDate({ dateTimeUtc }: { dateTimeUtc: string }): string {
  return new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(dateTimeUtc))
}
