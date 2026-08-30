import { createFileRoute } from '@tanstack/react-router'
import { CrewCreationFlow } from '~/components/crew-creation-flow'

export const Route = createFileRoute('/_authenticated/create-crew')({
  component: CreateCrewPage,
})

function CreateCrewPage() {
  return <CrewCreationFlow mode="additional" />
}
