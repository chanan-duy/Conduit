<script setup lang="ts">
import { useFocusLayout } from '@/composables/useFocusLayout'
import { useGridLayout } from '@/composables/useGridLayout'
import { useRoomLivekit } from '@/composables/useRoomLiveKit'
import { LocalParticipant, Participant, RemoteParticipant } from 'livekit-client'
import { computed, shallowRef, useTemplateRef, type ComputedRef } from 'vue'
import CoLocalParticipant from './CoLocalParticipant.vue'
import CoRemoteParticipant from './CoRemoteParticipant.vue'

const { localParticipant, remoteParticipants, isRecordingBot, focusedParticipant } =
  useRoomLivekit()

const allParticipants: ComputedRef<Participant[]> = computed(() => {
  const participants: Participant[] = Array.from(remoteParticipants.value.values())
  if (localParticipant.value && !isRecordingBot.value) {
    participants.unshift(localParticipant.value)
  }
  return participants
})

const otherParticipants = computed(() => {
  return allParticipants.value.filter((p) => p.sid !== focusedParticipant.value?.sid)
})

const container = useTemplateRef<HTMLElement>('container')
const participantCount = computed(() => allParticipants.value.length)

const { layout } = useGridLayout(container, participantCount, {
  gap: shallowRef(16),
  minWidth: shallowRef(250),
})
const { focusLayout } = useFocusLayout(container, {
  filmstripHeight: shallowRef(160),
  gap: shallowRef(16),
})
</script>

<template>
  <div ref="container" class="absolute inset-0 flex flex-col justify-center overflow-hidden m-8">
    <div v-if="focusedParticipant" class="w-full h-full flex flex-col">
      <div class="flex-1 min-h-0 flex items-center justify-center">
        <div
          :style="{
            width: `${focusLayout.focused.width}px`,
            height: `${focusLayout.focused.height}px`,
          }"
        >
          <CoRemoteParticipant
            v-if="focusedParticipant instanceof RemoteParticipant"
            :participant="focusedParticipant"
          />
          <CoLocalParticipant v-else :local-participant="focusedParticipant as LocalParticipant" />
        </div>
      </div>

      <div class="shrink-0" :style="{ height: `${focusLayout.filmstrip.tileHeight + 32}px` }">
        <div class="h-full w-full overflow-x-auto p-4">
          <div class="flex h-full items-center gap-4">
            <template v-for="participant in otherParticipants" :key="participant.sid">
              <div
                class="shrink-0"
                :style="{
                  width: `${focusLayout.filmstrip.tileWidth}px`,
                  height: `${focusLayout.filmstrip.tileHeight}px`,
                }"
              >
                <CoRemoteParticipant
                  v-if="participant instanceof RemoteParticipant"
                  :participant="participant"
                />
                <CoLocalParticipant v-else :local-participant="participant as LocalParticipant" />
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="flex justify-center w-full h-full overflow-y-auto overflow-x-hidden">
      <div
        class="grid gap-4 m-auto"
        :style="{
          gridTemplateColumns: `repeat(${layout.cols}, 1fr)`,
          width: `${layout.cols * layout.width + (layout.cols - 1) * layout.gap}px`,
          height: `${layout.rows * layout.height + (layout.rows - 1) * layout.gap}px`,
        }"
      >
        <div
          v-for="participant in allParticipants"
          :key="participant.sid"
          :style="{ width: `${layout.width}px`, height: `${layout.height}px` }"
        >
          <CoRemoteParticipant
            v-if="participant instanceof RemoteParticipant"
            :participant="participant"
          />
          <CoLocalParticipant
            v-else-if="participant instanceof LocalParticipant"
            :local-participant="participant"
          />
        </div>
      </div>
    </div>
  </div>
</template>
