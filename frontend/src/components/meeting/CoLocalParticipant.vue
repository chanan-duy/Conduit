<script setup lang="ts">
import { Track, type LocalParticipant, type LocalTrackPublication } from 'livekit-client'
import { onMounted, onUnmounted, shallowRef } from 'vue'
import CoParticipant from './CoParticipant.vue'

const props = defineProps<{
  localParticipant: LocalParticipant
}>()

const localVideoEl = shallowRef<HTMLVideoElement | null>(null)
const localScreenShareEl = shallowRef<HTMLVideoElement | null>(null)

onMounted(() => {
  props.localParticipant.videoTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleLocalTrackPublished(publication)
    }
  })

  props.localParticipant
    .on('localTrackPublished', handleLocalTrackPublished)
    .on('localTrackUnpublished', handleLocalTrackUnpublished)
})

onUnmounted(() => {
  props.localParticipant.videoTrackPublications.forEach((publication) => {
    if (publication.isSubscribed && publication.track) {
      handleLocalTrackUnpublished(publication)
    }
  })

  props.localParticipant
    .off('localTrackPublished', handleLocalTrackPublished)
    .off('localTrackUnpublished', handleLocalTrackUnpublished)
})

function handleLocalTrackPublished(publication: LocalTrackPublication) {
  console.log('localTrackPublished', publication)

  if (publication.track && publication.kind === Track.Kind.Video) {
    if (localVideoEl.value && publication.source === Track.Source.Camera) {
      publication.track.attach(localVideoEl.value)
    }
    if (localScreenShareEl.value && publication.source === Track.Source.ScreenShare) {
      publication.track.attach(localScreenShareEl.value)
    }
  }
}

function handleLocalTrackUnpublished(publication: LocalTrackPublication) {
  console.log('localTrackUnpublished', publication)

  publication.track?.detach()
}
</script>
<template>
  <CoParticipant
    :participant="localParticipant"
    is-local
    v-model:video-el="localVideoEl"
    v-model:screen-share-el="localScreenShareEl"
  />
</template>
