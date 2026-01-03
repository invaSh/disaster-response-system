namespace NotificationService.DTOs
{
    namespace NotificationService.DTOs
    {
        public class NotificationDTO
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string Category { get; set; }
            public string Type { get; set; }
            public string Severity { get; set; }
            public string RecipientType { get; set; }
            public string RecipientId { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ReadAt { get; set; }
            public string ReferenceType { get; set; }
            public string ReferenceId { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
        }
    }
}
