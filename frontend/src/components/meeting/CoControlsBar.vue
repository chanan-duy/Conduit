<script setup lang="ts">
import { useRoomLivekit } from '@/composables/useRoomLiveKit'
import Button from '@/volt/Button.vue'
import { ConnectionState } from 'livekit-client'
import {
  CameraIcon,
  CameraOffIcon,
  CircleIcon,
  MicIcon,
  MicOffIcon,
  ScreenShareIcon,
  ScreenShareOffIcon,
} from 'lucide-vue-next'
import { computed } from 'vue'

const {
  handleToggleCamera,
  handleToggleMicrophone,
  handleToggleScreenSharing,
  isActiveCam,
  isActiveMic,
  isActiveScreenSharing,
  isBeingRecordered,
  connectionState,
} = useRoomLivekit()

const isConnected = computed(() => connectionState.value === ConnectionState.Connected)
</script>

<template>
  <Button @click="() => handleToggleMicrophone()" variant="outlined" :disabled="!isConnected">
    <template v-slot:icon>
      <component
        :is="isActiveMic ? MicIcon : MicOffIcon"
        :size="22"
        :class="{ 'text-surface-300': isActiveMic }"
      />
    </template>
  </Button>
  <Button @click="() => handleToggleCamera()" variant="outlined" :disabled="!isConnected">
    <template v-slot:icon>
      <component
        :is="isActiveCam ? CameraIcon : CameraOffIcon"
        :size="22"
        :class="{ 'text-surface-300': isActiveCam }"
      />
    </template>
  </Button>
  <Button @click="() => handleToggleScreenSharing()" variant="outlined" :disabled="!isConnected">
    <template v-slot:icon>
      <component
        :is="isActiveScreenSharing ? ScreenShareIcon : ScreenShareOffIcon"
        :size="22"
        :class="{ 'text-surface-300': isActiveScreenSharing }"
      />
    </template>
  </Button>
  <span
    class="absolute right-4"
    :title="isBeingRecordered ? 'Meeting is being recorded' : 'Not Recording'"
  >
    <CircleIcon
      :size="14"
      class="opacity-50"
      :class="isBeingRecordered ? 'text-red-400 fill-red-400' : 'text-surface-500'"
    />
  </span>
</template>
