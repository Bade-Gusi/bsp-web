'use client'

import { useEffect, useState } from 'react'
import { usePathname } from 'next/navigation'
import Link from 'next/link'
import { AnimatePresence, motion } from 'framer-motion'
import '@/styles/globals.css'
import { LoadingOverlay } from '@/components/layout/LoadingOverlay'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'
import { AuthPage } from '@/components/auth/AuthPage'

const pt = {
  initial: { opacity: 0, y: 20 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -10 },
  transition: { duration: 0.3, ease: [0.16, 1, 0.3, 1] }
}

const navItems = [
  { label: '首页', path: '/dashboard' },
  { label: '快速匹配', path: '/match' },
  { label: '1v1对战', path: '/duel' },
  { label: '房间大厅', path: '/rooms' },
  { label: '服务器', path: '/servers' },
  { label: '好友', path: '/friends' },
  { label: '聊天', path: '/chat' },
  { label: '排行榜', path: '/leaderboard' },
  { label: '成就', path: '/achievements' },
  { label: '皮肤市场', path: '/market' },
  { label: '背水公益', path: '/welfare' },
  { label: '设置', path: '/settings' },
]

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading, token, loadFromStorage } = useAuthStore()
  const pathname = usePathname()
  const [pageTitle, setPageTitle] = useState('首页')
  const [init, setInit] = useState(false)

  useEffect(() => { loadFromStorage(); setTimeout(() => setInit(true), 300) }, [])
  useEffect(() => { api.setToken(token) }, [token])

  useEffect(() => {
    const t: Record<string, string> = {
      '/dashboard': '首页', '/match': '快速匹配', '/duel': '1v1对战', '/rooms': '房间大厅',
      '/servers': '服务器', '/friends': '好友', '/chat': '聊天', '/leaderboard': '排行榜',
      '/achievements': '成就', '/market': '皮肤市场', '/welfare': '背水公益',
      '/settings': '设置', '/minigames': '小游戏',
    }
    setPageTitle(t[pathname] || '背水对战平台')
  }, [pathname])

  const showContent = !loading && init
  const showAuth = showContent && !isAuthenticated
  const showApp = showContent && isAuthenticated

  return (
    <html lang="zh-CN" className="dark">
      <head><meta charSet="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>背水对战平台</title></head>
      <body className="bg-surface text-white overflow-hidden">
        {!showContent && <LoadingOverlay />}
        {showAuth && <AuthPage />}
        {showApp && (
          <div className="flex h-screen">
            <nav className="w-56 h-screen bg-card border-r border-border flex flex-col shrink-0">
              <div className="px-5 py-6 border-b border-border/50 flex items-center gap-3">
                <img src="/default_avatar.png" className="w-9 h-9 rounded-lg object-cover" alt="" />
                <div>
                  <h1 className="text-sm font-bold text-white">背水对战平台</h1>
                  <p className="text-[10px] text-surface-400 font-mono">BEISHUI</p>
                </div>
              </div>
              <div className="flex-1 overflow-y-auto py-2">
                {navItems.map((item) => {
                  const active = pathname === item.path || (item.path !== '/dashboard' && pathname.startsWith(item.path))
                  return (
                    <Link key={item.path} href={item.path}
                      className={'block px-5 py-2.5 text-sm transition-colors border-l-[3px] ' + (active ? 'bg-hover text-primary border-primary' : 'text-surface-300 hover:text-white hover:bg-hover border-transparent')}>
                      {item.label}
                    </Link>
                  )
                })}
              </div>
              <div className="px-4 py-4 border-t border-border/50">
                <button onClick={() => useAuthStore.getState().logout()} className="text-xs text-surface-400 hover:text-white transition-colors">退出登录</button>
              </div>
            </nav>
            <div className="flex-1 flex flex-col overflow-hidden">
              <header className="h-14 px-6 flex items-center border-b border-border bg-surface shrink-0">
                <h1 className="text-lg font-bold text-white">{pageTitle}</h1>
              </header>
              <main className="flex-1 overflow-y-auto p-6">
                <AnimatePresence mode="wait">
                  <motion.div key={pathname} {...pt}>{children}</motion.div>
                </AnimatePresence>
              </main>
            </div>
          </div>
        )}
      </body>
    </html>
  )
}
