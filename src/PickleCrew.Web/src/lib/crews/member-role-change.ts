import type { Result } from '~/lib/results'

type CrewMemberRoleChangeForbiddenError = Readonly<{
  type: 'crew-member-role-change-forbidden-error'
  message: string
}>

type CrewMemberRoleChangeNotFoundError = Readonly<{
  type: 'crew-member-role-change-not-found-error'
  message: string
}>

type CrewMemberRoleChangeUnavailableError = Readonly<{
  type: 'crew-member-role-change-unavailable-error'
  message: string
}>

export type CrewMemberRoleChangeResult = Result<
  void,
  | CrewMemberRoleChangeForbiddenError
  | CrewMemberRoleChangeNotFoundError
  | CrewMemberRoleChangeUnavailableError
>
