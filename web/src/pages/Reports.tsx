import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Download } from 'lucide-react';
import { getDevices, getEventsExportCsvUrl } from '../services/api';
import type { EventFilters } from '../services/api';
import { EventTypes, Severities } from '../types';

export function Reports() {
  const [filters, setFilters] = useState<EventFilters>({});

  const { data: devices } = useQuery({
    queryKey: ['devices'],
    queryFn: getDevices,
  });

  function handleExportCsv() {
    const url = getEventsExportCsvUrl(filters);
    window.open(url, '_blank');
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Reports</h1>

      <div className="bg-white rounded-lg border p-6 space-y-4">
        <h2 className="text-lg font-semibold">Export Events</h2>
        <p className="text-sm text-gray-600">
          Select filters and export events data as a CSV file.
        </p>

        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
            <input
              type="date"
              value={filters.start_date || ''}
              onChange={(e) => setFilters({ ...filters, start_date: e.target.value })}
              className="w-full border rounded-md p-2 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
            <input
              type="date"
              value={filters.end_date || ''}
              onChange={(e) => setFilters({ ...filters, end_date: e.target.value })}
              className="w-full border rounded-md p-2 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Device</label>
            <select
              value={filters.device_id || ''}
              onChange={(e) => setFilters({ ...filters, device_id: e.target.value })}
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
            <label className="block text-sm font-medium text-gray-700 mb-1">Event Type</label>
            <select
              value={filters.event_type || ''}
              onChange={(e) => setFilters({ ...filters, event_type: e.target.value })}
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
            <label className="block text-sm font-medium text-gray-700 mb-1">Severity</label>
            <select
              value={filters.severity || ''}
              onChange={(e) => setFilters({ ...filters, severity: e.target.value })}
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
        </div>

        <div className="pt-4 border-t">
          <button
            onClick={handleExportCsv}
            className="flex items-center gap-2 bg-green-600 text-white px-6 py-2.5 rounded-lg hover:bg-green-700"
          >
            <Download className="w-4 h-4" /> Export CSV
          </button>
        </div>
      </div>
    </div>
  );
}
