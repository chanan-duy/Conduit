import { createAudioAnalyser, type LocalAudioTrack, type RemoteAudioTrack } from 'livekit-client'
import { computed, shallowRef } from 'vue'

export function useAudioVisualizer() {
  const volume = shallowRef<number>(0)

  let animationFrameId: number | null = null
  let audioVisualizer: ReturnType<typeof createAudioAnalyser> | null = null

  const visualizerStyle = computed(() => {
    const hue = Math.max(0, 120 - volume.value * 1.2)
    return {
      'background-color': `hsl(${hue}, 100%, 50%)`,
      width: `${volume.value}%`,
    }
  })

  const setupVisualizer = (track: LocalAudioTrack | RemoteAudioTrack) => {
    audioVisualizer = createAudioAnalyser(track)

    const draw = () => {
      animationFrameId = requestAnimationFrame(draw)
      if (audioVisualizer) {
        volume.value = audioVisualizer.calculateVolume() * 100
      }
    }
    draw()
  }

  const teardownVisualizer = async () => {
    if (animationFrameId) {
      cancelAnimationFrame(animationFrameId)
      animationFrameId = null
    }
    if (audioVisualizer) {
      await audioVisualizer.cleanup()
      audioVisualizer = null
    }
    volume.value = 0
  }

  return {
    setupVisualizer,
    teardownVisualizer,
    volume,
    visualizerStyle,
  }
}
