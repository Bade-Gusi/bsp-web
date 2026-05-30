'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'

export default function FriendsPage() {
  const [tab, setTab] = useState('list')
  const [search, setSearch] = useState('')

  const [friends] = useState([
    { id: 1, name: 'ProPlayer', status: '在线', mmr: 1850, online: true },
    { id: 2, name: 'CS2Master', status: '游戏中', mmr: 2100, online: true },
    { id: 3, name: 'AceGunner', status: '离线', mmr: 1550, online: false },
    { id: 4, name: 'HeadshotKing', status: '在线', mmr: 1720, online: true },
    { id: 5, name: 'RushB', status: '离线', mmr: 1300, online: false },
  ])

  const [requests] = useState([
    { id: 6, name: 'NewPlayer', mmr: 800 },
    { id: 7, name: 'SilverStar', mmr: 950 },
  ])

  const [searchResults, setSearchResults] = useState<{ id: number; name: string; mmr: number }[]>([])

  const doSearch = () => {
    if (!search.trim()) return
    setSearchResults([
      { id: 8, name: 'FoundPlayer1', mmr: 1500 },
      { id: 9, name: 'FoundPlayer2', mmr: 1200 },
    ])
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex gap-2 mb-6">
        <button onClick={() => setTab('list')}
          className={'px-5 py-2 rounded-md text-sm font-semibold ' + (tab === 'list' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>好友列表</button>
        <button onClick={() => setTab('requests')}
          className={'px-5 py-2 rounded-md text-sm font-semibold relative ' + (tab === 'requests' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
          好友请求
          {requests.length > 0 && <span className="ml-1.5 w-5 h-5 inline-flex items-center justify-center rounded-full bg-red-500 text-white text-[10px]">{requests.length}</span>}
        </button>
        <button onClick={() => setTab('add')}
          className={'px-5 py-2 rounded-md text-sm font-semibold ' + (tab === 'add' ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>添加好友</button>
      </div>

      {tab === 'list' && (
        <div className="grid grid-cols-[1fr_2fr] gap-6">
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-3">在线 ({friends.filter(f => f.online).length})</p>
            {friends.filter(f => f.online).map((f, i) => (
              <div key={i} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors mb-1">
                <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{f.name[0]}</div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-white">{f.name}</p>
                  <p className="text-xs text-surface-400">{f.status} &middot; {f.mmr} MMR</p>
                </div>
                <span className={'w-2 h-2 rounded-full ' + (f.online ? 'bg-primary' : 'bg-surface-500')} />
              </div>
            ))}
          </Card>

          <Card variant="default">
            <p className="text-xs text-surface-400 mb-3">离线</p>
            {friends.filter(f => !f.online).map((f, i) => (
              <div key={i} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors mb-1">
                <div className="w-9 h-9 rounded-full bg-surface-600 flex items-center justify-center text-sm text-surface-300 font-bold">{f.name[0]}</div>
                <div className="flex-1">
                  <p className="text-sm text-surface-300">{f.name}</p>
                  <p className="text-xs text-surface-500">{f.status}</p>
                </div>
                <Link href={`/private-chat?id=${f.id}&name=${f.name}`}>
                  <Button variant="ghost" size="sm">私聊</Button>
                </Link>
              </div>
            ))}
          </Card>
        </div>
      )}

      {tab === 'requests' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">好友请求</h3>
          {requests.length === 0 ? (
            <p className="text-sm text-surface-400">暂无待处理的请求</p>
          ) : requests.map((r, i) => (
            <div key={i} className="flex items-center justify-between px-3 py-3 rounded-lg hover:bg-hover transition-colors mb-1">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{r.name[0]}</div>
                <div>
                  <p className="text-sm text-white">{r.name}</p>
                  <p className="text-xs text-surface-400">{r.mmr} MMR</p>
                </div>
              </div>
              <div className="flex gap-2">
                <Button size="sm">接受</Button>
                <Button variant="ghost" size="sm">拒绝</Button>
              </div>
            </div>
          ))}
        </Card>
      )}

      {tab === 'add' && (
        <div className="grid grid-cols-[1fr_1fr] gap-6">
          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">搜索玩家</h3>
            <div className="flex gap-2 mb-4">
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="输入用户名或SteamID"
                className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
              <Button onClick={doSearch}>搜索</Button>
            </div>
            {searchResults.map((r, i) => (
              <div key={i} className="flex items-center justify-between px-3 py-2.5 rounded-lg hover:bg-hover transition-colors">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{r.name[0]}</div>
                  <div>
                    <p className="text-sm text-white">{r.name}</p>
                    <p className="text-xs text-surface-400">{r.mmr} MMR</p>
                  </div>
                </div>
                <Button size="sm">加好友</Button>
              </div>
            ))}
          </Card>
          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">邀请好友对战</h3>
            <p className="text-sm text-surface-400 mb-4">选择在线好友发送游戏邀请</p>
            {friends.filter(f => f.online).map((f, i) => (
              <div key={i} className="flex items-center justify-between px-3 py-2 rounded-lg hover:bg-hover transition-colors mb-1">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{f.name[0]}</div>
                  <p className="text-sm text-white">{f.name}</p>
                </div>
                <Button variant="ghost" size="sm">邀请</Button>
              </div>
            ))}
          </Card>
        </div>
      )}
    </motion.div>
  )
}
