<script setup lang="ts">
import type { RemoteParticipant, RemoteTrack } from 'livekit-client'
import { Track } from 'livekit-client'
import { onMounted, onUnmounted, ref } from 'vue'
import CoParticipant from './CoParticipant.vue'

const props = defineProps<{
  participant: RemoteParticipant
}>()

const videoEl = ref<HTMLVideoElement | null>(null)
const screenShareEl = ref<HTMLVideoElement | null>(null)
const audioEl = ref<HTMLAudioElement | null>(null)
const audioScreenShareEl = ref<HTMLAudioElement | null>(null)

function handleTrackSubscribed(track: RemoteTrack) {
  if (track.kind === Track.Kind.Video) {
    if (videoEl.value && track.source === Track.Source.Camera) {
      track.attach(videoEl.value)
    }
    if (screenShareEl.value && track.source === Track.Source.ScreenShare) {
      track.attach(screenShareEl.value)
    }
  } else if (track.kind === Track.Kind.Audio) {
    if (audioEl.value && track.source === Track.Source.Microphone) {
      track.attach(audioEl.value)
    }
    if (audioScreenShareEl.value && track.source === Track.Source.ScreenShareAudio) {
      track.attach(audioScreenShareEl.value)
    }
  }
}

function handleTrackUnsubscribed(track: RemoteTrack) {
  track.detach()
}

onMounted(() => {
  props.participant.videoTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleTrackSubscribed(publication.track)
    }
  })
  props.participant.audioTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleTrackSubscribed(publication.track)
    }
  })

  props.participant
    .on('trackSubscribed', handleTrackSubscribed)
    .on('trackUnsubscribed', handleTrackUnsubscribed)
})

onUnmounted(() => {
  props.participant.videoTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleTrackUnsubscribed(publication.track)
    }
  })
  props.participant.audioTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleTrackUnsubscribed(publication.track)
    }
  })

  props.participant
    .off('trackSubscribed', handleTrackSubscribed)
    .off('trackUnsubscribed', handleTrackUnsubscribed)
})
</script>

<template>
  <CoParticipant
    :participant="participant"
    v-model:video-el="videoEl"
    v-model:screen-share-el="screenShareEl"
    v-model:audio-el="audioEl"
    v-model:audio-screen-share-el="audioScreenShareEl"
  />
</template>
