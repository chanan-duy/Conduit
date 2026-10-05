import './assets/main.css'

import { createPinia } from 'pinia'
import { ConfirmationService } from 'primevue'
import PrimeVue from 'primevue/config'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.use(PrimeVue, {
  unstyled: true,
})

app.use(ConfirmationService)

app.mount('#app')
