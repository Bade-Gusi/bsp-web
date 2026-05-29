'use client'

import { useEffect, useState, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/authStore'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

export function useSignalR(hubUrl: string) {
  const [connected, setConnected] = useState(false)
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const token = useAuthStore((s) => s.token)

  useEffect(() => {
    if (!token) return

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API}${hubUrl}`, { accessTokenFactory: () => token! })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build()

    conn.onclose(() => setConnected(false))
    conn.onreconnecting(() => setConnected(false))
    conn.onreconnected(() => setConnected(true))

    conn.start().then(() => setConnected(true)).catch(console.error)
    connectionRef.current = conn

    return () => { conn.stop(); connectionRef.current = null }
  }, [token, hubUrl])

  return { connected, connection: connectionRef }
}

// Match Hub
export function useMatchHub() {
  const { connected, connection } = useSignalR('/hubs/match')
  const joinQueue = (gameId: number, mode: number) => connection.current?.invoke('JoinQueue', gameId, mode)
  const leaveQueue = () => connection.current?.invoke('LeaveQueue')
  const acceptMatch = (matchId: number) => connection.current?.invoke('AcceptMatch', matchId)
  const rejectMatch = (matchId: number, gameId: number, mode: number) => connection.current?.invoke('RejectMatch', matchId, gameId, mode)
  return { connected, joinQueue, leaveQueue, acceptMatch, rejectMatch, connection }
}

// Chat Hub
export function useChatHub() {
  const { connected, connection } = useSignalR('/hubs/chat')
  const sendPrivate = (toUserId: number, content: string) => connection.current?.invoke('SendPrivateMessage', toUserId, content)
  const sendRoom = (roomCode: string, content: string) => connection.current?.invoke('SendRoomMessage', roomCode, content)
  const joinRoom = (roomCode: string) => connection.current?.invoke('JoinRoom', roomCode)
  const leaveRoom = (roomCode: string) => connection.current?.invoke('LeaveRoom', roomCode)
  return { connected, sendPrivate, sendRoom, joinRoom, leaveRoom, connection }
}

// Call Hub (WebRTC)
export function useCallHub() {
  const { connected, connection } = useSignalR('/callhub')
  const joinChannel = (channelId: string) => connection.current?.invoke('JoinChannel', channelId)
  const leaveChannel = (channelId: string) => connection.current?.invoke('LeaveChannel', channelId)
  const sendSignal = (channelId: string, signalType: string, data: string, targetId: string) =>
    connection.current?.invoke('SendSignal', channelId, signalType, data, targetId)
  const startScreenShare = (channelId: string) => connection.current?.invoke('StartScreenShare', channelId)
  const stopScreenShare = (channelId: string) => connection.current?.invoke('StopScreenShare', channelId)
  return { connected, joinChannel, leaveChannel, sendSignal, startScreenShare, stopScreenShare, connection }
}
