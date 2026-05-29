'use client'

import { useState, useRef, useCallback, useEffect } from 'react'
import { motion } from 'framer-motion'

type Phase = 'idle' | 'waiting' | 'ready' | 'result' | 'early'

export default function ReactionPage() {
  const [phase, setPhase] = useState<Phase>('idle')
  const [message, setMessage] = useState('点击开始测试')
  const [color, setColor] = useState('bg-[#1A2E1F]')
  const [time, setTime] = useState(0)
  const [times, setTimes] = useState<number[]>([])
  const [best, setBest] = useState<number | null>(null)
  const [round, setRound] = useState(0)
  const waitRef = useRef<NodeJS.Timeout | null>(null)
  const startRef = useRef(0)
  const highScoreRef = useRef<number | null>(null)

  useEffect(() => {
    const saved = localStorage.getItem('reaction_best')
    if (saved) {
      const n = parseInt(saved)
      highScoreRef.current = n
      setBest(n)
    }
  }, [])

  const startRound = useCallback(() => {
    setPhase('waiting')
    setMessage('等待绿色...')
    setColor('bg-[#1A2E1F]')
    const delay = 2000 + Math.random() * 3000
    waitRef.current = setTimeout(() => {
      setPhase('ready')
      setMessage('点击！')
      setColor('bg-[#166534]')
      startRef.current = Date.now()
    }, delay)
  }, [])

  const handleClick = () => {
    if (phase === 'idle') {
      setRound(1)
      startRound()
      return
    }
    if (phase === 'waiting') {
      if (waitRef.current) clearTimeout(waitRef.current)
      setPhase('early')
      setMessage('太早了！点击重新开始')
      setColor('bg-[#7F1D1D]')
      return
    }
    if (phase === 'ready') {
      const ms = Date.now() - startRef.current
      setTime(ms)
      setTimes(prev => [...prev, ms])
      setPhase('result')

      const allTimes = [...times, ms]
      const newBest = allTimes.length === 1 ? ms : Math.min(...allTimes)
      if (highScoreRef.current === null || ms < highScoreRef.current) {
        highScoreRef.current = ms
        setBest(ms)
        localStorage.setItem('reaction_best', String(ms))
      }

      const avg = Math.round(allTimes.reduce((a, b) => a + b, 0) / allTimes.length)
      setMessage(`${ms}ms  |  平均 ${avg}ms`)
      setColor('bg-[#1A2E1F]')
      return
    }
    if (phase === 'result' || phase === 'early') {
      setRound(r => r + 1)
      startRound()
    }
  }

  useEffect(() => {
    return () => { if (waitRef.current) clearTimeout(waitRef.current) }
  }, [])

  const grade = (ms: number) => {
    if (ms < 150) return { label: '超神', color: 'text-purple-400' }
    if (ms < 200) return { label: '优秀', color: 'text-primary' }
    if (ms < 260) return { label: '良好', color: 'text-blue-400' }
    if (ms < 320) return { label: '一般', color: 'text-amber-400' }
    return { label: '较慢', color: 'text-red-400' }
  }

  const lastGrade = phase === 'result' && times.length > 0 ? grade(times[times.length - 1]) : null

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}
      className="min-h-screen bg-surface p-6 flex flex-col items-center">
      <div className="flex items-center justify-between w-full max-w-md mb-4">
        <h2 className="text-xl font-bold text-white">反应力测试</h2>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-surface-400">最佳: <span className="text-primary font-bold">{best ? `${best}ms` : '-'}</span></span>
          <span className="text-surface-400">第 <span className="text-white font-bold">{round}</span> 轮</span>
        </div>
      </div>

      <button onClick={handleClick} className="w-full max-w-md aspect-[4/3] rounded-2xl transition-all duration-200 outline-none"
        style={{ backgroundColor: color === 'bg-[#1A2E1F]' ? '#1A2E1F' : color === 'bg-[#166534]' ? '#166534' : color === 'bg-[#7F1D1D]' ? '#7F1D1D' : '#1A2E1F' }}>
        <div className="flex flex-col items-center justify-center h-full px-6">
          <p className="text-white text-xl font-bold mb-2">{message}</p>
          {phase === 'result' && lastGrade && (
            <motion.p initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}
              className={`text-2xl font-bold ${lastGrade.color}`}>
              {lastGrade.label}
            </motion.p>
          )}
          {phase === 'ready' && (
            <motion.p initial={{ scale: 0 }} animate={{ scale: [1, 1.2, 1] }}
              transition={{ repeat: Infinity, duration: 1 }}
              className="text-4xl">👆</motion.p>
          )}
        </div>
      </button>

      {times.length > 0 && (
        <div className="mt-6 w-full max-w-md">
          <p className="text-sm text-surface-400 mb-2">历史记录</p>
          <div className="flex flex-wrap gap-2">
            {times.map((t, i) => (
              <span key={i} className={'px-2.5 py-1 rounded-md text-xs font-mono ' +
                (t === Math.min(...times) ? 'bg-primary/20 text-primary' : 'bg-surface-800 text-surface-300')}>
                {t}ms
              </span>
            ))}
          </div>
          <div className="mt-3 flex gap-3 text-xs text-surface-400">
            <span>平均: {Math.round(times.reduce((a, b) => a + b, 0) / times.length)}ms</span>
            <span>最快: {Math.min(...times)}ms</span>
            <span>最慢: {Math.max(...times)}ms</span>
          </div>
        </div>
      )}
    </motion.div>
  )
}
