<script setup lang="ts">
import type { MeetingInfoResponseDto } from '@/entities/meeting'
import { useMeetingsManagerStore } from '@/stores/meetings-manager'
import Menu from '@/volt/Menu.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { EllipsisIcon } from 'lucide-vue-next'
import type { ConfirmationOptions } from 'primevue/confirmationoptions'
import type { MenuItem } from 'primevue/menuitem'
import { useConfirm } from 'primevue/useconfirm'
import { computed, ref, shallowRef } from 'vue'
import CoParticipantsDialog from './CoParticipantsDialog.vue'
import CoRecordingsDialog from './CoRecordingsDialog.vue'

const { meeting } = defineProps<{
  meeting: MeetingInfoResponseDto
}>()

const meetingsStore = useMeetingsManagerStore()

const confirm = useConfirm()
const menu = ref()

const isParticipantsVisible = shallowRef(false)
const isRecordingsVisible = shallowRef(false)

const menuItems = computed<MenuItem[]>(() => [
  {
    label: 'Options',
    items: [
      ...(meeting.isOwner ? [{ label: 'Edit', command: () => handleEditMeeting() }] : []),
      { label: 'Participants', command: () => (isParticipantsVisible.value = true) },
      { label: 'Recordings', command: () => (isRecordingsVisible.value = true) },
      ...(meeting.isOwner ? [{ label: 'Delete', command: () => confirmDelete() }] : []),
    ],
  },
])

function confirmDelete() {
  confirm.require({
    message: `Are you sure you want to delete: "${meeting.title}"?`,
    header: 'Confirmation',
    rejectProps: {
      label: 'Cancel',
    },
    acceptProps: {
      label: 'Delete',
    },
    accept: () => {
      handleDeleteMeeting()
    },
  } satisfies ConfirmationOptions)
}

function handleEditMeeting() {
  meetingsStore.startEditing(meeting)
}

async function handleDeleteMeeting() {
  await meetingsStore.deleteMeeting(meeting.id)
}
</script>
<template>
  <SecondaryButton variant="outlined" size="small" @click="menu?.toggle($event)">
    <template #icon>
      <EllipsisIcon />
    </template>
  </SecondaryButton>
  <Menu ref="menu" id="overlay_menu" :model="menuItems" :popup="true" />
  <CoParticipantsDialog v-model:visible="isParticipantsVisible" :meeting="meeting" />
  <CoRecordingsDialog v-model:visible="isRecordingsVisible" :meeting="meeting" />
</template>
