<script setup lang="ts">
import { onErrorCaptured, shallowRef } from 'vue'
import CoErrorWrapper from './CoErrorWrapper.vue'

const errorCaptured = shallowRef<unknown>(null)

onErrorCaptured((error) => {
  errorCaptured.value = error
  return false
})
</script>

<template>
  <CoErrorWrapper :error="errorCaptured">
    <slot></slot>
  </CoErrorWrapper>
</template>
