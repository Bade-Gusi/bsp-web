'use client'

import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Badge } from '@/components/ui/Badge'
import { toast } from '@/components/ui/Toast'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

export default function ProfilePage() {
  const { user, token } = useAuthStore()
  const [tab, setTab] = useState<'view' | 'edit'>('view')
  const [stats, setStats] = useState<any>(null)
  const [matches, setMatches] = useState<any[]>([])
  const [nickname, setNickname] = useState('')
  const [phone, setPhone] = useState('')
  const [birthday, setBirthday] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!user || !token) return
    setNickname(user.nickname || '')
    setPhone(user.phone || '')
    setBirthday(user.birthday || '')
    api.getUserStats(user.id).then(s => setStats(s)).catch(() => {})
    api.getMatches(1).then(m => { if (Array.isArray(m)) setMatches(m.slice(0, 5)) }).catch(() => {})
  }, [user, token])

  const handleSave = async () => {
    if (!token) return
    setSaving(true)
    try {
      const res = await fetch('/api/auth/profile', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ nickname: nickname || undefined, phone: phone || undefined, birthday: birthday || undefined }),
      })
      if (!res.ok) throw new Error()
      toast('保存成功', 'success')
      setTab('view')
    } catch { toast('保存失败', 'error') }
    setSaving(false)
  }

  const wr = stats ? Math.round((stats.winCount / (stats.winCount + stats.loseCount || 1)) * 100) : '--'
  if (!user) return null

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-primary/20 flex items-center justify-center text-2xl font-bold text-primary">{(user.nickname || user.username)[0]}</div>
          <div>
            <h2 className="text-xl font-bold text-white">{user.nickname || user.username}</h2>
            <p className="text-xs text-surface-400">UID: {user.id} | MMR: {stats?.mmr || user.mmr || 1000}</p>
          </div>
        </div>
        <Button size="sm" onClick={() => { setTab(tab === 'edit' ? 'view' : 'edit'); setNickname(user.nickname || ''); setPhone(user.phone || ''); setBirthday(user.birthday || '') }}>
          {tab === 'edit' ? '取消' : '编辑资料'}
        </Button>
      </div>

      {tab === 'view' ? (
        <>
          <div className="grid grid-cols-4 gap-4 mb-6">
            {[
              { label: '胜场', value: stats?.winCount ?? '--' },
              { label: '负场', value: stats?.loseCount ?? '--' },
              { label: '胜率', value: wr + '%' },
              { label: '总场次', value: stats ? stats.winCount + stats.loseCount : '--' },
            ].map((s, i) => (
              <Card key={i} variant="default" className="text-center py-4">
                <p className="text-2xl font-bold text-primary">{s.value}</p>
                <p className="text-xs text-surface-400 mt-1">{s.label}</p>
              </Card>
            ))}
          </div>

          <div className="grid grid-cols-[1fr_1fr] gap-6 mb-6">
            <Card variant="default">
              <h3 className="text-sm font-bold text-white mb-3">账号信息</h3>
              <div className="space-y-2 text-sm">
                {[
                  ['用户名', user.username],
                  ['昵称', user.nickname || '未设置'],
                  ['手机', user.phone || '未绑定'],
                  ['生日', user.birthday || '未设置'],
                  ['Steam', user.steamId ? '已绑定' : '未绑定'],
                ].map(([l, v]) => (
                  <div key={l} className="flex justify-between"><span className="text-surface-400">{l}</span><span className="text-white">{v}</span></div>
                ))}
              </div>
            </Card>
            <Card variant="default">
              <h3 className="text-sm font-bold text-white mb-3">游戏数据</h3>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between"><span className="text-surface-400">MMR</span><span className="text-primary font-bold">{stats?.mmr || user.mmr || 1000}</span></div>
                <div className="flex justify-between"><span className="text-surface-400">胜场</span><span className="text-white">{stats?.winCount || 0}</span></div>
                <div className="flex justify-between"><span className="text-surface-400">负场</span><span className="text-white">{stats?.loseCount || 0}</span></div>
                <div className="flex justify-between"><span className="text-surface-400">爆头</span><span className="text-white">{stats?.headshotCount || 0}</span></div>
                <div className="flex justify-between"><span className="text-surface-400">段位</span><Badge variant="primary">{stats?.rankName || '未定级'}</Badge></div>
              </div>
            </Card>
          </div>

          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">最近比赛</h3>
            {matches.length === 0 ? <p className="text-sm text-surface-400">暂无比赛记录</p>
              : matches.map((m, i) => (
                <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover transition-colors">
                  <span className={'w-2 h-2 rounded-full ' + (m.isWin ? 'bg-primary' : 'bg-red-500')} />
                  <span className="flex-1 text-sm text-white">{m.mapName || m.map || 'de_dust2'}</span>
                  <span className={'text-sm font-bold ' + (m.isWin ? 'text-primary' : 'text-red-400')}>{m.isWin ? '胜利' : '失败'}</span>
                </div>
              ))}
          </Card>
        </>
      ) : (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">编辑资料</h3>
          <div className="space-y-4 max-w-md">
            <Input label="昵称" placeholder="输入昵称" value={nickname} onChange={e => setNickname(e.target.value)} />
            <Input label="手机号" placeholder="11位手机号" value={phone} onChange={e => setPhone(e.target.value)} />
            <Input label="生日" placeholder="MM-dd 如 01-15" value={birthday} onChange={e => setBirthday(e.target.value)} />
            <p className="text-sm text-surface-400">Steam: {user.steamId ? '已绑定' : '未绑定'}</p>
            <div className="flex gap-2">
              <Button onClick={handleSave} loading={saving}>保存</Button>
              <Button variant="ghost" onClick={() => window.open('/changelog', '_self')}>登录历史</Button>
            </div>
          </div>
        </Card>
      )}

      <div className="mt-6 p-4 bg-card rounded-md border border-border flex items-center justify-between">
        <p className="text-xs text-surface-400">背水对战平台 v2.0.0 Aurora</p>
        <a href="/changelog" className="text-xs text-primary hover:text-accent">更新日志</a>
      </div>
    </motion.div>
  )
}
