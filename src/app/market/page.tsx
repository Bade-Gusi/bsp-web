'use client';

import { motion } from 'framer-motion';
import { useState } from 'react';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/lib/api';

const filterTabs = ['全部', '步枪', '手枪', '瘾击', '刀'];

interface SkinItem {
  id: number;
  name: string;
  weapon: string;
  rarity: string;
  price: number;
  image: string;
}

export default function MarketPage() {
  const [activeFilter, setActiveFilter] = useState('全部');
  const { user } = useAuthStore();

  const skins: SkinItem[] = [
    { id: 1, name: 'Redline', weapon: 'AK-47', rarity: '不尸', price: 188, image: '' },
    { id: 2, name: 'Asiimov', weapon: 'AWP', rarity: '隐藏', price: 599, image: '' },
    { id: 3, name: 'Dragon Lore', weapon: 'AWP', rarity: '神品', price: 9999, image: '' },
    { id: 4, name: 'Kill Confirmed', weapon: 'M4A4', rarity: '隐藏', price: 788, image: '' },
    { id: 5, name: 'Howl', weapon: 'M4A1-S', rarity: '禁用', price: 2500, image: '' },
    { id: 6, name: 'Fire Serpent', weapon: 'AK-47', rarity: '隐藏', price: 1200, image: '' },
    { id: 7, name: 'Gamma Doppler', weapon: 'Karambit', rarity: '神品', price: 4500, image: '' },
    { id: 8, name: 'Deagle Blaze', weapon: 'Desert Eagle', rarity: '不尸', price: 299, image: '' },
    { id: 9, name: 'Orion', weapon: 'USP-S', rarity: '不尸', price: 168, image: '' },
    { id: 10, name: 'Printstream', weapon: 'Desert Eagle', rarity: '隐藏', price: 420, image: '' },
    { id: 11, name: 'Neon Rider', weapon: 'MAC-10', rarity: '不尸', price: 85, image: '' },
    { id: 12, name: 'Bloodsport', weapon: 'SSG 08', rarity: '不尸', price: 45, image: '' },
  ];
  const getRarityColor = (rarity: string) => {
    const map: Record<string,string> = {
      '神品': 'text-red-400 bg-red-500/10',
      '隐藏': 'text-purple-400 bg-purple-500/10',
      '不尸': 'text-blue-400 bg-blue-500/10',
      '禁用': 'text-yellow-400 bg-yellow-500/10',
    };
    return map[rarity] || 'text-surface-400 bg-surface-600';
  };

  const filteredSkins = activeFilter === '全部' ? skins : skins.filter(s => s.weapon.includes(activeFilter));

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <div className="mx-auto max-w-6xl">
        <motion.h1 className="mb-6 text-3xl font-bold text-white"
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.4 }}
        >皮肤市场</motion.h1>

        <div className="flex gap-2 rounded-xl bg-card p-1.5 mb-6">
          {filterTabs.map((tab) => (
            <button key={tab} onClick={() => setActiveFilter(tab)}
              className={`flex-1 rounded-lg px-6 py-2.5 text-sm font-medium transition-all duration-200 ` + (activeFilter === tab ? 'bg-primary text-black' : 'text-surface-400 hover:text-white hover:bg-surface-600')}
            >{tab}</button>
          ))}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {filteredSkins.map(function(skin) {
            return (
              <div key={skin.id} className="card-hover rounded-xl bg-card p-5 transition-all duration-300 hover:-translate-y-1">
                <div className="h-24 bg-surface-700/50 rounded-lg mb-4 flex items-center justify-center">
                  <span className="text-4xl">🔫</span>
                </div>
                <h3 className="text-sm font-semibold text-white mb-1">{skin.name}</h3>
                <p className="text-xs text-surface-400 mb-3">{skin.weapon}</p>
                <div className="flex items-center justify-between">
                  <span className={`text-xs rounded-full px-2 py-0.5 font-medium ` + getRarityColor(skin.rarity)}>{skin.rarity}</span>
                  <span className="text-sm font-bold text-primary">¥{skin.price}</span>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </motion.div>
  );
}