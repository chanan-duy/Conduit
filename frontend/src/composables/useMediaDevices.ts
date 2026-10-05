import { Room } from 'livekit-client'
import { ref, shallowRef, watch } from 'vue'

const AUDIO_DEVICE_ID_KEY = '__mtg_current_audio_device_id'
const VIDEO_DEVICE_ID_KEY = '__mtg_current_video_device_id'

const currentAudioDeviceId = shallowRef<string | undefined>(
  localStorage.getItem(AUDIO_DEVICE_ID_KEY) || undefined,
)
const currentVideoDeviceId = shallowRef<string | undefined>(
  localStorage.getItem(VIDEO_DEVICE_ID_KEY) || undefined,
)

const audioDevices = ref<MediaDeviceInfo[]>([])
const videoDevices = ref<MediaDeviceInfo[]>([])

watch(currentAudioDeviceId, (newId) => {
  if (newId) {
    localStorage.setItem(AUDIO_DEVICE_ID_KEY, newId)
  } else {
    localStorage.removeItem(AUDIO_DEVICE_ID_KEY)
  }
})

watch(currentVideoDeviceId, (newId) => {
  if (newId) {
    localStorage.setItem(VIDEO_DEVICE_ID_KEY, newId)
  } else {
    localStorage.removeItem(VIDEO_DEVICE_ID_KEY)
  }
})

const updateAudioDevices = async () => {
  const devices = await Room.getLocalDevices('audioinput')
  audioDevices.value = devices

  if (!currentAudioDeviceId.value && audioDevices.value.length > 0 && !!audioDevices.value[0]) {
    currentAudioDeviceId.value = audioDevices.value[0].deviceId
  }
}

const updateVideoDevices = async () => {
  const devices = await Room.getLocalDevices('videoinput')
  videoDevices.value = devices

  if (!currentVideoDeviceId.value && videoDevices.value.length > 0 && !!videoDevices.value[0]) {
    currentVideoDeviceId.value = videoDevices.value[0].deviceId
  }
}

export function useMediaDevices() {
  return {
    currentAudioDeviceId,
    currentVideoDeviceId,
    audioDevices,
    videoDevices,
    updateAudioDevices,
    updateVideoDevices,
  }
}
