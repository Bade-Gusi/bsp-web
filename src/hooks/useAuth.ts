'use client'

import { useEffect } from 'react'
import { useAuthStore, authApi } from '@/stores/authStore'

export function useAuth() {
  const store = useAuthStore()

  useEffect(() => {
    store.loadFromStorage()
  }, [])

  const login = async (username: string, password: string) => {
    const data = await authApi.login(username, password)
    // Fetch full profile after login
    const profile = await authApi.getProfile(data.token)
    store.setAuth(data.token, profile)
  }

  const register = async (username: string, password: string, nickname: string) => {
    await authApi.register(username, password, nickname)
  }

  return {
    user: store.user,
    token: store.token,
    isAuthenticated: store.isAuthenticated,
    loading: store.loading,
    login,
    register,
    logout: store.logout,
  }
}
