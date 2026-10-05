import { onMounted, onUnmounted, ref, watch, type Ref } from 'vue'

interface Layout {
  cols: number
  rows: number
  width: number
  height: number
  gap: number
}

export function useGridLayout(
  container: Ref<HTMLElement | null>,
  participantCount: Ref<number>,
  options: { gap?: Ref<number>; minWidth?: Ref<number>; minHeight?: Ref<number> } = {},
) {
  const aspectRatio = 16 / 9
  const layout = ref<Layout>({ cols: 1, rows: 1, width: 0, height: 0, gap: 0 })
  const { gap = ref(8), minWidth = ref(160), minHeight = ref(160 / aspectRatio) } = options

  const calculateLayout = () => {
    if (!container.value || participantCount.value === 0) {
      layout.value = { cols: 1, rows: 1, width: 0, height: 0, gap: 0 }
      return
    }

    const roundDown = (num: number) => Math.floor(num * 2) / 2

    const count = participantCount.value
    const containerWidth = container.value.clientWidth
    const containerHeight = container.value.clientHeight

    const minEffectiveWidth = Math.max(minWidth.value, minHeight.value * aspectRatio)
    let finalCols = 1

    for (let cols = count; cols >= 1; cols--) {
      const totalHGap = (cols - 1) * gap.value
      const tileWidth = (containerWidth - totalHGap) / cols
      if (tileWidth >= minEffectiveWidth) {
        finalCols = cols
        break
      }
    }

    const finalTotalHGap = (finalCols - 1) * gap.value
    let finalWidth = (containerWidth - finalTotalHGap) / finalCols
    let finalHeight = finalWidth / aspectRatio
    const finalRows = Math.ceil(count / finalCols)

    if (finalRows === 1 && finalHeight > containerHeight) {
      finalHeight = containerHeight
      finalWidth = finalHeight * aspectRatio
    }

    layout.value = {
      cols: finalCols,
      rows: finalRows,
      width: roundDown(finalWidth),
      height: roundDown(finalHeight),
      gap: gap.value,
    }
  }

  onMounted(() => {
    if (!container.value) return

    const resizeObserver = new ResizeObserver(calculateLayout)
    resizeObserver.observe(container.value)
    watch([participantCount, gap, minWidth, minHeight], calculateLayout, { immediate: true })

    onUnmounted(() => {
      resizeObserver.disconnect()
    })
  })

  return { layout }
}
