import type { Directive } from 'vue'

export const vAutoScroll: Directive<HTMLElement> = {
  mounted: (el) => {
    const sentinel = document.createElement('div')
    sentinel.style.height = '1px'
    el.appendChild(sentinel)

    el.dataset.isScrolledToBottom = 'true'

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries
        if (entry && entry.isIntersecting) {
          el.dataset.isScrolledToBottom = 'true'
        } else {
          el.dataset.isScrolledToBottom = 'false'
        }
      },
      {
        root: el,
        threshold: 1.0,
      },
    )

    observer.observe(sentinel)

    el.vAutoScrollObserver = observer

    el.scrollTo({ top: el.scrollHeight, behavior: 'instant' })
  },

  updated: (el) => {
    if (el.dataset.isScrolledToBottom === 'true') {
      el.scrollTo({ top: el.scrollHeight, behavior: 'instant' })
    }
  },

  beforeUnmount: (el) => {
    if (el.vAutoScrollObserver) {
      el.vAutoScrollObserver.disconnect()
      delete el.vAutoScrollObserver
    }
  },
}

declare global {
  interface HTMLElement {
    vAutoScrollObserver?: IntersectionObserver
  }
}
