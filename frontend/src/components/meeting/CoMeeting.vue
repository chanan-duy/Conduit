<script setup lang="ts">
import CoChat from '@/components/meeting/CoChat.vue'
import { useRoomLivekit, type JoinDevices } from '@/composables/useRoomLiveKit'
import { isBrowserSupported, Room } from 'livekit-client'
import { LoaderCircleIcon } from 'lucide-vue-next'
import { onMounted, onUnmounted, shallowRef } from 'vue'
import CoErrorWrapper from '../CoErrorWrapper.vue'
import CoControlsBar from './CoControlsBar.vue'
import CoMeetingLayout from './CoMeetingLayout.vue'

const {
  meetingId,
  meetingToken,
  liveKitUrl,
  room,
  isRecordingBot = false,
  joinDevices,
} = defineProps<{
  meetingId: string
  meetingToken: string
  liveKitUrl: string
  room: Room
  isRecordingBot?: boolean
  joinDevices?: JoinDevices
}>()

const {
  initMeeting,
  cleanupMeeting,
  subsribeToUpdates,
  connect,
  connectionState,
  handleToggleMicrophone,
  handleToggleCamera,
} = useRoomLivekit()

const errorCaptured = shallowRef<unknown>(null)

onMounted(async () => {
  if (!isBrowserSupported()) {
    return
  }

  try {
    initMeeting(room, isRecordingBot)
    subsribeToUpdates()
    await connect(liveKitUrl, meetingToken)
    if (joinDevices?.microphone) await handleToggleMicrophone()
    if (joinDevices?.camera) await handleToggleCamera()
  } catch (e) {
    errorCaptured.value = e
  }
})

onUnmounted(() => {
  cleanupMeeting()
})
</script>
<template>
  <div class="flex flex-col h-full max-h-full">
    <div class="flex flex-row h-full">
      <div class="flex flex-col w-full relative">
        <CoErrorWrapper :error="errorCaptured" class="m-2">
          <template v-if="connectionState === 'connecting'">
            <LoaderCircleIcon
              :size="32"
              class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-spin"
            />
          </template>
          <template v-else-if="connectionState === 'disconnected' || !connectionState">
            <div class="h-full flex flex-col justify-center items-center">
              <span class="font-semibold text-2xl tracking-tight">Disconnected</span>
            </div>
          </template>
          <template v-else>
            <CoMeetingLayout />
            <div
              v-if="connectionState === 'reconnecting' || connectionState === 'signalReconnecting'"
              class="absolute inset-0 z-10 flex items-center justify-center bg-black/60"
            >
              <span class="flex items-center gap-2 font-semibold text-xl tracking-tight">
                <LoaderCircleIcon :size="20" class="animate-spin" />
                Video connection lost, reconnecting
              </span>
            </div>
          </template>
        </CoErrorWrapper>
      </div>
      <CoChat v-if="meetingId" :meetingId :is-recording-bot="isRecordingBot" />
    </div>
    <div
      v-if="!isRecordingBot"
      class="flex flex-row items-center justify-center gap-4 px-4 py-2 border-t border-surface-600 relative"
    >
      <CoControlsBar />
    </div>
  </div>
</template>
