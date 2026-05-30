'use client'

import { useRef, useCallback } from 'react'

export function useRateLimit(delay = 1500) {
  const lastCall = useRef(0)

  const check = useCallback((): boolean => {
    const now = Date.now()
    if (now - lastCall.current < delay) return false
    lastCall.current = now
    return true
  }, [delay])

  const wrap = useCallback(<T extends (...args: any[]) => any>(fn: T): T => {
    return ((...args: any[]) => {
      if (!check()) return
      return fn(...args)
    }) as T
  }, [check])

  return { check, wrap }
}
