import { defineConfig } from "vite";

export default defineConfig({
  root: ".",
  server: {
    port: 6003,
    strictPort: true,
    open: true,
  },
  build: {
    outDir: "dist",
    rollupOptions: {
      input: {
        main: "index.html",
        symbol: "pages/symbol.html",
        download: "pages/download.html",
      },
    },
  },
});
