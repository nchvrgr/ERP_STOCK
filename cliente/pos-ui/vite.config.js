import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vuetify from 'vite-plugin-vuetify';

export default defineConfig({
  plugins: [
    vue(),
    vuetify({ autoImport: true })
  ],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) return;

          if (id.includes('firebase')) {
            return 'firebase';
          }

          if (id.includes('vuetify')) {
            return 'vuetify';
          }

          if (id.includes('/vue/') || id.includes('vue-router') || id.includes('pinia')) {
            return 'vue-vendor';
          }
        }
      }
    }
  },
  server: {
    port: 5173
  }
});
