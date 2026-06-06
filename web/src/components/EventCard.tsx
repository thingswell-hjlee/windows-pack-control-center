import dayjs from 'dayjs';
import { SeverityBadge } from './SeverityBadge';
import type { NormalizedEvent } from '../types';

interface EventCardProps {
  event: NormalizedEvent;
  onClick?: () => void;
}

export function EventCard({ event, onClick }: EventCardProps) {
  return (
    <div
      className="border rounded-lg p-3 hover:bg-gray-50 cursor-pointer transition-colors"
      onClick={onClick}
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <SeverityBadge severity={event.severity} />
          <span className="text-sm font-medium">{event.event_type.replace(/_/g, ' ')}</span>
        </div>
        <span className="text-xs text-gray-500">
          {dayjs(event.timestamp).format('HH:mm:ss')}
        </span>
      </div>
      <div className="mt-1 text-xs text-gray-500">
        Device: {event.device_id} | Camera: {event.camera_id}
      </div>
    </div>
  );
}
