using System;

namespace WebSocketService.Sys
{
    public interface IMetadataCollection
    {
        /// <summary>
        /// Returns null if the key is not in the metadata collection.
        /// </summary>
        /// <param name="key">The key to look up or set.</param>
        /// <returns>The metadata associated with the key.</returns>
        string this[string key] { get; set; }
    }
}
