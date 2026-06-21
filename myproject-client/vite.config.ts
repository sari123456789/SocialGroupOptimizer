import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import mkcert from 'vite-plugin-mkcert'

/** כתובת ה-API בפיתוח — תואמת לפרופיל https ב-launchSettings.json */
const API_HTTPS_TARGET = 'https://localhost:7044'

export default defineConfig({
  plugins: [react(), tailwindcss(), mkcert()],
  server: {
    proxy: {
      '/api': {
        target: API_HTTPS_TARGET,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
