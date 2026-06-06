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
} from '../services/signalr';
import type { NormalizedEvent } from '../types';
import type { DeviceStatusUpdate, CameraStatusUpdate } from '../services/signalr';

export function useSignalR(handlers?: {
  onNewEvent?: (event: NormalizedEvent) => void;
  onDeviceStatusChanged?: (update: DeviceStatusUpdate) => void;
  onCameraStatusChanged?: (update: CameraStatusUpdate) => void;
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

    onNewEvent(newEventHandler);
    onDeviceStatusChanged(deviceStatusHandler);
    onCameraStatusChanged(cameraStatusHandler);

    startConnection().then(updateState).catch(() => updateState());

    return () => {
      offNewEvent(newEventHandler);
      offDeviceStatusChanged(deviceStatusHandler);
      offCameraStatusChanged(cameraStatusHandler);
      stopConnection();
    };
  }, []);

  return { connectionState };
}
