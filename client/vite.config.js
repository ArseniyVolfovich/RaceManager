import { defineConfig } from 'vite';
import { resolve } from 'node:path';

export default defineConfig({
  root: '.',
  publicDir: 'public',
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5088',
        changeOrigin: true
      }
    }
  },
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        championships: resolve(__dirname, 'championships.html'),
        calendar: resolve(__dirname, 'calendar.html'),
        teams: resolve(__dirname, 'teams.html'),
        media: resolve(__dirname, 'media.html'),
        login: resolve(__dirname, 'pages/auth/login.html'),
        register: resolve(__dirname, 'pages/auth/register.html'),
        support: resolve(__dirname, 'pages/support/support.html'),
        account: resolve(__dirname, 'pages/account/account.html'),
        createEvent: resolve(__dirname, 'pages/account/CreateEvent.html'),
        createdEvent: resolve(__dirname, 'pages/account/CreatedEvent.html'),
        beteraDrift: resolve(__dirname, 'pages/championships/BeteraDriftChampionship.html'),
        drag402: resolve(__dirname, 'pages/championships/Drag402Championship.html'),
        drag402Stage2: resolve(__dirname, 'pages/championships/Drag402Stage2.html'),
        gorillaDrift: resolve(__dirname, 'pages/championships/GorillaDriftChampionship.html'),
        uracing: resolve(__dirname, 'pages/championships/UracingChampionship.html'),
        beteraTeam: resolve(__dirname, 'pages/teams/BeteraTeam.html'),
        blockchainTeam: resolve(__dirname, 'pages/teams/BlockchainSportsTeam.html'),
        driftRacingParkTeam: resolve(__dirname, 'pages/teams/DriftRacingParkTeam.html'),
        lowBudgetTeam: resolve(__dirname, 'pages/teams/LowBudgetTeam.html')
      }
    }
  }
});
