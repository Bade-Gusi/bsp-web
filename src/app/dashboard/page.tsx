'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'
import { LatestBroadcastBanner } from '@/components/ui/LatestBroadcastBanner'
import { VERSION_STRING } from '@/lib/version'

const stagger = { hidden: { opacity: 0 }, visible: { opacity: 1, transition: { staggerChildren: 0.08 } } }
const fadeUp = { hidden: { opacity: 0, y: 20 }, visible: { opacity: 1, y: 0, transition: { duration: 0.4 } } }
const scaleIn = { hidden: { opacity: 0, scale: 0.9 }, visible: { opacity: 1, scale: 1, transition: { duration: 0.4, ease: [0.34, 1.56, 0.64, 1] } } }

export default function DashboardPage() {
  const { user, token } = useAuthStore()
  const [stats, setStats] = useState<{ todayMatches: number; wins: number; losses: number; rating: number } | null>(null)
  const [matches, setMatches] = useState<any[] | null>(null)
  const [broadcasts, setBroadcasts] = useState<{ addr: string; time: string }[]>([])
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    async function load() {
      try {
        if (token && user) {
          const [s, m] = await Promise.all([
            api.getUserStats(user.id).catch(() => null),
            api.getMatches(1).catch(() => null),
          ])
          if (s) setStats({
            todayMatches: s.todayGames ?? s.totalGames ?? 0,
            wins: s.winCount ?? 0,
            losses: s.loseCount ?? 0,
            rating: s.mmr ?? 0,
          })
          if (Array.isArray(m)) setMatches(m.slice(0, 5))
        }
      } catch {}
      // 加载广播记录
      try {
        const res = await fetch('/api/admin/latest-broadcast')
        if (res.ok) {
          const d = await res.json()
          if (d?.serverAddress) setBroadcasts([{ addr: d.serverAddress, time: d.broadcastedAt || '' }])
        }
      } catch {}
      try { const c = localStorage.getItem('broadcast_history'); if (c) setBroadcasts(prev => [...JSON.parse(c), ...prev].slice(0, 10)) } catch {}
      setLoaded(true)
    }
    load()
  }, [token, user])

  if (!loaded) return null

  const displayStats = stats
    ? [
        { label: '胜场', value: String(stats.wins), color: 'text-primary' },
        { label: '负场', value: String(stats.losses), color: 'text-red-400' },
        { label: 'Rating', value: stats.rating.toLocaleString(), color: 'text-amber-400' },
        { label: '总场次', value: String(stats.wins + stats.losses), color: 'text-surface-300' },
      ]
    : [
        { label: '胜场', value: '--', color: 'text-primary' },
        { label: '负场', value: '--', color: 'text-red-400' },
        { label: 'Rating', value: '--', color: 'text-amber-400' },
        { label: '总场次', value: '--', color: 'text-surface-300' },
      ]

  const launchGame = () => window.open('steam://rungameid/730', '_blank')

  const handleLaunchGame = () => window.open('steam://rungameid/730', '_blank')

  return (
    <motion.div initial="hidden" animate="visible" variants={stagger}>
      <LatestBroadcastBanner />
      <div className="grid grid-cols-4 gap-4 mb-6">
        {displayStats.map((s, i) => (
          <motion.div key={i} variants={scaleIn}>
            <Card variant="default" className="h-24 flex flex-col justify-center">
              <p className="text-xs text-surface-400 mb-1">{s.label}</p>
              <p className={'text-3xl font-bold ' + s.color}>{s.value}</p>
            </Card>
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-[2fr_1fr] gap-6">
        <motion.div variants={stagger} className="space-y-4">
          <motion.div variants={fadeUp}>
            <Card variant="neon">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xl font-bold text-white">快速匹配</h3>
                  <p className="text-sm text-surface-300 mt-1">开始一场5v5竞技对战</p>
                </div>
                <Link href="/match"><Button size="lg">开始匹配</Button></Link>
                <Button variant="secondary" size="sm" onClick={handleLaunchGame} className="ml-2">启动CS2</Button>
              </div>
            </Card>
          </motion.div>
          <motion.div variants={fadeUp}>
            <Card variant="neon">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xl font-bold text-white">创建房间</h3>
                  <p className="text-sm text-surface-300 mt-1">邀请好友，自定义对战</p>
                </div>
                <Link href="/rooms"><Button variant="secondary" size="lg">创建</Button></Link>
              </div>
            </Card>
          </motion.div>
          <motion.div variants={fadeUp}>
            <Card variant="default">
              <h3 className="text-base font-bold text-white mb-4">最近战绩</h3>
              {matches === null ? (
                <div className="flex justify-center py-4">
                  <div className="w-6 h-6 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
                </div>
              ) : matches.length === 0 ? (
                <p className="text-sm text-surface-400 py-4 text-center">暂无比赛记录</p>
              ) : (
                <div className="space-y-1">
                  {matches.map((m, i) => (
                    <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover transition-colors cursor-pointer">
                      <span className={'w-2 h-2 rounded-full ' + (m.isWin ? 'bg-primary' : 'bg-red-500')} />
                      <span className="flex-1 text-sm text-white">{m.mapName || m.map || 'de_dust2'}</span>
                      <span className={'text-sm font-bold ' + (m.isWin ? 'text-primary' : 'text-red-400')}>
                        {m.isWin ? '胜利' : '失败'}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          </motion.div>
        </motion.div>

        <motion.div variants={stagger} className="space-y-4">
          <motion.div variants={fadeUp}>
            <Card variant="default">
              <div className="flex items-center gap-3 mb-4">
                <Badge variant="primary">运行正常</Badge>
                <h3 className="text-base font-bold text-white">反作弊系统</h3>
              </div>
              <div className="flex items-center gap-2 mb-1">
                <span className="w-2 h-2 rounded-full bg-primary" />
                <span className="text-sm text-primary">运行正常</span>
              </div>
              <p className="text-xs text-surface-400">上次扫描: --</p>
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="default">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-base font-bold text-white">广播记录</h3>
                <a href="/broadcast-history" className="text-xs text-primary hover:text-accent">查看全部</a>
              </div>
              {broadcasts.length === 0 ? <p className="text-xs text-surface-400">暂无广播</p>
                : broadcasts.slice(0, 4).map((b, i) => (
                  <div key={i} className="flex items-center justify-between py-1.5 border-b border-border/30 last:border-0">
                    <span className="text-sm font-mono text-primary">{b.addr}</span>
                    <button onClick={() => window.open('steam://connect/' + b.addr, '_blank')}
                      className="text-xs bg-primary/20 text-primary px-2 py-0.5 rounded hover:bg-primary/30">连接</button>
                  </div>
                ))}
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="default">
              <h3 className="text-base font-bold text-white mb-2">系统公告</h3>
              <p className="text-sm text-surface-300 leading-relaxed">背水对战平台 v2.0 正式发布</p>
              <Badge variant="warning" className="mt-3">2026-04-26</Badge>
            </Card>
          </motion.div>
        </motion.div>
      </div>
    </motion.div>
  )
}
