<script setup lang="ts">
import { useAudioVisualizer } from '@/composables/useAudioVisualizer'
import { useMediaDevices } from '@/composables/useMediaDevices'
import type { JoinDevices } from '@/composables/useRoomLiveKit'
import Button from '@/volt/Button.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import Select from '@/volt/Select.vue'
import {
  createLocalAudioTrack,
  createLocalVideoTrack,
  LocalAudioTrack,
  LocalVideoTrack,
} from 'livekit-client'
import { CameraIcon, CameraOffIcon, MicIcon, MicOffIcon, WebcamIcon } from 'lucide-vue-next'
import { onUnmounted, shallowRef, watch } from 'vue'

const { setupVisualizer, teardownVisualizer, visualizerStyle } = useAudioVisualizer()
const {
  currentAudioDeviceId,
  currentVideoDeviceId,
  audioDevices,
  videoDevices,
  updateAudioDevices,
  updateVideoDevices,
} = useMediaDevices()

const emit = defineEmits<{
  submitJoin: [devices: JoinDevices]
}>()

const isActiveMic = shallowRef<boolean>(false)
const isActiveCam = shallowRef<boolean>(false)

const videoEl = shallowRef<HTMLVideoElement | null>(null)
const audioEl = shallowRef<HTMLAudioElement | null>(null)

const audioTrack = shallowRef<LocalAudioTrack | null>(null)
const videoTrack = shallowRef<LocalVideoTrack | null>(null)

watch(currentAudioDeviceId, async (newDeviceId) => {
  if (audioTrack.value && newDeviceId) {
    await audioTrack.value.restartTrack({ deviceId: newDeviceId })
  }
})

watch(currentVideoDeviceId, async (newDeviceId) => {
  if (videoTrack.value && newDeviceId) {
    await videoTrack.value.restartTrack({ deviceId: newDeviceId })
  }
})

function handleJoin() {
  emit('submitJoin', { microphone: isActiveMic.value, camera: isActiveCam.value })
}

onUnmounted(() => {
  teardownVisualizer()
  audioTrack.value?.stop()
  videoTrack.value?.stop()
  audioTrack.value?.detach()
  videoTrack.value?.detach()
})

async function handleToggleMicrophone() {
  if (!audioEl.value) {
    return
  }

  if (!audioTrack.value) {
    const track = await createLocalAudioTrack({
      deviceId: currentAudioDeviceId.value,
      autoGainControl: true,
      echoCancellation: true,
      noiseSuppression: true,
    })
    track.attach(audioEl.value)

    audioTrack.value = track

    isActiveMic.value = true

    if (track.mediaStreamTrack) {
      setupVisualizer(track)
    }

    return
  }

  if (audioTrack.value.isMuted) {
    await audioTrack.value.unmute()
    isActiveMic.value = true
    if (audioTrack.value.mediaStreamTrack) {
      setupVisualizer(audioTrack.value)
    }
  } else {
    await audioTrack.value.mute()
    isActiveMic.value = false
    teardownVisualizer()
  }
}

async function handleToggleCamera() {
  if (!videoEl.value) {
    return
  }

  if (!videoTrack.value) {
    const track = await createLocalVideoTrack({
      deviceId: currentVideoDeviceId.value,
    })
    track.attach(videoEl.value)

    videoTrack.value = track

    isActiveCam.value = true
    return
  }

  if (videoTrack.value.isMuted) {
    await videoTrack.value.unmute()
    isActiveCam.value = true
  } else {
    await videoTrack.value.mute()
    isActiveCam.value = false
  }
}
</script>

<template>
  <div class="w-full flex flex-col gap-2">
    <div
      class="aspect-video bg-surface-100 dark:bg-surface-800 rounded-lg flex justify-center items-center"
    >
      <WebcamIcon :size="100" class="text-surface-400 dark:text-surface-500 absolute" />
      <video
        v-show="isActiveCam"
        ref="videoEl"
        class="w-full h-full object-cover z-10"
        autoplay
        playsinline
        muted
      ></video>

      <audio ref="audioEl" autoplay></audio>
    </div>
    <div class="rounded-lg w-full h-2 bg-surface-100 dark:bg-surface-800 overflow-hidden">
      <div class="h-full transition-all duration-100 ease-linear" :style="visualizerStyle"></div>
    </div>
    <div class="grid grid-cols-2 gap-2">
      <SecondaryButton label="Microphone" @click="handleToggleMicrophone">
        <template #icon>
          <component :is="isActiveMic ? MicIcon : MicOffIcon" :size="20" />
        </template>
      </SecondaryButton>
      <SecondaryButton label="Camera" @click="handleToggleCamera">
        <template #icon>
          <component :is="isActiveCam ? CameraIcon : CameraOffIcon" :size="20" />
        </template>
      </SecondaryButton>
      <Select
        v-model="currentAudioDeviceId"
        :options="audioDevices"
        option-label="label"
        option-value="deviceId"
        data-key="deviceId"
        placeholder="Audio Device"
        :title="audioDevices.find((d) => d.deviceId === currentAudioDeviceId)?.label || ''"
        @before-show="updateAudioDevices"
      >
        <template #value="slotProps: { value: string; placeholder: string }">
          {{
            slotProps.value
              ? audioDevices.find((d) => d.deviceId === slotProps.value)?.label || slotProps.value
              : slotProps.placeholder
          }}
        </template>
      </Select>
      <Select
        v-model="currentVideoDeviceId"
        :options="videoDevices"
        option-label="label"
        option-value="deviceId"
        data-key="deviceId"
        placeholder="Video Device"
        :title="videoDevices.find((d) => d.deviceId === currentVideoDeviceId)?.label || ''"
        @before-show="updateVideoDevices"
      >
        <template #value="slotProps: { value: string; placeholder: string }">
          {{
            slotProps.value
              ? videoDevices.find((d) => d.deviceId === slotProps.value)?.label || slotProps.value
              : slotProps.placeholder
          }}
        </template>
      </Select>
    </div>
    <Button class="mt-2" label="Join" @click="handleJoin" />
  </div>
</template>
