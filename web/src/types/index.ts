export interface Device {
  device_id: string;
  device_name: string;
  site_id: string;
  site_name: string;
  location: string;
  ip_address: string;
  mqtt_host: string;
  mqtt_port: number;
  mqtt_username: string;
  mqtt_password: string;
  event_topic: string;
  status_topic: string;
  enabled: boolean;
  description: string;
  created_at: string;
  updated_at: string;
  status: string;
}

export interface Camera {
  camera_id: string;
  camera_name: string;
  site_id: string;
  site_name: string;
  device_id: string;
  channel_no: number;
  location: string;
  ip_address: string;
  rtsp_url: string;
  onvif_enabled: boolean;
  onvif_host: string;
  onvif_username: string;
  onvif_password: string;
  status: string;
  enabled: boolean;
  description: string;
  created_at: string;
  updated_at: string;
}

export interface BoundingBox {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface NormalizedEvent {
  event_id: string;
  schema_version: string;
  tenant_id: string;
  site_id: string;
  site_name: string | null;
  device_id: string;
  device_name: string | null;
  camera_id: string;
  camera_name: string | null;
  event_type: string;
  severity: string;
  timestamp: string;
  ts_ms: number;
  track_id: string;
  bbox: BoundingBox | null;
  confidence: number;
  roi_id: string;
  roi_name: string | null;
  snapshot_url: string;
  clip_url: string;
  model_version: string | null;
  edge_app_version: string | null;
  ack_status: string;
  ack_user: string;
  ack_time: string | null;
  action_memo: string;
  sync_status: string;
  sync_retry_count: number;
  last_sync_time: string | null;
  cloud_event_id: string | null;
  sync_error_message: string | null;
  raw_payload: string;
  received_at: string;
}

export interface DashboardSummary {
  total_devices: number;
  online_devices: number;
  offline_devices: number;
  total_cameras: number;
  online_cameras: number;
  unconfirmed_events: number;
  today_events: number;
  high_severity_today: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  page_size: number;
  total_pages: number;
}

export const EventTypes = [
  'FALL_DETECTED',
  'ROI_INTRUSION',
  'WORK_ZONE_ENTRY',
  'HAZARD_PROXIMITY',
  'WORK_NO_HELMET',
  'WORK_NO_VEST',
  'WORK_NO_MASK',
  'DEVICE_OFFLINE',
  'CAMERA_OFFLINE',
  'SYSTEM_WARNING',
  'UNKNOWN_EVENT',
] as const;

export type EventType = (typeof EventTypes)[number];

export const Severities = ['HIGH', 'MEDIUM', 'LOW'] as const;
export type Severity = (typeof Severities)[number];
