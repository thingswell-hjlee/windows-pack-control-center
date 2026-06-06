import axios from 'axios';
import type {
  Device,
  Camera,
  NormalizedEvent,
  DashboardSummary,
  PaginatedResponse,
} from '../types';

const api = axios.create({
  baseURL: '/api',
});

// Dashboard
export async function getDashboardSummary(): Promise<DashboardSummary> {
  const { data } = await api.get<DashboardSummary>('/dashboard/summary');
  return data;
}

// Devices
export async function getDevices(): Promise<Device[]> {
  const { data } = await api.get<Device[]>('/devices');
  return data;
}

export async function getDevice(id: string): Promise<Device> {
  const { data } = await api.get<Device>(`/devices/${id}`);
  return data;
}

export async function createDevice(device: Partial<Device>): Promise<Device> {
  const { data } = await api.post<Device>('/devices', device);
  return data;
}

export async function updateDevice(id: string, device: Partial<Device>): Promise<Device> {
  const { data } = await api.put<Device>(`/devices/${id}`, device);
  return data;
}

export async function deleteDevice(id: string): Promise<void> {
  await api.delete(`/devices/${id}`);
}

// Cameras
export async function getCameras(): Promise<Camera[]> {
  const { data } = await api.get<Camera[]>('/cameras');
  return data;
}

export async function getCamera(id: string): Promise<Camera> {
  const { data } = await api.get<Camera>(`/cameras/${id}`);
  return data;
}

export async function createCamera(camera: Partial<Camera>): Promise<Camera> {
  const { data } = await api.post<Camera>('/cameras', camera);
  return data;
}

export async function updateCamera(id: string, camera: Partial<Camera>): Promise<Camera> {
  const { data } = await api.put<Camera>(`/cameras/${id}`, camera);
  return data;
}

export async function deleteCamera(id: string): Promise<void> {
  await api.delete(`/cameras/${id}`);
}

// Events
export interface EventFilters {
  page?: number;
  page_size?: number;
  device_id?: string;
  camera_id?: string;
  event_type?: string;
  severity?: string;
  ack_status?: string;
  start_date?: string;
  end_date?: string;
}

export async function getEvents(filters: EventFilters = {}): Promise<PaginatedResponse<NormalizedEvent>> {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      params.append(key, String(value));
    }
  });
  const { data } = await api.get<PaginatedResponse<NormalizedEvent>>(`/events?${params.toString()}`);
  return data;
}

export async function getEvent(id: string): Promise<NormalizedEvent> {
  const { data } = await api.get<NormalizedEvent>(`/events/${id}`);
  return data;
}

export async function acknowledgeEvent(id: string, user: string): Promise<void> {
  await api.put(`/events/${id}/ack`, { ack_user: user });
}

export async function updateEventMemo(id: string, memo: string): Promise<void> {
  await api.put(`/events/${id}/memo`, { action_memo: memo });
}

export function getEventsExportCsvUrl(filters: EventFilters = {}): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      params.append(key, String(value));
    }
  });
  return `/api/events/export/csv?${params.toString()}`;
}

export function getEventsExportExcelUrl(filters: EventFilters = {}): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      params.append(key, String(value));
    }
  });
  return `/api/events/export/excel?${params.toString()}`;
}
