import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { getDevices, createDevice, updateDevice, deleteDevice, getCameras } from '../services/api';
import { StatusBadge } from '../components/StatusBadge';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { Modal } from '../components/Modal';
import type { Device } from '../types';

const emptyDevice: Partial<Device> = {
  device_id: '',
  device_name: '',
  site_id: '',
  site_name: '',
  location: '',
  ip_address: '',
  mqtt_host: '',
  mqtt_port: 1883,
  mqtt_username: '',
  mqtt_password: '',
  event_topic: '',
  status_topic: '',
  enabled: true,
  description: '',
};

export function Devices() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingDevice, setEditingDevice] = useState<Partial<Device> | null>(null);
  const [formData, setFormData] = useState<Partial<Device>>(emptyDevice);

  const { data: devices, isLoading } = useQuery({
    queryKey: ['devices'],
    queryFn: getDevices,
  });

  const { data: cameras } = useQuery({
    queryKey: ['cameras'],
    queryFn: getCameras,
  });

  // Count cameras per device
  const cameraCountByDevice = (cameras || []).reduce<Record<string, number>>((acc, cam) => {
    acc[cam.device_id] = (acc[cam.device_id] || 0) + 1;
    return acc;
  }, {});

  // Derive effective status considering enabled flag
  function getEffectiveStatus(device: Device): string {
    if (!device.enabled) return 'disabled';
    return device.status || 'unknown';
  }

  const createMutation = useMutation({
    mutationFn: (device: Partial<Device>) => createDevice(device),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      closeModal();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, device }: { id: string; device: Partial<Device> }) => updateDevice(id, device),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      closeModal();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
    },
  });

  function openAddModal() {
    setEditingDevice(null);
    setFormData(emptyDevice);
    setModalOpen(true);
  }

  function openEditModal(device: Device) {
    setEditingDevice(device);
    setFormData(device);
    setModalOpen(true);
  }

  function closeModal() {
    setModalOpen(false);
    setEditingDevice(null);
    setFormData(emptyDevice);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (editingDevice) {
      updateMutation.mutate({ id: editingDevice.device_id!, device: formData });
    } else {
      createMutation.mutate(formData);
    }
  }

  function handleDelete(id: string) {
    if (confirm('Are you sure you want to delete this device?')) {
      deleteMutation.mutate(id);
    }
  }

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">AIBox Devices</h1>
        <button
          onClick={openAddModal}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          <Plus className="w-4 h-4" /> Add Device
        </button>
      </div>

      {devices && devices.length > 0 ? (
        <div className="bg-white rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3">Device Name</th>
                <th className="text-left p-3">Site</th>
                <th className="text-left p-3">Location</th>
                <th className="text-left p-3">IP Address</th>
                <th className="text-left p-3">Status</th>
                <th className="text-left p-3">Cameras</th>
                <th className="text-left p-3">Last Updated</th>
                <th className="text-left p-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {devices.map((device) => (
                <tr key={device.device_id} className="border-b hover:bg-gray-50">
                  <td className="p-3 font-medium">{device.device_name}</td>
                  <td className="p-3">{device.site_name}</td>
                  <td className="p-3">{device.location}</td>
                  <td className="p-3 font-mono text-xs">{device.ip_address}</td>
                  <td className="p-3">
                    <StatusBadge status={getEffectiveStatus(device)} />
                  </td>
                  <td className="p-3 text-center">
                    <span className="inline-flex items-center gap-1 text-xs text-gray-600">
                      {cameraCountByDevice[device.device_id] || 0}
                    </span>
                  </td>
                  <td className="p-3 text-xs text-gray-500">
                    {device.updated_at ? new Date(device.updated_at).toLocaleString() : '-'}
                  </td>
                  <td className="p-3">
                    <div className="flex gap-2">
                      <button onClick={() => openEditModal(device)} className="p-1 hover:bg-gray-200 rounded">
                        <Pencil className="w-4 h-4 text-gray-600" />
                      </button>
                      <button onClick={() => handleDelete(device.device_id)} className="p-1 hover:bg-gray-200 rounded">
                        <Trash2 className="w-4 h-4 text-red-600" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <EmptyState message="No devices registered yet" />
      )}

      <Modal open={modalOpen} onClose={closeModal} title={editingDevice ? 'Edit Device' : 'Add Device'}>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Device ID" value={formData.device_id || ''} onChange={(v) => setFormData({ ...formData, device_id: v })} disabled={!!editingDevice} />
            <FormField label="Device Name" value={formData.device_name || ''} onChange={(v) => setFormData({ ...formData, device_name: v })} />
            <FormField label="Site ID" value={formData.site_id || ''} onChange={(v) => setFormData({ ...formData, site_id: v })} />
            <FormField label="Site Name" value={formData.site_name || ''} onChange={(v) => setFormData({ ...formData, site_name: v })} />
            <FormField label="Location" value={formData.location || ''} onChange={(v) => setFormData({ ...formData, location: v })} />
            <FormField label="IP Address" value={formData.ip_address || ''} onChange={(v) => setFormData({ ...formData, ip_address: v })} />
            <FormField label="MQTT Host" value={formData.mqtt_host || ''} onChange={(v) => setFormData({ ...formData, mqtt_host: v })} />
            <FormField label="MQTT Port" value={String(formData.mqtt_port || 1883)} onChange={(v) => setFormData({ ...formData, mqtt_port: parseInt(v) || 1883 })} />
            <FormField label="MQTT Username" value={formData.mqtt_username || ''} onChange={(v) => setFormData({ ...formData, mqtt_username: v })} />
            <FormField label="MQTT Password" value={formData.mqtt_password || ''} onChange={(v) => setFormData({ ...formData, mqtt_password: v })} type="password" />
            <FormField label="Event Topic" value={formData.event_topic || ''} onChange={(v) => setFormData({ ...formData, event_topic: v })} />
            <FormField label="Status Topic" value={formData.status_topic || ''} onChange={(v) => setFormData({ ...formData, status_topic: v })} />
          </div>
          <div className="col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full border rounded-md p-2 text-sm"
              rows={2}
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={formData.enabled ?? true}
              onChange={(e) => setFormData({ ...formData, enabled: e.target.checked })}
              className="rounded"
            />
            <label className="text-sm">Enabled</label>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" onClick={closeModal} className="px-4 py-2 border rounded-lg hover:bg-gray-50">
              Cancel
            </button>
            <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
              {editingDevice ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

function FormField({
  label,
  value,
  onChange,
  type = 'text',
  disabled = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  disabled?: boolean;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className="w-full border rounded-md p-2 text-sm disabled:bg-gray-100"
      />
    </div>
  );
}
