<script setup lang="ts">
import CoErrorWrapper from '@/components/CoErrorWrapper.vue'
import type { MeetingInfoResponseDto, ParticipantDto } from '@/entities/meeting'
import { api } from '@/services/apiService'
import Button from '@/volt/Button.vue'
import Dialog from '@/volt/Dialog.vue'
import InputText from '@/volt/InputText.vue'
import Message from '@/volt/Message.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { isAxiosError } from 'axios'
import { shallowRef, watch } from 'vue'

const { meeting } = defineProps<{
  meeting: MeetingInfoResponseDto
}>()

const visible = defineModel<boolean>('visible', { required: true })

const participants = shallowRef<ParticipantDto[]>([])
const email = shallowRef('')
const addError = shallowRef('')
const errorCaptured = shallowRef<unknown>(null)
const isLoading = shallowRef(false)

watch(visible, async (isVisible) => {
  if (!isVisible) return

  email.value = ''
  addError.value = ''
  errorCaptured.value = null
  try {
    participants.value = await api.getParticipants(meeting.id)
  } catch (e) {
    errorCaptured.value = e
  }
})

async function handleAdd() {
  addError.value = ''
  isLoading.value = true
  try {
    const added = await api.addParticipant(meeting.id, email.value.trim())
    if (!participants.value.some((p) => p.userId === added.userId)) {
      participants.value = [...participants.value, added]
    }
    email.value = ''
  } catch (e) {
    const problem = isAxiosError(e) ? e.response?.data : null
    addError.value = problem?.errors?.email?.[0] ?? 'Could not add the participant.'
  } finally {
    isLoading.value = false
  }
}

async function handleRemove(participant: ParticipantDto) {
  try {
    await api.removeParticipant(meeting.id, participant.userId)
    participants.value = participants.value.filter((p) => p.userId !== participant.userId)
  } catch (e) {
    errorCaptured.value = e
  }
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    header="Participants"
    class="sm:w-110 w-11/12"
    modal
    dismissable-mask
  >
    <CoErrorWrapper :error="errorCaptured">
      <div class="flex flex-col gap-4">
        <form v-if="meeting.isOwner" class="flex flex-col gap-2" @submit.prevent="handleAdd">
          <label for="participant-email" class="font-semibold">Add by email</label>
          <div class="flex gap-2">
            <InputText
              id="participant-email"
              v-model="email"
              type="email"
              autocomplete="off"
              fluid
              :invalid="!!addError"
            />
            <Button type="submit" label="Add" :loading="isLoading" :disabled="!email.trim()" />
          </div>
          <Message v-if="addError" severity="error" size="small" :closable="false">
            {{ addError }}
          </Message>
        </form>

        <ul class="flex flex-col gap-2">
          <li
            v-for="participant in participants"
            :key="participant.userId"
            class="flex items-center justify-between gap-2"
          >
            <div class="flex flex-col min-w-0">
              <span class="font-semibold truncate">{{ participant.displayName }}</span>
              <span class="text-xs text-surface-500 dark:text-surface-400 truncate">
                {{ participant.email }}
              </span>
            </div>
            <SecondaryButton
              v-if="meeting.isOwner"
              label="Remove"
              size="small"
              variant="outlined"
              @click="handleRemove(participant)"
            />
          </li>
          <li v-if="!participants.length" class="text-sm text-surface-500 dark:text-surface-400">
            Only the owner is in this meeting.
          </li>
        </ul>
      </div>
    </CoErrorWrapper>
  </Dialog>
</template>
