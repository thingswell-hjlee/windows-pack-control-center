import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Check, Save } from 'lucide-react';
import { getEvent, acknowledgeEvent, updateEventMemo } from '../services/api';
import { SeverityBadge } from '../components/SeverityBadge';
import { LoadingSpinner } from '../components/LoadingSpinner';
import dayjs from 'dayjs';

export function EventDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [memo, setMemo] = useState('');
  const [memoLoaded, setMemoLoaded] = useState(false);

  const { data: event, isLoading } = useQuery({
    queryKey: ['event', id],
    queryFn: () => getEvent(id!),
    enabled: !!id,
  });

  if (event && !memoLoaded) {
    setMemo(event.action_memo || '');
    setMemoLoaded(true);
  }

  const ackMutation = useMutation({
    mutationFn: () => acknowledgeEvent(id!, 'admin'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['event', id] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
    },
  });

  const memoMutation = useMutation({
    mutationFn: () => updateEventMemo(id!, memo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['event', id] });
    },
  });

  if (isLoading) return <LoadingSpinner />;
  if (!event) return <div className="p-4">Event not found</div>;

  return (
    <div className="space-y-4">
      <button
        onClick={() => navigate('/events')}
        className="flex items-center gap-1 text-sm text-gray-600 hover:text-gray-900"
      >
        <ArrowLeft className="w-4 h-4" /> Back to Events
      </button>

      <div className="bg-white rounded-lg border p-6 space-y-6">
        {/* Header */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-xl font-bold">{event.event_type.replace(/_/g, ' ')}</h1>
            <p className="text-sm text-gray-500 mt-1">
              {dayjs(event.timestamp).format('YYYY-MM-DD HH:mm:ss')}
            </p>
          </div>
          <SeverityBadge severity={event.severity} />
        </div>

        {/* Snapshot */}
        {event.snapshot_url && (
          <div>
            <h3 className="text-sm font-medium text-gray-700 mb-2">Snapshot</h3>
            <img
              src={event.snapshot_url}
              alt="Event snapshot"
              className="rounded-lg border max-w-full max-h-64 object-contain"
            />
          </div>
        )}

        {/* Event Details */}
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          <DetailField label="Event ID" value={event.event_id} />
          <DetailField label="Schema Version" value={event.schema_version} />
          <DetailField label="Tenant ID" value={event.tenant_id} />
          <DetailField label="Site ID" value={event.site_id} />
          <DetailField label="Device ID" value={event.device_id} />
          <DetailField label="Camera ID" value={event.camera_id} />
          <DetailField label="Event Type" value={event.event_type} />
          <DetailField label="Severity" value={event.severity} />
          <DetailField label="Timestamp (ms)" value={String(event.ts_ms)} />
          <DetailField label="Track ID" value={event.track_id} />
          <DetailField label="Confidence" value={event.confidence ? `${(event.confidence * 100).toFixed(1)}%` : 'N/A'} />
          <DetailField label="ROI ID" value={event.roi_id || 'N/A'} />
          <DetailField label="Sync Status" value={event.sync_status} />
          {event.bbox && (
            <DetailField
              label="Bounding Box"
              value={`x:${event.bbox.x} y:${event.bbox.y} w:${event.bbox.w} h:${event.bbox.h}`}
            />
          )}
        </div>

        {/* Acknowledge */}
        <div className="border-t pt-4">
          <h3 className="text-sm font-medium text-gray-700 mb-2">Acknowledgment</h3>
          <div className="flex items-center gap-4">
            <span
              className={`text-sm px-2 py-1 rounded ${
                event.ack_status === 'confirmed'
                  ? 'bg-green-100 text-green-800'
                  : 'bg-yellow-100 text-yellow-800'
              }`}
            >
              {event.ack_status}
            </span>
            {event.ack_user && (
              <span className="text-sm text-gray-500">
                by {event.ack_user} at {event.ack_time ? dayjs(event.ack_time).format('YYYY-MM-DD HH:mm:ss') : ''}
              </span>
            )}
            {event.ack_status !== 'confirmed' && (
              <button
                onClick={() => ackMutation.mutate()}
                disabled={ackMutation.isPending}
                className="flex items-center gap-1 px-3 py-1.5 bg-green-600 text-white text-sm rounded-lg hover:bg-green-700 disabled:opacity-50"
              >
                <Check className="w-4 h-4" /> Acknowledge
              </button>
            )}
          </div>
        </div>

        {/* Action Memo */}
        <div className="border-t pt-4">
          <h3 className="text-sm font-medium text-gray-700 mb-2">Action Memo</h3>
          <textarea
            value={memo}
            onChange={(e) => setMemo(e.target.value)}
            className="w-full border rounded-md p-3 text-sm"
            rows={4}
            placeholder="Enter action notes..."
          />
          <button
            onClick={() => memoMutation.mutate()}
            disabled={memoMutation.isPending}
            className="flex items-center gap-1 mt-2 px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            <Save className="w-4 h-4" /> Save Memo
          </button>
        </div>

        {/* Raw Payload */}
        {event.raw_payload && (
          <div className="border-t pt-4">
            <h3 className="text-sm font-medium text-gray-700 mb-2">Raw Payload</h3>
            <pre className="bg-gray-50 border rounded-md p-3 text-xs overflow-auto max-h-64">
              {(() => {
                try {
                  return JSON.stringify(JSON.parse(event.raw_payload), null, 2);
                } catch {
                  return event.raw_payload;
                }
              })()}
            </pre>
          </div>
        )}
      </div>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-gray-500">{label}</dt>
      <dd className="text-sm font-medium break-all">{value || '-'}</dd>
    </div>
  );
}
