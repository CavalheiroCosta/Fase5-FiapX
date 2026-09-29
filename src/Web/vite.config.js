import { defineConfig } from 'vite'

const auth = process.env.AUTH_PROXY ?? 'http://localhost:5298'
const video = process.env.VIDEO_PROXY ?? 'http://localhost:5299'

export default defineConfig({
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/auth': {
        target: auth,
        changeOrigin: true,
        rewrite: (caminho) => caminho.replace(/^\/auth/, ''),
      },
      '/video': {
        target: video,
        changeOrigin: true,
        rewrite: (caminho) => caminho.replace(/^\/video/, ''),
      },
    },
  },
})
