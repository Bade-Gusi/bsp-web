'use client'

import { useState, useRef, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'

export default function ReactionGamePage() {
  const [state, setState] = useState<'idle' | 'waiting' | 'ready' | 'result'>('idle')
  const [time, setTime] = useState<number | null>(null)
  const [best, setBest] = useState<number | null>(null)
  const [attempts, setAttempts] = useState(0)
  const [avg, setAvg] = useState<number | null>(null)
  const timerRef = useRef<ReturnType<typeof setTimeout>>()
  const startRef = useRef(0)
  const timesRef = useRef<number[]>([])

  const startWaiting = () => {
    setState('waiting')
    setTime(null)
    const delay = 1000 + Math.random() * 3000
    timerRef.current = setTimeout(() => {
      setState('ready')
      startRef.current = performance.now()
    }, delay)
  }

  const handleClick = useCallback(() => {
    if (state === 'idle') { startWaiting(); return }
    if (state === 'waiting') {
      clearTimeout(timerRef.current)
      setState('idle')
      return
    }
    if (state === 'ready') {
      const elapsed = performance.now() - startRef.current
      const ms = Math.round(elapsed)
      setTime(ms)
      setAttempts(a => a + 1)
      timesRef.current.push(ms)
      const total = timesRef.current.reduce((a, b) => a + b, 0)
      const avgMs = Math.round(total / timesRef.current.length)
      setAvg(avgMs)
      if (best === null || ms < best) {
        setBest(ms)
        try { localStorage.setItem('reaction_best', String(ms)) } catch {}
      }
      setState('result')
      return
    }
    if (state === 'result') { startWaiting(); return }
  }, [state, best])

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="flex items-center justify-center min-h-[80vh]">
      <Card variant="default" className="text-center p-12 w-full max-w-md">
        <h2 className="text-xl font-bold text-white mb-6">反应测试</h2>

        <motion.div
          onClick={handleClick}
          whileHover={{ scale: 1.02 }}
          whileTap={{ scale: 0.98 }}
          className={'w-48 h-48 mx-auto rounded-2xl flex items-center justify-center cursor-pointer select-none transition-colors ' +
            (state === 'idle' ? 'bg-elevated border border-border' :
             state === 'waiting' ? 'bg-red-500/20 border border-red-500' :
             state === 'ready' ? 'bg-primary/20 border border-primary' :
             'bg-card border border-border')}
        >
          <p className={'text-lg font-bold ' + (
            state === 'idle' ? 'text-surface-300' :
            state === 'waiting' ? 'text-red-400' :
            state === 'ready' ? 'text-primary' :
            'text-white'
          )}>
            {state === 'idle' && '点击开始'}
            {state === 'waiting' && '等待绿色...'}
            {state === 'ready' && '点击！'}
            {state === 'result' && `${time}ms`}
          </p>
        </motion.div>

        {state === 'result' && time !== null && (
          <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="mt-6 space-y-1">
            <p className="text-sm">反应时间: <span className="text-primary font-bold text-lg">{time}ms</span></p>
            <p className="text-xs text-surface-400">最佳: {best}ms | 平均: {avg}ms | 次数: {attempts}</p>
          </motion.div>
        )}

        <p className="text-xs text-surface-500 mt-4">等待绿色后点击，测试你的反应速度</p>
      </Card>
    </motion.div>
  )
}
