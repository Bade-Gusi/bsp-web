'use client';

import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/lib/api';

interface LeaderboardEntry {
  id: number;
  rank: number;
  name: string;
  avatar: string;
  mmr: number;
  wins: number;
  losses: number;
  kills: number;
  deaths: number;
  matches: number;
}

const tabs = ['Rating', '胜率', '击杀', '场次'];

export default function LeaderboardPage() {
  const [activeTab, setActiveTab] = useState('Rating');
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const { user } = useAuthStore();

  useEffect(() => {
    const names = ['Player_One','AceGunner','ShadowKill','FireStorm','NightHawk','ProGamer','HeadShot','ClutchKing','NoScope','AWP_Master','RushB','EcoRound','SaveAWP','TradeFrag','EntryFrag','SupportMain','Lurker','IGL_Captain','StarPlayer','RisingStar'];
    const data: LeaderboardEntry[] = [];
    for (let i = 0; i < names.length; i++) {
      data.push({ id: i+1, rank: i+1, name: names[i], avatar: '', mmr: 2000 - i*80 + Math.floor(Math.random()*40), wins: 100 - i*4, losses: 50 + i*3, kills: 2000 - i*80, deaths: 1500 - i*50, matches: 150 - i*5 });
    }
    setEntries(data);
  }, []);
  const getRankStyle = (rank: number) => {
    if (rank === 1) return 'text-yellow-400';
    if (rank === 2) return 'text-gray-300';
    if (rank === 3) return 'text-amber-600';
    return 'text-surface-400';
  };

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <div className="mx-auto max-w-4xl">
        <motion.h1 className="mb-6 text-3xl font-bold text-white"
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.4 }}
        >排行榜</motion.h1>

        <div className="flex gap-2 rounded-xl bg-card p-1.5 mb-6">
          {tabs.map((tab) => (
            <button key={tab} onClick={() => setActiveTab(tab)}
              className={`flex-1 rounded-lg px-6 py-2.5 text-sm font-medium transition-all duration-200 ` + (activeTab === tab ? 'bg-primary text-black' : 'text-surface-400 hover:text-white hover:bg-surface-600')}
            >{tab}</button>
          ))}
        </div>

        <div className="space-y-2">
          {entries.map(function(entry, idx) {
            var medals = ['🥇','🥈','🥉'];
            return (
              <div key={entry.id}
                className={`flex items-center gap-4 rounded-xl px-5 py-3 transition-all duration-200 ` + (
                  idx < 3 ? 'bg-card ring-1 ring-primary/20' : 'bg-card hover:bg-surface-600'
                )}
              >
                <span className={`w-8 text-center text-lg font-bold ` + getRankStyle(entry.rank)}>
                  {idx < 3 ? medals[idx] : '#' + entry.rank}
                </span>
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-surface-500 text-sm text-white font-semibold">
                  {entry.name[0]}
                </div>
                <div className="flex-1">
                  <p className="text-sm font-medium text-white">{entry.name}</p>
                </div>
                <div className="flex items-center gap-6 text-sm">
                  <span className="text-primary font-bold">{entry.mmr}</span>
                  <span className="text-surface-400 text-xs">{Math.round(entry.wins/(entry.wins+entry.losses)*100)}%</span>
                  <span className="text-surface-400 text-xs">{entry.kills}</span>
                  <span className="text-surface-400 text-xs">{entry.matches}</span>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </motion.div>
  );
}