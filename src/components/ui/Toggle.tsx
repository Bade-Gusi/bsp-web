'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { clsx } from 'clsx';

interface ToggleProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
}

export default function Toggle({ checked, onChange, label }: ToggleProps) {
  return (
    <label className="inline-flex items-center gap-3 cursor-pointer select-none">
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={() => onChange(!checked)}
        className={clsx(
          'relative inline-flex h-6 w-11 shrink-0 rounded-full transition-colors duration-300',
          checked ? 'bg-primary' : 'bg-surface-700 border border-border',
        )}
      >
        <motion.span
          className={clsx(
            'absolute top-0.5 left-0.5 h-5 w-5 rounded-full shadow-md',
            checked ? 'bg-[#0B0E0F]' : 'bg-surface-300',
          )}
          animate={{ x: checked ? 20 : 0 }}
          transition={{ type: 'spring', stiffness: 500, damping: 30 }}
        />
      </button>
      {label && (
        <span className="text-sm text-surface-300">{label}</span>
      )}
    </label>
  );
}
