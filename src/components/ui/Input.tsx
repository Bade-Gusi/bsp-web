'use client';

import React, { useState } from 'react';
import { clsx } from 'clsx';

interface InputProps {
  label?: string;
  placeholder?: string;
  type?: string;
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  error?: string;
  icon?: React.ReactNode;
  password?: boolean;
}

export { Input }
export default function Input({
  label,
  placeholder,
  type = 'text',
  value,
  onChange,
  error,
  icon,
  password,
}: InputProps) {
  const [showPassword, setShowPassword] = useState(false);
  const inputType = password ? (showPassword ? 'text' : 'password') : type;

  return (
    <div className="flex flex-col gap-1.5 w-full">
      {label && (
        <label className="text-sm font-medium text-surface-300">{label}</label>
      )}
      <div className="relative">
        {icon && (
          <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-surface-400 pointer-events-none">
            {icon}
          </span>
        )}
        <input
          type={inputType}
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          className={clsx(
            'w-full bg-[#0B0E0F] border text-white placeholder-surface-500 rounded-md',
            'outline-none transition-all duration-200',
            'focus:border-primary focus:shadow-[0_0_16px_rgba(34,217,126,0.25)]',
            error
              ? 'border-red-500 shadow-[0_0_12px_rgba(239,68,68,0.2)]'
              : 'border-border',
            icon ? 'pl-10' : 'pl-4',
            password ? 'pr-12' : 'pr-4',
            'py-2.5 text-sm',
          )}
        />
        {password && (
          <button
            type="button"
            onClick={() => setShowPassword((prev) => !prev)}
            className={clsx(
              'absolute top-1/2 -translate-y-1/2 text-surface-400 hover:text-white',
              'transition-colors duration-150 cursor-pointer',
              'right-3.5',
            )}
            tabIndex={-1}
          >
            {showPassword ? (
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
                <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" />
                <line x1="1" y1="1" x2="23" y2="23" />
              </svg>
            ) : (
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <circle cx="12" cy="12" r="3" />
              </svg>
            )}
          </button>
        )}
      </div>
      {error && (
        <span className="text-xs text-red-400 mt-0.5">{error}</span>
      )}
    </div>
  );
}
