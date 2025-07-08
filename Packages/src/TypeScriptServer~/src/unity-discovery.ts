import * as net from 'net';
import { UNITY_CONNECTION, POLLING } from './constants.js';
import { infoToFile, errorToFile, debugToFile } from './utils/log-to-file.js';
import { UnityClient } from './unity-client.js';

/**
 * Unity Discovery Service with Unified Connection Management
 * Handles both Unity discovery and connection polling in a single timer
 * to prevent multiple concurrent timers and improve stability.
 *
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityClient: TCP client that this service manages discovery for
 * - UnityConnectionManager: Higher-level connection manager that uses this service
 * - UnityMcpServer: Main server that initiates discovery
 */
export class UnityDiscovery {
  private discoveryInterval: NodeJS.Timeout | null = null;
  private unityClient: UnityClient;
  private onDiscoveredCallback: ((port: number) => Promise<void>) | null = null;
  private onConnectionLostCallback: (() => void) | null = null;
  private isDiscovering: boolean = false;
  private isDevelopment: boolean = false;

  // Singleton pattern to prevent multiple instances
  private static instance: UnityDiscovery | null = null;
  private static activeTimerCount: number = 0; // Track active timers for debugging

  constructor(unityClient: UnityClient) {
    this.unityClient = unityClient;
    this.isDevelopment = process.env.NODE_ENV === 'development';

    // Singleton enforcement
    if (UnityDiscovery.instance) {
      if (this.isDevelopment) {
        debugToFile(
          '[UnityDiscovery] WARNING: Multiple instances detected. Using singleton pattern.',
        );
        debugToFile(`[UnityDiscovery] Active timer count: ${UnityDiscovery.activeTimerCount}`);
      }
      return UnityDiscovery.instance;
    }
    UnityDiscovery.instance = this;
  }

  /**
   * Set callback for when Unity is discovered
   */
  setOnDiscoveredCallback(callback: (port: number) => Promise<void>): void {
    this.onDiscoveredCallback = callback;
  }

  /**
   * Set callback for when connection is lost
   */
  setOnConnectionLostCallback(callback: () => void): void {
    this.onConnectionLostCallback = callback;
  }

  /**
   * Start Unity discovery polling with unified connection management
   */
  start(): void {
    if (this.discoveryInterval) {
      if (this.isDevelopment) {
        debugToFile('[UnityDiscovery] Timer already running - skipping duplicate start');
      }
      return; // Already running
    }

    if (this.isDiscovering) {
      if (this.isDevelopment) {
        debugToFile('[UnityDiscovery] Discovery already in progress - skipping start');
      }
      return; // Already discovering
    }

    infoToFile('[Unity Discovery] Starting unified discovery and connection management...');

    // Immediate discovery attempt
    void this.unifiedDiscoveryAndConnectionCheck();

    // Set up periodic unified discovery and connection checking
    this.discoveryInterval = setInterval(() => {
      void this.unifiedDiscoveryAndConnectionCheck();
    }, POLLING.INTERVAL_MS);

    // Track active timer count for debugging
    UnityDiscovery.activeTimerCount++;

    if (this.isDevelopment) {
      debugToFile(`[UnityDiscovery] Timer started with ${POLLING.INTERVAL_MS}ms interval`);
      debugToFile(`[UnityDiscovery] Active timer count: ${UnityDiscovery.activeTimerCount}`);
      this.logTimerStatus();
    }
  }

  /**
   * Stop Unity discovery polling
   */
  stop(): void {
    if (this.discoveryInterval) {
      clearInterval(this.discoveryInterval);
      this.discoveryInterval = null;
      this.isDiscovering = false;

      // Track active timer count for debugging
      UnityDiscovery.activeTimerCount = Math.max(0, UnityDiscovery.activeTimerCount - 1);

      infoToFile('[Unity Discovery] Unity discovery and connection management stopped');

      if (this.isDevelopment) {
        debugToFile('[UnityDiscovery] Timer stopped and cleanup completed');
        debugToFile(`[UnityDiscovery] Active timer count: ${UnityDiscovery.activeTimerCount}`);
        this.logTimerStatus();
      }
    }
  }

