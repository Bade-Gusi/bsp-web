'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { clsx } from 'clsx';

interface BadgeProps {
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'default';
  children: React.ReactNode;
  className?: string;
}

const variantStyles: Record<string, string> = {
  primary: 'bg-primary/15 text-primary border border-primary/25',
  success: 'bg-accent/15 text-accent border border-accent/25',
  warning: 'bg-amber-500/15 text-amber-400 border border-amber-500/25',
  danger: 'bg-red-500/15 text-red-400 border border-red-500/25',
  default: 'bg-surface-800 text-surface-300 border border-border',
};

export { Badge }
export default function Badge({
  variant = 'default',
  children,
  className,
}: BadgeProps) {
  return (
    <motion.span
      initial={{ scale: 0, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{
        type: 'spring',
        stiffness: 500,
        damping: 15,
        mass: 0.8,
      }}
      className={clsx(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap',
        variantStyles[variant],
        className,
      )}
    >
      {children}
    </motion.span>
  );
}
