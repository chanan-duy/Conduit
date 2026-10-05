<script setup lang="ts">
import Button from '@/volt/Button.vue'
</script>

<template>
  <div
    class="container mx-auto sm:w-130 w-11/12 flex flex-col items-center justify-center gap-6 h-full"
  >
    <h1 class="text-3xl font-semibold tracking-tight">Conduit</h1>
    <RouterLink to="/meetings" custom v-slot="{ href, navigate }">
      <Button label="Open meetings" as="a" :href="href" @click="navigate" />
    </RouterLink>
  </div>
</template>
