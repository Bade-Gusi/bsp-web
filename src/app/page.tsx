'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'

export default function HomePage() {
  const router = useRouter()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  useEffect(() => {
    if (isAuthenticated) router.replace('/dashboard')
  }, [isAuthenticated, router])

  // 不能返回 null，否则静态导出会生成 404
  return (
    <div className="fixed inset-0 bg-surface z-50 flex items-center justify-center">
      <div className="w-16 h-16 mx-auto mb-5 rounded-xl bg-card border border-border flex items-center justify-center">
        <div className="w-10 h-10 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
      </div>
    </div>
  )
}
