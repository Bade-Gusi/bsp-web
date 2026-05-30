'use client'

import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

interface ToastItem { id: number; message: string; type: 'success' | 'error' | 'info' }
let toastId = 0
let addToastFn: ((msg: string, type: 'success' | 'error' | 'info') => void) | null = null

export function toast(message: string, type: 'success' | 'error' | 'info' = 'info') {
  if (addToastFn) addToastFn(message, type)
}

export function ToastContainer() {
  const [items, setItems] = useState<ToastItem[]>([])

  const add = useCallback((message: string, type: 'success' | 'error' | 'info') => {
    const id = ++toastId
    setItems(prev => [...prev, { id, message, type }])
    setTimeout(() => setItems(prev => prev.filter(i => i.id !== id)), 3000)
  }, [])

  useEffect(() => { addToastFn = add; return () => { addToastFn = null } }, [add])

  const colors = { success: 'border-primary text-primary', error: 'border-red-500 text-red-400', info: 'border-blue-500 text-blue-400' }

  return (
    <div className="fixed top-4 right-4 z-[9999] space-y-2">
      <AnimatePresence>
        {items.map(item => (
          <motion.div key={item.id}
            initial={{ opacity: 0, x: 100, scale: 0.9 }}
            animate={{ opacity: 1, x: 0, scale: 1 }}
            exit={{ opacity: 0, x: 100, scale: 0.9 }}
            transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className={'bg-card border-l-4 rounded-md px-4 py-3 shadow-surface text-sm ' + colors[item.type]}>
            {item.message}
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  )
}
