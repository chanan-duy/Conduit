<script setup lang="ts">
import { computed } from 'vue'
import type { ChatMessage } from '../../entities/chat-message'

const { msg, currentUserId } = defineProps<{
  msg: ChatMessage
  currentUserId: string
}>()

const timeComputed = computed(() => {
  const time = msg.CreatedAt
  const formattedDate =
    `${time.getHours().toString().padStart(2, '0')}` +
    `:${time.getMinutes().toString().padStart(2, '0')}` +
    `:${time.getSeconds().toString().padStart(2, '0')}`
  return formattedDate
})

const isCurrentUser = computed(() => msg.CreatorId === currentUserId)
</script>

<template>
  <div class="flex flex-col text-sm px-1">
    <div class="flex gap-1 items-center justify-between">
      <span class="font-semibold truncate" :title="msg.CreatorName">{{ msg.CreatorName }}</span>
      <span
        :title="timeComputed"
        class="text-xs"
        :class="
          isCurrentUser
            ? 'text-purple-600 dark:text-purple-400'
            : 'text-blue-600 dark:text-blue-400'
        "
      >
        ({{ timeComputed }})
      </span>
    </div>
    <p class="text-pretty wrap-break-word">{{ msg.Content }}</p>
  </div>
</template>
