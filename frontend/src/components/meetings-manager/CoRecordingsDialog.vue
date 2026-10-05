<script setup lang="ts">
import CoErrorWrapper from '@/components/CoErrorWrapper.vue'
import type { MeetingInfoResponseDto, RecordingDto } from '@/entities/meeting'
import { api } from '@/services/apiService'
import Button from '@/volt/Button.vue'
import Dialog from '@/volt/Dialog.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { shallowRef, watch } from 'vue'

const { meeting } = defineProps<{
  meeting: MeetingInfoResponseDto
}>()

const visible = defineModel<boolean>('visible', { required: true })

const recordings = shallowRef<RecordingDto[]>([])
const errorCaptured = shallowRef<unknown>(null)
const downloadingId = shallowRef<string | null>(null)

watch(visible, async (isVisible) => {
  if (isVisible) await load()
})

async function load() {
  errorCaptured.value = null
  try {
    recordings.value = await api.getRecordings(meeting.id)
  } catch (e) {
    errorCaptured.value = e
  }
}

async function handleDownload(recording: RecordingDto) {
  downloadingId.value = recording.id
  try {
    const file = await api.getRecordingFile(recording.id)
    const url = URL.createObjectURL(file)
    const link = document.createElement('a')
    link.href = url
    link.download = `${meeting.title}-${recording.createdAt.slice(0, 16).replace(':', '-')}.mp4`
    link.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    errorCaptured.value = e
  } finally {
    downloadingId.value = null
  }
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    header="Recordings"
    class="sm:w-110 w-11/12"
    modal
    dismissable-mask
  >
    <CoErrorWrapper :error="errorCaptured">
      <ul class="flex flex-col gap-2">
        <li
          v-for="recording in recordings"
          :key="recording.id"
          class="flex items-center justify-between gap-2"
        >
          <div class="flex flex-col min-w-0">
            <span class="font-semibold">{{ new Date(recording.createdAt).toLocaleString() }}</span>
            <span class="text-xs text-surface-500 dark:text-surface-400 truncate">
              {{ recording.status }}{{ recording.error ? `: ${recording.error}` : '' }}
            </span>
          </div>
          <Button
            v-if="recording.status === 'Completed'"
            label="Download"
            size="small"
            variant="outlined"
            :loading="downloadingId === recording.id"
            @click="handleDownload(recording)"
          />
        </li>
        <li v-if="!recordings.length" class="text-sm text-surface-500 dark:text-surface-400">
          No recordings yet. Recording starts when someone joins the meeting.
        </li>
      </ul>
    </CoErrorWrapper>
    <template #footer>
      <SecondaryButton label="Refresh" @click="load" />
    </template>
  </Dialog>
</template>
