'use client'

import { motion } from 'framer-motion'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'

const logs = [
  { ver: 'v2.0.0', date: '2026-05-30', changes: [
    'Web 版正式发布，30个页面全部完成',
    '完整的用户认证系统（登录/注册/Steam/忘记密码）',
    '好友系统（搜索/添加/删除/私聊/邀请对战）',
    '即时聊天（SignalR 实时消息）',
    '语音房间（创建/加入/离开）',
    '快速匹配 + 1v1 单挑',
    '房间系统 + 服务器管理',
    '排行榜 + 成就系统',
    '皮肤市场 + 背水公益',
    '小游戏合集（井字棋/贪吃蛇/记忆翻牌/反应测试）',
    '管理员广播系统',
    '反作弊状态监控',
    '深色主题 + 粒子动画',
  ]},
  { ver: 'v1.0.0', date: '2026-05-11', changes: [
    'WPF 桌面客户端发布',
    'CS2 对战平台基础功能',
    '反作弊系统',
  ]},
]

export default function ChangelogPage() {
  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="max-w-2xl mx-auto">
      <h2 className="text-xl font-bold text-white mb-6">更新日志</h2>
      <div className="space-y-6">
        {logs.map((log, i) => (
          <motion.div key={i} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.1 }}>
            <div className="flex items-center gap-3 mb-3">
              <span className="px-3 py-1 rounded-md bg-primary/20 text-primary text-sm font-bold">{log.ver}</span>
              <span className="text-xs text-surface-400">{log.date}</span>
            </div>
            <Card variant="default">
              <ul className="space-y-2">
                {log.changes.map((c, j) => (
                  <li key={j} className="text-sm text-surface-200 flex items-start gap-2">
                    <span className="text-primary mt-0.5">▸</span>
                    {c}
                  </li>
                ))}
              </ul>
            </Card>
          </motion.div>
        ))}
      </div>
      <div className="text-center mt-8">
        <Link href="/" className="text-sm text-primary hover:text-accent">返回首页</Link>
      </div>
    </motion.div>
  )
}
