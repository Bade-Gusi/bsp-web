'use client'

interface HeaderProps { pageTitle: string }

export function Header({ pageTitle }: HeaderProps) {
  return (
    <header className="h-16 px-6 flex items-center justify-between border-b border-border bg-surface shrink-0">
      <div className="flex items-center gap-4">
        <h1 className="text-2xl font-bold text-white">{pageTitle}</h1>
        <span className="badge-primary text-xs">1,234 在线</span>
      </div>
      <div className="flex items-center gap-1">
        <button className="w-[46px] h-8 flex items-center justify-center text-surface-400 hover:text-white hover:bg-hover rounded-md transition-colors">─</button>
        <button className="w-[46px] h-8 flex items-center justify-center text-surface-400 hover:text-white hover:bg-hover rounded-md transition-colors">□</button>
        <button className="w-[46px] h-8 flex items-center justify-center text-surface-400 hover:text-white hover:bg-hover rounded-md transition-colors">✕</button>
      </div>
    </header>
  )
}
