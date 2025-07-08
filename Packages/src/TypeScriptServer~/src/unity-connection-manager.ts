import { UnityClient } from './unity-client.js';
import { UnityDiscovery } from './unity-discovery.js';
import { errorToFile, debugToFile, infoToFile } from './utils/log-to-file.js';
import { ENVIRONMENT } from './constants.js';

/**
 * Unity Connection Manager - Manages Unity connection and discovery functionality
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityClient: Manages TCP connection to Unity Editor
 * - UnityDiscovery: Handles Unity discovery and polling
 * - UnityMcpServer: Main server class that uses this manager
 *
 * Key features:
 * - Unity connection waiting and establishment
 * - Integration with discovery service
 * - Connection state monitoring
 * - Reconnection handling
 */
export class UnityConnectionManager {
  private unityClient: UnityClient;
  private unityDiscovery: UnityDiscovery;
  private readonly isDevelopment: boolean;
  private isInitialized: boolean = false;

  constructor(unityClient: UnityClient) {
    this.unityClient = unityClient;
    this.isDevelopment = process.env.NODE_ENV === ENVIRONMENT.NODE_ENV_DEVELOPMENT;

    // Initialize Unity discovery service (singleton pattern prevents duplicates)
    this.unityDiscovery = new UnityDiscovery(this.unityClient);

    // Set UnityDiscovery reference in UnityClient for unified connection management
    this.unityClient.setUnityDiscovery(this.unityDiscovery);
  }

  /**
   * Get Unity discovery instance
   */
  getUnityDiscovery(): UnityDiscovery {
    return this.unityDiscovery;
  }

  /**
   * Wait for Unity connection with timeout
   */
  async waitForUnityConnectionWithTimeout(timeoutMs: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`Unity connection timeout after ${timeoutMs}ms`));
      }, timeoutMs);

      const checkConnection = async () => {
        if (this.unityClient.connected) {
          clearTimeout(timeout);
          resolve();
          return;
        }

        // Start Unity discovery if not already running
        this.unityDiscovery.start();

        // Set up callback for when Unity is discovered
        this.unityDiscovery.setOnDiscoveredCallback(async () => {
          // Wait for actual connection establishment
          await this.unityClient.ensureConnected();
          clearTimeout(timeout);
          resolve();
        });
      };

      void checkConnection();
    });
  }

  /**
   * Handle Unity discovery and establish connection
   */
  async handleUnityDiscovered(onConnectionEstablished?: () => Promise<void>): Promise<void> {
    try {
      await this.unityClient.ensureConnected();
      infoToFile('[Unity Connection] Unity connection established');

      // Execute callback if provided
      if (onConnectionEstablished) {
        await onConnectionEstablished();
      }

      // Stop discovery after successful connection
      this.unityDiscovery.stop();
    } catch (error) {
      errorToFile(
        '[Unity Connection] Failed to establish Unity connection after discovery:',
        error,
      );
    }
  }

  /**
   * Initialize connection manager
   */
  async initialize(onConnectionEstablished?: () => Promise<void>): Promise<void> {
    if (this.isInitialized) {
      return;
    }

    this.isInitialized = true;

    // Setup discovery callback
    this.unityDiscovery.setOnDiscoveredCallback(async () => {
      await this.handleUnityDiscovered(onConnectionEstablished);
    });

    // Setup connection lost callback for connection recovery
    this.unityDiscovery.setOnConnectionLostCallback(() => {
      if (this.isDevelopment) {
        debugToFile('[Unity Connection] Connection lost detected - ready for reconnection');
      }
    });

    // Start Unity discovery immediately
    this.unityDiscovery.start();

    if (this.isDevelopment) {
      debugToFile('[Unity Connection] Connection manager initialized');
    }
  }

  /**
   * Setup reconnection callback
   */
  setupReconnectionCallback(callback: () => Promise<void>): void {
    this.unityClient.setReconnectedCallback(async () => {
      // Force Unity discovery for faster reconnection
      await this.unityDiscovery.forceDiscovery();
      await callback();
    });
  }

  /**
   * Check if Unity is connected
   */
  isConnected(): boolean {
    return this.unityClient.connected;
  }

  /**
   * Disconnect from Unity
   */
  disconnect(): void {
    this.unityDiscovery.stop();
    this.unityClient.disconnect();
  }
}
