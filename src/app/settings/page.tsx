'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import Toggle from '@/components/ui/Toggle'
import { Modal } from '@/components/ui/Modal'

type TabKey = 'general' | 'display' | 'notifications' | 'account' | 'about'

const TABS: { id: TabKey; label: string; icon: string }[] = [
  { id: 'general', label: '通用', icon: '⚙️' },
  { id: 'display', label: '显示', icon: '🎨' },
  { id: 'notifications', label: '通知', icon: '🔔' },
  { id: 'account', label: '账号', icon: '👤' },
  { id: 'about', label: '关于', icon: 'ℹ️' },
]

const LANGUAGES = [
  { value: 'zh-CN', label: '简体中文' },
  { value: 'en', label: 'English' },
  { value: 'ja', label: '日本語' },
]

const THEMES = [
  { value: 'dark', label: '深色' },
  { value: 'light', label: '浅色' },
  { value: 'system', label: '跟随系统' },
]

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState<TabKey>('general')

  // General
  const [language, setLanguage] = useState('zh-CN')
  const [autoStart, setAutoStart] = useState(false)
  const [autoUpdate, setAutoUpdate] = useState(true)

  // Display
  const [theme, setTheme] = useState('dark')
  const [animations, setAnimations] = useState(true)
  const [animSpeed, setAnimSpeed] = useState('normal')

  // Notifications
  const [matchNotifs, setMatchNotifs] = useState(true)
  const [friendNotifs, setFriendNotifs] = useState(true)
  const [systemNotifs, setSystemNotifs] = useState(true)
  const [dnd, setDnd] = useState(false)

  // Account
  const [birthMonth, setBirthMonth] = useState('')
  const [birthDay, setBirthDay] = useState('')
  const [showChangePw, setShowChangePw] = useState(false)
  const [oldPw, setOldPw] = useState('')
  const [newPw, setNewPw] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [pwError, setPwError] = useState('')

  // Load from localStorage
  useEffect(() => {
    try {
      const saved = localStorage.getItem('bsp_settings')
      if (saved) {
        const s = JSON.parse(saved)
        setLanguage(s.language || 'zh-CN')
        setAutoStart(s.autoStart || false)
        setAutoUpdate(s.autoUpdate ?? true)
        setTheme(s.theme || 'dark')
        setAnimations(s.animations ?? true)
        setAnimSpeed(s.animSpeed || 'normal')
        setMatchNotifs(s.matchNotifs ?? true)
        setFriendNotifs(s.friendNotifs ?? true)
        setSystemNotifs(s.systemNotifs ?? true)
        setDnd(s.dnd || false)
        setBirthMonth(s.birthMonth || '')
        setBirthDay(s.birthDay || '')
      }
    } catch {}
  }, [])

  const saveSettings = (updates: Record<string, any>) => {
    try {
      const current = JSON.parse(localStorage.getItem('bsp_settings') || '{}')
      localStorage.setItem('bsp_settings', JSON.stringify({ ...current, ...updates }))
    } catch {}
  }

  const TabButton = ({ tab: t }: { tab: typeof TABS[0] }) => (
    <button onClick={() => setActiveTab(t.id)}
      className={'flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-all ' +
        (activeTab === t.id ? 'bg-primary/10 text-primary border border-primary/20' : 'text-surface-400 hover:text-white hover:bg-hover')}>
      <span className="text-lg">{t.icon}</span>
      {t.label}
    </button>
  )

  const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
    <div className="mb-6">
      <p className="text-sm font-bold text-white mb-3 pb-2 border-b border-border">{title}</p>
      <div className="space-y-4">{children}</div>
    </div>
  )

  const SettingRow = ({ label, desc, children }: { label: string; desc?: string; children: React.ReactNode }) => (
    <div className="flex items-center justify-between">
      <div>
        <p className="text-sm text-white">{label}</p>
        {desc && <p className="text-xs text-surface-400 mt-0.5">{desc}</p>}
      </div>
      {children}
    </div>
  )

  const Select = ({ value, onChange, options }: { value: string; onChange: (v: string) => void; options: { value: string; label: string }[] }) => (
    <select value={value} onChange={e => onChange(e.target.value)}
      className="bg-elevated border border-border rounded-md px-3 py-1.5 text-sm text-white outline-none focus:border-primary">
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  )

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="min-h-screen bg-surface p-6">
      <h2 className="text-xl font-bold text-white mb-6">设置</h2>

      <div className="flex gap-6">
        {/* Sidebar tabs */}
        <div className="w-48 shrink-0 space-y-1">
          {TABS.map(t => <TabButton key={t.id} tab={t} />)}
        </div>

        {/* Content */}
        <div className="flex-1">
          <Card variant="default" className="p-6 min-h-[500px]">
            {/* General */}
            {activeTab === 'general' && (
              <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
                <Section title="语言">
                  <SettingRow label="界面语言" desc="更改语言后需刷新页面">
                    <Select value={language} onChange={v => { setLanguage(v); saveSettings({ language: v }) }} options={LANGUAGES} />
                  </SettingRow>
                </Section>
                <Section title="启动">
                  <SettingRow label="开机自启" desc="系统启动时自动运行">
                    <Toggle checked={autoStart} onChange={v => { setAutoStart(v); saveSettings({ autoStart: v }) }} />
                  </SettingRow>
                  <SettingRow label="自动更新" desc="启动时自动检查更新">
                    <Toggle checked={autoUpdate} onChange={v => { setAutoUpdate(v); saveSettings({ autoUpdate: v }) }} />
                  </SettingRow>
                </Section>
                <Section title="存储">
                  <SettingRow label="缓存">
                    <Button size="sm" variant="ghost">清理缓存</Button>
                  </SettingRow>
                </Section>
              </motion.div>
            )}

            {/* Display */}
            {activeTab === 'display' && (
              <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
                <Section title="主题">
                  <SettingRow label="界面主题">
                    <Select value={theme} onChange={v => { setTheme(v); saveSettings({ theme: v }) }} options={THEMES} />
                  </SettingRow>
                </Section>
                <Section title="动画">
                  <SettingRow label="启用动画">
                    <Toggle checked={animations} onChange={v => { setAnimations(v); saveSettings({ animations: v }) }} />
                  </SettingRow>
                  <SettingRow label="动画速度">
                    <Select value={animSpeed} onChange={v => { setAnimSpeed(v); saveSettings({ animSpeed: v }) }}
                      options={[{ value: 'slow', label: '慢' }, { value: 'normal', label: '正常' }, { value: 'fast', label: '快' }]} />
                  </SettingRow>
                </Section>
              </motion.div>
            )}

            {/* Notifications */}
            {activeTab === 'notifications' && (
              <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
                <Section title="通知">
                  <SettingRow label="比赛通知" desc="匹配成功、比赛开始等通知">
                    <Toggle checked={matchNotifs} onChange={v => { setMatchNotifs(v); saveSettings({ matchNotifs: v }) }} />
                  </SettingRow>
                  <SettingRow label="好友通知" desc="好友请求、上线提醒">
                    <Toggle checked={friendNotifs} onChange={v => { setFriendNotifs(v); saveSettings({ friendNotifs: v }) }} />
                  </SettingRow>
                  <SettingRow label="系统通知" desc="公告、更新等系统消息">
                    <Toggle checked={systemNotifs} onChange={v => { setSystemNotifs(v); saveSettings({ systemNotifs: v }) }} />
                  </SettingRow>
                </Section>
                <Section title="免打扰">
                  <SettingRow label="开启免打扰" desc="暂停所有通知">
                    <Toggle checked={dnd} onChange={v => { setDnd(v); saveSettings({ dnd: v }) }} />
                  </SettingRow>
                </Section>
              </motion.div>
            )}

            {/* Account */}
            {activeTab === 'account' && (
              <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
                <Section title="生日">
                  <div className="flex gap-2">
                    <select value={birthMonth} onChange={e => { setBirthMonth(e.target.value); saveSettings({ birthMonth: e.target.value }) }}
                      className="bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white outline-none focus:border-primary">
                      <option value="">月份</option>
                      {Array.from({ length: 12 }, (_, i) => i + 1).map(m => (
                        <option key={m} value={String(m).padStart(2, '0')}>{m}月</option>
                      ))}
                    </select>
                    <select value={birthDay} onChange={e => { setBirthDay(e.target.value); saveSettings({ birthDay: e.target.value }) }}
                      className="bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white outline-none focus:border-primary">
                      <option value="">日期</option>
                      {Array.from({ length: 31 }, (_, i) => i + 1).map(d => (
                        <option key={d} value={String(d).padStart(2, '0')}>{d}日</option>
                      ))}
                    </select>
                    <Button size="sm" variant="secondary">保存</Button>
                  </div>
                </Section>
                <Section title="安全">
                  <SettingRow label="修改密码">
                    <Button size="sm" variant="ghost" onClick={() => setShowChangePw(true)}>修改</Button>
                  </SettingRow>
                </Section>
              </motion.div>
            )}

            {/* About */}
            {activeTab === 'about' && (
              <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
                <Section title="背水对战平台">
                  <div className="space-y-3">
                    {[
                      { label: '版本', value: 'v2.0 Aurora' },
                      { label: '运行环境', value: 'Web' },
                      { label: '框架', value: 'Next.js 14 / React 18' },
                      { label: '后端', value: '.NET 9' },
                    ].map((item, i) => (
                      <div key={i} className="flex items-center justify-between py-1">
                        <span className="text-sm text-surface-400">{item.label}</span>
                        <span className="text-sm text-white font-mono">{item.value}</span>
                      </div>
                    ))}
                  </div>
                </Section>
                <Section title="法律">
                  <div className="flex gap-3">
                    <Button variant="ghost" size="sm">用户协议</Button>
                    <Button variant="ghost" size="sm">隐私政策</Button>
                    <Button variant="ghost" size="sm">免责声明</Button>
                  </div>
                </Section>
              </motion.div>
            )}
          </Card>
        </div>
      </div>

      {/* Change password modal */}
      <Modal isOpen={showChangePw} onClose={() => { setShowChangePw(false); setPwError(''); setOldPw(''); setNewPw(''); setConfirmPw('') }} title="修改密码" size="sm">
        <div className="space-y-4">
          <Input type="password" label="当前密码" placeholder="输入当前密码" value={oldPw} onChange={e => setOldPw(e.target.value)} />
          <Input type="password" label="新密码" placeholder="至少6位" value={newPw} onChange={e => setNewPw(e.target.value)} />
          <Input type="password" label="确认新密码" placeholder="再次输入新密码" value={confirmPw} onChange={e => setConfirmPw(e.target.value)} />
          {pwError && <p className="text-sm text-red-400">{pwError}</p>}
          <Button className="w-full">确认修改</Button>
        </div>
      </Modal>
    </motion.div>
  )
}
