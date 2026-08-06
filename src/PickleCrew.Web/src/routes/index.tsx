import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/')({
  component: HomePage,
  ssr: true,
})

function HomePage() {
  return (
    <main>
      <h1>Hello world</h1>
    </main>
  )
}
