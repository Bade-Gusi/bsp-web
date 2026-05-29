'use client'

import { usePathname } from 'next/navigation'
import Link from 'next/link'
import { useAuthStore } from '@/stores/authStore'
import { cn } from '@/lib/utils'

const navGroups = [
  { label: '游戏', items: [
    { icon: '🏠', label: '首页', path: '/dashboard' },
    { icon: '🎮', label: '快速匹配', path: '/match' },
    { icon: '⚔️', label: '1v1对战', path: '/duel' },
    { icon: '🏠', label: '房间大厅', path: '/rooms' },
    { icon: '🖥', label: '服务器', path: '/servers' },
  ]},
  { label: '社交', items: [
    { icon: '👥', label: '好友', path: '/friends' },
    { icon: '💬', label: '聊天', path: '/chat' },
  ]},
  { label: '数据', items: [
    { icon: '📊', label: '排行榜', path: '/leaderboard' },
    { icon: '🏆', label: '成就', path: '/achievements' },
  ]},
  { label: '娱乐', items: [
    { icon: '🎯', label: '小游戏', path: '/minigames' },
    { icon: '🛒', label: '皮肤市场', path: '/market' },
    { icon: '❤️', label: '背水公益', path: '/welfare' },
  ]},
  { label: '系统', items: [
    { icon: '⚙️', label: '设置', path: '/settings' },
  ]},
]

export function Sidebar() {
  const pathname = usePathname()
  const { user, logout } = useAuthStore()

  return (
    <aside className="w-[260px] h-screen bg-card border-r border-border flex flex-col shrink-0">
      {/* Logo */}
      <div className="px-6 py-6 pb-8">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-elevated border border-primary/30 flex items-center justify-center text-lg">🎮</div>
          <div>
            <p className="text-lg font-bold text-white">背水对战平台</p>
            <p className="text-[10px] text-surface-400 font-mono">BEISHUI</p>
          </div>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto px-4 space-y-1">
        {navGroups.map((group) => (
          <div key={group.label}>
            <p className="text-[10px] text-surface-500 uppercase tracking-wider px-2 py-2 mt-2">{group.label}</p>
            {group.items.map((item) => {
              const active = pathname === item.path || (item.path !== '/dashboard' && pathname.startsWith(item.path))
              return (
                <Link key={item.path} href={item.path}
                  className={cn(
                    'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-all duration-200',
                    active
                      ? 'bg-hover text-primary border-l-[3px] border-primary pl-[9px]'
                      : 'text-surface-300 hover:text-white hover:bg-hover border-l-[3px] border-transparent'
                  )}>
                  <span className="text-lg">{item.icon}</span>
                  <span>{item.label}</span>
                </Link>
              )
            })}
          </div>
        ))}
      </nav>

      {/* User */}
      <div className="px-4 py-4 mx-4 mb-6 bg-elevated rounded-md">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-surface font-bold">
            {user?.nickname?.[0] || 'D'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-white truncate">{user?.nickname || '用户'}</p>
            <div className="flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-primary" />
              <span className="text-xs text-primary">在线</span>
            </div>
          </div>
          <button onClick={logout} className="text-surface-400 hover:text-white text-lg">&times;</button>
        </div>
      </div>
    </aside>
  )
}
