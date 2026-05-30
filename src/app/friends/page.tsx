'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { toast } from '@/components/ui/Toast'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

export default function FriendsPage() {
  const [friends, setFriends] = useState<any[]>([])
  const [requests, setRequests] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<any[]>([])
  const [showRequests, setShowRequests] = useState(false)
  const [showAdd, setShowAdd] = useState(false)
  const [error, setError] = useState('')

  const loadFriends = async () => {
    try {
      setLoading(true)
      const data = await api.getFriends()
      setFriends(Array.isArray(data) ? data.map((f: any) => ({
        id: f.friendId || f.id || f.userId, nickname: f.nickname || f.username || '未知',
        mmr: f.mmr || 0, isOnline: f.isOnline ?? true, status: f.status || '在线'
      })) : [])
    } catch {}
    setLoading(false)
  }

  useEffect(() => { loadFriends() }, [])

  const handleSearch = async () => {
    if (!searchQuery.trim()) return
    setError('')
    try {
      const data = await api.searchUsers(searchQuery.trim())
      setSearchResults(Array.isArray(data) ? data : [])
      if (!data?.length) setError('未找到用户')
    } catch { setError('搜索失败') }
  }

  const handleAdd = async (uid: number) => {
    try { await api.sendFriendRequest(uid); toast('好友请求已发送', 'success'); setSearchResults([]); setSearchQuery('') }
    catch { toast('发送失败', 'error') }
  }

  const handleAccept = async (uid: number) => {
    try { await api.acceptFriendRequest(uid); toast('已接受', 'success'); setRequests(prev => prev.filter(r => r.id != uid)); loadFriends() }
    catch { toast('操作失败', 'error') }
  }

  const handleRemove = async (uid: number) => {
    try { await api.removeFriend(uid); toast('已删除', 'info'); loadFriends() }
    catch { toast('删除失败', 'error') }
  }

  const online = friends.filter(f => f.isOnline)
  const offline = friends.filter(f => !f.isOnline)

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-white">好友 <span className="text-sm text-surface-400 font-normal">({friends.length})</span></h2>
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => setShowAdd(true)}>添加好友</Button>
          <Button variant="ghost" size="sm" onClick={loadFriends}>刷新</Button>
        </div>
      </div>
      <div className="grid grid-cols-[1fr_2fr] gap-6">
        <div className="space-y-4">
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-2">在线 ({online.length})</p>
            {loading ? <div className="flex justify-center py-4"><div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" /></div>
              : online.map(f => (
                <div key={f.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors group">
                  <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{f.nickname[0]}</div>
                  <div className="flex-1 min-w-0"><p className="text-sm text-white truncate">{f.nickname}</p><p className="text-xs text-surface-400">{f.mmr} MMR</p></div>
                  <span className={'w-2 h-2 rounded-full ' + (f.isOnline ? 'bg-primary' : 'bg-surface-500')} />
                  <Link href={'/private-chat?id=' + f.id + '&name=' + f.nickname}><Button variant="ghost" size="sm">私聊</Button></Link>
                  <button onClick={() => { if (confirm('确认删除？')) handleRemove(f.id) }} className="text-xs text-surface-500 hover:text-red-400 opacity-0 group-hover:opacity-100 shrink-0">删除</button>
                </div>
              ))}
          </Card>
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-2">离线 ({offline.length})</p>
            {offline.map(f => (
              <div key={f.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors group">
                <div className="w-9 h-9 rounded-full bg-surface-600 flex items-center justify-center text-sm text-surface-300 font-bold">{f.nickname[0]}</div>
                <div className="flex-1 min-w-0"><p className="text-sm text-surface-300 truncate">{f.nickname}</p></div>
                <button onClick={() => { if (confirm('确认删除？')) handleRemove(f.id) }} className="text-xs text-surface-500 hover:text-red-400 opacity-0 group-hover:opacity-100">删除</button>
              </div>
            ))}
          </Card>
        </div>
        <div className="space-y-4">
          <Card variant="default">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-base font-bold text-white">好友请求</h3>
              <button onClick={() => setShowRequests(true)} className="text-xs text-primary hover:text-accent">
                查看全部{requests.length > 0 ? ' (' + requests.length + ')' : ''}
              </button>
            </div>
            <p className="text-sm text-surface-400">{requests.length === 0 ? '暂无请求' : requests.length + ' 条待处理'}</p>
          </Card>
          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">邀请好友对战</h3>
            {online.length === 0 ? <p className="text-sm text-surface-400">没有在线好友</p>
              : online.slice(0, 3).map(f => (
                <div key={f.id} className="flex items-center justify-between px-3 py-2 rounded-lg hover:bg-hover mb-1">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{f.nickname[0]}</div>
                    <p className="text-sm text-white">{f.nickname}</p>
                  </div>
                  <Button variant="ghost" size="sm">邀请</Button>
                </div>
              ))}
          </Card>
        </div>
      </div>

      <Modal isOpen={showAdd} onClose={() => { setShowAdd(false); setSearchResults([]); setSearchQuery('') }} title="添加好友" size="sm">
        <div className="space-y-4">
          <div className="flex gap-2">
            <input value={searchQuery} onChange={e => setSearchQuery(e.target.value)} placeholder="输入用户名搜索"
              className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
            <Button size="sm" onClick={handleSearch}>搜索</Button>
          </div>
          {error && <p className="text-sm text-red-400">{error}</p>}
          {searchResults.map((r: any) => (
            <div key={r.id} className="flex items-center justify-between px-3 py-2.5 rounded-lg hover:bg-hover transition-colors">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{(r.nickname || r.username || '?')[0]}</div>
                <div><p className="text-sm text-white">{r.nickname || r.username}</p><p className="text-xs text-surface-400">{r.mmr || 0} MMR</p></div>
              </div>
              <Button size="sm" onClick={() => handleAdd(r.id)}>加好友</Button>
            </div>
          ))}
        </div>
      </Modal>

      <Modal isOpen={showRequests} onClose={() => setShowRequests(false)} title="好友请求" size="sm">
        {requests.length === 0 ? <p className="text-sm text-surface-400">暂无请求</p>
          : requests.map((r: any) => (
            <div key={r.id} className="flex items-center justify-between px-3 py-3 rounded-lg hover:bg-hover mb-1">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{(r.nickname || r.username || '?')[0]}</div>
                <div><p className="text-sm text-white">{r.nickname || r.username}</p></div>
              </div>
              <div className="flex gap-2">
                <Button size="sm" onClick={() => handleAccept(r.id)}>接受</Button>
                <Button variant="ghost" size="sm">拒绝</Button>
              </div>
            </div>
          ))}
      </Modal>
    </motion.div>
  )
}
