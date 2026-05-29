'use client';

import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/lib/api';

interface Achievement {
  id: number;
  name: string;
  description: string;
  icon: string;
  unlocked: boolean;
  unlockedDate: string;
}

export default function AchievementsPage() {
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const { user } = useAuthStore();

  useEffect(() => {
    setAchievements([
      { id: 1, name: '初入背水', description: '完成第一场比赛', icon: '🏆', unlocked: true, unlockedDate: '2024-01-01' },
      { id: 2, name: '连胜专家', description: '连续赢得5场比赛', icon: '🔥', unlocked: true, unlockedDate: '2024-01-05' },
      { id: 3, name: '头部猎手', description: '单场比赛完成20个头部击杀', icon: '🎯', unlocked: true, unlockedDate: '2024-01-10' },
      { id: 4, name: '防御大师', description: '单场比赛完成CCB少年组织', icon: '🛡️', unlocked: true, unlockedDate: '2024-01-15' },
      { id: 5, name: '影响力', description: '达到Rating 2000', icon: '⭐', unlocked: false, unlockedDate: '' },
      { id: 6, name: '背水老将', description: '完成100场比赛', icon: '🎖️', unlocked: false, unlockedDate: '' },
      { id: 7, name: '社交达人', description: '添加10个好友', icon: '🤝', unlocked: false, unlockedDate: '' },
      { id: 8, name: '战赜宝库', description: '收集所有成就', icon: '🏅', unlocked: false, unlockedDate: '' },
    ]);
  }, []);
  const unlocked = achievements.filter(a => a.unlocked).length;
  const total = achievements.length;
  const pct = total > 0 ? Math.round(unlocked/total*100) : 0;

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <div className="mx-auto max-w-5xl">
        <motion.h1 className="mb-6 text-3xl font-bold text-white"
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.4 }}
        >成就</motion.h1>

        <div className="card-hover rounded-xl bg-card p-6 mb-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm text-surface-400">总进度</span>
            <span className="text-sm text-white font-medium">{unlocked}/{total} ({pct}%)</span>
          </div>
          <div className="h-2 bg-surface-700 rounded-full overflow-hidden">
            <motion.div className="h-full bg-gradient-to-r from-primary to-accent rounded-full"
              initial={{ width: 0 }}
              animate={{ width: pct + '%' }}
              transition={{ duration: 0.8, ease: 'easeOut' }}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {achievements.map(function(ach) {
            return (
              <div key={ach.id}
                className={`card-hover rounded-xl p-5 transition-all duration-300 ` + (ach.unlocked ? 'bg-card' : 'bg-surface-700/30 opacity-50')}
              >
                <div className="text-3xl mb-3">{ach.icon}</div>
                <h3 className="text-sm font-semibold text-white mb-1">{ach.name}</h3>
                <p className="text-xs text-surface-400 mb-2">{ach.description}</p>
                {ach.unlocked ? (
                  <span className="text-xs text-primary">已解锁 {ach.unlockedDate}</span>
                ) : (
                  <span className="text-xs text-surface-500">未解锁</span>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </motion.div>
  );
}