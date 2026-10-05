import { onMounted, onUnmounted, ref, watch, type Ref } from 'vue'

interface FocusLayout {
  focused: {
    width: number
    height: number
  }
  filmstrip: {
    tileWidth: number
    tileHeight: number
  }
}

interface FocusLayoutOptions {
  filmstripHeight?: Ref<number>
  gap?: Ref<number>
}

export function useFocusLayout(
  container: Ref<HTMLElement | null>,
  options: FocusLayoutOptions = {},
) {
  const aspectRatio = 16 / 9
  const { filmstripHeight = ref(160), gap = ref(16) } = options

  const layout = ref<FocusLayout>({
    focused: { width: 0, height: 0 },
    filmstrip: { tileWidth: 0, tileHeight: 0 },
  })

  const calculateLayout = () => {
    if (!container.value) {
      return
    }

    const containerWidth = container.value.clientWidth
    const containerHeight = container.value.clientHeight

    const mainAreaHeight = containerHeight - filmstripHeight.value - gap.value

    let focusedWidth = mainAreaHeight * aspectRatio
    let focusedHeight = mainAreaHeight

    if (focusedWidth > containerWidth) {
      focusedWidth = containerWidth
      focusedHeight = containerWidth / aspectRatio
    }

    const tileHeight = filmstripHeight.value - gap.value * 2
    const tileWidth = tileHeight * aspectRatio

    layout.value = {
      focused: {
        width: Math.round(focusedWidth),
        height: Math.round(focusedHeight),
      },
      filmstrip: {
        tileWidth: Math.round(tileWidth),
        tileHeight: Math.round(tileHeight),
      },
    }
  }

  onMounted(() => {
    if (!container.value) return

    const resizeObserver = new ResizeObserver(calculateLayout)
    resizeObserver.observe(container.value)

    watch([filmstripHeight, gap], calculateLayout, { immediate: true })

    onUnmounted(() => {
      resizeObserver.disconnect()
    })
  })

  return { focusLayout: layout }
}
