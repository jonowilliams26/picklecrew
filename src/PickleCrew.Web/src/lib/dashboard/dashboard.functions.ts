import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import {
  getBestDoublesPairing as getBestDoublesPairingRequest,
  getCrewLeaderboard as getCrewLeaderboardRequest,
  getCrewLongestWinStreak as getCrewLongestWinStreakRequest,
  getPersonalStatistics as getPersonalStatisticsRequest,
  getRecentGames as getRecentGamesRequest,
} from '~/lib/api/generated/sdk.gen'
import { authenticatedMiddleware } from '~/lib/auth/auth.middleware'
import type { DashboardResult } from '~/lib/dashboard/dashboard'
import { failure, success } from '~/lib/results'

export const getDashboard = createServerFn({ method: 'GET' })
  .middleware([authenticatedMiddleware])
  .handler(async ({ context }): Promise<DashboardResult> => {
    try {
      const client = createApiClient({
        accessToken: context.auth.accessToken,
      })
      const [
        personalStatistics,
        recentGames,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
      ] = await Promise.all([
        getPersonalStatisticsRequest({ client }),
        getRecentGamesRequest({ client }),
        getCrewLeaderboardRequest({ client }),
        getCrewLongestWinStreakRequest({ client }),
        getBestDoublesPairingRequest({ client }),
      ])
      const results = [
        personalStatistics,
        recentGames,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
      ]

      if (
        personalStatistics.error ||
        recentGames.error ||
        leaderboard.error ||
        longestWinStreak.error ||
        bestDoublesPairing.error
      ) {
        const isForbidden = results.some(
          (result) =>
            result.response?.status === 401 || result.response?.status === 403,
        )

        return failure({
          type: isForbidden
            ? 'dashboard-forbidden-error'
            : 'dashboard-unavailable-error',
          message: isForbidden
            ? 'Your active Crew membership could not be verified. Refresh your Crew and try again.'
            : 'Dashboard statistics could not be loaded. Try again.',
        })
      }

      return success({
        personalStatistics: personalStatistics.data,
        recentGames: recentGames.data,
        leaderboard: leaderboard.data,
        longestWinStreak: longestWinStreak.data,
        bestDoublesPairing: bestDoublesPairing.data,
      })
    } catch {
      return failure({
        type: 'dashboard-unavailable-error',
        message: 'Dashboard statistics could not be loaded. Try again.',
      })
    }
  })
