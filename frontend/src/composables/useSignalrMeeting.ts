import type { ChatMessage } from '@/entities/chat-message'
import type { AuthService } from '@/services/authService'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'
import { computed, shallowReactive } from 'vue'

const HubUrl: string = '/hubs/Meeting'
const HistoryPageSize: number = 50

export function useSignalrMeeting(auth: AuthService) {
  const connection = new HubConnectionBuilder()
    .withUrl(HubUrl, {
      accessTokenFactory: async () => (await auth.getVerifiedAccessToken()) || '',
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
        Math.min(1000 * 2 ** previousRetryCount, 5000),
    })
    .withHubProtocol(new MessagePackHubProtocol())
    .build()

  let meetingIdInternal: string = ''
  let lastSequence: number = 0

  const chatMessagesById = shallowReactive<Map<string, ChatMessage>>(new Map())
  const chatMessages = computed(() =>
    [...chatMessagesById.values()].sort((a, b) => a.Sequence - b.Sequence),
  )

  connection.onreconnected(() => resubscribe())

  async function resubscribe() {
    try {
      await subscribe()
      await retrieveChatMessagesHistory()
    } catch (e) {
      console.log('resubscribe failed, retrying', e)
      setTimeout(() => {
        if (connection.state === HubConnectionState.Connected) resubscribe()
      }, 3000)
    }
  }

  async function connect(meetingId: string) {
    await disconnect()
    meetingIdInternal = meetingId
    lastSequence = 0
    chatMessagesById.clear()

    connection.on('ReceiveChatMessage', addChatMessage)

    await connection.start()
  }

  async function disconnect() {
    connection.off('ReceiveChatMessage', addChatMessage)

    await connection.stop()

    meetingIdInternal = ''
  }

  async function subscribe() {
    await connection.invoke('SubscribeToMeeting', meetingIdInternal)
  }

  async function sendChatMessage(msg: string) {
    const sent = await connection.invoke<ChatMessage>('SendChatMessage', meetingIdInternal, msg)
    addChatMessage(sent)
  }

  async function retrieveChatMessagesHistory() {
    let page: ChatMessage[]
    do {
      page = await connection.invoke<ChatMessage[]>(
        'RetrieveChatMessagesHistory',
        meetingIdInternal,
        lastSequence,
      )
      page.forEach(addChatMessage)
      lastSequence = Math.max(lastSequence, ...page.map((msg) => msg.Sequence))
    } while (page.length === HistoryPageSize)
  }

  function addChatMessage(msg: ChatMessage) {
    chatMessagesById.set(msg.Id, msg)
  }

  return {
    connect,
    subscribe,
    disconnect,
    sendChatMessage,
    retrieveChatMessagesHistory,
    chatMessages,
  }
}
