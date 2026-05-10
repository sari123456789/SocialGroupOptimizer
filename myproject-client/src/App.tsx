import { useEffect, useState } from 'react'
import './App.css'

/**
 * שלב 1 — מה זה "קומפוננטה"?
 * --------------------------------
 * פונקציה בשם App שמחזירה JSX (HTML דמוי) היא קומפוננטת React.
 * React מציירת את מה שחוזר מהפונקציה על המסך.
 */
function App() {
  // שלב 2 — state (מצב): ערכים שכשהם משתנים, React מציירת מחדש את המסך.
  const [serverMessage, setServerMessage] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [errorText, setErrorText] = useState<string | null>(null)

  // שלב 3 — useEffect: קוד שרץ אחרי שהקומפוננטה "עלתה" למסך.
  // כאן אנחנו קוראים לשרת פעם אחת (בטעינה הראשונה).
  useEffect(() => {
    let cancelled = false

    async function loadHealth() {
      setLoading(true)
      setErrorText(null)
      try {
        // ה-proxy ב-vite.config מפנה ל־API — לכן נשתמש בנתיב יחסי.
        const res = await fetch('/api/health')
        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`)
        }
        const data = (await res.json()) as { ok?: boolean; message?: string }
        if (!cancelled) {
          setServerMessage(data.message ?? JSON.stringify(data))
        }
      } catch (e) {
        if (!cancelled) {
          setErrorText(e instanceof Error ? e.message : 'שגיאה לא ידועה')
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadHealth()

    // ניקוי: אם המשתמש עוזב את הדף לפני שהבקשה הסתיימה — לא מעדכנים state.
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <main className="app-shell">
      <h1>MyProject — צד לקוח (שלב ראשון)</h1>

      <section className="card">
        <h2>חיבור ל־API</h2>
        <p className="hint">
          הריצי את ה־API על פורט 5256 (פרופיל http ב־launchSettings), ואז הריצי כאן{' '}
          <code>npm run dev</code>.
        </p>
        {loading && <p>טוען…</p>}
        {!loading && errorText && (
          <p className="error" role="alert">
            לא הצלחנו לדבר עם השרת: {errorText}
          </p>
        )}
        {!loading && !errorText && serverMessage && (
          <p className="ok">תשובה מהשרת: {serverMessage}</p>
        )}
      </section>

      <section className="card">
        <h2>מושגים בסיסיים</h2>
        <ul className="concepts">
          <li>
            <strong>JSX</strong> — HTML בתוך JavaScript; חייבים עטיפה אחת (כאן{' '}
            <code>&lt;main&gt;</code>).
          </li>
          <li>
            <strong>useState</strong> — משתנה שמשנה את המסך כשקוראים ל־setter (
            <code>setServerMessage</code> וכו׳).
          </li>
          <li>
            <strong>useEffect</strong> — צד לוגיקה/רשת; הריצה כאן אחרי טעינת הדף.
          </li>
          <li>
            <strong>fetch</strong> — בקשת HTTP מהדפדפן לשרת.
          </li>
        </ul>
      </section>
    </main>
  )
}

export default App
