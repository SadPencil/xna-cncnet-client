#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// Thread-safe message de-duplicator for LAN lobby messages.
    /// Generates unique message IDs for outgoing messages and tracks received message IDs
    /// to filter out duplicates. Message IDs expire after a configurable timeout to prevent
    /// memory leaks.
    /// </summary>
    internal class LANMessageDeduplicator
    {
        private readonly Random random;
        private readonly object lockObject = new object();
        
        // Track received message IDs with their expiration time
        private readonly ConcurrentDictionary<string, DateTime> receivedMessageIds = new();
        
        // Message ID expiration time in seconds
        private readonly double messageIdExpirationSeconds;
        
        /// <summary>
        /// Initializes a new instance of the LANMessageDeduplicator class.
        /// </summary>
        /// <param name="random">Random number generator for creating message IDs.</param>
        /// <param name="messageIdExpirationSeconds">How long to keep message IDs before expiring them (default 60 seconds).</param>
        public LANMessageDeduplicator(Random random, double messageIdExpirationSeconds = 60.0)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.messageIdExpirationSeconds = messageIdExpirationSeconds;
        }
        
        /// <summary>
        /// Generates a unique random message ID.
        /// Message IDs are 8-character alphanumeric strings.
        /// </summary>
        /// <returns>A unique message ID string.</returns>
        public string GenerateMessageId()
        {
            lock (lockObject)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                char[] id = new char[8];
                
                for (int i = 0; i < id.Length; i++)
                {
                    id[i] = chars[random.Next(chars.Length)];
                }
                
                return new string(id);
            }
        }
        
        /// <summary>
        /// Checks if a message ID has already been received (is a duplicate).
        /// If the message is not a duplicate, it is recorded.
        /// </summary>
        /// <param name="messageId">The message ID to check.</param>
        /// <returns>True if this is a duplicate message, false if it's new.</returns>
        public bool IsDuplicate(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                // If no message ID provided, consider it not a duplicate
                // This maintains backward compatibility with old clients
                return false;
            }
            
            DateTime now = DateTime.UtcNow;
            DateTime expirationTime = now.AddSeconds(messageIdExpirationSeconds);
            
            // Try to add the message ID; if it already exists, it's a duplicate
            bool isDuplicate = !receivedMessageIds.TryAdd(messageId, expirationTime);
            
            return isDuplicate;
        }
        
        /// <summary>
        /// Removes expired message IDs from the tracking dictionary.
        /// This should be called periodically to prevent memory leaks.
        /// </summary>
        public void CleanupExpiredMessageIds()
        {
            DateTime now = DateTime.UtcNow;
            
            // Find all expired message IDs
            var expiredIds = receivedMessageIds
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();
            
            // Remove expired IDs
            foreach (var id in expiredIds)
            {
                receivedMessageIds.TryRemove(id, out _);
            }
        }
        
        /// <summary>
        /// Gets the current count of tracked message IDs.
        /// Useful for monitoring and debugging.
        /// </summary>
        public int TrackedMessageCount
        {
            get { return receivedMessageIds.Count; }
        }
        
        /// <summary>
        /// Clears all tracked message IDs.
        /// </summary>
        public void Clear()
        {
            receivedMessageIds.Clear();
        }
    }
}
