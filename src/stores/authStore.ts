import { create } from 'zustand'

interface User {
  id: number; username: string; nickname: string; avatarUrl: string
  steamId: string | null; mmr: number; rankId: number
  winCount: number; loseCount: number; killCount: number; headshotCount: number
  totalGames: number; status: number; phone: string; email: string
  birthday: string | null; createdAt: string; lastLoginAt: string | null
}

interface AuthState {
  token: string | null
  user: User | null
  isAuthenticated: boolean
  loading: boolean
  setAuth: (token: string, user: User) => void
  logout: () => void
  loadFromStorage: () => void
  setLoading: (v: boolean) => void
}

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  isAuthenticated: false,
  loading: true,

  setAuth: (token, user) => {
    localStorage.setItem('bsp_token', token)
    localStorage.setItem('bsp_user', JSON.stringify(user))
    set({ token, user, isAuthenticated: true, loading: false })
  },

  logout: () => {
    localStorage.removeItem('bsp_token')
    localStorage.removeItem('bsp_user')
    set({ token: null, user: null, isAuthenticated: false })
  },

  loadFromStorage: () => {
    try {
      const token = localStorage.getItem('bsp_token')
      const userStr = localStorage.getItem('bsp_user')
      if (token && userStr) {
        const user = JSON.parse(userStr)
        set({ token, user, isAuthenticated: true, loading: false })
        return
      }
    } catch {}
    set({ loading: false })
  },

  setLoading: (v) => set({ loading: v }),
}))

export const authApi = {
  login: async (username: string, password: string) => {
    const res = await fetch(`${API}/api/auth/login`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    })
    if (!res.ok) { const e = await res.json(); throw new Error(e.error || '登录失败') }
    return res.json()
  },

  register: async (username: string, password: string, nickname: string) => {
    const res = await fetch(`${API}/api/auth/register`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password, nickname }),
    })
    if (!res.ok) { const e = await res.json(); throw new Error(e.error || '注册失败') }
    return res.json()
  },

  getProfile: async (token: string) => {
    const res = await fetch(`${API}/api/auth/profile`, { headers: { Authorization: `Bearer ${token}` } })
    if (!res.ok) throw new Error('获取用户信息失败')
    return res.json()
  },

  steamLogin: async (steamId: string) => {
    const res = await fetch(`${API}/api/auth/steam/login`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ steamId }),
    })
    if (!res.ok) throw new Error('Steam登录失败')
    return res.json()
  },
}
