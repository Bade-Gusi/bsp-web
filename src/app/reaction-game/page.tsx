'use client'

import { useState, useRef, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'

export default function ReactionGamePage() {
  const [state, setState] = useState<'ready' | 'waiting' | 'clickable' | 'result'>('ready')
  const [time, setTime] = useState<number | null>(null)
  const [best, setBest] = useState<number | null>(null)
  const [times, setTimes] = useState<number[]>([])
  const [msg, setMsg] = useState('点击区域开始...')
  const [msgColor, setMsgColor] = useState('text-surface-300')
  const timerRef = useRef<ReturnType<typeof setTimeout>>()
  const startRef = useRef(0)
  const clickableRef = useRef(false)

  useEffect(() => { try { const v = localStorage.getItem('reaction_best'); if (v) setBest(parseInt(v)) } catch {} }, [])

  const showResult = (ms: number) => {
    clickableRef.current = false
    setState('result'); setTime(ms)
    setTimes(prev => [...prev, ms].slice(-5))
    if (best === null || ms < best) { setBest(ms); localStorage.setItem('reaction_best', String(ms)) }
    const c = ms < 200 ? 'text-amber-400' : ms < 300 ? 'text-primary' : ms < 400 ? 'text-cyan-400' : 'text-surface-400'
    setMsgColor(c); setMsg('点击继续下一轮')
  }

  const handleClick = () => {
    if (state === 'ready') {
      setState('waiting'); setMsg('等待绿色...'); setMsgColor('text-red-400')
      timerRef.current = setTimeout(() => {
        clickableRef.current = true
        setState('clickable'); setMsg('点击！'); setMsgColor('text-primary')
        startRef.current = performance.now()
        timerRef.current = setTimeout(() => { if (clickableRef.current) showResult(5000) }, 5000)
      }, 2000 + Math.random() * 3000)
    } else if (state === 'waiting') {
      clearTimeout(timerRef.current)
      setState('ready'); setMsg('太早了！点击重新开始'); setMsgColor('text-red-400')
    } else if (state === 'clickable') {
      clearTimeout(timerRef.current); clickableRef.current = false; showResult(Math.round(performance.now() - startRef.current))
    } else {
      setState('ready'); setMsg('点击区域开始...'); setMsgColor('text-surface-300')
    }
  }

  const bg = { ready: 'bg-elevated', waiting: 'bg-red-500/20', clickable: 'bg-primary/20', result: 'bg-elevated' }
  const bd = { ready: 'border-border', waiting: 'border-red-500', clickable: 'border-primary', result: 'border-border' }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="flex items-center justify-center min-h-[80vh]">
      <Card variant="default" className="text-center p-8 w-full max-w-md">
        <h2 className="text-xl font-bold text-white mb-6">反应速度测试</h2>
        <motion.div onClick={handleClick} whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }}
          className={'w-56 h-56 mx-auto rounded-3xl flex flex-col items-center justify-center cursor-pointer select-none border-2 transition-colors ' + bg[state] + ' ' + bd[state]}>
          <p className={'text-xl font-bold mb-2 ' + msgColor}>{msg}</p>
          {state === 'result' && time !== null && <p className={'text-5xl font-bold ' + msgColor}>{time}<span className="text-lg">ms</span></p>}
        </motion.div>
        <p className="text-sm text-surface-400 mt-2">最佳: <span className="text-primary font-bold">{best ?? '--'}</span> ms</p>
        {times.length > 0 && (
          <div className="mt-4 pt-4 border-t border-border">
            <p className="text-xs text-surface-400 mb-2">最近{times.length}次测试</p>
            <div className="flex justify-center gap-2 flex-wrap">
              {times.map((t, i) => (
                <span key={i} className={'text-sm font-bold px-2 ' + (t < 200 ? 'text-amber-400' : t < 300 ? 'text-primary' : t < 400 ? 'text-cyan-400' : 'text-surface-400')}>{t}ms</span>
              ))}
            </div>
          </div>
        )}
      </Card>
    </motion.div>
  )
}
