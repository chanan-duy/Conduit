import type {
  CreateMeetingRequestDto,
  MeetingInfoResponseDto,
  PatchMeetingRequestDto,
} from '@/entities/meeting'
import { api } from '@/services/apiService'
import { defineStore } from 'pinia'
import { readonly, shallowReactive, shallowRef } from 'vue'

export const useMeetingsManagerStore = defineStore('meetings', () => {
  const meetings = shallowReactive<Map<string, MeetingInfoResponseDto>>(new Map())
  const isLoading = shallowRef<boolean>(false)
  const editingMeeting = shallowRef<MeetingInfoResponseDto | null>(null)
  const isEditorVisible = shallowRef<boolean>(false)

  async function initMeetings() {
    isLoading.value = true
    try {
      const meets = await api.getMeetingsInfosAvailable()
      meetings.clear()
      meets.forEach((m) => meetings.set(m.id, m))
    } finally {
      isLoading.value = false
    }
  }

  async function createNewMeeting(meeting: CreateMeetingRequestDto) {
    const created = await api.createMeeting(meeting)
    const newMeeting = await api.getMeetingInfo(created.id)
    meetings.set(newMeeting.id, newMeeting)
  }

  async function deleteMeeting(meetingId: string) {
    await api.deleteMeeting(meetingId)
    meetings.delete(meetingId)
  }

  async function updateMeeting(meetingId: string, payload: PatchMeetingRequestDto) {
    try {
      const updatedMeeting = await api.patchMeeting(meetingId, payload)
      meetings.set(meetingId, updatedMeeting)
    } finally {
      cancelEditing()
    }
  }

  function startEditing(meeting: MeetingInfoResponseDto) {
    editingMeeting.value = { ...meeting }
    isEditorVisible.value = true
  }

  function cancelEditing() {
    editingMeeting.value = null
    isEditorVisible.value = false
  }

  return {
    meetings: readonly(meetings),
    isLoading: readonly(isLoading),
    editingMeeting: readonly(editingMeeting),
    isEditorVisible: readonly(isEditorVisible),
    initMeetings,
    createNewMeeting,
    deleteMeeting,
    updateMeeting,
    startEditing,
    cancelEditing,
  }
})
