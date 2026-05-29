'use client'

import { motion } from 'framer-motion'
import { Sidebar } from './Sidebar'
import { Header } from './Header'

interface AppShellProps { children: React.ReactNode; pageTitle: string }

export function AppShell({ children, pageTitle }: AppShellProps) {
  return (
    <div className="flex h-screen">
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <Header pageTitle={pageTitle} />
        <main className="flex-1 overflow-y-auto p-6 bg-surface">
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}>
            {children}
          </motion.div>
        </main>
      </div>
    </div>
  )
}
