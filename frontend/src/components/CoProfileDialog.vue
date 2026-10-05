<script setup lang="ts">
import CoErrorWrapper from '@/components/CoErrorWrapper.vue'
import type { ProfileDto } from '@/entities/profile'
import { api } from '@/services/apiService'
import { auth } from '@/services/authService'
import Button from '@/volt/Button.vue'
import Dialog from '@/volt/Dialog.vue'
import InputText from '@/volt/InputText.vue'
import Message from '@/volt/Message.vue'
import SecondaryButton from '@/volt/SecondaryButton.vue'
import { shallowRef, watch } from 'vue'

const visible = defineModel<boolean>('visible', { required: true })

const profile = shallowRef<ProfileDto | null>(null)
const displayName = shallowRef('')
const errorCaptured = shallowRef<unknown>(null)
const isSaving = shallowRef(false)
const isSaved = shallowRef(false)

watch(visible, async (isVisible) => {
  if (!isVisible) return

  errorCaptured.value = null
  isSaved.value = false
  try {
    profile.value = await api.getProfile()
    displayName.value = profile.value.displayName
  } catch (e) {
    errorCaptured.value = e
  }
})

async function handleSave() {
  isSaving.value = true
  isSaved.value = false
  try {
    profile.value = await api.updateProfile(displayName.value.trim())
    displayName.value = profile.value.displayName
    await auth.renewToken()
    isSaved.value = true
  } catch (e) {
    errorCaptured.value = e
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    header="Profile"
    class="sm:w-100 w-11/12"
    modal
    dismissable-mask
  >
    <CoErrorWrapper :error="errorCaptured">
      <form v-if="profile" class="flex flex-col gap-4" @submit.prevent="handleSave">
        <div class="flex flex-col gap-2">
          <span class="font-semibold">Email</span>
          <span class="text-surface-500 dark:text-surface-400">{{ profile.email }}</span>
        </div>
        <div class="flex flex-col gap-2">
          <label for="display-name" class="font-semibold">Name in chat</label>
          <InputText id="display-name" v-model="displayName" maxlength="254" autocomplete="name" />
        </div>
        <Message v-if="isSaved" severity="success" size="small" :closable="false">Saved.</Message>
      </form>
    </CoErrorWrapper>
    <template #footer>
      <SecondaryButton label="Close" @click="visible = false" />
      <Button
        label="Save"
        :loading="isSaving"
        :disabled="!profile || !displayName.trim() || displayName.trim() === profile.displayName"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
