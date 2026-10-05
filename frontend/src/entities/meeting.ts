export type MeetingInfoResponseDto = {
  id: string
  title: string
  startDate: Date
  endDate: Date
  isOwner: boolean
}

export type CreateMeetingRequestDto = {
  title: string
  startDate: Date
  endDate: Date
}

export type CreateMeetingResponseDto = {
  id: string
}

export type PatchMeetingRequestDto = {
  title: string
}

export type MeetingTokenResponseDto = {
  token: string
  url: string
}

export type ParticipantDto = {
  userId: string
  email: string
  displayName: string
}

export type RecordingDto = {
  id: string
  status: 'Starting' | 'Recording' | 'Completed' | 'Failed'
  createdAt: string
  updatedAt: string
  error: string | null
}
