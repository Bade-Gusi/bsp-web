'use client'

import { useEffect, useState } from 'react'
import { usePathname } from 'next/navigation'
import { AnimatePresence, motion } from 'framer-motion'
import '@/styles/globals.css'
import { LoadingOverlay } from '@/components/layout/LoadingOverlay'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'
import { AuthPage } from '@/components/auth/AuthPage'

const pageTransition = {
  initial: { opacity: 0, y: 20, scale: 0.98 },
  animate: { opacity: 1, y: 0, scale: 1 },
  exit: { opacity: 0, y: -10, scale: 0.98 },
  transition: { duration: 0.35, ease: [0.16, 1, 0.3, 1] }
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading, token, loadFromStorage } = useAuthStore()
  const pathname = usePathname()
  const [pageTitle, setPageTitle] = useState('首页')
  const [init, setInit] = useState(false)
  const [particles] = useState(() =>
    Array.from({ length: 20 }, (_, i) => ({
      id: i, x: Math.random() * 100, y: Math.random() * 100,
      size: Math.random() * 3 + 1, delay: Math.random() * 5, duration: Math.random() * 5 + 5
    }))
  )

  useEffect(() => { loadFromStorage(); setTimeout(() => setInit(true), 300) }, [])
  useEffect(() => { api.setToken(token) }, [token])

  useEffect(() => {
    const titles: Record<string, string> = {
      '/': '登录', '/dashboard': '首页', '/match': '快速匹配', '/duel': '1v1对战',
      '/rooms': '房间大厅', '/servers': '服务器', '/friends': '好友', '/chat': '聊天',
      '/leaderboard': '排行榜', '/achievements': '成就', '/market': '皮肤市场',
      '/welfare': '背水公益', '/settings': '设置', '/minigames': '小游戏',
    }
    setPageTitle(titles[pathname] || '背水对战平台')
  }, [pathname])

  const showContent = !loading && init
  const showAuth = showContent && !isAuthenticated
  const showApp = showContent && isAuthenticated

  return (
    <html lang="zh-CN" className="dark">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>背水对战平台</title>
      </head>
      <body className="bg-surface text-white overflow-hidden">
        {!showContent && <LoadingOverlay />}
        {showAuth && <AuthPage />}
        {showApp && (
          <div className="flex h-screen">
            {/* 粒子背景 */}
            <div className="fixed inset-0 pointer-events-none z-0 overflow-hidden">
              {particles.map((p) => (
                <motion.div key={p.id}
                  className="absolute rounded-full bg-primary/10"
                  style={{ left: `${p.x}%`, top: `${p.y}%`, width: p.size, height: p.size }}
                  animate={{ y: [0, -30, 0], opacity: [0.1, 0.3, 0.1] }}
                  transition={{ repeat: Infinity, duration: p.duration, delay: p.delay, ease: 'easeInOut' }}
                />
              ))}
            </div>

            {/* 侧边栏 */}
            <nav className="w-[260px] h-screen bg-card border-r border-border flex flex-col shrink-0 relative z-10">
              <div className="px-6 py-6 pb-8">
                <p className="text-lg font-bold text-white">背水对战平台</p>
                <p className="text-[10px] text-surface-400 font-mono mt-1">BEISHUI</p>
              </div>
              <div className="flex-1 overflow-y-auto px-4 space-y-1">
                {[
                  { icon: '🏠', label: '首页', path: '/dashboard' },
                  { icon: '🎮', label: '匹配', path: '/match' },
                  { icon: '⚔️', label: '1v1', path: '/duel' },
                  { icon: '👥', label: '好友', path: '/friends' },
                  { icon: '💬', label: '聊天', path: '/chat' },
                  { icon: '🏠', label: '房间', path: '/rooms' },
                  { icon: '🖥', label: '服务器', path: '/servers' },
                  { icon: '📊', label: '排行榜', path: '/leaderboard' },
                  { icon: '🏆', label: '成就', path: '/achievements' },
                  { icon: '🛒', label: '市场', path: '/market' },
                  { icon: '❤️', label: '公益', path: '/welfare' },
                  { icon: '⚙️', label: '设置', path: '/settings' },
                ].map((item) => (
                  <a key={item.path} href={item.path}
                    className={'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors ' + (pathname.startsWith(item.path) ? 'bg-hover text-primary' : 'text-surface-300 hover:text-white hover:bg-hover')}>
                    <span>{item.icon}</span><span>{item.label}</span>
                  </a>
                ))}
              </div>
            </nav>

            {/* 主内容 */}
            <div className="flex-1 flex flex-col overflow-hidden relative z-10">
              <header className="h-16 px-6 flex items-center border-b border-border bg-surface shrink-0">
                <h1 className="text-xl font-bold text-white">{pageTitle}</h1>
              </header>
              <main className="flex-1 overflow-y-auto p-6">
                <AnimatePresence mode="wait">
                  <motion.div key={pathname} {...pageTransition}>{children}</motion.div>
                </AnimatePresence>
              </main>
            </div>
          </div>
        )}
      </body>
    </html>
  )
}
