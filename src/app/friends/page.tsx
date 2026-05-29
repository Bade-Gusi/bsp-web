'use client'

import { motion } from 'framer-motion'
import { useState, useEffect } from 'react'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Modal } from '@/components/ui/Modal'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

interface Friend {
  id: number
  nickname: string
  status?: string
  mmr?: number
  isOnline?: boolean
}

export default function FriendsPage() {
  const [friends, setFriends] = useState<Friend[]>([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<any[]>([])
  const [searching, setSearching] = useState(false)
  const [showRequests, setShowRequests] = useState(false)
  const [requests, setRequests] = useState<any[]>([])
  const [error, setError] = useState('')
  const { user } = useAuthStore()

  const fetchFriends = async () => {
    try {
      setLoading(true)
      const data = await api.getFriends()
      if (Array.isArray(data)) {
        setFriends(data.map((f: any) => ({
          id: f.friendId || f.id || f.userId,
          nickname: f.nickname || f.friendName || f.username || '未知',
          status: f.status || (f.isOnline ? '在线' : '离线'),
          mmr: f.mmr || 0,
          isOnline: f.isOnline ?? f.status === '在线',
        })))
      }
    } catch {}
    setLoading(false)
  }

  useEffect(() => { fetchFriends() }, [])

  const handleSearch = async () => {
    if (!searchQuery.trim()) return
    setError('')
    setSearching(true)
    try {
      const data = await api.searchUsers(searchQuery.trim())
      setSearchResults(Array.isArray(data) ? data : [])
      if (Array.isArray(data) && data.length === 0) setError('未找到用户')
    } catch (err: any) { setError(err.message || '搜索失败') }
    finally { setSearching(false) }
  }

  const handleAddFriend = async (friendId: number) => {
    try {
      await api.sendFriendRequest(friendId)
      setError('好友请求已发送')
      setSearchResults([])
      setSearchQuery('')
    } catch (err: any) { setError(err.message || '发送失败') }
  }

  const handleRemoveFriend = async (friendId: number) => {
    try {
      await api.removeFriend(friendId)
      fetchFriends()
    } catch {}
  }

  const online = friends.filter(f => f.isOnline)
  const offline = friends.filter(f => !f.isOnline)

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-white">
          好友
          <span className="text-sm font-normal text-surface-400 ml-2">({friends.length})</span>
        </h2>
      </div>

      <div className="grid grid-cols-[1fr_2fr] gap-6">
        {/* Friend list */}
        <div className="space-y-4">
          <Card variant="default">
            <div className="flex items-center justify-between mb-3">
              <p className="text-xs text-surface-400">在线 {online.length}</p>
            </div>
            {loading ? (
              <div className="flex justify-center py-8">
                <div className="w-6 h-6 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
              </div>
            ) : online.length === 0 ? (
              <p className="text-xs text-surface-500 py-4 text-center">暂无在线好友</p>
            ) : (
              online.map(f => (
                <div key={f.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors cursor-pointer">
                  <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">
                    {f.nickname[0]}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-white truncate">{f.nickname}</p>
                    <p className="text-xs text-surface-400">{f.status || '在线'} · {f.mmr} MMR</p>
                  </div>
                  <span className="w-2 h-2 rounded-full bg-primary shrink-0" />
                </div>
              ))
            )}
          </Card>

          <Card variant="default">
            <p className="text-xs text-surface-400 mb-3">离线 {offline.length}</p>
            {offline.length === 0 ? (
              <p className="text-xs text-surface-500 py-4 text-center">暂无离线好友</p>
            ) : (
              offline.map(f => (
                <div key={f.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors cursor-pointer group">
                  <div className="w-9 h-9 rounded-full bg-surface-600 flex items-center justify-center text-sm text-surface-300 font-bold">
                    {f.nickname[0]}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-surface-300 truncate">{f.nickname}</p>
                    <p className="text-xs text-surface-500">离线</p>
                  </div>
                  <button onClick={() => handleRemoveFriend(f.id)}
                    className="text-xs text-surface-500 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all shrink-0">
                    删除
                  </button>
                </div>
              ))
            )}
          </Card>
        </div>

        {/* Right panel */}
        <div className="space-y-4">
          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">添加好友</h3>
            <div className="flex gap-2 mb-4">
              <input placeholder="输入用户名搜索" value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleSearch()}
                className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
              <Button onClick={handleSearch} loading={searching}>搜索</Button>
            </div>

            {error && <p className={'text-sm mb-3 ' + (error.includes('已发送') ? 'text-primary' : 'text-red-400')}>{error}</p>}

            {searchResults.length > 0 && (
              <div className="space-y-1">
                <p className="text-xs text-surface-400 mb-2">搜索结果</p>
                {searchResults.map((r: any) => (
                  <div key={r.id || r.userId} className="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-surface-800">
                    <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">
                      {(r.nickname || r.username || '?')[0]}
                    </div>
                    <span className="flex-1 text-sm text-white">{r.nickname || r.username}</span>
                    <Button size="sm" onClick={() => handleAddFriend(r.id || r.userId)}>
                      添加好友
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </Card>

          <Card variant="default">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-base font-bold text-white">好友请求</h3>
              <Badge variant="primary">0</Badge>
            </div>
            <p className="text-xs text-surface-500">暂无好友请求</p>
          </Card>
        </div>
      </div>
    </motion.div>
  )
}
