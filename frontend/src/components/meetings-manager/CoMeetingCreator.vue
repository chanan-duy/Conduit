<script setup lang="ts">
import type { CreateMeetingRequestDto } from '@/entities/meeting'
import { useMeetingsManagerStore } from '@/stores/meetings-manager'
import Button from '@/volt/Button.vue'
import DatePicker from '@/volt/DatePicker.vue'
import Dialog from '@/volt/Dialog.vue'
import InputText from '@/volt/InputText.vue'
import Message from '@/volt/Message.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { computed, ref, shallowRef, watch } from 'vue'

const { createNewMeeting } = useMeetingsManagerStore()

const visible = defineModel<boolean>('visible', { required: true })

const title = shallowRef<string>('')
const startDate = shallowRef<Date | null>(null)
const endDate = shallowRef<Date | null>(null)

const errors = ref({
  title: '',
  startDate: '',
  endDate: '',
})

const isCreateDisabled = computed(() => {
  return !title.value || !startDate.value || !endDate.value
})

function clearInputs() {
  title.value = ''
  startDate.value = null
  endDate.value = null
  errors.value.title = ''
  errors.value.startDate = ''
  errors.value.endDate = ''
}

function validateAndCreateMeeting() {
  errors.value.title = ''
  errors.value.startDate = ''
  errors.value.endDate = ''

  let isValid = true
  if (!title.value.trim()) {
    errors.value.title = 'Title is required.'
    isValid = false
  }
  if (title.value.length < 3 || title.value.length > 100) {
    errors.value.title = 'Title must be between 3 and 100 characters.'
    isValid = false
  }
  if (!startDate.value) {
    errors.value.startDate = 'Start date is required.'
    isValid = false
  }
  if (!endDate.value) {
    errors.value.endDate = 'End date is required.'
    isValid = false
  }

  if (startDate.value && endDate.value && endDate.value <= startDate.value) {
    errors.value.endDate = 'End date must be after start date.'
    isValid = false
  }

  if (!isValid) return

  sendMeeting()
}

async function sendMeeting() {
  const newMeeting: CreateMeetingRequestDto = {
    title: title.value.trim(),
    startDate: startDate.value!,
    endDate: endDate.value!,
  } satisfies CreateMeetingRequestDto

  await createNewMeeting(newMeeting)

  visible.value = false
}

watch(visible, (newValue) => {
  if (!newValue) {
    clearInputs()
  }
})
</script>

<template>
  <Dialog
    v-model:visible="visible"
    :show-header="false"
    class="sm:w-100 w-11/12"
    modal
    dismissable-mask
  >
    <div class="flex flex-col gap-4 p-2">
      <div class="mb-4 mt-4 text-center">
        <h2 class="text-2xl font-bold text-surface-900 dark:text-surface-0">
          Create a New Meeting
        </h2>
        <p class="text-surface-500 dark:text-surface-400 mt-2">
          Fill out the details below to schedule your meeting.
        </p>
      </div>

      <div class="flex flex-col gap-2">
        <label for="title" class="font-semibold">Title</label>
        <InputText
          v-model="title"
          id="title"
          minlength="3"
          maxlength="100"
          autocomplete="off"
          placeholder="e.g., Project Meeting"
          :invalid="!!errors.title"
        />
        <Message v-if="errors.title" severity="error" size="small" :closable="false">
          {{ errors.title }}
        </Message>
      </div>
      <div class="flex flex-col gap-2">
        <label for="start-date">Start Date</label>
        <DatePicker
          update-model-type="date"
          v-model="startDate"
          input-id="start-date"
          show-time
          hour-format="24"
          show-icon
          icon-display="input"
          :min-date="new Date()"
          :invalid="!!errors.startDate"
        />
        <Message v-if="errors.startDate" severity="error" size="small" :closable="false">
          {{ errors.startDate }}
        </Message>
      </div>
      <div class="flex flex-col gap-2">
        <label for="end-date">End Date</label>
        <DatePicker
          update-model-type="date"
          v-model="endDate"
          input-id="end-date"
          show-time
          hour-format="24"
          show-icon
          icon-display="input"
          :min-date="startDate || new Date()"
          :invalid="!!errors.endDate"
        />
        <Message v-if="errors.endDate" severity="error" size="small" :closable="false">
          {{ errors.endDate }}
        </Message>
      </div>
      <div class="flex justify-end gap-2 mt-4">
        <SecondaryButton type="button" label="Cancel" @click="visible = false" />
        <Button
          type="button"
          label="Create"
          :disabled="isCreateDisabled"
          @click="validateAndCreateMeeting"
        />
      </div>
    </div>
  </Dialog>
</template>
