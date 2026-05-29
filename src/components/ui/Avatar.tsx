'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { clsx } from 'clsx';

interface AvatarProps {
  src?: string;
  name: string;
  size?: 'sm' | 'md' | 'lg';
  status?: 'online' | 'offline' | 'in-game';
  className?: string;
}

const sizeStyles: Record<string, { container: string; text: string; dot: string }> = {
  sm: { container: 'w-8 h-8', text: 'text-xs', dot: 'w-2.5 h-2.5 border-[1.5px]' },
  md: { container: 'w-10 h-10', text: 'text-sm', dot: 'w-3 h-3 border-2' },
  lg: { container: 'w-14 h-14', text: 'text-lg', dot: 'w-3.5 h-3.5 border-2' },
};

const statusColors: Record<string, string> = {
  online: 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]',
  offline: 'bg-surface-500',
  'in-game': 'bg-amber-400 shadow-[0_0_8px_rgba(251,191,36,0.5)]',
};

function getInitials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

function getColorFromName(name: string): string {
  const colors = [
    'bg-primary-600',
    'bg-accent-600',
    'bg-purple-600',
    'bg-blue-600',
    'bg-rose-600',
    'bg-amber-600',
    'bg-cyan-600',
  ];
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return colors[Math.abs(hash) % colors.length];
}

export default function Avatar({
  src,
  name,
  size = 'md',
  status,
  className,
}: AvatarProps) {
  const s = sizeStyles[size];
  const initials = getInitials(name || '?');
  const bgColor = getColorFromName(name || '?');

  return (
    <div className={clsx('relative inline-flex shrink-0', className)}>
      {/* Image or fallback */}
      {src ? (
        <img
          src={src}
          alt={name}
          className={clsx(
            'rounded-full object-cover',
            s.container,
          )}
        />
      ) : (
        <div
          className={clsx(
            'rounded-full flex items-center justify-center font-bold text-white select-none',
            bgColor,
            s.container,
            s.text,
          )}
        >
          {initials}
        </div>
      )}

      {/* Status dot */}
      {status && (
        <motion.span
          className={clsx(
            'absolute bottom-0 right-0 rounded-full border-surface',
            s.dot,
            statusColors[status],
          )}
          animate={
            status === 'online'
              ? {
                  boxShadow: [
                    '0 0 0 0 rgba(34,197,94,0.6)',
                    '0 0 0 6px rgba(34,197,94,0)',
                    '0 0 0 0 rgba(34,197,94,0)',
                  ],
                }
              : undefined
          }
          transition={
            status === 'online'
              ? { duration: 2, repeat: Infinity, ease: 'easeInOut' }
              : undefined
          }
        />
      )}
    </div>
  );
}
