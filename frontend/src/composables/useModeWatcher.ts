import { shallowRef } from 'vue'

const localStorageKey: string = '__theme'

const isDark = shallowRef<boolean>(localStorage.getItem(localStorageKey) !== 'light')

function toggleTheme() {
  isDark.value = !isDark.value

  if (isDark.value) {
    document.documentElement.classList.add('dark')
    localStorage.setItem(localStorageKey, 'dark')
  } else {
    document.documentElement.classList.remove('dark')
    localStorage.setItem(localStorageKey, 'light')
  }
}

export function useModeWatcher() {
  return {
    isDark,
    toggleTheme,
  }
}
