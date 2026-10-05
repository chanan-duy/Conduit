<script setup lang="ts">
import { useAuth } from '@/composables/useAuth'
import { auth } from '@/services/authService'
import Button from '@/volt/Button.vue'
import Dialog from '@/volt/Dialog.vue'
import InputText from '@/volt/InputText.vue'
import Message from '@/volt/Message.vue'
import { isAxiosError } from 'axios'
import { computed, reactive, ref, watch } from 'vue'

type Mode = 'login' | 'register'

const { isLoginModalVisible, onLoginSuccess } = useAuth()

const mode = ref<Mode>('login')
const isLoading = ref(false)
const registrationSuccess = ref(false)

const form = reactive({
  email: '',
  displayName: '',
  password: '',
  confirmPassword: '',
})

const errors = reactive({
  email: '',
  password: '',
  confirmPassword: '',
  api: '',
})

const isFormInvalid = computed(() => {
  if (mode.value === 'login') {
    return !form.email || !form.password
  }
  return !form.email || !form.password || form.password !== form.confirmPassword
})

watch(mode, () => {
  Object.assign(form, { email: '', displayName: '', password: '', confirmPassword: '' })
  Object.assign(errors, { email: '', password: '', confirmPassword: '', api: '' })
  isLoading.value = false
})

const handleLogin = async () => {
  try {
    await auth.login({ email: form.email, password: form.password })
    onLoginSuccess()
  } catch (error) {
    console.log(error)
    if (isAxiosError(error)) {
      if (error.response?.status === 401) {
        errors.api = 'Invalid email or password.'
      } else {
        errors.api = `Error during login:\n${error.response?.data ? JSON.stringify(error.response.data, undefined, 2) : error}`
      }
    } else {
      errors.api = `Error during login:\n${error}`
    }
  }
}

const handleRegister = async () => {
  if (form.password !== form.confirmPassword) {
    errors.confirmPassword = 'Passwords do not match.'
    return
  }

  try {
    await auth.register({
      email: form.email,
      password: form.password,
      displayName: form.displayName.trim() || undefined,
    })
    mode.value = 'login'
    registrationSuccess.value = true
  } catch (error) {
    console.log(error)
    if (isAxiosError(error)) {
      const validationErrors = error.response?.data?.errors as Record<string, string[]> | undefined
      if (error.response?.status === 400 && validationErrors) {
        errors.api = Object.values(validationErrors).flat().join('\n')
      } else {
        errors.api = `Error during registration:\n${error.response?.data ? JSON.stringify(error.response.data, undefined, 2) : error}`
      }
    } else {
      errors.api = `Error during registration:\n${error}`
    }
  }
}

const handleSubmit = async () => {
  isLoading.value = true
  errors.api = ''
  errors.confirmPassword = ''

  if (mode.value === 'login') {
    await handleLogin()
  } else {
    await handleRegister()
  }

  isLoading.value = false
}
</script>

<template>
  <Dialog
    v-model:visible="isLoginModalVisible"
    modal
    :show-header="false"
    :draggable="false"
    :closable="false"
    class="mx-4 w-full max-w-xs sm:max-w-sm"
  >
    <div class="mb-6 text-center mt-6">
      <h2 class="text-2xl font-bold tracking-tight text-surface-900 dark:text-surface-0">
        {{ mode === 'login' ? 'Welcome Back' : 'Create an Account' }}
      </h2>
      <p class="text-surface-500 dark:text-surface-400 mt-2">
        {{ mode === 'login' ? 'Login to continue.' : 'Enter your details to get started.' }}
      </p>
    </div>

    <form class="flex flex-col gap-4 px-1 md:px-2" @submit.prevent="handleSubmit">
      <Message v-if="registrationSuccess" severity="success" :closable="false">
        Registration successful! Please log in.
      </Message>

      <div class="flex flex-col gap-2">
        <label for="email" class="font-semibold">Email</label>
        <InputText
          id="email"
          v-model="form.email"
          type="email"
          required
          autocomplete="email"
          placeholder=""
          :invalid="!!errors.email"
        />
        <small v-if="errors.email" class="text-red-500">{{ errors.email }}</small>
      </div>

      <div v-if="mode === 'register'" class="flex flex-col gap-2">
        <label for="display-name" class="font-semibold">Name in chat (optional)</label>
        <InputText
          id="display-name"
          v-model="form.displayName"
          maxlength="254"
          autocomplete="name"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label for="password" class="font-semibold">Password</label>
        <InputText
          id="password"
          v-model="form.password"
          type="password"
          required
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          placeholder=""
          :invalid="!!errors.password"
        />
        <small v-if="errors.password" class="text-red-500">{{ errors.password }}</small>
      </div>

      <div v-if="mode === 'register'" class="flex flex-col gap-2">
        <label for="confirm-password" class="font-semibold">Confirm Password</label>
        <InputText
          id="confirm-password"
          v-model="form.confirmPassword"
          type="password"
          required
          autocomplete="new-password"
          placeholder=""
          :invalid="!!errors.confirmPassword"
        />
        <small v-if="errors.confirmPassword" class="text-red-500">
          {{ errors.confirmPassword }}
        </small>
      </div>

      <Message
        v-if="errors.api"
        severity="error"
        size="small"
        :closable="false"
        class="break-all text-pretty whitespace-pre-wrap"
      >
        {{ errors.api }}
      </Message>

      <div class="flex flex-col items-center gap-4 mt-4">
        <Button
          type="submit"
          fluid
          :label="mode === 'login' ? 'Login' : 'Create Account'"
          :loading="isLoading"
          :disabled="isLoading || isFormInvalid"
        />
        <Button
          :label="mode === 'login' ? 'Don\'t have an account?' : 'Already have an account?'"
          :text="true"
          @click="mode = mode === 'login' ? 'register' : 'login'"
        />
      </div>
    </form>
  </Dialog>
</template>
