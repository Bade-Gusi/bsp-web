'use client'

import { useEffect, useRef, useState } from 'react'
import { toast } from './Toast'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

export function FriendRequestNotifier() {
  const { token } = useAuthStore()
  const lastCount = useRef(0)
  const [knownIds] = useState<Set<number>>(new Set())

  useEffect(() => {
    if (!token) return
    const check = async () => {
      try {
        const data = await api.getFriends()
        if (!Array.isArray(data)) return
        // 检测到新的好友（通过ID变化判断）
        const ids = new Set(data.map((f: any) => f.friendId || f.id))
        if (lastCount.current > 0 && ids.size > lastCount.current) {
          toast('有新的好友！', 'success')
        }
        lastCount.current = ids.size
      } catch {}
    }
    const interval = setInterval(check, 15000)
    return () => clearInterval(interval)
  }, [token])

  return null
}
