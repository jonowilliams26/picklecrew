import { cloudflare } from '@cloudflare/vite-plugin'
import { tanstackStart } from '@tanstack/react-start/plugin/vite'
import tailwindcss from '@tailwindcss/vite'
import viteReact from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const runtimeVariables = Object.fromEntries(
  Object.entries({
    API_BASE_URL: process.env.API_BASE_URL,
    WORKOS_CLIENT_ID: process.env.WORKOS_CLIENT_ID,
    WORKOS_API_KEY: process.env.WORKOS_API_KEY,
    WORKOS_COOKIE_PASSWORD: process.env.WORKOS_COOKIE_PASSWORD,
    WORKOS_REDIRECT_URI: process.env.WORKOS_REDIRECT_URI,
  }).filter((entry): entry is [string, string] => entry[1] !== undefined),
)

export default defineConfig(({ command }) => ({
  server: {
    host: true,
    port: Number(process.env.PORT ?? 3000),
  },
  resolve: {
    tsconfigPaths: true,
  },
  plugins: [
    cloudflare({
      configPath: '../../infra/web/wrangler.jsonc',
      config:
        command === 'serve' && Object.keys(runtimeVariables).length > 0
          ? { vars: runtimeVariables }
          : undefined,
      viteEnvironment: { name: 'ssr' },
    }),
    tailwindcss(),
    tanstackStart({
      srcDirectory: 'src',
    }),
    viteReact(),
  ],
}))
