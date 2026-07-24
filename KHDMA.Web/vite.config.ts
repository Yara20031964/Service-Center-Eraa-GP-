import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The Admin Portal talks to the PRODUCTION KHDMA API.
// Swagger: https://khdma.runasp.net/swagger/index.html
const API_TARGET = 'https://khdma.runasp.net'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Production CORS ("AllowFrontends") whitelists this origin, but we proxy
    // anyway so the browser only ever makes same-origin /api calls in dev.
    port: 5173,
    proxy: {
      // Proxied server-side to production so login works with no CORS friction.
      '/api': { target: API_TARGET, changeOrigin: true, secure: true },
      // SignalR hubs: BookingHub, ChatHub, NotificationHub
      '/hubs': { target: API_TARGET, changeOrigin: true, ws: true, secure: true },
    },
  },
})
