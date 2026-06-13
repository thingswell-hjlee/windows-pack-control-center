import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Filter } from 'lucide-react';
import { getCameras, createCamera, updateCamera, deleteCamera, getDevices } from '../services/api';
import { useSignalR } from '../hooks/useSignalR';
import { StatusBadge } from '../components/StatusBadge';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { Modal } from '../components/Modal';
import type { Camera } from '../types';

const emptyCamera: Partial<Camera> = {
  camera_id: '',
  camera_name: '',
  site_id: '',
  site_name: '',
  device_id: '',
  channel_no: 1,
  location: '',
  ip_address: '',
  rtsp_url: '',
  onvif_enabled: false,
  onvif_host: '',
  onvif_username: '',
  onvif_password: '',
  enabled: true,
  description: '',
};

export function Cameras() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingCamera, setEditingCamera] = useState<Partial<Camera> | null>(null);
  const [formData, setFormData] = useState<Partial<Camera>>(emptyCamera);
  const [filterDeviceId, setFilterDeviceId] = useState<string>('');

  // Real-time SignalR updates for camera status changes
  useSignalR({
    onCameraStatusChanged: () => {
      queryClient.invalidateQueries({ queryKey: ['cameras'] });
    },
  });

  const { data: cameras, isLoading } = useQuery({
    queryKey: ['cameras'],
    queryFn: getCameras,
  });

  const { data: devices } = useQuery({
    queryKey: ['devices'],
    queryFn: getDevices,
  });

  const createMutation = useMutation({
    mutationFn: (camera: Partial<Camera>) => createCamera(camera),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cameras'] });
      closeModal();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, camera }: { id: string; camera: Partial<Camera> }) => updateCamera(id, camera),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cameras'] });
      closeModal();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCamera(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cameras'] });
    },
  });

  function openAddModal() {
    setEditingCamera(null);
    setFormData(emptyCamera);
    setModalOpen(true);
  }

  function openEditModal(camera: Camera) {
    setEditingCamera(camera);
    setFormData(camera);
    setModalOpen(true);
  }

  function closeModal() {
    setModalOpen(false);
    setEditingCamera(null);
    setFormData(emptyCamera);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (editingCamera) {
      updateMutation.mutate({ id: editingCamera.camera_id!, camera: formData });
    } else {
      createMutation.mutate(formData);
    }
  }

  function handleDelete(id: string) {
    if (confirm('Are you sure you want to delete this camera?')) {
      deleteMutation.mutate(id);
    }
  }

  // Filter cameras by selected device
  const filteredCameras = filterDeviceId
    ? cameras?.filter((camera) => camera.device_id === filterDeviceId)
    : cameras;

  // Group cameras by device
  const groupedCameras = filteredCameras?.reduce(
    (acc, camera) => {
      const deviceId = camera.device_id || 'unassigned';
      if (!acc[deviceId]) acc[deviceId] = [];
      acc[deviceId].push(camera);
      return acc;
    },
    {} as Record<string, Camera[]>
  );

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Cameras</h1>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-gray-500" />
            <select
              value={filterDeviceId}
              onChange={(e) => setFilterDeviceId(e.target.value)}
              className="border rounded-md px-3 py-2 text-sm"
            >
              <option value="">All Devices</option>
              {devices?.map((d) => (
                <option key={d.device_id} value={d.device_id}>
                  {d.device_name}
                </option>
              ))}
            </select>
          </div>
          <button
            onClick={openAddModal}
            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            <Plus className="w-4 h-4" /> Add Camera
          </button>
        </div>
      </div>

      {groupedCameras && Object.keys(groupedCameras).length > 0 ? (
        <div className="space-y-4">
          {Object.entries(groupedCameras).map(([deviceId, deviceCameras]) => {
            const device = devices?.find((d) => d.device_id === deviceId);
            return (
              <div key={deviceId} className="bg-white rounded-lg border overflow-hidden">
                <div className="bg-gray-50 px-4 py-2 border-b">
                  <span className="font-medium text-sm">
                    {device?.device_name || deviceId}
                  </span>
                </div>
                <table className="w-full text-sm">
                  <thead className="border-b">
                    <tr>
                      <th className="text-left p-3">Camera Name</th>
                      <th className="text-left p-3">Channel</th>
                      <th className="text-left p-3">Location</th>
                      <th className="text-left p-3">IP Address</th>
                      <th className="text-left p-3">Status</th>
                      <th className="text-left p-3">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deviceCameras.map((camera) => (
                      <tr key={camera.camera_id} className="border-b hover:bg-gray-50">
                        <td className="p-3 font-medium">{camera.camera_name}</td>
                        <td className="p-3">{camera.channel_no}</td>
                        <td className="p-3">{camera.location}</td>
                        <td className="p-3 font-mono text-xs">{camera.ip_address}</td>
                        <td className="p-3">
                          <StatusBadge status={camera.status} />
                        </td>
                        <td className="p-3">
                          <div className="flex gap-2">
                            <button onClick={() => openEditModal(camera)} className="p-1 hover:bg-gray-200 rounded">
                              <Pencil className="w-4 h-4 text-gray-600" />
                            </button>
                            <button onClick={() => handleDelete(camera.camera_id)} className="p-1 hover:bg-gray-200 rounded">
                              <Trash2 className="w-4 h-4 text-red-600" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            );
          })}
        </div>
      ) : (
        <EmptyState message="No cameras registered yet" />
      )}

      <Modal open={modalOpen} onClose={closeModal} title={editingCamera ? 'Edit Camera' : 'Add Camera'}>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Camera ID" value={formData.camera_id || ''} onChange={(v) => setFormData({ ...formData, camera_id: v })} disabled={!!editingCamera} />
            <FormField label="Camera Name" value={formData.camera_name || ''} onChange={(v) => setFormData({ ...formData, camera_name: v })} />
            <FormField label="Site ID" value={formData.site_id || ''} onChange={(v) => setFormData({ ...formData, site_id: v })} />
            <FormField label="Site Name" value={formData.site_name || ''} onChange={(v) => setFormData({ ...formData, site_name: v })} />
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Device</label>
              <select
                value={formData.device_id || ''}
                onChange={(e) => setFormData({ ...formData, device_id: e.target.value })}
                className="w-full border rounded-md p-2 text-sm"
              >
                <option value="">Select device</option>
                {devices?.map((d) => (
                  <option key={d.device_id} value={d.device_id}>
                    {d.device_name}
                  </option>
                ))}
              </select>
            </div>
            <FormField label="Channel No" value={String(formData.channel_no || 1)} onChange={(v) => setFormData({ ...formData, channel_no: parseInt(v) || 1 })} />
            <FormField label="Location" value={formData.location || ''} onChange={(v) => setFormData({ ...formData, location: v })} />
            <FormField label="IP Address" value={formData.ip_address || ''} onChange={(v) => setFormData({ ...formData, ip_address: v })} />
            <div className="col-span-2">
              <FormField label="RTSP URL" value={formData.rtsp_url || ''} onChange={(v) => setFormData({ ...formData, rtsp_url: v })} />
            </div>
            <FormField label="ONVIF Host" value={formData.onvif_host || ''} onChange={(v) => setFormData({ ...formData, onvif_host: v })} />
            <FormField label="ONVIF Username" value={formData.onvif_username || ''} onChange={(v) => setFormData({ ...formData, onvif_username: v })} />
            <FormField label="ONVIF Password" value={formData.onvif_password || ''} onChange={(v) => setFormData({ ...formData, onvif_password: v })} type="password" />
          </div>
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={formData.onvif_enabled ?? false}
                onChange={(e) => setFormData({ ...formData, onvif_enabled: e.target.checked })}
                className="rounded"
              />
              ONVIF Enabled
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={formData.enabled ?? true}
                onChange={(e) => setFormData({ ...formData, enabled: e.target.checked })}
                className="rounded"
              />
              Enabled
            </label>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full border rounded-md p-2 text-sm"
              rows={2}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" onClick={closeModal} className="px-4 py-2 border rounded-lg hover:bg-gray-50">
              Cancel
            </button>
            <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
              {editingCamera ? 'Update' : 'Create'}
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
