<script setup lang="ts">
import { useModeWatcher } from '@/composables/useModeWatcher'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { MoonIcon, SunIcon } from 'lucide-vue-next'

const { isDark, toggleTheme } = useModeWatcher()
</script>

<template>
  <div>
    <SecondaryButton @click="toggleTheme" variant="outlined">
      <template #icon>
        <Transition name="swap" mode="out-in">
          <SunIcon v-if="isDark" :size="16" key="sun" />
          <MoonIcon v-else :size="16" key="moon" />
        </Transition>
      </template>
    </SecondaryButton>
  </div>
</template>

<style>
html {
  transition: background-color 300ms cubic-bezier(0.65, 0, 0.35, 1);
}

.swap-enter-active,
.swap-leave-active {
  transition: all 0.1s ease-in-out;
}

.swap-enter-from {
  opacity: 0;
  transform: rotate(-90deg) scale(0.8);
}

.swap-leave-to {
  opacity: 0;
  transform: rotate(90deg) scale(0.8);
}
</style>
