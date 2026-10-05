<script setup lang="ts">
import CoMeeting from '@/components/meeting/CoMeeting.vue'
import { auth } from '@/services/authService'
import { Room } from 'livekit-client'
import { onMounted, shallowRef } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const meetingId = shallowRef<string | null>()
const liveKitUrl = shallowRef<string | null>()
const liveKitToken = shallowRef<string | null>()
const exchangeToken = shallowRef<string | null>()

const room = shallowRef<Room | null>(null)

const recordingState = shallowRef<boolean>(false)

onMounted(async () => {
  meetingId.value = route.params.meetingId?.toString() || ''
  liveKitUrl.value = route.query.url?.toString() || ''
  liveKitToken.value = route.query.token?.toString() || ''
  exchangeToken.value = route.query.exchangeToken?.toString() || ''

  if (!meetingId.value) {
    throw new Error('Meeting id is empty')
  }

  if (!liveKitUrl.value) {
    throw new Error('Livekit url is empty')
  }

  if (!liveKitToken.value) {
    throw new Error('Livekit token is empty')
  }

  if (!exchangeToken.value) {
    throw new Error('Exchange token is empty')
  }

  await auth.exchangeToken({ meetingId: meetingId.value, exchangeToken: exchangeToken.value })

  room.value = new Room({})

  room.value.on('connected', () => {
    startRecording()
  })

  room.value.on('disconnected', () => {
    endRecording()
  })
})

function startRecording() {
  if (recordingState.value) {
    return
  }
  console.log('START_RECORDING')
  recordingState.value = true
}

function endRecording() {
  if (!recordingState.value) {
    return
  }
  console.log('END_RECORDING')
  recordingState.value = false
}
</script>

<template>
  <template v-if="meetingId && liveKitUrl && liveKitToken && exchangeToken && room">
    <CoMeeting
      :meeting-id="meetingId"
      :meeting-token="liveKitToken"
      :live-kit-url="liveKitUrl"
      :room="room"
      :is-recording-bot="true"
    />
  </template>
  <template v-else>
    <div class="flex flex-col">
      <p>Meeting id: {{ meetingId }}</p>
      <p>LiveKit url: {{ liveKitUrl }}</p>
      <p>LiveKit token: {{ liveKitToken }}</p>
      <p>Exchange token: {{ exchangeToken }}</p>
    </div>
  </template>
</template>
