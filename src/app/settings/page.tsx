'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'

const tabs = ['常规', '游戏', '工具', '账号', '关于']

export default function SettingsPage() {
  const [tab, setTab] = useState('常规')
  const [gamePath, setGamePath] = useState('')
  const [launchArgs, setLaunchArgs] = useState('-novid -high')
  const [base64Input, setBase64Input] = useState('')
  const [base64Output, setBase64Output] = useState('')
  const [guessInput, setGuessInput] = useState('')
  const [guessMsg, setGuessMsg] = useState('')
  const [guessTarget] = useState(() => Math.floor(Math.random() * 100) + 1)
  const [guessAttempts, setGuessAttempts] = useState(0)

  const base64Encode = () => { try { setBase64Output(btoa(base64Input)) } catch { setBase64Output('编码失败') } }
  const base64Decode = () => { try { setBase64Output(atob(base64Input)) } catch { setBase64Output('解码失败，请检查输入是否为有效Base64') } }

  const guessNumber = () => {
    const n = parseInt(guessInput)
    if (isNaN(n)) { setGuessMsg('请输入数字'); return }
    setGuessAttempts(a => a + 1)
    if (n === guessTarget) setGuessMsg(`恭喜！猜对了！共猜了${guessAttempts + 1}次`)
    else if (n < guessTarget) setGuessMsg('猜小了')
    else setGuessMsg('猜大了')
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">设置</h2>

      <div className="flex gap-2 mb-6 flex-wrap">
        {tabs.map((t) => (
          <button key={t} onClick={() => setTab(t)}
            className={'px-5 py-2 rounded-md text-sm font-semibold transition-all ' + (tab === t ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300 hover:text-white')}>
            {t}
          </button>
        ))}
      </div>

      <div className="max-w-2xl space-y-4">
        {tab === '常规' && (
          <>
            <Card variant="default" className="flex items-center justify-between">
              <span className="text-sm text-white">开机自启</span>
              <div className="w-12 h-7 bg-elevated border border-border rounded-full cursor-pointer flex items-center px-1">
                <div className="w-5 h-5 bg-surface-400 rounded-full transition-all" />
              </div>
            </Card>
            <Card variant="default" className="flex items-center justify-between">
              <span className="text-sm text-white">最小化到托盘</span>
              <div className="w-12 h-7 bg-elevated border border-border rounded-full cursor-pointer flex items-center px-1">
                <div className="w-5 h-5 bg-surface-400 rounded-full transition-all" />
              </div>
            </Card>
            <Card variant="default">
              <Input label="下载目录" placeholder="选择下载目录" value="" onChange={() => {}} />
              <div className="flex gap-2 mt-3">
                <Button variant="ghost" size="sm">浏览</Button>
                <Button variant="ghost" size="sm">清理缓存</Button>
              </div>
            </Card>
          </>
        )}

        {tab === '游戏' && (
          <>
            <Card variant="default">
              <Input label="CS2 安装路径" placeholder="D:/Steam/steamapps/common/Counter-Strike Global Offensive" value={gamePath} onChange={e => setGamePath(e.target.value)} />
              <div className="flex gap-2 mt-3">
                <Button variant="ghost" size="sm">浏览</Button>
                <Button variant="ghost" size="sm">自动检测</Button>
              </div>
            </Card>
            <Card variant="default">
              <Input label="启动参数" placeholder="-novid -high -tickrate 128" value={launchArgs} onChange={e => setLaunchArgs(e.target.value)} />
              <Button variant="secondary" size="sm" className="mt-3">保存参数</Button>
            </Card>
          </>
        )}

        {tab === '工具' && (
          <>
            <Card variant="default">
              <p className="text-sm text-white mb-2 font-semibold">小游戏</p>
              <div className="flex gap-2 flex-wrap">
                <Link href="/minigames/tictactoe"><Button variant="ghost" size="sm">井字棋</Button></Link>
                <Link href="/minigames/snake"><Button variant="ghost" size="sm">贪吃蛇</Button></Link>
                <Link href="/minigames/memory"><Button variant="ghost" size="sm">记忆翻牌</Button></Link>
                <Button variant="ghost" size="sm">猜数字</Button>
              </div>
            </Card>

            {guessMsg && (
              <Card variant="default" className="text-sm">
                <p className={guessMsg.includes('恭喜') ? 'text-primary' : 'text-surface-300'}>{guessMsg}</p>
                {guessMsg.includes('恭喜') && (
                  <Button variant="ghost" size="sm" className="mt-2" onClick={() => { setGuessMsg(''); setGuessAttempts(0) }}>重新开始</Button>
                )}
              </Card>
            )}
            {!guessMsg && (
              <Card variant="default">
                <p className="text-sm text-white mb-2">猜数字（1-100）</p>
                <div className="flex gap-2">
                  <input value={guessInput} onChange={e => setGuessInput(e.target.value)} placeholder="输入数字" className="flex-1 bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
                  <Button size="sm" onClick={guessNumber}>猜</Button>
                </div>
                {guessMsg && <p className="text-xs text-surface-300 mt-2">{guessMsg}</p>}
                <p className="text-xs text-surface-500 mt-1">已猜: {guessAttempts} 次</p>
              </Card>
            )}

            <Card variant="default">
              <p className="text-sm text-white mb-2 font-semibold">编码工具</p>
              <textarea value={base64Input} onChange={e => setBase64Input(e.target.value)} placeholder="输入文本" rows={3}
                className="w-full bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary resize-none" />
              <div className="flex gap-2 mt-2">
                <Button size="sm" onClick={base64Encode}>编码</Button>
                <Button variant="ghost" size="sm" onClick={base64Decode}>解码</Button>
              </div>
              {base64Output && (
                <textarea value={base64Output} readOnly rows={3}
                  className="w-full bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white mt-2 resize-none" />
              )}
            </Card>

            <Card variant="default">
              <p className="text-sm text-white mb-2 font-semibold">网络</p>
              <Button variant="ghost" size="sm">检查 IPv6 状态</Button>
            </Card>
          </>
        )}

        {tab === '账号' && (
          <>
            <Card variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white">密码修改</p>
                <p className="text-xs text-surface-400 mt-0.5">功能开发中</p>
              </div>
              <Button variant="ghost" size="sm">修改</Button>
            </Card>
            <Card variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white">Steam 绑定</p>
                <p className="text-xs text-surface-400 mt-0.5">已绑定</p>
              </div>
              <Button variant="ghost" size="sm">解绑</Button>
            </Card>
            <Card variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white">实名认证</p>
                <p className="text-xs text-surface-400 mt-0.5">未认证</p>
              </div>
              <Button variant="ghost" size="sm">认证</Button>
            </Card>
            <Card variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white">登录历史</p>
                <p className="text-xs text-surface-400 mt-0.5">查看最近登录记录</p>
              </div>
              <Button variant="ghost" size="sm">查看</Button>
            </Card>
            <Card variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white">设备管理</p>
                <p className="text-xs text-surface-400 mt-0.5">管理已授权的设备</p>
              </div>
              <Button variant="ghost" size="sm">管理</Button>
            </Card>
          </>
        )}

        {tab === '关于' && (
          <>
            <Card variant="default">
              <p className="text-sm text-white">背水对战平台</p>
              <p className="text-xs text-surface-400 mt-1">版本: v2.0.0 Aurora</p>
              <p className="text-xs text-surface-400 mt-0.5">网络状态: 已连接</p>
              <div className="flex gap-2 mt-4">
                <Button variant="secondary" size="sm">检查更新</Button>
              </div>
            </Card>
            <Card variant="default">
              <div className="flex gap-4">
                <Link href="/legal" className="text-xs text-surface-400 underline hover:text-primary">用户协议</Link>
                <Link href="/legal" className="text-xs text-surface-400 underline hover:text-primary">隐私政策</Link>
                <Link href="/legal" className="text-xs text-surface-400 underline hover:text-primary">平台声明</Link>
              </div>
            </Card>
          </>
        )}
      </div>
    </motion.div>
  )
}
