<script setup lang="ts">
import { isAxiosError } from 'axios'
import { twMerge } from 'tailwind-merge'
import { computed, type HTMLAttributes } from 'vue'

const { error, class: classes } = defineProps<{
  error: unknown
  class?: HTMLAttributes['class']
}>()

const readableError = computed<string>(() => {
  if (typeof error === 'string') {
    return error
  }

  if (isAxiosError(error) && error.response && error.response.status >= 502) {
    return `The service is temporarily unavailable (HTTP ${error.response.status}). Try again later.`
  }

  if (isAxiosError(error)) {
    const propertiecToDisplay = {
      message: error.message,
      code: error.code,
      baseUrl: error.config?.baseURL,
      url: error.config?.url,
      method: error.config?.method,
    }

    return `Axios Error\n${Object.entries(propertiecToDisplay)
      .map(([key, value]) => `${key}: ${value}`)
      .join('\n')}}`
  }

  if (error instanceof Error) {
    return error.message
  }

  return JSON.stringify(error, undefined, 2)
})
</script>

<template>
  <div
    v-if="error"
    :class="
      twMerge(
        'border-2 border-dashed border-rose-800 dark:border-rose-700 rounded-lg py-2 px-4 bg-surface-100 dark:bg-surface-900',
        classes,
      )
    "
  >
    <h2 class="text-lg font-bold mb-2">Something went wrong</h2>
    <p class="text-xs whitespace-pre-wrap">
      {{ readableError }}
    </p>
  </div>
  <slot v-else></slot>
</template>
