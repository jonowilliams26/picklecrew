import type { GetCrewMembersHttpResponse } from '~/lib/api/generated/types.gen'
import type { Result } from '~/lib/results'

type CrewMembersForbiddenError = Readonly<{
  type: 'crew-members-forbidden-error'
  message: string
}>

type CrewMembersUnavailableError = Readonly<{
  type: 'crew-members-unavailable-error'
  message: string
}>

export type CrewMembersResult = Result<
  GetCrewMembersHttpResponse,
  CrewMembersForbiddenError | CrewMembersUnavailableError
>