  /**
   * Unified discovery and connection checking
   * Handles both Unity discovery and connection health monitoring
   */
  private async unifiedDiscoveryAndConnectionCheck(): Promise<void> {
    if (this.isDiscovering) {
      if (this.isDevelopment) {
        debugToFile('[UnityDiscovery] Discovery already in progress - skipping');
      }
      return;
    }

    this.isDiscovering = true;

    try {
      // If already connected, check connection health
      if (this.unityClient.connected) {
        const isConnectionHealthy = await this.checkConnectionHealth();

        if (isConnectionHealthy) {
          // Connection is healthy - stop discovery
          this.stop();
          return;
        } else {
          // Connection lost - trigger callback and continue discovery
          if (this.isDevelopment) {
            debugToFile('[UnityDiscovery] Connection lost - resuming discovery');
          }

          if (this.onConnectionLostCallback) {
            this.onConnectionLostCallback();
          }
        }
      }

      // Discover Unity by checking port range
      await this.discoverUnityOnPorts();
    } finally {
      this.isDiscovering = false;
    }
  }

  /**
   * Check if the current connection is healthy
   */
  private async checkConnectionHealth(): Promise<boolean> {
    try {
      return await this.unityClient.testConnection();
    } catch (error) {
      return false;
    }
  }

  /**
   * Discover Unity by checking default port range
   */
  private async discoverUnityOnPorts(): Promise<void> {
    const basePort = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
    // Expanded port range for better Unity discovery
    const portRange = [
      basePort,
      basePort + 100,
      basePort + 200,
      basePort + 300,
      basePort + 400,
      basePort - 100, // Also check lower ports
    ];

    for (const port of portRange) {
      try {
        if (await this.isUnityAvailable(port)) {
          infoToFile(`[Unity Discovery] Unity discovered on port ${port}`);

          // Update client port and notify callback
          this.unityClient.updatePort(port);
          if (this.onDiscoveredCallback) {
            await this.onDiscoveredCallback(port);
          }
          return;
        }
      } catch (error) {
        // Silent polling - expected failures when Unity is not running on this port
        // Continue checking other ports
      }
    }
  }

  /**
   * Force immediate Unity discovery for connection recovery
   */
  async forceDiscovery(): Promise<boolean> {
    if (this.unityClient.connected) {
      return true;
    }

    infoToFile('[Unity Discovery] Force discovery initiated');

    // Use the unified discovery method
    await this.unifiedDiscoveryAndConnectionCheck();

    return this.unityClient.connected;
  }

  /**
   * Handle connection lost event (called by UnityClient)
   */
  handleConnectionLost(): void {
    if (this.isDevelopment) {
      debugToFile('[UnityDiscovery] Connection lost event received - restarting discovery');
    }

    // Restart discovery if not already running
    if (!this.discoveryInterval) {
      this.start();
    }

    // Trigger callback
    if (this.onConnectionLostCallback) {
      this.onConnectionLostCallback();
    }
  }

  /**
   * Check if Unity is available on specific port
   */
  private async isUnityAvailable(port: number): Promise<boolean> {
    return new Promise((resolve) => {
      const socket = new net.Socket();
      const timeout = 500; // Shorter timeout for faster discovery

      const timer = setTimeout(() => {
        socket.destroy();
        resolve(false);
      }, timeout);

      socket.connect(port, UNITY_CONNECTION.DEFAULT_HOST, () => {
        clearTimeout(timer);
        socket.destroy();
        resolve(true);
      });

      socket.on('error', () => {
        clearTimeout(timer);
        resolve(false);
      });
    });
  }

  /**
   * Log current timer status for debugging (development mode only)
   */
  private logTimerStatus(): void {
    if (!this.isDevelopment) {
      return;
    }

    const status = {
      isTimerActive: this.discoveryInterval !== null,
      isDiscovering: this.isDiscovering,
      activeTimerCount: UnityDiscovery.activeTimerCount,
      isConnected: this.unityClient.connected,
      intervalMs: POLLING.INTERVAL_MS,
      timestamp: new Date().toISOString(),
    };

    debugToFile('[UnityDiscovery] Timer Status:', status);

    // Warning if multiple timers are detected
    if (UnityDiscovery.activeTimerCount > 1) {
      errorToFile(
        `[UnityDiscovery] WARNING: Multiple timers detected! Count: ${UnityDiscovery.activeTimerCount}`,
      );
    }
  }

  /**
   * Get debugging information about current timer state
   */
  getDebugInfo(): object {
    return {
      isTimerActive: this.discoveryInterval !== null,
      isDiscovering: this.isDiscovering,
      activeTimerCount: UnityDiscovery.activeTimerCount,
      isConnected: this.unityClient.connected,
      intervalMs: POLLING.INTERVAL_MS,
      hasSingleton: UnityDiscovery.instance !== null,
    };
  }
}
