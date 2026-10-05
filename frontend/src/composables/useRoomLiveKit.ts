import {
  ConnectionState,
  isBrowserSupported,
  LocalParticipant,
  RemoteParticipant,
  type Participant,
  type Room,
} from 'livekit-client'
import { nextTick, readonly, shallowRef, triggerRef, watch } from 'vue'
import { useMediaDevices } from './useMediaDevices'

const { currentAudioDeviceId, currentVideoDeviceId } = useMediaDevices()

export type JoinDevices = { microphone: boolean; camera: boolean }

const room = shallowRef<Room | null>(null)
const isRecordingBot = shallowRef<boolean>(false)

const remoteParticipants = shallowRef<Map<string, RemoteParticipant>>(new Map())
const localParticipant = shallowRef<LocalParticipant | null>(null)
const focusedParticipant = shallowRef<Participant | null>(null)

const isActiveMic = shallowRef<boolean>(false)
const isActiveCam = shallowRef<boolean>(false)
const isActiveScreenSharing = shallowRef<boolean>(false)

const isBeingRecordered = shallowRef<boolean>(false)
const connectionState = shallowRef<ConnectionState>(ConnectionState.Disconnected)

const initMeeting = (_room: Room, _isRecordingBot: boolean = false) => {
  if (!isBrowserSupported()) {
    throw new Error('Browser is not supported')
  }

  if (room.value) {
    cleanupMeeting()
  }

  room.value = _room
  isRecordingBot.value = _isRecordingBot
}

const cleanupMeeting = () => {
  if (!room.value) {
    return
  }

  room.value.disconnect()
  room.value = null
}

const subsribeToUpdates = () => {
  if (!room.value) {
    return
  }

  room.value
    .on('participantConnected', (participant) => {
      console.log(`Participant ${participant.identity} connected`)
      recalculateState()
    })
    .on('participantDisconnected', (participant) => {
      console.log(`Participant ${participant.identity} disconnected`)
      if (
        (focusedParticipant.value && focusedParticipant.value.sid === participant.sid) ||
        remoteParticipants.value.size <= 0
      ) {
        unfocusParticipant()
      }
      recalculateState()
    })
    .on('connectionStateChanged', (stated) => {
      console.log(`Connection state changed ${stated}`)
      recalculateState()
    })
    .on('moved', (name) => {
      console.log(`Moved ${name}`)
      recalculateState()
    })
    .on('activeSpeakersChanged', (speakers) => {
      console.log('activeSpeakersChanged', speakers)
      recalculateState()
    })
    .on('activeDeviceChanged', (kind, deviceId) => {
      console.log('activeDeviceChanged', kind, deviceId)
      recalculateState()
    })
    .on('roomMetadataChanged', (metadata) => {
      console.log('roomMetadataChanged', metadata)
      recalculateState()
    })
    .on('mediaDevicesChanged', () => {
      console.log('mediaDevicesChanged')
      recalculateState()
    })
    .on('mediaDevicesError', (error, kind) => {
      console.log('mediaDevicesError', error, kind)
      recalculateState()
    })
    .on('recordingStatusChanged', (recodring) => {
      console.log('recordingStatusChanged', recodring)
      recalculateState()
    })
}

const connect = async (liveKitUrl: string, meetingToken: string) => {
  if (!room.value) {
    return
  }

  await room.value.connect(liveKitUrl, meetingToken)
  await recalculateState()
}

const recalculateState = async () => {
  await nextTick()

  if (!room.value) {
    return
  }

  remoteParticipants.value = room.value.remoteParticipants
  triggerRef(remoteParticipants)
  localParticipant.value = room.value.localParticipant
  triggerRef(localParticipant)

  isActiveMic.value = room.value.localParticipant.isMicrophoneEnabled

  isActiveCam.value = room.value.localParticipant.isCameraEnabled

  isActiveScreenSharing.value = room.value.localParticipant.isScreenShareEnabled

  isBeingRecordered.value = room.value.isRecording
  connectionState.value = room.value.state
}

const handleToggleMicrophone = async () => {
  if (!room.value) {
    return
  }

  await room.value.localParticipant.setMicrophoneEnabled(!isActiveMic.value, {
    deviceId: currentAudioDeviceId.value,
  })
  isActiveMic.value = room.value.localParticipant.isMicrophoneEnabled
}

const handleToggleCamera = async () => {
  if (!room.value) {
    return
  }

  await room.value.localParticipant.setCameraEnabled(!isActiveCam.value, {
    deviceId: currentVideoDeviceId.value,
  })
  isActiveCam.value = room.value.localParticipant.isCameraEnabled
}

const handleToggleScreenSharing = async () => {
  if (!room.value) {
    return
  }

  await room.value.localParticipant.setScreenShareEnabled(!isActiveScreenSharing.value, {
    audio: true,
    systemAudio: 'include',
  })
  isActiveScreenSharing.value = room.value.localParticipant.isScreenShareEnabled
}

const toggleFocusParticipant = (participant: Participant) => {
  if (focusedParticipant.value?.sid === participant.sid) {
    unfocusParticipant()
  } else {
    focusedParticipant.value = participant
  }
}

const unfocusParticipant = () => {
  focusedParticipant.value = null
}

watch(currentAudioDeviceId, async (newVal) => {
  if (newVal && room.value) {
    await room.value.switchActiveDevice('audioinput', newVal)
  }
})

watch(currentVideoDeviceId, async (newVal) => {
  if (newVal && room.value) {
    await room.value.switchActiveDevice('videoinput', newVal)
  }
})

export function useRoomLivekit() {
  return {
    initMeeting,
    cleanupMeeting,
    subsribeToUpdates,
    connect,
    handleToggleMicrophone,
    handleToggleCamera,
    handleToggleScreenSharing,
    toggleFocusParticipant,
    remoteParticipants: remoteParticipants,
    localParticipant: localParticipant,
    focusedParticipant: focusedParticipant,
    isActiveMic: readonly(isActiveMic),
    isActiveCam: readonly(isActiveCam),
    isActiveScreenSharing: readonly(isActiveScreenSharing),
    isBeingRecordered: readonly(isBeingRecordered),
    connectionState: readonly(connectionState),
    isRecordingBot: readonly(isRecordingBot),
  }
}
