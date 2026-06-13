interface StatusBadgeProps {
  status: string;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const colorMap: Record<string, string> = {
    online: 'bg-green-100 text-green-800',
    offline: 'bg-red-100 text-red-800',
    warning: 'bg-yellow-100 text-yellow-800',
    unknown: 'bg-gray-100 text-gray-600',
    disabled: 'bg-slate-100 text-slate-500',
  };

  const classes = colorMap[status.toLowerCase()] || 'bg-gray-100 text-gray-800';

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${classes}`}>
      {status}
    </span>
  );
}
