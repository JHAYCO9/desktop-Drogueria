using System;

namespace DrogueriaMickey.Desktop.Models
{
    public class SyncEvent
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty; // "user_login", "sale", "inventory_movement", etc.
        public Guid EntityId { get; set; }
        public string Operation { get; set; } = string.Empty;  // "create", "update", "delete"
        public string PayloadJson { get; set; } = string.Empty;
        public string Status { get; set; } = "pendiente";      // pendiente, enviado, confirmado, error
        public string? ErrorDetail { get; set; }
        public DateTime ClientCreatedAt { get; set; }
    }
}