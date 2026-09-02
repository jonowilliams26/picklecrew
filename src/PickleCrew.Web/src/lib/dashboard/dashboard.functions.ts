import { createServerFn } from '@tanstack/react-start'
import { createApiClient } from '~/lib/api/api.server'
import {
  getBestDoublesPairing as getBestDoublesPairingRequest,
  getBestPartner as getBestPartnerRequest,
  getBiggestRivals as getBiggestRivalsRequest,
  getBrickWall as getBrickWallRequest,
  getChiefPaddler as getChiefPaddlerRequest,
  getCrewHealth as getCrewHealthRequest,
  getIceMan as getIceManRequest,
  getCrewLeaderboard as getCrewLeaderboardRequest,
  getCrewLongestWinStreak as getCrewLongestWinStreakRequest,
  getEasyWin as getEasyWinRequest,
  getFriendsOrLovers as getFriendsOrLoversRequest,
  getHotPaddle as getHotPaddleRequest,
  getLimpWristSyndrome as getLimpWristSyndromeRequest,
  getNemesis as getNemesisRequest,
  getPersonalStatistics as getPersonalStatisticsRequest,
  getPointMachine as getPointMachineRequest,
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
        easyWin,
        nemesis,
        recentGames,
        crewHealth,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
        bestPartner,
        biggestRivals,
        brickWall,
        chiefPaddler,
        iceMan,
        friendsOrLovers,
        hotPaddle,
        limpWristSyndrome,
        pointMachine,
      ] = await Promise.all([
        getPersonalStatisticsRequest({ client }),
        getEasyWinRequest({ client }),
        getNemesisRequest({ client }),
        getRecentGamesRequest({ client }),
        getCrewHealthRequest({ client }),
        getCrewLeaderboardRequest({ client }),
        getCrewLongestWinStreakRequest({ client }),
        getBestDoublesPairingRequest({ client }),
        getBestPartnerRequest({ client }),
        getBiggestRivalsRequest({ client }),
        getBrickWallRequest({ client }),
        getChiefPaddlerRequest({ client }),
        getIceManRequest({ client }),
        getFriendsOrLoversRequest({ client }),
        getHotPaddleRequest({ client }),
        getLimpWristSyndromeRequest({ client }),
        getPointMachineRequest({ client }),
      ])
      const results = [
        personalStatistics,
        easyWin,
        nemesis,
        recentGames,
        crewHealth,
        leaderboard,
        longestWinStreak,
        bestDoublesPairing,
        bestPartner,
        biggestRivals,
        brickWall,
        chiefPaddler,
        iceMan,
        friendsOrLovers,
        hotPaddle,
        limpWristSyndrome,
        pointMachine,
      ]

      if (
        personalStatistics.error ||
        easyWin.error ||
        nemesis.error ||
        recentGames.error ||
        crewHealth.error ||
        leaderboard.error ||
        longestWinStreak.error ||
        bestDoublesPairing.error ||
        bestPartner.error ||
        biggestRivals.error ||
        brickWall.error ||
        chiefPaddler.error ||
        iceMan.error ||
        friendsOrLovers.error ||
        hotPaddle.error ||
        limpWristSyndrome.error ||
        pointMachine.error
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
        easyWin: easyWin.data,
        nemesis: nemesis.data,
        recentGames: recentGames.data,
        crewHealth: crewHealth.data,
        leaderboard: leaderboard.data,
        longestWinStreak: longestWinStreak.data,
        bestDoublesPairing: bestDoublesPairing.data,
        bestPartner: bestPartner.data,
        biggestRivals: biggestRivals.data,
        brickWall: brickWall.data,
        chiefPaddler: chiefPaddler.data,
        iceMan: iceMan.data,
        friendsOrLovers: friendsOrLovers.data,
        hotPaddle: hotPaddle.data,
        limpWristSyndrome: limpWristSyndrome.data,
        pointMachine: pointMachine.data,
      })
    } catch {
      return failure({
        type: 'dashboard-unavailable-error',
        message: 'Dashboard statistics could not be loaded. Try again.',
      })
    }
  })
