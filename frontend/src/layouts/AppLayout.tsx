import { useState } from 'react'
import toast from 'react-hot-toast'
import { Outlet, useNavigate } from 'react-router-dom'
import { queryClient } from '../app/queryClient'
import { Sidebar } from '../components/navigation/Sidebar'
import { Topbar } from '../components/navigation/Topbar'
import { tokenStorage } from '../services/tokenStorage'

export function AppLayout() {
  const navigate = useNavigate()
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)

  function handleLogout() {
    tokenStorage.remove()
    queryClient.clear()

    toast.success('Sesión cerrada correctamente.')

    navigate('/login', {
      replace: true,
    })
  }

  return (
    <div className="min-h-screen bg-slate-100">
      <Sidebar
        isOpen={isSidebarOpen}
        onClose={() => setIsSidebarOpen(false)}
      />

      <div className="min-h-screen lg:pl-72">
        <Topbar
          onOpenMenu={() => setIsSidebarOpen(true)}
          onLogout={handleLogout}
        />

        <main className="px-5 py-7 sm:px-8 sm:py-8">
          <div className="mx-auto max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}