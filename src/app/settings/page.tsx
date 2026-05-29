'use client'

import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'

export default function SettingsPage() {
  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">设置</h2>
      <Card variant="default">
        <p className="text-white">设置功能开发中</p>
      </Card>
    </motion.div>
  )
}
