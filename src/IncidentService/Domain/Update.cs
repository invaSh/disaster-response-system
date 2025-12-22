namespace IncidentService.Domain
{
    public class Update
    {
        public Guid ID { get; set; }
        public Guid IncidentId { get; set; }
        public Incident Incident { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
