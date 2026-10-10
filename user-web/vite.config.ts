import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5174,
    watch: {
      ignored: ['**/dist.zip', '**/*.pdf'],
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5209',
        changeOrigin: true,
      },
      // Same-origin proxy so canvas can use live-site profile photos without tainting
      '/php-site': {
        target: 'https://miniwebsite.in',
        changeOrigin: true,
        rewrite: (p) => p.replace(/^\/php-site/, ''),
      },
    },
  },
})
