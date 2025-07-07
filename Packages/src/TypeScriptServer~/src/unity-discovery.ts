import * as net from 'net';
import { UNITY_CONNECTION, POLLING } from './constants.js';
import { infoToFile, errorToFile } from './utils/log-to-file.js';
import { UnityClient } from './unity-client.js';

/**
 * Unity Discovery Service
 * Actively discovers Unity instances on various ports
 */
export class UnityDiscovery {
  private discoveryInterval: NodeJS.Timeout | null = null;
  private unityClient: UnityClient;
  private onDiscoveredCallback: ((port: number) => Promise<void>) | null = null;

  constructor(unityClient: UnityClient) {
    this.unityClient = unityClient;
  }

  /**
   * Set callback for when Unity is discovered
   */
  setOnDiscoveredCallback(callback: (port: number) => Promise<void>): void {
    this.onDiscoveredCallback = callback;
  }

  /**
   * Start Unity discovery polling
   */
  start(): void {
    if (this.discoveryInterval) {
      return; // Already running
    }

    infoToFile('[Unity Discovery] Starting Unity discovery...');
    
    // Immediate discovery attempt
    this.discover();
    
    // Set up periodic discovery
    this.discoveryInterval = setInterval(() => {
      this.discover();
    }, POLLING.INTERVAL_MS); // Use standard interval for more frequent discovery
  }

  /**
   * Stop Unity discovery polling
   */
  stop(): void {
    if (this.discoveryInterval) {
      clearInterval(this.discoveryInterval);
      this.discoveryInterval = null;
      infoToFile('[Unity Discovery] Unity discovery stopped');
    }
  }

  /**
   * Discover Unity by checking default port range
   */
  private async discover(): Promise<void> {
    if (this.unityClient.connected) {
      this.stop(); // Stop discovery once connected
      return;
    }

    const basePort = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
    // Expanded port range for better Unity discovery
    const portRange = [
      basePort,
      basePort + 100,
      basePort + 200,
      basePort + 300,
      basePort + 400,
      basePort - 100  // Also check lower ports
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
    
    const basePort = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
    const portRange = [
      basePort,
      basePort + 100,
      basePort + 200,
      basePort + 300,
      basePort + 400,
      basePort - 100
    ];
    
    for (const port of portRange) {
      try {
        if (await this.isUnityAvailable(port)) {
          this.unityClient.updatePort(port);
          if (this.onDiscoveredCallback) {
            await this.onDiscoveredCallback(port);
          }
          return true;
        }
      } catch (error) {
        // Silent polling - expected failures when Unity is not running on this port
        // Continue checking other ports
      }
    }
    
    return false;
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
}