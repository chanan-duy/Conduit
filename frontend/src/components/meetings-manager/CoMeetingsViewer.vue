<script setup lang="ts">
import type { MeetingInfoResponseDto } from '@/entities/meeting'
import { computed } from 'vue'
import CoMeetingCard from './CoMeetingCard.vue'

const props = defineProps<{
  meetings: ReadonlyMap<string, MeetingInfoResponseDto>
}>()

const groupedMeetings = computed(() => {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  const sorted = [...props.meetings.values()].sort(
    (a, b) => a.startDate.getTime() - b.startDate.getTime(),
  )

  const upcoming = sorted.filter((m) => m.endDate.getTime() >= now.getTime())
  const past = sorted
    .filter((m) => m.endDate.getTime() < now.getTime())
    .sort((a, b) => b.endDate.getTime() - a.endDate.getTime())

  const groups = new Map<string, MeetingInfoResponseDto[]>()

  upcoming.forEach((meeting) => {
    const meetingDate = new Date(
      meeting.startDate.getFullYear(),
      meeting.startDate.getMonth(),
      meeting.startDate.getDate(),
    )
    let dayKey: string

    if (meetingDate.getTime() === today.getTime()) {
      dayKey = 'Today'
    } else if (meetingDate.getTime() === tomorrow.getTime()) {
      dayKey = 'Tomorrow'
    } else {
      dayKey = meeting.startDate.toLocaleDateString(undefined, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      })
    }

    if (!groups.has(dayKey)) {
      groups.set(dayKey, [])
    }
    groups.get(dayKey)!.push(meeting)
  })

  if (past.length > 0) {
    groups.set('Past', past)
  }

  return Array.from(groups.entries()).map(([day, meetings]) => ({ day, meetings }))
})
</script>
<template>
  <div v-if="meetings.size" class="flex flex-col gap-6">
    <div v-for="group in groupedMeetings" :key="group.day" class="flex flex-col gap-3">
      <h2 class="font-semibold text-lg">
        {{ group.day }}
      </h2>
      <div class="grid grid-cols-[repeat(auto-fill,minmax(clamp(200px,15vw,260px),1fr))] gap-4">
        <template v-for="meeting in group.meetings" :key="meeting.id">
          <CoMeetingCard :meeting="meeting" :day="group.day" />
        </template>
      </div>
    </div>
  </div>
  <div
    v-else
    class="text-center py-10 border-2 border-dashed rounded-lg border-surface-200 dark:border-surface-700 container mx-auto"
  >
    <p class="text-surface-500 dark:text-surface-400 font-semibold">No meetings scheduled</p>
    <p class="text-sm text-surface-400 dark:text-surface-500 mt-1">
      Click "Create Meeting" to get started.
    </p>
  </div>
</template>
