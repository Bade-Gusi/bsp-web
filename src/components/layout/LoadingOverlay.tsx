'use client'

import { motion } from 'framer-motion'

export function LoadingOverlay() {
  return (
    <div className="fixed inset-0 bg-surface z-50 flex items-center justify-center">
      <div className="text-center">
        <div className="w-16 h-16 mx-auto mb-5 rounded-xl bg-card border border-border flex items-center justify-center">
          <div className="w-10 h-10 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
        </div>
        <motion.p initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="text-white text-base font-semibold">加载中...</motion.p>
        <p className="text-surface-400 text-xs mt-2">背水对战平台</p>
        <div className="w-64 h-1 bg-elevated rounded-full mx-auto mt-6 overflow-hidden">
          <motion.div initial={{ width: '0%' }} animate={{ width: '60%' }} transition={{ duration: 2, repeat: Infinity }} className="h-full bg-primary rounded-full" />
        </div>
      </div>
    </div>
  )
}
