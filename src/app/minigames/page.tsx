'use client';

import { motion } from 'framer-motion';

import Link from 'next/link';
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

const games = [
  { name: '井字棋', desc: '经典三子棋对局', icon: '⭕', href: '/minigames/tictactoe' },
  { name: '贪吃蛇', desc: '控制蛇吃到更多食物', icon: '🐍', href: '/minigames/snake' },
  { name: '记忆翻牌', desc: '翻牌匹配测试记忆力', icon: '🎮', href: '/minigames/memory' },
  { name: '反应测试', desc: '测试你的反应速度', icon: '⚡', href: '/minigames/reaction' },
];

export default function MinigamesPage() {
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
        >小游戏</motion.h1>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {games.map(function(game) {
            return (
              <Link key={game.href} href={game.href}>
                <div className="card-hover rounded-xl bg-card p-6 transition-all duration-300 hover:-translate-y-1 hover:shadow-lg hover:shadow-primary/10">
                  <div className="text-4xl mb-4">{game.icon}</div>
                  <h3 className="text-lg font-semibold text-white mb-2">{game.name}</h3>
                  <p className="text-sm text-surface-400">{game.desc}</p>
                </div>
              </Link>
            );
          })}
        </div>
      </div>
    </motion.div>
  );
}