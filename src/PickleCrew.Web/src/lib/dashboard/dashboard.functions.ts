import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import {
  getBestDoublesPairing as getBestDoublesPairingRequest,
  getBestPartner as getBestPartnerRequest,
  getBiggestRivals as getBiggestRivalsRequest,
  getChiefPaddler as getChiefPaddlerRequest,
  getIceMan as getIceManRequest,
  getCrewLeaderboard as getCrewLeaderboardRequest,
  getCrewLongestWinStreak as getCrewLongestWinStreakRequest,
  getFriendsOrLovers as getFriendsOrLoversRequest,
  getLimpWristSyndrome as getLimpWristSyndromeRequest,
  getNemesis as getNemesisRequest,
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
        nemesis,
        recentGames,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
        bestPartner,
        biggestRivals,
        chiefPaddler,
        iceMan,
        friendsOrLovers,
        limpWristSyndrome,
      ] = await Promise.all([
        getPersonalStatisticsRequest({ client }),
        getNemesisRequest({ client }),
        getRecentGamesRequest({ client }),
        getCrewLeaderboardRequest({ client }),
        getCrewLongestWinStreakRequest({ client }),
        getBestDoublesPairingRequest({ client }),
        getBestPartnerRequest({ client }),
        getBiggestRivalsRequest({ client }),
        getChiefPaddlerRequest({ client }),
        getIceManRequest({ client }),
        getFriendsOrLoversRequest({ client }),
        getLimpWristSyndromeRequest({ client }),
      ])
      const results = [
        personalStatistics,
        nemesis,
        recentGames,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
        bestPartner,
        biggestRivals,
        chiefPaddler,
        iceMan,
        friendsOrLovers,
        limpWristSyndrome,
      ]

      if (
        personalStatistics.error ||
        nemesis.error ||
        recentGames.error ||
        leaderboard.error ||
        longestWinStreak.error ||
        bestDoublesPairing.error ||
        bestPartner.error ||
        biggestRivals.error ||
        chiefPaddler.error ||
        iceMan.error ||
        friendsOrLovers.error ||
        limpWristSyndrome.error
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
            ? 'Your active crew membership could not be verified. Refresh your crew and try again.'
            : 'Dashboard statistics could not be loaded. Try again.',
        })
      }

      return success({
        personalStatistics: personalStatistics.data,
        nemesis: nemesis.data,
        recentGames: recentGames.data,
        leaderboard: leaderboard.data,
        longestWinStreak: longestWinStreak.data,
        bestDoublesPairing: bestDoublesPairing.data,
        bestPartner: bestPartner.data,
        biggestRivals: biggestRivals.data,
        chiefPaddler: chiefPaddler.data,
        iceMan: iceMan.data,
        friendsOrLovers: friendsOrLovers.data,
        limpWristSyndrome: limpWristSyndrome.data,
      })
    } catch {
      return failure({
        type: 'dashboard-unavailable-error',
        message: 'Dashboard statistics could not be loaded. Try again.',
      })
    }
  })
