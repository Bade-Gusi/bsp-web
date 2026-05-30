'use client'

import { motion } from 'framer-motion'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'

export default function MatchResultPage() {
  const params = useSearchParams()
  const score = params.get('score') || '16:12'
  const win = params.get('win') !== 'false'
  const map = params.get('map') || 'de_dust2'

  const ctPlayers = ['队友1', '队友2', '队友3', '队友4', '队友5']
  const tPlayers = ['对手1', '对手2', '对手3', '对手4', '对手5']

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="text-center mb-8">
        <motion.p initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', stiffness: 200 }}
          className={'text-5xl font-bold mb-2 ' + (win ? 'text-primary' : 'text-red-400')}>
          {win ? '胜利' : '失败'}
        </motion.p>
        <p className="text-2xl text-white font-bold">{score}</p>
        <p className="text-surface-400 text-sm mt-1">{map}</p>
      </div>

      <div className="grid grid-cols-2 gap-6">
        <Card variant="default">
          <h3 className="text-sm font-bold text-primary mb-3">Counter-Terrorists</h3>
          {ctPlayers.map((p, i) => (
            <div key={i} className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-hover text-sm">
              <span className="text-white">{p}</span>
              <span className="text-surface-400">{win ? '+25' : '-18'}</span>
            </div>
          ))}
        </Card>
        <Card variant="default">
          <h3 className="text-sm font-bold text-red-400 mb-3">Terrorists</h3>
          {tPlayers.map((p, i) => (
            <div key={i} className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-hover text-sm">
              <span className="text-white">{p}</span>
              <span className="text-surface-400">{win ? '-18' : '+25'}</span>
            </div>
          ))}
        </Card>
      </div>

      <div className="flex justify-center gap-4 mt-8">
        <Link href="/match"><Button>再来一局</Button></Link>
        <Link href="/dashboard"><Button variant="ghost">返回首页</Button></Link>
      </div>
    </motion.div>
  )
}
