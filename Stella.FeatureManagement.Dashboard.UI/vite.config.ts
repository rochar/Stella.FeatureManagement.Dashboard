import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import fs from 'fs'

function getAppVersion(): string {
  try {
    const versionFile = path.resolve(__dirname, 'version.json')
    if (fs.existsSync(versionFile)) {
      const content = JSON.parse(fs.readFileSync(versionFile, 'utf-8'))
      return content.version || 'dev'
    }
  } catch {
    // Ignore errors
  }
  return 'dev'
}

export default defineConfig({
  plugins: [react()],
  base: './',
  define: {
    __APP_VERSION__: JSON.stringify(getAppVersion()),
  },
  build: {
    outDir: path.resolve(__dirname, '../Stella.FeatureManagement.Dashboard/wwwroot'),
    emptyDirBeforeWrite: true,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/[name].js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: 'assets/[name].[ext]'
      }
    }
  }
})
