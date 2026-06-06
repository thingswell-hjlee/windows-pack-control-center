import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  LayoutDashboard,
  Monitor,
  Camera,
  Bell,
  FileText,
  Wifi,
  WifiOff,
} from 'lucide-react';
import * as signalR from '@microsoft/signalr';
import { useSignalR } from './hooks/useSignalR';
import { Dashboard } from './pages/Dashboard';
import { Devices } from './pages/Devices';
import { Cameras } from './pages/Cameras';
import { Events } from './pages/Events';
import { EventDetail } from './pages/EventDetail';
import { Reports } from './pages/Reports';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function AppContent() {
  const { connectionState } = useSignalR();
  const isConnected = connectionState === signalR.HubConnectionState.Connected;

  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-56 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <h1 className="text-lg font-bold text-gray-800">Control Center</h1>
        </div>
        <nav className="flex-1 p-2 space-y-1">
          <SidebarLink to="/" icon={<LayoutDashboard className="w-4 h-4" />} label="Dashboard" />
          <SidebarLink to="/devices" icon={<Monitor className="w-4 h-4" />} label="AIBox Devices" />
          <SidebarLink to="/cameras" icon={<Camera className="w-4 h-4" />} label="Cameras" />
          <SidebarLink to="/events" icon={<Bell className="w-4 h-4" />} label="Events" />
          <SidebarLink to="/reports" icon={<FileText className="w-4 h-4" />} label="Reports" />
        </nav>
        <div className="p-3 border-t">
          <div className="flex items-center gap-2 text-xs">
            {isConnected ? (
              <>
                <Wifi className="w-3.5 h-3.5 text-green-600" />
                <span className="text-green-700">Connected</span>
              </>
            ) : (
              <>
                <WifiOff className="w-3.5 h-3.5 text-red-600" />
                <span className="text-red-700">Disconnected</span>
              </>
            )}
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto p-6">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/devices" element={<Devices />} />
          <Route path="/cameras" element={<Cameras />} />
          <Route path="/events" element={<Events />} />
          <Route path="/events/:id" element={<EventDetail />} />
          <Route path="/reports" element={<Reports />} />
        </Routes>
      </main>
    </div>
  );
}

function SidebarLink({ to, icon, label }: { to: string; icon: React.ReactNode; label: string }) {
  return (
    <NavLink
      to={to}
      end={to === '/'}
      className={({ isActive }) =>
        `flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors ${
          isActive ? 'bg-blue-50 text-blue-700 font-medium' : 'text-gray-700 hover:bg-gray-100'
        }`
      }
    >
      {icon}
      {label}
    </NavLink>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppContent />
      </BrowserRouter>
    </QueryClientProvider>
  );
}
