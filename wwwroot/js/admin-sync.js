/**
 * AdminSync - Cross-tab synchronization for admin panel
 * Uses BroadcastChannel API with localStorage fallback for older browsers
 */
class AdminSync {
    constructor() {
        this.channelName = 'CODENEX_admin_sync';
        this.listeners = new Map();
        this.useBroadcastChannel = typeof BroadcastChannel !== 'undefined';
        
        if (this.useBroadcastChannel) {
            console.log('🔄 AdminSync: Using BroadcastChannel API');
            this.channel = new BroadcastChannel(this.channelName);
            this.channel.onmessage = (event) => this.handleMessage(event.data);
        } else {
            console.log('🔄 AdminSync: Using localStorage fallback');
            // Fallback to localStorage events for older browsers
            window.addEventListener('storage', (event) => {
                if (event.key === this.channelName && event.newValue) {
                    try {
                        const data = JSON.parse(event.newValue);
                        this.handleMessage(data);
                    } catch (e) {
                        console.error('Failed to parse sync message:', e);
                    }
                }
            });
        }
    }

    /**
     * Broadcast an update event to all other admin tabs
     * @param {string} type - Event type (e.g., 'product_updated', 'solution_updated')
     * @param {object} data - Optional additional data
     */
    broadcastUpdate(type, data = {}) {
        const message = {
            type,
            timestamp: Date.now(),
            ...data
        };

        console.log(`🔄 AdminSync: Broadcasting ${type}`, message);

        if (this.useBroadcastChannel) {
            this.channel.postMessage(message);
        } else {
            // Use localStorage for fallback
            localStorage.setItem(this.channelName, JSON.stringify(message));
            // Remove immediately to ensure it can be set again
            localStorage.removeItem(this.channelName);
        }
        
        // Also trigger site-wide navigation refresh for all tabs/windows
        // This ensures navigation dropdowns update across the entire site
        localStorage.setItem('navigationDataUpdated', Date.now().toString());
        localStorage.removeItem('navigationDataUpdated');
        console.log('🔄 AdminSync: Triggered site-wide navigation refresh');
    }

    /**
     * Handle incoming sync messages
     * @param {object} message - The sync message
     */
    handleMessage(message) {
        console.log(`🔄 AdminSync: Received ${message.type}`, message);
        
        const callbacks = this.listeners.get(message.type);
        if (callbacks) {
            callbacks.forEach(callback => {
                try {
                    callback(message);
                } catch (e) {
                    console.error(`Error in sync listener for ${message.type}:`, e);
                }
            });
        }
    }

    /**
     * Listen for specific update events
     * @param {string} type - Event type to listen for
     * @param {function} callback - Function to call when event occurs
     */
    on(type, callback) {
        if (!this.listeners.has(type)) {
            this.listeners.set(type, []);
        }
        this.listeners.get(type).push(callback);
        console.log(`🔄 AdminSync: Registered listener for ${type}`);
    }

    /**
     * Remove a listener
     * @param {string} type - Event type
     * @param {function} callback - Callback to remove
     */
    off(type, callback) {
        const callbacks = this.listeners.get(type);
        if (callbacks) {
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    /**
     * Clean up resources
     */
    destroy() {
        if (this.useBroadcastChannel && this.channel) {
            this.channel.close();
        }
        this.listeners.clear();
    }
}

// Create singleton instance
window.AdminSync = new AdminSync();
