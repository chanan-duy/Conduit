<script setup lang="ts">
import CoErrorWrapper from '@/components/CoErrorWrapper.vue'
import CoMeetingCreator from '@/components/meetings-manager/CoMeetingCreator.vue'
import CoMeetingEditor from '@/components/meetings-manager/CoMeetingEditor.vue'
import CoMeetingsViewer from '@/components/meetings-manager/CoMeetingsViewer.vue'
import type { PatchMeetingRequestDto } from '@/entities/meeting'
import { useMeetingsManagerStore } from '@/stores/meetings-manager'
import Button from '@/volt/Button.vue'
import { storeToRefs } from 'pinia'
import { onMounted, shallowRef } from 'vue'

const meetingsStore = useMeetingsManagerStore()
const { meetings, isLoading, isEditorVisible, editingMeeting } = storeToRefs(meetingsStore)

const isCreatorVisible = shallowRef<boolean>(false)
const errorCaptured = shallowRef<unknown>(null)

onMounted(async () => {
  try {
    await meetingsStore.initMeetings()
  } catch (e) {
    errorCaptured.value = e
  }
})

function handleUpdateMeeting(payload: PatchMeetingRequestDto) {
  if (editingMeeting.value) {
    meetingsStore.updateMeeting(editingMeeting.value.id, payload)
  }
}
</script>

<template>
  <div class="p-4 sm:p-6 flex flex-col gap-6 container mx-auto">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold">Meetings</h1>
      <Button label="Create Meeting" @click="isCreatorVisible = true" />
    </div>
    <CoErrorWrapper :error="errorCaptured">
      <div v-if="isLoading">
        <span>Loading...</span>
      </div>
      <template v-else>
        <CoMeetingsViewer :meetings="meetings" />
        <CoMeetingCreator v-model:visible="isCreatorVisible" />
        <CoMeetingEditor
          v-if="editingMeeting"
          :visible="isEditorVisible"
          :meeting="editingMeeting"
          @update-visible="meetingsStore.cancelEditing"
          @update-meeting="handleUpdateMeeting"
        />
      </template>
    </CoErrorWrapper>
  </div>
</template>
