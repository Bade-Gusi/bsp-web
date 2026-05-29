'use client'

import { useState, useEffect, useCallback, useRef } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

const EMOJIS = ['🐶', '🐱', '🐸', '🦊', '🐻', '🐼', '🐨', '🦁']
const GRID_SIZE = 16 // 4x4

function shuffleArray<T>(arr: T[]): T[] {
  const a = [...arr]
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]]
  }
  return a
}

interface Card {
  id: number
  emoji: string
  flipped: boolean
  matched: boolean
}

export default function MemoryPage() {
  const [cards, setCards] = useState<Card[]>([])
  const [flippedIds, setFlippedIds] = useState<number[]>([])
  const [matched, setMatched] = useState(0)
  const [moves, setMoves] = useState(0)
  const [timeLeft, setTimeLeft] = useState(60)
  const [gameState, setGameState] = useState<'idle' | 'playing' | 'won' | 'lost'>('idle')
  const [bestScore, setBestScore] = useState<number | null>(null)
  const processing = useRef(false)
  const timerRef = useRef<NodeJS.Timeout | null>(null)

  useEffect(() => {
    const saved = localStorage.getItem('memory_best')
    if (saved) setBestScore(parseInt(saved))
  }, [])

  const initGame = useCallback(() => {
    const deck: Card[] = shuffleArray(
      [...EMOJIS, ...EMOJIS].map((emoji, i) => ({ id: i, emoji, flipped: false, matched: false }))
    )
    setCards(deck)
    setFlippedIds([])
    setMatched(0)
    setMoves(0)
    setTimeLeft(60)
    setGameState('playing')
    processing.current = false
  }, [])

  useEffect(() => {
    if (gameState !== 'playing') return
    timerRef.current = setInterval(() => {
      setTimeLeft(t => {
        if (t <= 1) { setGameState('lost'); return 0 }
        return t - 1
      })
    }, 1000)
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [gameState])

  const handleFlip = (id: number) => {
    if (processing.current || gameState !== 'playing') return
    const card = cards.find(c => c.id === id)
    if (!card || card.flipped || card.matched) return

    const newCards = cards.map(c => c.id === id ? { ...c, flipped: true } : c)
    const newFlipped = [...flippedIds, id]
    setCards(newCards)
    setFlippedIds(newFlipped)

    if (newFlipped.length === 2) {
      setMoves(m => m + 1)
      processing.current = true
      const [first, second] = newFlipped
      const cardA = newCards.find(c => c.id === first)!
      const cardB = newCards.find(c => c.id === second)!

      if (cardA.emoji === cardB.emoji) {
        setTimeout(() => {
          setCards(c => c.map(cc => cc.id === first || cc.id === second ? { ...cc, matched: true } : cc))
          setFlippedIds([])
          const newMatched = matched + 1
          setMatched(newMatched)
          if (newMatched === 8) {
            setGameState('won')
            if (bestScore === null || moves + 1 < bestScore) {
              setBestScore(moves + 1)
              localStorage.setItem('memory_best', String(moves + 1))
            }
          }
          processing.current = false
        }, 500)
      } else {
        setTimeout(() => {
          setCards(c => c.map(cc => cc.id === first || cc.id === second ? { ...cc, flipped: false } : cc))
          setFlippedIds([])
          processing.current = false
        }, 800)
      }
    }
  }

  // Win/loss check effect
  useEffect(() => {
    if (gameState === 'won' && bestScore !== null) {
      if (moves < bestScore) {
        setBestScore(moves)
        localStorage.setItem('memory_best', String(moves))
      }
    }
  }, [gameState, moves, bestScore])

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}
      className="min-h-screen bg-surface p-6 flex flex-col items-center">
      <div className="flex items-center justify-between w-full max-w-[420px] mb-4">
        <h2 className="text-xl font-bold text-white">记忆翻牌</h2>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-surface-400">最佳: <span className="text-primary font-bold">{bestScore ?? '-'}</span></span>
          <span className="text-surface-400">步数: <span className="text-white font-bold">{moves}</span></span>
          <span className={'text-sm font-bold ' + (timeLeft <= 15 ? 'text-red-400' : 'text-surface-400')}>
            {timeLeft}s
          </span>
        </div>
      </div>

      {gameState === 'idle' ? (
        <div className="flex flex-col items-center justify-center py-20">
          <p className="text-5xl mb-4">🧠</p>
          <p className="text-white text-lg font-bold mb-2">记忆翻牌</p>
          <p className="text-surface-400 text-sm mb-6">在60秒内匹配所有卡片</p>
          <button onClick={initGame}
            className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
            开始游戏
          </button>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-4 gap-3 mb-6">
            {cards.map((card, i) => (
              <motion.button key={card.id} onClick={() => handleFlip(card.id)}
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: i * 0.03 }}
                className={'w-16 h-20 sm:w-20 sm:h-24 rounded-xl flex items-center justify-center text-2xl sm:text-3xl transition-all duration-300 ' +
                  (card.matched
                    ? 'bg-primary/10 border border-primary/30 cursor-default'
                    : card.flipped
                      ? 'bg-card border border-border cursor-pointer'
                      : 'bg-surface-800 border border-border hover:bg-surface-700 cursor-pointer')}
                whileHover={!card.flipped && !card.matched ? { scale: 1.05 } : {}}
                whileTap={!card.flipped && !card.matched ? { scale: 0.95 } : {}}
              >
                <AnimatePresence mode="wait">
                  {card.flipped || card.matched ? (
                    <motion.span key="emoji" initial={{ rotateY: 180, opacity: 0 }}
                      animate={{ rotateY: 0, opacity: 1 }} transition={{ duration: 0.2 }}>
                      {card.matched ? '✨' : card.emoji}
                    </motion.span>
                  ) : (
                    <motion.span key="back" initial={{ rotateY: -180, opacity: 0 }}
                      animate={{ rotateY: 0, opacity: 1 }} transition={{ duration: 0.2 }}
                      className="text-surface-500">?</motion.span>
                  )}
                </AnimatePresence>
              </motion.button>
            ))}
          </div>

          {/* Progress bar */}
          <div className="w-full max-w-[420px] h-1.5 bg-surface-800 rounded-full overflow-hidden mb-6">
            <motion.div className="h-full bg-gradient-to-r from-primary to-accent rounded-full"
              initial={{ width: 0 }} animate={{ width: `${(matched / 8) * 100}%` }}
              transition={{ duration: 0.3 }} />
          </div>

          {/* Game over overlays integrated */}
          {gameState === 'won' && (
            <motion.div initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }}
              className="text-center py-4">
              <p className="text-3xl mb-2">🎉</p>
              <p className="text-primary font-bold text-lg">恭喜通关！</p>
              <p className="text-surface-400 text-sm mb-3">用了 {moves} 步完成</p>
              <button onClick={initGame}
                className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
                再来一局
              </button>
            </motion.div>
          )}
          {gameState === 'lost' && (
            <motion.div initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 0.9 }}
              className="text-center py-4">
              <p className="text-3xl mb-2">⏰</p>
              <p className="text-red-400 font-bold text-lg">时间到！</p>
              <p className="text-surface-400 text-sm mb-3">匹配了 {matched}/8 对</p>
              <button onClick={initGame}
                className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
                重新开始
              </button>
            </motion.div>
          )}
        </>
      )}
    </motion.div>
  )
}
