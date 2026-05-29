'use client'

import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/Input'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api'

interface Room {
  roomCode?: string
  name: string
  map: string
  mode: string
  region?: string
  currentPlayers: number
  maxPlayers: number
  hasPassword: boolean
  isOfficial?: boolean
  players?: { nickname: string; isOwner: boolean }[]
  createdAt?: string
}

const MAPS = [
  { id: 'de_dust2', name: 'Dust II' },
  { id: 'de_mirage', name: 'Mirage' },
  { id: 'de_inferno', name: 'Inferno' },
  { id: 'de_nuke', name: 'Nuke' },
  { id: 'de_overpass', name: 'Overpass' },
  { id: 'de_ancient', name: 'Ancient' },
  { id: 'de_vertigo', name: 'Vertigo' },
  { id: 'de_anubis', name: 'Anubis' },
]

const MODES = ['竞技', '休闲', '双人']

export default function RoomsPage() {
  const [rooms, setRooms] = useState<Room[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [showJoin, setShowJoin] = useState<string | null>(null)
  const [showDetail, setShowDetail] = useState<Room | null>(null)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')

  // Create room form
  const [roomName, setRoomName] = useState('')
  const [selectedMap, setSelectedMap] = useState('de_dust2')
  const [selectedMode, setSelectedMode] = useState('竞技')
  const [roomPassword, setRoomPassword] = useState('')
  const [creating, setCreating] = useState(false)

  // Join room
  const [joinPassword, setJoinPassword] = useState('')
  const [joining, setJoining] = useState(false)

  const fetchRooms = async () => {
    try {
      setLoading(true)
      const data = await api.getRooms(1)
      setRooms(Array.isArray(data) ? data : [])
    } catch { /* ignore */ }
    finally { setLoading(false) }
  }

  useEffect(() => { fetchRooms() }, [])

  const handleCreate = async () => {
    setError('')
    if (!roomName.trim() && !selectedMap) { setError('请填写房间名称'); return }
    setCreating(true)
    try {
      const data = await api.createRoom({
        gameId: 1,
        mapName: selectedMap,
        mode: selectedMode,
        password: roomPassword || undefined,
        maxPlayers: selectedMode === '双人' ? 4 : selectedMode === '休闲' ? 20 : 10,
        name: roomName.trim() || `${selectedMap} ${selectedMode}房`,
      })
      setShowCreate(false)
      setRoomName('')
      setRoomPassword('')
      fetchRooms()
    } catch (err: any) { setError(err.message || '创建失败') }
    finally { setCreating(false) }
  }

  const handleJoin = async (code: string) => {
    setError('')
    setJoining(true)
    try {
      await api.joinRoom(code, joinPassword || undefined)
      setShowJoin(null)
      setJoinPassword('')
      fetchRooms()
    } catch (err: any) { setError(err.message || '加入失败') }
    finally { setJoining(false) }
  }

  const handleLeave = async (code: string) => {
    try { await api.leaveRoom(code); fetchRooms() }
    catch { /* ignore */ }
  }

  const filteredRooms = rooms.filter(r =>
    !search || r.name?.toLowerCase().includes(search.toLowerCase()) ||
    r.map?.toLowerCase().includes(search.toLowerCase())
  )

  const maxPlayersForMode = (mode?: string) =>
    mode === '双人' ? 4 : mode === '休闲' ? 20 : 10

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="min-h-screen bg-surface p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-bold text-white">房间大厅</h2>
          <p className="text-sm text-surface-400 mt-0.5">创建或加入自定义房间</p>
        </div>
        <div className="flex gap-3">
          <input placeholder="搜索房间..."
            value={search} onChange={e => setSearch(e.target.value)}
            className="w-48 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
          <Button onClick={() => setShowCreate(true)}>创建房间</Button>
        </div>
      </div>

      {/* Room list */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
        </div>
      ) : filteredRooms.length === 0 ? (
        <Card variant="default" className="text-center py-20">
          <p className="text-4xl mb-4">🏠</p>
          <p className="text-surface-400">{search ? '没有匹配的房间' : '暂无房间，点击右上角创建'}</p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {filteredRooms.map((room, i) => (
            <motion.div key={room.roomCode || i} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.03 }}>
              <Card variant="hover" className="cursor-pointer" onClick={() => setShowDetail(room)}>
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <p className="text-white font-semibold">{room.name || room.map}</p>
                    <p className="text-xs text-surface-400 mt-0.5">{room.map} · {room.mode}</p>
                  </div>
                  {room.hasPassword && <span className="text-surface-400 text-lg">🔒</span>}
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <div className="flex -space-x-1.5">
                      {Array.from({ length: Math.min(room.currentPlayers || 1, 3) }).map((_, j) => (
                        <div key={j} className="w-6 h-6 rounded-full bg-primary/20 border-2 border-card flex items-center justify-center text-[10px] text-primary font-bold">
                          {room.players?.[j]?.nickname?.[0] || '?'}
                        </div>
                      ))}
                    </div>
                    <span className="text-xs text-surface-400">
                      {room.currentPlayers || 1}/{room.maxPlayers || maxPlayersForMode(room.mode)}
                    </span>
                  </div>
                  <Button size="sm" variant={room.hasPassword ? 'secondary' : 'primary'}
                    onClick={() => setShowJoin(room.roomCode || '')}>
                    {room.hasPassword ? '密码加入' : '加入'}
                  </Button>
                </div>
              </Card>
            </motion.div>
          ))}
        </div>
      )}

      {/* Create room modal */}
      <Modal isOpen={showCreate} onClose={() => { setShowCreate(false); setError('') }} title="创建房间" size="md">
        <div className="space-y-4">
          <Input label="房间名称" placeholder="留空自动生成" value={roomName} onChange={e => setRoomName(e.target.value)} />

          <div>
            <p className="text-sm font-medium text-surface-300 mb-2">游戏模式</p>
            <div className="flex gap-2">
              {MODES.map(m => (
                <button key={m} onClick={() => setSelectedMode(m)}
                  className={'px-4 py-2 rounded-lg text-sm font-medium transition-all ' +
                    (selectedMode === m ? 'bg-primary text-black' : 'bg-surface-800 text-surface-300 hover:bg-surface-700')}>
                  {m}
                </button>
              ))}
            </div>
          </div>

          <div>
            <p className="text-sm font-medium text-surface-300 mb-2">地图</p>
            <div className="grid grid-cols-4 gap-2">
              {MAPS.map(m => (
                <button key={m.id} onClick={() => setSelectedMap(m.id)}
                  className={'px-3 py-2 rounded-lg text-xs font-medium transition-all ' +
                    (selectedMap === m.id ? 'bg-primary text-black' : 'bg-surface-800 text-surface-300 hover:bg-surface-700')}>
                  {m.name}
                </button>
              ))}
            </div>
          </div>

          <Input label="房间密码（可选）" type="password" placeholder="留空为公开房" value={roomPassword} onChange={e => setRoomPassword(e.target.value)} />

          {error && <p className="text-sm text-red-400">{error}</p>}

          <Button loading={creating} className="w-full" onClick={handleCreate}>创建房间</Button>
        </div>
      </Modal>

      {/* Join room password modal */}
      <Modal isOpen={!!showJoin} onClose={() => { setShowJoin(null); setJoinPassword(''); setError('') }} title="加入房间" size="sm">
        <div className="space-y-4">
          {showJoin && (
            <Input label="房间密码" type="password" placeholder="输入房间密码" value={joinPassword} onChange={e => setJoinPassword(e.target.value)} />
          )}
          {error && <p className="text-sm text-red-400">{error}</p>}
          <Button loading={joining} className="w-full" onClick={() => showJoin && handleJoin(showJoin)}>加入</Button>
        </div>
      </Modal>

      {/* Room detail modal */}
      <Modal isOpen={!!showDetail} onClose={() => setShowDetail(null)} title="房间详情" size="md">
        {showDetail && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-surface-800 rounded-lg p-4">
                <p className="text-xs text-surface-400 mb-1">地图</p>
                <p className="text-white font-semibold">{showDetail.map}</p>
              </div>
              <div className="bg-surface-800 rounded-lg p-4">
                <p className="text-xs text-surface-400 mb-1">模式</p>
                <p className="text-white font-semibold">{showDetail.mode}</p>
              </div>
              <div className="bg-surface-800 rounded-lg p-4">
                <p className="text-xs text-surface-400 mb-1">人数</p>
                <p className="text-white font-semibold">{showDetail.currentPlayers || 1}/{showDetail.maxPlayers || maxPlayersForMode(showDetail.mode)}</p>
              </div>
              <div className="bg-surface-800 rounded-lg p-4">
                <p className="text-xs text-surface-400 mb-1">密码</p>
                <p className="text-white font-semibold">{showDetail.hasPassword ? '🔒 已加密' : '公开'}</p>
              </div>
            </div>

            <div>
              <p className="text-sm font-medium text-surface-300 mb-2">玩家列表</p>
              <div className="space-y-1">
                {(showDetail.players?.length ? showDetail.players : [{ nickname: '房主', isOwner: true }]).map((p, i) => (
                  <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-surface-800">
                    <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-bold">{p.nickname[0]}</div>
                    <span className="flex-1 text-sm text-white">{p.nickname}</span>
                    {p.isOwner && <Badge variant="primary">房主</Badge>}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex gap-3">
              <Button className="flex-1" onClick={() => { setShowDetail(null); setShowJoin(showDetail.roomCode || ''); }}>
                {showDetail.hasPassword ? '输入密码加入' : '加入房间'}
              </Button>
              {showDetail.roomCode && (
                <Button variant="ghost" onClick={() => { handleLeave(showDetail.roomCode || ''); setShowDetail(null); }}>
                  离开房间
                </Button>
              )}
            </div>
          </div>
        )}
      </Modal>
    </motion.div>
  )
}
