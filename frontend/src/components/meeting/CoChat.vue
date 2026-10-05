<script setup lang="ts">
import { useSignalrMeeting } from '@/composables/useSignalrMeeting'
import { vAutoScroll } from '@/directives/autoScroll'
import { auth } from '@/services/authService'
import Button from '@/volt/Button.vue'
import InputText from '@/volt/InputText.vue'
import { onMounted, onUnmounted, shallowRef } from 'vue'
import CoErrorWrapper from '../CoErrorWrapper.vue'
import CoChatMessage from './CoChatMessage.vue'

const {
  connect,
  subscribe,
  disconnect,
  sendChatMessage,
  retrieveChatMessagesHistory,
  chatMessages,
} = useSignalrMeeting(auth)

const { meetingId } = defineProps<{
  meetingId: string
  isRecordingBot?: boolean
}>()

const currentUserId = auth.getUserId() ?? ''

const errorCaptured = shallowRef<unknown>(null)

const newChatMessage = shallowRef('')
const sendError = shallowRef('')

const MAX_CHAT_MESSAGE_SIZE: number = 10_000

onMounted(async () => {
  try {
    await connect(meetingId)
    await subscribe()
    await retrieveChatMessagesHistory()
  } catch (e) {
    errorCaptured.value = e
  }
})

onUnmounted(async () => {
  await disconnect()
})

let isSendingChatMessage: boolean = false
async function handleSendChatMessage(text: string) {
  if (text.trim() === '') {
    return
  }

  if (isSendingChatMessage) {
    return
  }

  if (text.length > MAX_CHAT_MESSAGE_SIZE) {
    console.log('Message too large to be sent')
    return
  }

  isSendingChatMessage = true
  sendError.value = ''

  try {
    await sendChatMessage(text)
    newChatMessage.value = ''
  } catch (e) {
    sendError.value = 'Message was not sent. Try again.'
    console.log('send failed', e)
  } finally {
    isSendingChatMessage = false
  }
}
</script>

<template>
  <div class="flex flex-col gap-1 w-[max(200px,min(24rem,22dvw))]">
    <CoErrorWrapper :error="errorCaptured" class="w-full h-full my-2">
      <div class="flex flex-col h-full relative">
        <div v-auto-scroll class="flex flex-col gap-2 overflow-y-auto absolute inset-0 p-2">
          <template v-for="msg in chatMessages" :key="msg.Id">
            <CoChatMessage :msg="msg" :current-user-id="currentUserId" />
          </template>
        </div>
      </div>
      <form
        v-if="!isRecordingBot"
        @submit.prevent="() => handleSendChatMessage(newChatMessage)"
        class="flex flex-row gap-2 p-2"
      >
        <InputText type="text" v-model="newChatMessage" size="small" fluid />
        <Button type="submit" label="Send" size="small" variant="outlined" />
      </form>
      <small v-if="sendError" class="px-2 pb-2 text-red-500">{{ sendError }}</small>
    </CoErrorWrapper>
  </div>
</template>
