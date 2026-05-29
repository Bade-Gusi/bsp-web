'use client'

import { motion } from 'framer-motion'
import { useState, useEffect } from 'react'
import { Card } from '@/components/ui/Card'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

const DEFAULT_ITEMS = [
  { title: '贫困山区助学计划', desc: '每场对局捐赠0.1元用于山区儿童教育', icon: '📚' },
  { title: '流浪动物救助', desc: '与动物保护协会合作', icon: '🐾' },
  { title: '绿色地球种树', desc: '在沙漠地区种植树木', icon: '🌳' },
]

export default function WelfarePage() {
  const [items, setItems] = useState(DEFAULT_ITEMS)
  const [loading, setLoading] = useState(true)
  const { user } = useAuthStore()

  useEffect(() => {
    async function load() {
      try {
        const data = await api.getWelfare()
        if (Array.isArray(data) && data.length > 0) {
          setItems(data.map((w: any) => ({
            title: w.title || '',
            desc: w.description || '',
            icon: w.icon || '❤️',
          })))
        }
      } catch {}
      setLoading(false)
    }
    load()
  }, [])

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="text-center mb-8">
        <p className="text-4xl mb-3">❤️</p>
        <h2 className="text-xl font-bold text-white">背水公益</h2>
        <p className="text-sm text-surface-400 mt-1">玩游戏，做公益</p>
      </div>

      {loading ? (
        <div className="flex justify-center py-20">
          <div className="w-8 h-8 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {items.map((item, i) => (
            <motion.div key={i} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.1 }}>
              <Card variant="hover">
                <p className="text-4xl mb-4">{item.icon}</p>
                <h3 className="text-lg font-bold text-white mb-2">{item.title}</h3>
                <p className="text-sm text-surface-300">{item.desc}</p>
              </Card>
            </motion.div>
          ))}
        </div>
      )}
    </motion.div>
  )
}
