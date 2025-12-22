namespace IncidentService.Domain
{
    public class MediaFile
    {
        public Guid ID { get; set; }
        public Guid IncidentId { get; set; }
        public Incident Incident { get; set; }
        public string URL { get; set; }
        public string MediaType { get; set; }
    }
}
