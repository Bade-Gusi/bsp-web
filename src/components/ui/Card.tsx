'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { clsx } from 'clsx';

interface CardProps {
  variant?: 'default' | 'hover' | 'glass' | 'neon';
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}

const variantStyles: Record<string, string> = {
  default:
    'bg-card border border-border shadow-card',
  hover:
    'bg-card border border-border shadow-card hover:border-primary/40 hover:shadow-glow',
  glass:
    'bg-card/60 backdrop-blur-xl border border-white/5 shadow-card',
  neon:
    'bg-card border border-primary/30 shadow-glow hover:border-primary/60 hover:shadow-glow-lg',
};

export { Card }
export default function Card({
  variant = 'default',
  children,
  className,
  onClick,
}: CardProps) {
  return (
    <motion.div
      onClick={onClick}
      initial={{ opacity: 0, y: 20 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-40px' }}
      transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
      whileHover={
        variant === 'hover' || variant === 'neon'
          ? { scale: 1.02 }
          : undefined
      }
      className={clsx(
        'rounded-lg p-5 transition-all duration-300',
        onClick && 'cursor-pointer',
        variantStyles[variant],
        className,
      )}
    >
      {children}
    </motion.div>
  );
}
