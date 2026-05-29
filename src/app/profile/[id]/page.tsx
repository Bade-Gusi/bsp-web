'use client';

import { motion } from 'framer-motion';
import { useEffect, useState } from 'react';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/lib/api';
import { useParams } from 'next/navigation';


interface ProfileData {
  name: string;
  avatar: string;
  mmr: number;
  wins: number;
  losses: number;
  kills: number;
  deaths: number;
  headshotPct: number;
}

interface MatchResult {
  id: number;
  map: string;
  result: string;
  score: string;
  date: string;
}

export default function ProfilePage() {
  const params = useParams();
  const userId = params.id as string;
  const { user } = useAuthStore();
  const [profile, setProfile] = useState<ProfileData | null>(null);
  const [matches, setMatches] = useState<MatchResult[]>([]);
  const kd = profile ? (profile.deaths > 0 ? (profile.kills/profile.deaths).toFixed(2) : '0') : '0';

  useEffect(() => {
    setProfile({ name: 'Player_' + userId, avatar: '', mmr: 1500 + Math.floor(Math.random()*500), wins: 50, losses: 30, kills: 1000, deaths: 800, headshotPct: 42 });
    setMatches([
      { id: 1, map: 'Mirage', result: 'win', score: '16-12', date: '2024-01-15' },
      { id: 2, map: 'Inferno', result: 'loss', score: '9-16', date: '2024-01-14' },
      { id: 3, map: 'Overpass', result: 'win', score: '16-5', date: '2024-01-13' },
    ]);
  }, [userId]);
  if (!profile) {
    return <div className="min-h-screen bg-surface p-6 flex items-center justify-center"><p className="text-surface-400">加载中...</p></div>;
  }

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <div className="mx-auto max-w-4xl">
        <div className="card-hover rounded-xl bg-card p-8 mb-6">
          <div className="flex items-center gap-6">
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-surface-500 text-3xl text-white font-bold">
              {profile.name[0]}
            </div>
            <div className="flex-1">
              <h1 className="text-2xl font-bold text-white mb-1">{profile.name}</h1>
              <div className="flex items-center gap-4">
                <span className="text-primary text-lg font-bold">{profile.mmr} MMR</span>
                <span className="text-surface-400 text-sm">胜: {profile.wins} / 负: {profile.losses}</span>
              </div>
            </div>
            <button onClick={() => alert('已发送好友请求到 ' + userId)}
              className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
              添加好友
            </button>
          </div>
        </div>

        <div className="grid grid-cols-4 gap-4 mb-6">
          <div className="card-hover rounded-xl bg-card p-4 text-center">
            <p className="text-2xl font-bold text-white">{profile.wins}</p>
            <p className="text-xs text-surface-400">胜场</p></div>
          <div className="card-hover rounded-xl bg-card p-4 text-center">
            <p className="text-2xl font-bold text-white">{profile.losses}</p>
            <p className="text-xs text-surface-400">负场</p></div>
          <div className="card-hover rounded-xl bg-card p-4 text-center">
            <p className="text-2xl font-bold text-white">{kd}</p>
            <p className="text-xs text-surface-400">K/D</p></div>
          <div className="card-hover rounded-xl bg-card p-4 text-center">
            <p className="text-2xl font-bold text-white">{profile.headshotPct}%</p>
            <p className="text-xs text-surface-400">HS %</p></div>
        </div>

        <div className="card-hover rounded-xl bg-card p-6">
          <h2 className="text-lg font-semibold text-white mb-4">最近比赛</h2>
          <div className="space-y-3">
            {matches.map(function(m) {
              return (
                <div key={m.id} className="flex items-center justify-between rounded-lg bg-surface-700/50 px-4 py-3">
                  <div className="flex items-center gap-3">
                    <span className={`h-2.5 w-2.5 rounded-full ` + (m.result === 'win' ? 'bg-primary' : 'bg-red-500')} />
                    <span className="text-sm text-white">{m.map}</span>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="text-xs text-surface-400">{m.date}</span>
                    <span className={`text-sm font-semibold ` + (m.result === 'win' ? 'text-primary' : 'text-red-400')}>{m.score}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </motion.div>
  );
}