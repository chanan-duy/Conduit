<script setup lang="ts">
import type { MeetingInfoResponseDto, PatchMeetingRequestDto } from '@/entities/meeting'
import Button from '@/volt/Button.vue'
import Dialog from '@/volt/Dialog.vue'
import InputText from '@/volt/InputText.vue'
import Message from '@/volt/Message.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  meeting: MeetingInfoResponseDto | null
  visible: boolean
}>()

const emit = defineEmits<{
  updateVisible: [value: boolean]
  updateMeeting: [payload: PatchMeetingRequestDto]
}>()

const localMeeting = ref<MeetingInfoResponseDto | null>(null)

const areLocalAndPropsMeetingEqual = computed(() => {
  return !!localMeeting.value && !!props.meeting && localMeeting.value.title === props.meeting.title
})

const errors = ref({
  title: '',
})

watch(
  () => props.meeting,
  (newMeeting) => {
    if (newMeeting) {
      localMeeting.value = { ...newMeeting }
      errors.value.title = ''
    }
  },
  { immediate: true },
)

function save() {
  if (!localMeeting.value) return

  errors.value.title = ''

  let isValid = true
  if (!localMeeting.value.title.trim()) {
    errors.value.title = 'Title is required.'
    isValid = false
  }
  if (localMeeting.value.title.length < 3 || localMeeting.value.title.length > 100) {
    errors.value.title = 'Title must be between 3 and 100 characters.'
    isValid = false
  }

  if (!isValid) return

  emit('updateMeeting', { title: localMeeting.value.title })
  close()
}

function close() {
  emit('updateVisible', false)
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="close"
    header="Edit Meeting"
    class="sm:w-100 w-11/12"
    modal
    dismissable-mask
  >
    <div v-if="localMeeting" class="flex flex-col gap-4">
      <div class="flex flex-col gap-2">
        <label for="title" class="font-semibold">Title</label>
        <InputText
          v-model="localMeeting.title"
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
    </div>
    <template #footer>
      <SecondaryButton label="Cancel" @click="close" />
      <Button label="Save" @click="save" :disabled="areLocalAndPropsMeetingEqual" />
    </template>
  </Dialog>
</template>
