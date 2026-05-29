'use client'
import { motion } from 'framer-motion'
export default function SnakePage() {
  return <motion.div initial={{opacity:0}} animate={{opacity:1}}><h2 className="text-xl font-bold text-white">贪吃蛇</h2><p className="text-surface-400">构建中</p></motion.div>
}
