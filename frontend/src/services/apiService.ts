import type {
  CreateMeetingRequestDto,
  CreateMeetingResponseDto,
  MeetingInfoResponseDto,
  MeetingTokenResponseDto,
  ParticipantDto,
  PatchMeetingRequestDto,
  RecordingDto,
} from '@/entities/meeting'
import type { ProfileDto } from '@/entities/profile'
import axios, { type AxiosInstance } from 'axios'
import { auth, type AuthService } from './authService'

const ApiBase: string = '/api/v1/'
const ApiBaseMeeting: string = 'meetings/'
const ApiBaseRecording: string = 'recordings/'

const ApiEndpointsConstructor = {
  meeting: {
    getMeetingInfo: function (meetingId: string): string {
      return ApiBaseMeeting + meetingId
    },
    getMeetingsInfosAvailable: function (): string {
      return ApiBaseMeeting
    },
    createMeeting: function (): string {
      return ApiBaseMeeting
    },
    deleteMeeting: function (meetingId: string): string {
      return ApiBaseMeeting + meetingId
    },
    patchMeeting: function (meetingId: string): string {
      return ApiBaseMeeting + meetingId
    },
    getMeetingToken: function (meetingId: string): string {
      return ApiBaseMeeting + meetingId + '/token'
    },
    participants: function (meetingId: string): string {
      return ApiBaseMeeting + meetingId + '/participants'
    },
    participant: function (meetingId: string, userId: string): string {
      return ApiBaseMeeting + meetingId + '/participants/' + userId
    },
  },
  profile: function (): string {
    return 'auth/profile'
  },
  recording: {
    getRecordings: function (): string {
      return ApiBaseRecording
    },
    getRecordingFile: function (recordingId: string): string {
      return ApiBaseRecording + recordingId + '/file'
    },
  },
}

function toMeetingDates(meeting: MeetingInfoResponseDto): MeetingInfoResponseDto {
  if (typeof meeting.startDate == 'string') meeting.startDate = new Date(meeting.startDate)
  if (typeof meeting.endDate == 'string') meeting.endDate = new Date(meeting.endDate)
  return meeting
}

export class ApiService {
  private ax: AxiosInstance = axios.create({
    baseURL: ApiBase,
  })
  private auth: AuthService

  constructor(authService: AuthService) {
    this.auth = authService

    auth.addAxiosInterceptor(this.ax)
  }

  public async getMeetingInfo(meetingId: string): Promise<MeetingInfoResponseDto> {
    const { data } = await this.ax.get<MeetingInfoResponseDto>(
      ApiEndpointsConstructor.meeting.getMeetingInfo(meetingId),
    )

    return toMeetingDates(data)
  }

  public async getMeetingsInfosAvailable(): Promise<MeetingInfoResponseDto[]> {
    const { data } = await this.ax.get<MeetingInfoResponseDto[]>(
      ApiEndpointsConstructor.meeting.getMeetingsInfosAvailable(),
    )

    return data.map(toMeetingDates)
  }

  public async createMeeting(
    requestDto: CreateMeetingRequestDto,
  ): Promise<CreateMeetingResponseDto> {
    const { data } = await this.ax.post<CreateMeetingResponseDto>(
      ApiEndpointsConstructor.meeting.createMeeting(),
      requestDto,
    )

    return data
  }

  public async deleteMeeting(meetingId: string): Promise<void> {
    await this.ax.delete(ApiEndpointsConstructor.meeting.deleteMeeting(meetingId))
  }

  public async patchMeeting(
    meetingId: string,
    requestDto: PatchMeetingRequestDto,
  ): Promise<MeetingInfoResponseDto> {
    const { data } = await this.ax.patch<MeetingInfoResponseDto>(
      ApiEndpointsConstructor.meeting.patchMeeting(meetingId),
      requestDto,
    )

    return toMeetingDates(data)
  }

  public async getMeetingToken(meetingId: string): Promise<MeetingTokenResponseDto> {
    const { data } = await this.ax.get<MeetingTokenResponseDto>(
      ApiEndpointsConstructor.meeting.getMeetingToken(meetingId),
    )

    return data
  }

  public async getParticipants(meetingId: string): Promise<ParticipantDto[]> {
    const { data } = await this.ax.get<ParticipantDto[]>(
      ApiEndpointsConstructor.meeting.participants(meetingId),
    )

    return data
  }

  public async addParticipant(meetingId: string, email: string): Promise<ParticipantDto> {
    const { data } = await this.ax.post<ParticipantDto>(
      ApiEndpointsConstructor.meeting.participants(meetingId),
      { email },
    )

    return data
  }

  public async removeParticipant(meetingId: string, userId: string): Promise<void> {
    await this.ax.delete(ApiEndpointsConstructor.meeting.participant(meetingId, userId))
  }

  public async getProfile(): Promise<ProfileDto> {
    const { data } = await this.ax.get<ProfileDto>(ApiEndpointsConstructor.profile())

    return data
  }

  public async updateProfile(displayName: string): Promise<ProfileDto> {
    const { data } = await this.ax.patch<ProfileDto>(ApiEndpointsConstructor.profile(), {
      displayName,
    })

    return data
  }

  public async getRecordings(meetingId: string): Promise<RecordingDto[]> {
    const { data } = await this.ax.get<RecordingDto[]>(
      ApiEndpointsConstructor.recording.getRecordings(),
      { params: { meetingId } },
    )

    return data
  }

  public async getRecordingFile(recordingId: string): Promise<Blob> {
    const { data } = await this.ax.get<Blob>(
      ApiEndpointsConstructor.recording.getRecordingFile(recordingId),
      { responseType: 'blob' },
    )

    return data
  }
}

export const api = new ApiService(auth)
