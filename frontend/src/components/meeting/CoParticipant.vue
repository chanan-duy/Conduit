<script setup lang="ts">
import { useRoomLivekit } from '@/composables/useRoomLiveKit'
import {
  LocalTrackPublication,
  RemoteTrack,
  RemoteTrackPublication,
  Track,
  type ConnectionQuality,
  type Participant,
  type TrackPublication,
} from 'livekit-client'
import { FocusIcon, MicOffIcon } from 'lucide-vue-next'
import { computed, nextTick, onMounted, onUnmounted, shallowRef } from 'vue'

const { participant, isLocal = false } = defineProps<{
  participant: Participant
  isLocal?: boolean
}>()

const { toggleFocusParticipant, remoteParticipants, localParticipant } = useRoomLivekit()

const participantCount = computed(() => {
  return remoteParticipants.value.size + (localParticipant.value ? 1 : 0)
})

const canFocus = computed(() => participantCount.value > 1)

const videoEl = defineModel<HTMLVideoElement | null>('videoEl', {})
const screenShareEl = defineModel<HTMLVideoElement | null>('screenShareEl', {})
const audioEl = defineModel<HTMLAudioElement | null>('audioEl', { required: false })
const audioScreenShareEl = defineModel<HTMLAudioElement | null>('audioScreenShareEl', {
  required: false,
})

const isMicOn = shallowRef<boolean>(false)
const isCamOn = shallowRef<boolean>(false)
const isScreenSharing = shallowRef<boolean>(false)
const isScreenSharingAudio = shallowRef<boolean>(false)
const isSpeaking = shallowRef<boolean>(false)

const compDisplayName = shallowRef<string>('')
const compDisplayNameShort = shallowRef<string>('')

async function recalculateState() {
  await nextTick()

  compDisplayName.value =
    (participant.name || participant.identity || '?unkown') + (isLocal ? ' (You)' : '')
  compDisplayNameShort.value = (compDisplayName.value && compDisplayName.value[0]) || '?'

  const micPub = participant.getTrackPublication(Track.Source.Microphone)
  isMicOn.value = !!micPub && micPub.isSubscribed && !micPub.isMuted

  const camPub = participant.getTrackPublication(Track.Source.Camera)
  isCamOn.value = !!camPub && camPub.isSubscribed && !camPub.isMuted

  const screenSharePub = participant.getTrackPublication(Track.Source.ScreenShare)
  isScreenSharing.value = !!screenSharePub && screenSharePub.isSubscribed && !screenSharePub.isMuted

  const screenShareAudioPub = participant.getTrackPublication(Track.Source.ScreenShareAudio)
  isScreenSharingAudio.value =
    !!screenShareAudioPub && screenShareAudioPub.isSubscribed && !screenShareAudioPub.isMuted

  isSpeaking.value = participant.isSpeaking
}

onMounted(() => {
  participant
    .on('localTrackPublished', handleLocalTrackPublished)
    .on('localTrackUnpublished', handleLocalTrackUnpublished)
    .on('trackSubscribed', handleTrackSubscribed)
    .on('trackUnsubscribed', handleTrackUnsubscribed)
    .on('trackMuted', handleTrackMuted)
    .on('trackUnmuted', handleTackUnmuted)
    .on('isSpeakingChanged', handleIsSpeakingChanged)
    .on('connectionQualityChanged', handleConnectionQualityChanged)
    .on('active', handleActive)
    .on('participantNameChanged', handleParticipantNameChanged)
    .on('participantMetadataChanged', handleParticipantMetadataChanged)
    .on('participantPermissionsChanged', handleParticipantPermissionsChanged)

  recalculateState()
})

onUnmounted(() => {
  participant
    .off('localTrackPublished', handleLocalTrackPublished)
    .off('localTrackUnpublished', handleLocalTrackUnpublished)
    .off('trackSubscribed', handleTrackSubscribed)
    .off('trackUnsubscribed', handleTrackUnsubscribed)
    .off('trackMuted', handleTrackMuted)
    .off('trackUnmuted', handleTackUnmuted)
    .off('isSpeakingChanged', handleIsSpeakingChanged)
    .off('connectionQualityChanged', handleConnectionQualityChanged)
    .off('active', handleActive)
    .off('participantNameChanged', handleParticipantNameChanged)
    .off('participantMetadataChanged', handleParticipantMetadataChanged)
    .off('participantPermissionsChanged', handleParticipantPermissionsChanged)
})

function handleLocalTrackPublished(publication: LocalTrackPublication) {
  console.log('localTrackPublished', publication)
  recalculateState()
}

function handleLocalTrackUnpublished(publication: LocalTrackPublication) {
  console.log('localTrackUnpublished', publication)
  recalculateState()
}

function handleTrackSubscribed(track: RemoteTrack, publication: RemoteTrackPublication) {
  console.log('trackSubscribed', track, publication)
  recalculateState()
}

function handleTrackUnsubscribed(track: RemoteTrack, publication: RemoteTrackPublication) {
  console.log('trackUnsubscribed', track, publication)
  recalculateState()
}

function handleTrackMuted(publication: TrackPublication) {
  console.log('trackMuted', publication)
  recalculateState()
}

function handleTackUnmuted(publication: TrackPublication) {
  console.log('trackUnmuted', publication)
  recalculateState()
}

function handleIsSpeakingChanged(speaking: boolean) {
  console.log('isSpeakingChanged', speaking)
  isSpeaking.value = speaking
}

function handleConnectionQualityChanged(quality: ConnectionQuality) {
  console.log('connectionQualityChanged', quality)
  recalculateState()
}

function handleActive() {
  console.log('active')
  recalculateState()
}

function handleParticipantNameChanged() {
  console.log('participantNameChanged')
  recalculateState()
}

function handleParticipantMetadataChanged() {
  console.log('participantMetadataChanged')
  recalculateState()
}

function handleParticipantPermissionsChanged() {
  console.log('participantPermissionsChanged')
  recalculateState()
}
</script>

<template>
  <div
    class="border rounded border-surface-700 bg-surface-50 dark:bg-surface-900 flex items-center justify-center text-center aspect-video relative transition-shadow duration-300 ease-[cubic-bezier(0.25,1,0.5,1)] ring-4 object-contain h-full w-full"
    :class="{
      'ring-green-500': isSpeaking,
      'ring-transparent': !isSpeaking,
      'cursor-pointer group': canFocus,
    }"
    @click="canFocus && toggleFocusParticipant(participant)"
  >
    <div
      v-if="canFocus"
      class="absolute inset-0 bg-black/20 z-20 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
    >
      <FocusIcon :size="30" class="dark:text-surface-100/80" />
    </div>
    <video
      v-show="isCamOn && !isScreenSharing"
      ref="videoEl"
      class="w-full h-full object-cover z-10"
      autoplay
      playsinline
      muted
    ></video>
    <video
      v-show="isScreenSharing"
      ref="screenShareEl"
      class="w-full h-full object-cover z-10"
      autoplay
      playsinline
      muted
    ></video>

    <audio v-if="!isLocal" ref="audioEl" autoplay></audio>
    <audio v-if="!isLocal" ref="audioScreenShareEl" autoplay></audio>
    <span class="text-5xl absolute">{{ compDisplayNameShort }}</span>
    <div
      class="flex flex-row gap-2 absolute bottom-2 bg-black/50 text-white px-2 py-1 rounded z-10 items-center"
    >
      <span class="text-sm break-all">
        {{ compDisplayName }}
      </span>
      <MicOffIcon v-if="!isMicOn" :size="16" class="text-red-400" />
    </div>
  </div>
</template>
