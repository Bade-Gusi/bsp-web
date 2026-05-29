'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import { motion } from 'framer-motion'

const GRID = 20
const CELL = 20
const TICK_BASE = 150

type Dir = 'UP' | 'DOWN' | 'LEFT' | 'RIGHT'
interface Point { x: number; y: number }

const INITIAL: Point[] = [{ x: 10, y: 10 }, { x: 9, y: 10 }, { x: 8, y: 10 }]

export default function SnakePage() {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const [snake, setSnake] = useState<Point[]>(INITIAL)
  const [food, setFood] = useState<Point>({ x: 15, y: 10 })
  const [dir, setDir] = useState<Dir>('RIGHT')
  const [nextDir, setNextDir] = useState<Dir>('RIGHT')
  const [score, setScore] = useState(0)
  const [level, setLevel] = useState(1)
  const [gameOver, setGameOver] = useState(false)
  const [paused, setPaused] = useState(false)
  const [started, setStarted] = useState(false)
  const [highScore, setHighScore] = useState(0)
  const dirRef = useRef<Dir>('RIGHT')
  const snakeRef = useRef<Point[]>(INITIAL)

  useEffect(() => {
    const saved = localStorage.getItem('snake_highscore')
    if (saved) setHighScore(parseInt(saved))
  }, [])

  const spawnFood = useCallback((s: Point[]) => {
    const occupied = new Set(s.map(p => `${p.x},${p.y}`))
    let p: Point
    do { p = { x: Math.floor(Math.random() * GRID), y: Math.floor(Math.random() * GRID) } }
    while (occupied.has(`${p.x},${p.y}`))
    return p
  }, [])

  const reset = () => {
    const init = INITIAL.map(p => ({ ...p }))
    setSnake(init)
    snakeRef.current = init
    setDir('RIGHT')
    setNextDir('RIGHT')
    dirRef.current = 'RIGHT'
    setScore(0)
    setLevel(1)
    setGameOver(false)
    setPaused(false)
    setStarted(true)
    setFood(spawnFood(init))
  }

  const draw = useCallback((s: Point[], f: Point) => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.clearRect(0, 0, GRID * CELL, GRID * CELL)

    // Grid
    ctx.strokeStyle = '#1a1d23'
    ctx.lineWidth = 0.5
    for (let i = 0; i <= GRID; i++) {
      ctx.beginPath(); ctx.moveTo(i * CELL, 0); ctx.lineTo(i * CELL, GRID * CELL); ctx.stroke()
      ctx.beginPath(); ctx.moveTo(0, i * CELL); ctx.lineTo(GRID * CELL, i * CELL); ctx.stroke()
    }

    // Food
    ctx.fillStyle = '#ef4444'
    ctx.shadowColor = '#ef4444'
    ctx.shadowBlur = 8
    ctx.beginPath()
    ctx.arc(f.x * CELL + CELL / 2, f.y * CELL + CELL / 2, CELL / 2 - 2, 0, Math.PI * 2)
    ctx.fill()
    ctx.shadowBlur = 0

    // Snake
    s.forEach((p, i) => {
      const isHead = i === 0
      ctx.fillStyle = isHead ? '#22d97e' : '#166534'
      ctx.shadowColor = isHead ? '#22d97e' : 'transparent'
      ctx.shadowBlur = isHead ? 10 : 0
      const r = isHead ? 4 : 3
      ctx.beginPath()
      ctx.roundRect(p.x * CELL + 2, p.y * CELL + 2, CELL - 4, CELL - 4, r)
      ctx.fill()
    })
    ctx.shadowBlur = 0
  }, [])

  // Game loop
  useEffect(() => {
    if (!started || gameOver || paused) return
    const interval = Math.max(50, TICK_BASE - (level - 1) * 10)
    const timer = setInterval(() => {
      setSnake(prev => {
        const s = [...prev]
        dirRef.current = nextDir
        const head = { ...s[0] }
        switch (dirRef.current) {
          case 'UP': head.y -= 1; break
          case 'DOWN': head.y += 1; break
          case 'LEFT': head.x -= 1; break
          case 'RIGHT': head.x += 1; break
        }
        // Wall collision
        if (head.x < 0 || head.x >= GRID || head.y < 0 || head.y >= GRID) {
          setGameOver(true); return prev
        }
        // Self collision
        if (s.some(p => p.x === head.x && p.y === head.y)) {
          setGameOver(true); return prev
        }
        s.unshift(head)
        // Food
        const f = food
        if (head.x === f.x && head.y === f.y) {
          const newScore = score + 1
          setScore(newScore)
          if (newScore % 5 === 0) setLevel(l => l + 1)
          setFood(spawnFood(s))
        } else { s.pop() }
        snakeRef.current = s
        draw(s, food)
        return s
      })
    }, interval)
    return () => clearInterval(timer)
  }, [started, gameOver, paused, nextDir, level, food, score, spawnFood, draw])

  // Draw on state change
  useEffect(() => { if (started) draw(snake, food) }, [snake, food, started, draw])

  // Keyboard
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (!started || gameOver) return
      if (e.key === 'p' || e.key === 'P') { setPaused(p => !p); return }
      const opposite: Record<Dir, Dir> = { UP: 'DOWN', DOWN: 'UP', LEFT: 'RIGHT', RIGHT: 'LEFT' }
      const map: Record<string, Dir> = { ArrowUp: 'UP', ArrowDown: 'DOWN', ArrowLeft: 'LEFT', ArrowRight: 'RIGHT' }
      const d = map[e.key]
      if (d && opposite[d] !== dirRef.current) {
        e.preventDefault()
        setNextDir(d)
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [started, gameOver])

  // High score
  useEffect(() => {
    if (gameOver && score > highScore) {
      setHighScore(score)
      localStorage.setItem('snake_highscore', String(score))
    }
  }, [gameOver, score, highScore])

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}
      className="min-h-screen bg-surface p-6 flex flex-col items-center">
      <div className="flex items-center justify-between w-full max-w-[420px] mb-4">
        <h2 className="text-xl font-bold text-white">贪吃蛇</h2>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-surface-400">最高: <span className="text-primary font-bold">{highScore}</span></span>
          <span className="text-surface-400">得分: <span className="text-white font-bold">{score}</span></span>
          <span className="text-surface-400">等级: <span className="text-amber-400 font-bold">{level}</span></span>
        </div>
      </div>

      <div className="relative">
        <canvas ref={canvasRef} width={GRID * CELL} height={GRID * CELL}
          className="rounded-xl border border-border bg-[#0B0E0F]" />

        {/* Overlay */}
        {!started && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/60 rounded-xl">
            <p className="text-white text-lg font-bold mb-4">贪吃蛇</p>
            <button onClick={reset}
              className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
              开始游戏
            </button>
            <p className="text-xs text-surface-400 mt-4">方向键控制 · P 暂停</p>
          </div>
        )}
        {started && paused && !gameOver && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/60 rounded-xl">
            <p className="text-white text-lg font-bold">已暂停</p>
          </div>
        )}
        {gameOver && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/70 rounded-xl">
            <p className="text-red-400 text-lg font-bold mb-1">游戏结束</p>
            <p className="text-white text-sm mb-4">得分: {score}</p>
            <button onClick={reset}
              className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
              再来一局
            </button>
          </div>
        )}
      </div>

      {/* Mobile controls */}
      {started && !gameOver && (
        <div className="mt-6 grid grid-cols-3 gap-2 w-48">
          <div />
          <button onTouchStart={() => setNextDir('UP')} onClick={() => { if (dirRef.current !== 'DOWN') setNextDir('UP') }}
            className="bg-surface-800 rounded-lg py-3 text-white text-lg hover:bg-surface-700">↑</button>
          <div />
          <button onTouchStart={() => setNextDir('LEFT')} onClick={() => { if (dirRef.current !== 'RIGHT') setNextDir('LEFT') }}
            className="bg-surface-800 rounded-lg py-3 text-white text-lg hover:bg-surface-700">←</button>
          <button onTouchStart={() => setNextDir('DOWN')} onClick={() => { if (dirRef.current !== 'UP') setNextDir('DOWN') }}
            className="bg-surface-800 rounded-lg py-3 text-white text-lg hover:bg-surface-700">↓</button>
          <button onTouchStart={() => setNextDir('RIGHT')} onClick={() => { if (dirRef.current !== 'LEFT') setNextDir('RIGHT') }}
            className="bg-surface-800 rounded-lg py-3 text-white text-lg hover:bg-surface-700">→</button>
        </div>
      )}
    </motion.div>
  )
}
