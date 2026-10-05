<script setup lang="ts">
import { useAuth } from '@/composables/useAuth'
import Button from '@/volt/Button.vue'
import ContrastButton from '@/volt/ContrastButton.vue'
import { shallowRef } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../services/authService'
import CoProfileDialog from './CoProfileDialog.vue'
import CoThemeSwitcher from './CoThemeSwitcher.vue'

const { showLoginModal } = useAuth()

const isProfileVisible = shallowRef(false)
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  await router.push('/')
}
</script>

<template>
  <header
    class="border-b p-2 sticky top-0 z-10 border-surface-200 bg-white/80 backdrop-blur-lg dark:border-surface-600 dark:bg-black/50 dark:backdrop-blur-lg dark:backdrop-brightness-50 dark:backdrop-saturate-150"
  >
    <div class="mx-auto">
      <nav class="flex justify-between items-center">
        <div class="flex items-center gap-2">
          <RouterLink to="/" custom v-slot="{ href, navigate, isActive }">
            <ContrastButton
              label="Home"
              size="small"
              variant="text"
              link
              as="a"
              :href="href"
              @click="navigate"
              class="after:content-[''] after:absolute after:bottom-0 after:left-0 after:right-0 after:transition-transform after:duration-200 after:scale-x-0 after:border-b after:border-surface-400 dark:after:border-surface-200"
              :class="{
                'after:scale-x-100': isActive,
              }"
            />
          </RouterLink>
          <RouterLink to="/meetings" custom v-slot="{ href, navigate, isActive }">
            <ContrastButton
              label="Meetings"
              size="small"
              variant="text"
              link
              as="a"
              :href="href"
              @click="navigate"
              class="after:content-[''] after:absolute after:bottom-0 after:left-0 after:right-0 after:transition-transform after:duration-200 after:scale-x-0 after:border-b after:border-surface-400 dark:after:border-surface-200"
              :class="{
                'after:scale-x-100': isActive,
              }"
            />
          </RouterLink>
        </div>
        <div class="flex items-center gap-2">
          <template v-if="auth.isLoggedIn.value == null" />
          <template v-else-if="auth.isLoggedIn.value">
            <Button @click="isProfileVisible = true" label="Profile" size="small" variant="text" />
            <Button @click="handleLogout" label="Logout" size="small" variant="outlined" />
          </template>
          <template v-else>
            <Button label="Login" size="small" @click="showLoginModal()" />
          </template>
          <CoThemeSwitcher />
        </div>
      </nav>
    </div>
    <CoProfileDialog v-model:visible="isProfileVisible" />
  </header>
</template>
