import { shallowRef } from 'vue'

const isLoginModalVisible = shallowRef<boolean>(false)
let resolveAuthPromise: (isSuccess: boolean) => void

const showLoginModal = (): Promise<boolean> => {
  isLoginModalVisible.value = true
  return new Promise((resolve) => {
    resolveAuthPromise = resolve
  })
}

const onLoginSuccess = () => {
  isLoginModalVisible.value = false
  resolveAuthPromise?.(true)
}

const onLoginCancel = () => {
  isLoginModalVisible.value = false
  resolveAuthPromise?.(false)
}

export function useAuth() {
  return {
    isLoginModalVisible,
    showLoginModal,
    onLoginSuccess,
    onLoginCancel,
  }
}
