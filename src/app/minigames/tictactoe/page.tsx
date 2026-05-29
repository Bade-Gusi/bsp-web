'use client';

import { motion } from 'framer-motion';
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';
import { useState } from 'react';

export default function TicTacToePage() {
  const [board, setBoard] = useState<string[]>(Array(9).fill(''));
  const [xIsNext, setXIsNext] = useState(true);
  const [winner, setWinner] = useState<string | null>(null);

  const lines = [
    [0,1,2],[3,4,5],[6,7,8],[0,3,6],[1,4,7],[2,5,8],[0,4,8],[2,4,6]
  ];

  function calculateWinner(squares: string[]) {
    for (const [a,b,c] of lines) {
      if (squares[a] && squares[a] === squares[b] && squares[a] === squares[c]) return squares[a];
    }
    return squares.every(Boolean) ? 'draw' : null;
  }

  function handleClick(i: number) {
    if (board[i] || winner) return;
    const newBoard = [...board];
    newBoard[i] = xIsNext ? 'X' : 'O';
    setBoard(newBoard);
    setXIsNext(!xIsNext);
    const w = calculateWinner(newBoard);
    if (w) setWinner(w);
  }

  function resetGame() {
    setBoard(Array(9).fill(''));
    setXIsNext(true);
    setWinner(null);
  }

  const status = winner ? (winner === 'draw' ? '平局' : '胜利者: ' + winner) : '下一手: ' + (xIsNext ? 'X' : 'O');

  return (
    <motion.div className="min-h-screen bg-surface p-6 flex flex-col items-center"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <h1 className="text-2xl font-bold text-white mb-6">井字棋</h1>
      <p className="text-sm text-surface-400 mb-4">{status}</p>
      <div className="grid grid-cols-3 gap-2 mb-6">
        {board.map(function(cell, i) {
          return (
            <motion.button key={i} onClick={function() { handleClick(i); }}
              className="w-20 h-20 rounded-xl bg-card text-3xl font-bold text-white hover:bg-surface-600 transition-all"
              whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}
            >{cell}</motion.button>
          );
        })}
      </div>
      <button onClick={resetGame}
        className="rounded-lg bg-primary px-6 py-2.5 text-sm font-semibold text-black hover:bg-accent transition-all">
        重新开始
      </button>
    </motion.div>
  );
}