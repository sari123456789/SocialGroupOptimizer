import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { AssignmentDetailPage } from './pages/AssignmentDetailPage'
import { CreateAssignmentPage } from './pages/CreateAssignmentPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { ParticipantsPage } from './pages/ParticipantsPage'

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/participants" element={<ParticipantsPage />} />
            <Route path="/assignments/new" element={<CreateAssignmentPage />} />
            <Route
              path="/assignments/:assignmentId"
              element={<AssignmentDetailPage />}
            />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

export default App
