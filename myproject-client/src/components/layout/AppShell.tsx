import type { ReactNode } from 'react'
import { TopBar } from './TopBar'

interface AppShellProps {
  children: ReactNode
  wide?: boolean
}

export function AppShell({ children, wide = false }: AppShellProps) {
  return (
    <div className="min-h-screen">
      <TopBar />
      <main
        className={`mx-auto px-4 py-6 ${wide ? 'max-w-7xl' : 'max-w-6xl'}`}
      >
        {children}
      </main>
    </div>
  )
}
