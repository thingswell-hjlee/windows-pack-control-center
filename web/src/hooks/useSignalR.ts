import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import {
  getConnection,
  startConnection,
  stopConnection,
  onNewEvent,
  offNewEvent,
  onDeviceStatusChanged,
  offDeviceStatusChanged,
  onCameraStatusChanged,
  offCameraStatusChanged,
  onEventAcknowledged,
  offEventAcknowledged,
  onEventMemoUpdated,
  offEventMemoUpdated,
} from '../services/signalr';
import type { NormalizedEvent } from '../types';
import type { DeviceStatusUpdate, CameraStatusUpdate, EventAcknowledgedUpdate, EventMemoUpdatedUpdate } from '../services/signalr';

export function useSignalR(handlers?: {
  onNewEvent?: (event: NormalizedEvent) => void;
  onDeviceStatusChanged?: (update: DeviceStatusUpdate) => void;
  onCameraStatusChanged?: (update: CameraStatusUpdate) => void;
  onEventAcknowledged?: (update: EventAcknowledgedUpdate) => void;
  onEventMemoUpdated?: (update: EventMemoUpdatedUpdate) => void;
}) {
  const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    const conn = getConnection();

    const updateState = () => setConnectionState(conn.state);

    conn.onreconnecting(updateState);
    conn.onreconnected(updateState);
    conn.onclose(updateState);

    const newEventHandler = (event: NormalizedEvent) => {
      handlersRef.current?.onNewEvent?.(event);
    };
    const deviceStatusHandler = (update: DeviceStatusUpdate) => {
      handlersRef.current?.onDeviceStatusChanged?.(update);
    };
    const cameraStatusHandler = (update: CameraStatusUpdate) => {
      handlersRef.current?.onCameraStatusChanged?.(update);
    };
    const eventAckHandler = (update: EventAcknowledgedUpdate) => {
      handlersRef.current?.onEventAcknowledged?.(update);
    };
    const eventMemoHandler = (update: EventMemoUpdatedUpdate) => {
      handlersRef.current?.onEventMemoUpdated?.(update);
    };

    onNewEvent(newEventHandler);
    onDeviceStatusChanged(deviceStatusHandler);
    onCameraStatusChanged(cameraStatusHandler);
    onEventAcknowledged(eventAckHandler);
    onEventMemoUpdated(eventMemoHandler);

    startConnection().then(updateState).catch(() => updateState());

    return () => {
      offNewEvent(newEventHandler);
      offDeviceStatusChanged(deviceStatusHandler);
      offCameraStatusChanged(cameraStatusHandler);
      offEventAcknowledged(eventAckHandler);
      offEventMemoUpdated(eventMemoHandler);
      stopConnection();
    };
  }, []);

  return { connectionState };
}
