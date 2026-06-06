import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Monitor, Camera, AlertTriangle, Activity } from 'lucide-react';
import { getDashboardSummary, getEvents } from '../services/api';
import { EventCard } from '../components/EventCard';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { StatusBadge } from '../components/StatusBadge';
import { getDevices } from '../services/api';

export function Dashboard() {
  const navigate = useNavigate();

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: getDashboardSummary,
    refetchInterval: 10000,
  });

  const { data: recentEvents } = useQuery({
    queryKey: ['recent-events'],
    queryFn: () => getEvents({ page: 1, page_size: 10 }),
    refetchInterval: 10000,
  });

  const { data: devices } = useQuery({
    queryKey: ['devices'],
    queryFn: getDevices,
    refetchInterval: 10000,
  });

  if (summaryLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Dashboard</h1>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        <SummaryCard
          icon={<Monitor className="w-5 h-5 text-blue-600" />}
          label="Total Devices"
          value={summary?.total_devices ?? 0}
        />
        <SummaryCard
          icon={<Monitor className="w-5 h-5 text-green-600" />}
          label="Online Devices"
          value={summary?.online_devices ?? 0}
        />
        <SummaryCard
          icon={<Camera className="w-5 h-5 text-blue-600" />}
          label="Total Cameras"
          value={summary?.total_cameras ?? 0}
        />
        <SummaryCard
          icon={<Camera className="w-5 h-5 text-green-600" />}
          label="Online Cameras"
          value={summary?.online_cameras ?? 0}
        />
        <SummaryCard
          icon={<AlertTriangle className="w-5 h-5 text-orange-600" />}
          label="Unconfirmed"
          value={summary?.unconfirmed_events ?? 0}
        />
        <SummaryCard
          icon={<Activity className="w-5 h-5 text-purple-600" />}
          label="Today's Events"
          value={summary?.today_events ?? 0}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Events */}
        <div className="bg-white rounded-lg border p-4">
          <h2 className="text-lg font-semibold mb-3">Recent Events</h2>
          <div className="space-y-2">
            {recentEvents?.items?.map((event) => (
              <EventCard
                key={event.event_id}
                event={event}
                onClick={() => navigate(`/events/${event.event_id}`)}
              />
            ))}
            {(!recentEvents?.items || recentEvents.items.length === 0) && (
              <p className="text-sm text-gray-500 text-center py-4">No recent events</p>
            )}
          </div>
        </div>

        {/* Device Status Overview */}
        <div className="bg-white rounded-lg border p-4">
          <h2 className="text-lg font-semibold mb-3">Device Status</h2>
          <div className="space-y-2">
            {devices?.map((device) => (
              <div key={device.device_id} className="flex items-center justify-between py-2 border-b last:border-b-0">
                <div>
                  <p className="text-sm font-medium">{device.device_name}</p>
                  <p className="text-xs text-gray-500">{device.location}</p>
                </div>
                <StatusBadge status={device.status} />
              </div>
            ))}
            {(!devices || devices.length === 0) && (
              <p className="text-sm text-gray-500 text-center py-4">No devices registered</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function SummaryCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: number }) {
  return (
    <div className="bg-white rounded-lg border p-4">
      <div className="flex items-center gap-2 mb-1">
        {icon}
        <span className="text-xs text-gray-500">{label}</span>
      </div>
      <p className="text-2xl font-bold">{value}</p>
    </div>
  );
}
