<script setup lang="ts">
import { useAuth } from '@/composables/useAuth'
import ConfirmDialog from '@/volt/ConfirmDialog.vue'
import { useRoute } from 'vue-router'
import CoHeader from './components/CoHeader.vue'
import CoLoginModal from './components/CoLoginModal.vue'

const { isLoginModalVisible } = useAuth()
const route = useRoute()
</script>

<template>
  <div class="flex flex-col h-full">
    <CoHeader v-if="route.name !== 'recording-bot'" />

    <main class="h-full">
      <RouterView />
    </main>

    <CoLoginModal v-if="isLoginModalVisible" />
  </div>
  <ConfirmDialog />
</template>
