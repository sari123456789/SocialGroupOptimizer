import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // בזמן פיתוח: בקשות ל־/api מועברות ל־ASP.NET (אין בעיית CORS בדפדפן).
    proxy: {
      '/api': {
        target: 'http://localhost:5256',
        changeOrigin: true,
      },
    },
  },
})