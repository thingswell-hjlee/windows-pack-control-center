import { Inbox } from 'lucide-react';

interface EmptyStateProps {
  message?: string;
}

export function EmptyState({ message = 'No data available' }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-gray-500">
      <Inbox className="w-12 h-12 mb-2" />
      <p className="text-sm">{message}</p>
    </div>
  );
}
