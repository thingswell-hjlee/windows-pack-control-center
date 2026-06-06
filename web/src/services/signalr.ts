import * as signalR from '@microsoft/signalr';
import type { NormalizedEvent } from '../types';

export type DeviceStatusUpdate = {
  device_id: string;
  status: string;
};

export type CameraStatusUpdate = {
  camera_id: string;
  status: string;
};

let connection: signalR.HubConnection | null = null;
let subscriberCount = 0;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/events')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }
  return connection;
}

export async function startConnection(): Promise<void> {
  subscriberCount++;
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
}

export async function stopConnection(): Promise<void> {
  subscriberCount = Math.max(0, subscriberCount - 1);
  if (subscriberCount === 0) {
    const conn = getConnection();
    if (conn.state === signalR.HubConnectionState.Connected) {
      await conn.stop();
    }
  }
}

export function onNewEvent(callback: (event: NormalizedEvent) => void): void {
  const conn = getConnection();
  conn.on('NewEvent', callback);
}

export function offNewEvent(callback: (event: NormalizedEvent) => void): void {
  const conn = getConnection();
  conn.off('NewEvent', callback);
}

export function onDeviceStatusChanged(callback: (update: DeviceStatusUpdate) => void): void {
  const conn = getConnection();
  conn.on('DeviceStatusChanged', callback);
}

export function offDeviceStatusChanged(callback: (update: DeviceStatusUpdate) => void): void {
  const conn = getConnection();
  conn.off('DeviceStatusChanged', callback);
}

export function onCameraStatusChanged(callback: (update: CameraStatusUpdate) => void): void {
  const conn = getConnection();
  conn.on('CameraStatusChanged', callback);
}

export function offCameraStatusChanged(callback: (update: CameraStatusUpdate) => void): void {
  const conn = getConnection();
  conn.off('CameraStatusChanged', callback);
}
