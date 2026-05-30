'use client'

import { useState, useEffect, useRef } from 'react'
import { motion } from 'framer-motion'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { toast } from '@/components/ui/Toast'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

export default function FriendsPage() {
  const [tab, setTab] = useState<'list' | 'requests' | 'add'>('list')
  const [friends, setFriends] = useState<any[]>([])
  const [requests, setRequests] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<any[]>([])
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState('')
  const { user, token } = useAuthStore()
  const pollRef = useRef<ReturnType<typeof setInterval>>()

  const loadFriends = async () => {
    try {
      const data = await api.getFriends()
      setFriends(Array.isArray(data) ? data : [])
    } catch {}
    setLoading(false)
  }

  // 每隔10秒轮询好友请求
  useEffect(() => {
    loadFriends()
    pollRef.current = setInterval(async () => {
      try {
        const data = await api.getFriends()
        setFriends(Array.isArray(data) ? data : [])
        const invites = await api.getDuelInvites()
        if (Array.isArray(invites)) {
          setRequests(invites.filter((i: any) => i.status === 0).map((i: any) => ({
            id: i.id, fromUserId: i.fromUserId,
            name: i.fromName || '玩家', mmr: i.fromMMR || 0
          })))
        }
      } catch {}
    }, 10000)
    return () => { if (pollRef.current) clearInterval(pollRef.current) }
  }, [])

  const handleSearch = async () => {
    if (!searchQuery.trim()) return
    setError(''); setSearching(true)
    try {
      const data = await api.searchUsers(searchQuery.trim())
      setSearchResults(Array.isArray(data) ? data.filter((u: any) => u.id !== user?.id) : [])
      if (!data?.length) setError('未找到用户')
    } catch { setError('搜索失败') }
    setSearching(false)
  }

  const handleAdd = async (uid: number) => {
    try {
      await api.sendFriendRequest(uid)
      toast('好友请求已发送', 'success')
      setSearchResults([]); setSearchQuery('')
    } catch { toast('发送失败', 'error') }
  }

  const handleAccept = async (uid: number) => {
    try {
      await api.acceptFriendRequest(uid)
      toast('已接受', 'success')
      setRequests(prev => prev.filter(r => r.id !== uid && r.fromUserId !== uid))
      loadFriends()
    } catch { toast('操作失败', 'error') }
  }

  const handleReject = (uid: number) => {
    setRequests(prev => prev.filter(r => r.id !== uid && r.fromUserId !== uid))
    toast('已拒绝', 'info')
  }

  const handleRemove = async (uid: number) => {
    try { await api.removeFriend(uid); toast('已删除', 'info'); loadFriends() }
    catch { toast('删除失败', 'error') }
  }

  const online = friends.filter((f: any) => f.isOnline || f.status === '在线')
  const offline = friends.filter((f: any) => !f.isOnline && f.status !== '在线')

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-white">好友 <span className="text-sm text-surface-400 font-normal">({friends.length})</span></h2>
        <div className="flex gap-2">
          <button onClick={() => setTab('requests')}
            className={'px-4 py-2 rounded-md text-sm font-semibold relative ' + (tab === 'requests' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            请求{requests.length > 0 ? ` (${requests.length})` : ''}
          </button>
          <button onClick={() => setTab('add')}
            className={'px-4 py-2 rounded-md text-sm font-semibold ' + (tab === 'add' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            添加
          </button>
          <button onClick={() => setTab('list')}
            className={'px-4 py-2 rounded-md text-sm font-semibold ' + (tab === 'list' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            列表
          </button>
          <Button variant="ghost" size="sm" onClick={loadFriends}>刷新</Button>
        </div>
      </div>

      {tab === 'list' && (
        <div className="grid grid-cols-[1fr_2fr] gap-6">
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-2">在线 ({online.length})</p>
            {loading ? <div className="flex justify-center py-4"><div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" /></div>
              : online.length === 0 ? <p className="text-xs text-surface-500 py-4">暂无在线好友</p>
              : online.map((f: any) => (
                <div key={f.id || f.friendId} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors group">
                  <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{(f.nickname || f.friendName || f.username || '?')[0]}</div>
                  <div className="flex-1 min-w-0"><p className="text-sm text-white truncate">{f.nickname || f.friendName || f.username}</p><p className="text-xs text-surface-400">{f.mmr || 0} MMR</p></div>
                  <Link href={'/private-chat?id=' + (f.friendId || f.id) + '&name=' + encodeURIComponent(f.nickname || f.friendName || f.username)}><Button variant="ghost" size="sm">私聊</Button></Link>
                  <button onClick={() => { if (confirm('确认删除？')) handleRemove(f.friendId || f.id) }} className="text-xs text-surface-500 hover:text-red-400 opacity-0 group-hover:opacity-100 shrink-0">删除</button>
                </div>
              ))}
          </Card>
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-2">离线 ({offline.length})</p>
            {offline.map((f: any) => (
              <div key={f.id || f.friendId} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors group">
                <div className="w-9 h-9 rounded-full bg-surface-600 flex items-center justify-center text-sm text-surface-300 font-bold">{(f.nickname || f.friendName || f.username || '?')[0]}</div>
                <div className="flex-1 min-w-0"><p className="text-sm text-surface-300 truncate">{f.nickname || f.friendName || f.username}</p></div>
                <button onClick={() => { if (confirm('确认删除？')) handleRemove(f.friendId || f.id) }} className="text-xs text-surface-500 hover:text-red-400 opacity-0 group-hover:opacity-100">删除</button>
              </div>
            ))}
          </Card>
        </div>
      )}

      {tab === 'requests' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">好友请求</h3>
          {requests.length === 0 ? <p className="text-sm text-surface-400">暂无待处理的请求</p>
            : requests.map((r, i) => (
              <div key={i} className="flex items-center justify-between px-3 py-3 rounded-lg hover:bg-hover transition-colors mb-1">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{r.name[0]}</div>
                  <div><p className="text-sm text-white">{r.name}</p><p className="text-xs text-surface-400">{r.mmr || 0} MMR</p></div>
                </div>
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => handleAccept(r.fromUserId || r.id)}>接受</Button>
                  <Button variant="ghost" size="sm" onClick={() => handleReject(r.fromUserId || r.id)}>拒绝</Button>
                </div>
              </div>
            ))}
        </Card>
      )}

      {tab === 'add' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">搜索玩家</h3>
          <div className="flex gap-2 mb-4">
            <input value={searchQuery} onChange={e => setSearchQuery(e.target.value)} placeholder="输入用户名搜索"
              className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary"
              onKeyDown={e => e.key === 'Enter' && handleSearch()} />
            <Button onClick={handleSearch} disabled={searching}>{searching ? '搜索中...' : '搜索'}</Button>
          </div>
          {error && <p className="text-sm text-red-400 mb-2">{error}</p>}
          {searchResults.map((r: any) => (
            <div key={r.id} className="flex items-center justify-between px-3 py-2.5 rounded-lg hover:bg-hover transition-colors mb-1">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{(r.nickname || r.username || '?')[0]}</div>
                <div><p className="text-sm text-white">{r.nickname || r.username}</p><p className="text-xs text-surface-400">{r.mmr || 0} MMR</p></div>
              </div>
              <Button size="sm" onClick={() => handleAdd(r.id)}>加好友</Button>
            </div>
          ))}
        </Card>
      )}
    </motion.div>
  )
}
