import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

export function TopBar() {
  const { session, logout } = useAuth()
  const location = useLocation()

  const navItems = [
    { to: '/', label: 'חלוקות' },
    { to: '/participants', label: 'משתתפים' },
  ]

  return (
    <header className="border-b border-slate-200/80 bg-white/80 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
        <div className="flex items-center gap-6">
          <Link to="/" className="text-lg font-semibold text-indigo-700">
            מערכת חלוקות
          </Link>
          <nav className="flex items-center gap-2">
            {navItems.map((item) => {
              const isActive = location.pathname === item.to

              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={`rounded-lg px-3 py-1.5 text-sm transition ${
                    isActive
                      ? 'bg-indigo-50 font-medium text-indigo-700'
                      : 'text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  {item.label}
                </Link>
              )
            })}
          </nav>
        </div>

        <div className="flex items-center gap-4">
          {session && (
            <span className="text-sm text-slate-600">
              שלום, {session.managerName}
            </span>
          )}
          <button
            type="button"
            onClick={logout}
            className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm text-slate-700 transition hover:bg-slate-50"
          >
            התנתקות
          </button>
        </div>
      </div>
    </header>
  )
}
