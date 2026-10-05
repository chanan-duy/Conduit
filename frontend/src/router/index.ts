import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/meetings/:meetingId',
      name: 'meeting',
      component: () => import('../views/MeetingView.vue'),
    },
    {
      path: '/meetings',
      name: 'meetings',
      component: () => import('../views/MeetingsManagerView.vue'),
    },
    {
      path: '/recording-bot/:meetingId',
      name: 'recording-bot',
      component: () => import('../views/RecodringBotView.vue'),
    },

    {
      path: '/:pathMatch(.*)*',
      name: 'NotFound',
      component: () => import('../views/NotFoundView.vue'),
    },
  ],
})

export default router
