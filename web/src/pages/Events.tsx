import { useState, useCallback } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Filter, Download } from 'lucide-react';
import { getEvents, getDevices, getCameras, getEventsExportCsvUrl, getEventsExportExcelUrl } from '../services/api';
import type { EventFilters } from '../services/api';
import { SeverityBadge } from '../components/SeverityBadge';
import { Pagination } from '../components/Pagination';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { useSignalR } from '../hooks/useSignalR';
import { EventTypes, Severities } from '../types';
import type { NormalizedEvent } from '../types';
import dayjs from 'dayjs';

export function Events() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<EventFilters>({
    page: 1,
    page_size: 20,
  });
  const [showFilters, setShowFilters] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['events', filters],
    queryFn: () => getEvents(filters),
  });

  const { data: devices } = useQuery({
    queryKey: ['devices'],
    queryFn: getDevices,
  });

  const { data: cameras } = useQuery({
    queryKey: ['cameras'],
    queryFn: getCameras,
  });

  const activeFilterCount = [
    filters.device_id,
    filters.camera_id,
    filters.event_type,
    filters.severity,
    filters.ack_status,
    filters.start_date,
    filters.end_date,
  ].filter((v) => v !== undefined && v !== '').length;

  const handleNewEvent = useCallback(
    (_event: NormalizedEvent) => {
      queryClient.invalidateQueries({ queryKey: ['events'] });
    },
    [queryClient]
  );

  useSignalR({ onNewEvent: handleNewEvent });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Events</h1>
        <div className="flex items-center gap-2">
          <a
            href={getEventsExportCsvUrl(filters)}
            download
            className="flex items-center gap-1 border px-3 py-2 rounded-lg hover:bg-gray-50 text-sm"
          >
            <Download className="w-4 h-4" /> CSV
          </a>
          <a
            href={getEventsExportExcelUrl(filters)}
            download
            className="flex items-center gap-1 border px-3 py-2 rounded-lg hover:bg-gray-50 text-sm"
          >
            <Download className="w-4 h-4" /> Excel
          </a>
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="relative flex items-center gap-2 border px-3 py-2 rounded-lg hover:bg-gray-50"
          >
            <Filter className="w-4 h-4" /> Filters
            {activeFilterCount > 0 && (
              <span className="absolute -top-2 -right-2 bg-blue-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                {activeFilterCount}
              </span>
            )}
          </button>
        </div>
      </div>

      {showFilters && (
        <div className="bg-white rounded-lg border p-4">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
              <input
                type="date"
                value={filters.start_date || ''}
                onChange={(e) => setFilters({ ...filters, start_date: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
              <input
                type="date"
                value={filters.end_date || ''}
                onChange={(e) => setFilters({ ...filters, end_date: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Device</label>
              <select
                value={filters.device_id || ''}
                onChange={(e) => setFilters({ ...filters, device_id: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">All Devices</option>
                {devices?.map((d) => (
                  <option key={d.device_id} value={d.device_id}>
                    {d.device_name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Camera</label>
              <select
                value={filters.camera_id || ''}
                onChange={(e) => setFilters({ ...filters, camera_id: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">All Cameras</option>
                {cameras
                  ?.filter((c) => !filters.device_id || c.device_id === filters.device_id)
                  .map((c) => (
                    <option key={c.camera_id} value={c.camera_id}>
                      {c.camera_name}
                    </option>
                  ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Event Type</label>
              <select
                value={filters.event_type || ''}
                onChange={(e) => setFilters({ ...filters, event_type: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">All Types</option>
                {EventTypes.map((t) => (
                  <option key={t} value={t}>
                    {t.replace(/_/g, ' ')}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Severity</label>
              <select
                value={filters.severity || ''}
                onChange={(e) => setFilters({ ...filters, severity: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">All Severities</option>
                {Severities.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Ack Status</label>
              <select
                value={filters.ack_status || ''}
                onChange={(e) => setFilters({ ...filters, ack_status: e.target.value, page: 1 })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">All</option>
                <option value="unconfirmed">Unconfirmed</option>
                <option value="confirmed">Confirmed</option>
              </select>
            </div>
            <div className="flex items-end">
              <button
                onClick={() => setFilters({ page: 1, page_size: 20 })}
                className="px-3 py-2 text-sm border rounded-lg hover:bg-gray-50"
              >
                Clear Filters
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <LoadingSpinner />
      ) : data?.items && data.items.length > 0 ? (
        <>
          <div className="bg-white rounded-lg border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left p-3">Time</th>
                  <th className="text-left p-3">Event Type</th>
                  <th className="text-left p-3">Severity</th>
                  <th className="text-left p-3">Device</th>
                  <th className="text-left p-3">Camera</th>
                  <th className="text-left p-3">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((event) => (
                  <tr
                    key={event.event_id}
                    className="border-b hover:bg-gray-50 cursor-pointer"
                    onClick={() => navigate(`/events/${event.event_id}`)}
                  >
                    <td className="p-3 text-xs">{dayjs(event.timestamp).format('YYYY-MM-DD HH:mm:ss')}</td>
                    <td className="p-3">{event.event_type.replace(/_/g, ' ')}</td>
                    <td className="p-3">
                      <SeverityBadge severity={event.severity} />
                    </td>
                    <td className="p-3">{event.device_id}</td>
                    <td className="p-3">{event.camera_id}</td>
                    <td className="p-3">
                      <span
                        className={`text-xs px-2 py-0.5 rounded ${
                          event.ack_status === 'confirmed'
                            ? 'bg-green-100 text-green-800'
                            : 'bg-yellow-100 text-yellow-800'
                        }`}
                      >
                        {event.ack_status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={filters.page || 1}
            pageSize={filters.page_size || 20}
            total={data.total}
            onPageChange={(page) => setFilters({ ...filters, page })}
          />
        </>
      ) : (
        <EmptyState message="No events found" />
      )}
    </div>
  );
}
