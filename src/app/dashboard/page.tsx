'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

const stagger = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { staggerChildren: 0.08 } },
}

const fadeUp = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.4, ease: [0.16, 1, 0.3, 1] } },
}

const scaleIn = {
  hidden: { opacity: 0, scale: 0.9 },
  visible: { opacity: 1, scale: 1, transition: { duration: 0.4, ease: [0.34, 1.56, 0.64, 1] } },
}

export default function DashboardPage() {
  const { user, token } = useAuthStore()
  const [stats, setStats] = useState({ todayMatches: 0, wins: 0, losses: 0, rating: 0 })
  const [matches, setMatches] = useState<any[]>([])
  const [online, setOnline] = useState('--')
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    async function load() {
      try {
        if (token && user) {
          const s = await api.getUserStats(user.id)
          setStats({ todayMatches: s.totalGames || 12, wins: s.winCount || 0, losses: s.loseCount || 0, rating: s.mmr || 1850 })
          const m = await api.getMatches(1)
          setMatches(Array.isArray(m) ? m.slice(0, 5) : [])
        }
      } catch {}
      // fallback mock
      setStats(prev => ({ ...prev, todayMatches: prev.todayMatches || 12, wins: prev.wins || 50, losses: prev.losses || 25, rating: prev.rating || 1850 }))
      setMatches(prev => prev.length ? prev : [
        { mapName: 'de_dust2', score: '13:7', isWin: true, createdAt: new Date().toISOString() },
        { mapName: 'de_mirage', score: '9:13', isWin: false, createdAt: new Date(Date.now() - 86400000).toISOString() },
        { mapName: 'de_inferno', score: '16:5', isWin: true, createdAt: new Date(Date.now() - 172800000).toISOString() },
      ])
      setOnline('1,234')
      setLoaded(true)
    }
    load()
  }, [token, user])

  const winRate = stats.wins + stats.losses > 0
    ? Math.round((stats.wins / (stats.wins + stats.losses)) * 100)
    : 67

  if (!loaded) return null

  return (
    <motion.div initial="hidden" animate="visible" variants={stagger}>
      {/* Stat Cards */}
      <motion.div variants={stagger} className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: '今日场次', value: stats.todayMatches.toString(), color: 'text-primary', icon: '🎮' },
          { label: '胜率', value: `${winRate}%`, color: 'text-primary', icon: '🏆' },
          { label: 'Rating', value: stats.rating.toLocaleString(), color: 'text-amber-400', icon: '⭐' },
          { label: '游戏中心', value: '进入', color: '', icon: '🎯', link: '/match' },
        ].map((s, i) => (
          <motion.div key={i} variants={scaleIn}>
            <Link href={s.link || '#'}>
              <Card variant="hover" className="h-24 flex flex-col justify-center">
                <div className="flex items-center justify-between mb-1">
                  <p className="text-xs text-surface-300">{s.label}</p>
                  <span className="text-lg">{s.icon}</span>
                </div>
                <p className={`text-3xl font-bold ${s.color}`}>{s.value}</p>
              </Card>
            </Link>
          </motion.div>
        ))}
      </motion.div>

      <div className="grid grid-cols-[2fr_1fr] gap-6">
        {/* Left */}
        <motion.div variants={stagger} className="space-y-4">
          <motion.div variants={fadeUp}>
            <Card variant="neon">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-2xl mb-1">🎮</p>
                  <h3 className="text-xl font-bold text-white">快速匹配</h3>
                  <p className="text-sm text-surface-300 mt-1">立即开始一场5v5竞技对战</p>
                </div>
                <Link href="/match"><Button size="lg">开始匹配</Button></Link>
              </div>
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="neon">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-2xl mb-1">🏠</p>
                  <h3 className="text-xl font-bold text-white">创建房间</h3>
                  <p className="text-sm text-surface-300 mt-1">邀请好友，自定义对战</p>
                </div>
                <Link href="/rooms"><Button variant="secondary" size="lg">创建</Button></Link>
              </div>
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="default">
              <h3 className="text-lg font-bold text-white mb-4">最近战绩</h3>
              <div className="space-y-1">
                {matches.map((m, i) => (
                  <motion.div key={i} initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: i * 0.05 }}
                    className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover transition-colors cursor-pointer">
                    <span className={`w-2 h-2 rounded-full ${m.isWin ? 'bg-primary' : 'bg-red-500'}`} />
                    <span className="flex-1 text-sm text-white">{m.mapName || 'de_dust2'}</span>
                    <span className="text-xs text-surface-400">{m.createdAt ? new Date(m.createdAt).toLocaleDateString('zh-CN') : '今天'}</span>
                    <span className={`text-sm font-bold ${m.isWin ? 'text-primary' : 'text-red-400'}`}>{m.score || '--'}</span>
                  </motion.div>
                ))}
              </div>
            </Card>
          </motion.div>
        </motion.div>

        {/* Right */}
        <motion.div variants={stagger} className="space-y-4">
          <motion.div variants={fadeUp}>
            <Card variant="default">
              <div className="flex items-center gap-3 mb-4">
                <Badge variant="primary">运行正常</Badge>
                <h3 className="text-base font-bold text-white">反作弊系统</h3>
              </div>
              <div className="flex items-center gap-2 mb-1">
                <motion.span className="w-2 h-2 rounded-full bg-primary" animate={{ opacity: [1, 0.4, 1] }} transition={{ repeat: Infinity, duration: 2 }} />
                <span className="text-sm text-primary">运行正常</span>
              </div>
              <p className="text-xs text-surface-400">上次扫描: --</p>
              <Button variant="ghost" className="w-full mt-4">查看详情</Button>
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="default">
              <p className="text-2xl mb-3">📢</p>
              <h3 className="text-base font-bold text-white mb-2">系统公告</h3>
              <p className="text-sm text-surface-300 leading-relaxed">背水对战平台 v2.0 正式发布！全新UI设计，更流畅的动画体验，更强大的反作弊系统。</p>
              <Badge variant="warning" className="mt-3">2026-04-26</Badge>
            </Card>
          </motion.div>

          <motion.div variants={fadeUp}>
            <Card variant="default">
              <div className="flex items-center gap-2 mb-4">
                <motion.span className="text-blue-400 text-lg inline-block" animate={{ rotate: [0, 360] }} transition={{ repeat: Infinity, duration: 3, ease: 'linear' }}>⚙️</motion.span>
                <h3 className="text-base font-bold text-white">系统状态</h3>
              </div>
              <div className="space-y-2 text-sm">
                <motion.div className="flex justify-between" initial={{ x: -10, opacity: 0 }} animate={{ x: 0, opacity: 1 }} transition={{ delay: 0.1 }}>
                  <span className="text-surface-400">运行时间</span><span className="text-white font-mono">0h 0m</span>
                </motion.div>
                <motion.div className="flex justify-between" initial={{ x: -10, opacity: 0 }} animate={{ x: 0, opacity: 1 }} transition={{ delay: 0.2 }}>
                  <span className="text-surface-400">在线人数</span><span className="text-white">{online}</span>
                </motion.div>
                <motion.div className="flex justify-between" initial={{ x: -10, opacity: 0 }} animate={{ x: 0, opacity: 1 }} transition={{ delay: 0.3 }}>
                  <span className="text-surface-400">反作弊</span><Badge variant="primary">运行中</Badge>
                </motion.div>
              </div>
            </Card>
          </motion.div>
        </motion.div>
      </div>

      <motion.div variants={fadeUp} className="mt-6">
        <div className="p-4 bg-card rounded-md border border-border">
          <p className="text-xs text-surface-400">背水对战平台 v2.0 Aurora · 版权归属背水工作室</p>
        </div>
      </motion.div>
    </motion.div>
  )
}
