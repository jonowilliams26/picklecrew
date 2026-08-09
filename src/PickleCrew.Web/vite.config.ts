import { cloudflare } from '@cloudflare/vite-plugin'
import { tanstackStart } from '@tanstack/react-start/plugin/vite'
import tailwindcss from '@tailwindcss/vite'
import viteReact from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const workosRedirectUri = process.env.WORKOS_REDIRECT_URI

export default defineConfig({
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
      config: workosRedirectUri
        ? { vars: { WORKOS_REDIRECT_URI: workosRedirectUri } }
        : undefined,
      viteEnvironment: { name: 'ssr' },
    }),
    tailwindcss(),
    tanstackStart({
      srcDirectory: 'src',
    }),
    viteReact(),
  ],
})
