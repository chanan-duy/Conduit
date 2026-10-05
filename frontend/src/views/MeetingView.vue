<script setup lang="ts">
import CoErrorWrapper from '@/components/CoErrorWrapper.vue'
import CoMeeting from '@/components/meeting/CoMeeting.vue'
import CoPreJoin from '@/components/meeting/CoPreJoin.vue'
import type { JoinDevices } from '@/composables/useRoomLiveKit'
import type { MeetingInfoResponseDto } from '@/entities/meeting'
import { api } from '@/services/apiService'
import { isBrowserSupported, Room } from 'livekit-client'
import { CalendarIcon } from 'lucide-vue-next'
import { onMounted, shallowRef } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const errorCaptured = shallowRef<unknown>(null)
const isBrowserSupports = shallowRef<boolean>(isBrowserSupported())

const meetingId = shallowRef<string | null>(null)
const meetingInfo = shallowRef<MeetingInfoResponseDto | null>(null)
const meetingToken = shallowRef<string | null>(null)
const liveKitUrl = shallowRef<string | null>(null)
const meetingRoom = shallowRef<Room | null>(null)

const acceptedJoin = shallowRef<boolean>(false)
const joinDevices = shallowRef<JoinDevices>({ microphone: false, camera: false })

onMounted(async () => {
  meetingId.value = route.params.meetingId?.toString() || ''

  try {
    if (!meetingId.value) {
      throw new Error('Meeting id is empty')
    }

    meetingInfo.value = await api.getMeetingInfo(meetingId.value)

    const { token, url } = await api.getMeetingToken(meetingId.value)
    meetingToken.value = token
    liveKitUrl.value = url

    if (!isBrowserSupports.value) {
      return
    }

    const room = new Room({
      adaptiveStream: true,
      dynacast: true,
      disconnectOnPageLeave: true,
      audioCaptureDefaults: {
        autoGainControl: true,
        echoCancellation: true,
        noiseSuppression: true,
      },
    })
    meetingRoom.value = room

    room.prepareConnection(url, token)
  } catch (error) {
    errorCaptured.value = error
  }
})

async function handleJoinSubmit(devices: JoinDevices) {
  joinDevices.value = devices
  acceptedJoin.value = true

  if (meetingRoom.value) {
    meetingRoom.value.startAudio()
  }
}
</script>

<template>
  <CoErrorWrapper :error="errorCaptured">
    <template v-if="!acceptedJoin">
      <div
        class="container mx-auto sm:w-130 w-11/12 flex flex-col items-center justify-center h-full"
      >
        <template v-if="!isBrowserSupports">
          <span class="text-2xl">Browser is not supported</span>
        </template>
        <template v-else>
          <div class="text-center w-full mb-8 flex flex-col gap-4" v-if="meetingInfo">
            <h2
              class="text-2xl md:text-3xl font-semibold text-pretty wrap-break-word tracking-tight"
              :title="meetingInfo.title"
            >
              {{ meetingInfo.title }}
            </h2>
            <div
              class="flex gap-1.5 justify-center items-center text-surface-500 dark:text-surface-400"
            >
              <CalendarIcon :size="14" />
              <span :title="meetingInfo.startDate.toLocaleString()">
                {{
                  meetingInfo.startDate.toLocaleDateString(undefined, {
                    weekday: 'short',
                    month: 'short',
                    day: 'numeric',
                    hour: '2-digit',
                    hour12: false,
                    minute: '2-digit',
                  })
                }}
              </span>
            </div>
          </div>

          <CoPreJoin @submitJoin="handleJoinSubmit" />
        </template>
      </div>
    </template>
    <template v-else-if="meetingId && meetingToken && liveKitUrl && meetingRoom">
      <CoMeeting
        :meetingId="meetingId"
        :meetingToken="meetingToken"
        :liveKitUrl="liveKitUrl"
        :room="meetingRoom"
        :joinDevices="joinDevices"
      />
    </template>
  </CoErrorWrapper>
</template>
