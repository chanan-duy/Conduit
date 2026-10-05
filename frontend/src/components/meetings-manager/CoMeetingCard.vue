<script setup lang="ts">
import type { MeetingInfoResponseDto } from '@/entities/meeting'
import Button from '@/volt/Button.vue'
import { CalendarIcon, ClockIcon } from 'lucide-vue-next'
import { computed } from 'vue'
import CoMeetingCardOptions from './CoMeetingCardOptions.vue'

const { meeting, day } = defineProps<{
  meeting: MeetingInfoResponseDto
  day: string
}>()

const isEndDateIsAnotherDay = computed(() => {
  return meeting.startDate.getDate() !== meeting.endDate.getDate()
})

function padNumber(num: number) {
  return num.toString().padStart(2, '0')
}

function toNormalTime(date: Date) {
  return `${padNumber(date.getHours())}:${padNumber(date.getMinutes())}`
}

function toNormalYearMonthDay(date: Date) {
  return `${date.getFullYear()}-${padNumber(date.getMonth() + 1)}-${padNumber(date.getDate())}`
}
</script>
<template>
  <div
    class="p-4 border rounded-lg bg-surface-50 dark:bg-surface-900 border-surface-200 dark:border-surface-700 flex flex-col gap-5 justify-between transition-all duration-200"
    :class="{
      'hover:border-primary-500 dark:hover:border-primary-400 hover:ring-2 hover:ring-primary-500/50':
        day !== 'Past',
      'opacity-60': day === 'Past',
      'ring-1 ring-emerald-400 dark:ring-emerald-300':
        meeting.startDate <= new Date() && new Date() <= meeting.endDate,
    }"
  >
    <div class="flex flex-col gap-3 flex-1 justify-between">
      <h3
        class="font-semibold line-clamp-4 w-full tracking-tight text-pretty wrap-break-word"
        :title="meeting.title"
      >
        {{ meeting.title }}
      </h3>
      <div class="text-xs text-surface-500 dark:text-surface-400 flex flex-col gap-1.5">
        <div class="flex items-center gap-1.5">
          <ClockIcon :size="14" />
          <span>
            {{ toNormalTime(meeting.startDate) }}
            -
            {{ toNormalTime(meeting.endDate) }}
            {{ isEndDateIsAnotherDay ? `(${toNormalYearMonthDay(meeting.endDate)})` : '' }}
          </span>
        </div>
        <div class="flex gap-1.5">
          <CalendarIcon :size="14" />
          <span :title="meeting.startDate.toLocaleString()">
            {{
              meeting.startDate.toLocaleDateString(undefined, {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
              })
            }}
            ({{ toNormalYearMonthDay(meeting.startDate) }})
          </span>
        </div>
      </div>
    </div>
    <div class="flex flex-row gap-2 justify-between">
      <Button
        :label="day === 'Past' ? 'View' : 'Join'"
        size="small"
        link
        as="a"
        :href="$route.path + '/' + meeting.id"
        target="_blank"
        class="!px-4"
      />
      <CoMeetingCardOptions :meeting="meeting" />
    </div>
  </div>
</template>
