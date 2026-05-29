// 浏览器端走同源代理（/api/* → next.config.js rewrites → localhost:5001）
// 服务端渲染时直连后端
const isBrowser = typeof window !== 'undefined'
const API = isBrowser ? '' : (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001')

class ApiClient {
  private token: string | null = null

  setToken(t: string | null) { this.token = t }

  private async request<T>(path: string, opts?: RequestInit): Promise<T> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    if (this.token) headers['Authorization'] = `Bearer ${this.token}`
    const res = await fetch(`${API}${path}`, { ...opts, headers })
    if (!res.ok) {
      const body = await res.text()
      try { const j = JSON.parse(body); throw new Error(j.error || `请求失败 ${res.status}`) }
      catch { throw new Error(`请求失败 ${res.status}`) }
    }
    return res.json()
  }

  // Auth
  login = (u: string, p: string) => this.request<{token:string;user:any}>('/api/auth/login', {method:'POST',body:JSON.stringify({username:u,password:p})})
  register = (u: string, p: string, n: string) => this.request('/api/auth/register', {method:'POST',body:JSON.stringify({username:u,password:p,nickname:n})})
  steamLogin = (sid: string) => this.request<{token:string;user:any;needRegister?:boolean}>('/api/auth/steam/login', {method:'POST',body:JSON.stringify({steamId:sid})})
  bindSteam = (sid: string) => this.request('/api/auth/bind-steam', {method:'POST',body:JSON.stringify({steamId:sid})})
  getProfile = () => this.request<any>('/api/auth/profile')
  updateProfile = (d: any) => this.request<any>('/api/auth/profile', {method:'PUT',body:JSON.stringify(d)})

  // Users
  getUser = (id: number) => this.request<any>(`/api/users/${id}`)
  searchUsers = (q: string) => this.request<any[]>(`/api/users/search?q=${encodeURIComponent(q)}`)

  // Friends
  getFriends = () => this.request<any[]>('/api/friends')
  sendFriendRequest = (fid: number) => this.request(`/api/friends/request/${fid}`, {method:'POST'})
  acceptFriendRequest = (fid: number) => this.request(`/api/friends/accept/${fid}`, {method:'POST'})
  removeFriend = (fid: number) => this.request(`/api/friends/${fid}`, {method:'DELETE'})

  // Rooms
  getRooms = (page=1) => this.request<any[]>(`/api/rooms?page=${page}&size=20`)
  getRoom = (code: string) => this.request<any>(`/api/rooms/${code}`)
  createRoom = (d: any) => this.request<any>('/api/rooms', {method:'POST',body:JSON.stringify(d)})
  joinRoom = (code: string, pw?: string) => this.request(`/api/rooms/${code}/join`, {method:'POST',body:JSON.stringify({password:pw||''})})
  leaveRoom = (code: string) => this.request(`/api/rooms/${code}/leave`, {method:'POST'})

  // Matches
  getMatches = (page=1) => this.request<any[]>(`/api/matches?page=${page}&size=20`)
  getMatch = (id: number) => this.request<any>(`/api/matches/${id}`)
  getUserStats = (uid: number) => this.request<any>(`/api/matches/stats/${uid}`)

  // Leaderboard
  getLeaderboard = (page=1) => this.request<any[]>(`/api/leaderboard?page=${page}&size=50`)

  // Servers
  getServers = () => this.request<any[]>('/api/servers')
  createServer = (d: any) => this.request<any>('/api/servers', {method:'POST',body:JSON.stringify(d)})
  deleteServer = (code: string) => this.request(`/api/servers/${code}`, {method:'DELETE'})
  joinServer = (code: string, pw?: string) => this.request<any>(`/api/servers/${code}/join`, {method:'POST',body:JSON.stringify({password:pw||''})})

  // Duel
  getDuelInvites = () => this.request<any[]>('/api/duel/invites')
  sendDuelInvite = (toUserId: number) => this.request('/api/duel/invite', {method:'POST',body:JSON.stringify({toUserId})})
  acceptDuel = (inviteId: number) => this.request(`/api/duel/accept/${inviteId}`, {method:'POST'})
  rejectDuel = (inviteId: number) => this.request(`/api/duel/reject/${inviteId}`, {method:'POST'})
  joinDuelQueue = () => this.request('/api/duel/queue/join', {method:'POST'})
  leaveDuelQueue = () => this.request('/api/duel/queue/leave', {method:'POST'})

  // Achievements
  getAchievements = (uid: number) => this.request<any[]>(`/api/achievements/${uid}`)

  // Games
  getGames = () => this.request<any[]>('/api/games')

  // Holiday
  getTodayHoliday = () => this.request<any>('/api/holiday/today')

  // Welfare
  getWelfare = () => this.request<any[]>('/api/welfare/list')
}

export const api = new ApiClient()
