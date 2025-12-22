using IncidentService.Persistance;
using IncidentService.Domain;
using Microsoft.EntityFrameworkCore;
using IncidentService.DTOs.Incidents;
using AutoMapper;

namespace IncidentService.Services
{
    public class IncidentSvc
    {
        private readonly IncidentDbContext _context;
        private readonly IMapper mapper;

        public IncidentSvc(IncidentDbContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        public async Task<List<Incident>> GetAllIncidents()
        {
            return await _context.Incidents
             .AsNoTracking()    
             .OrderByDescending(i => i.ReportedAt)
             .ToListAsync();
        }

        public async Task<Incident> CreateIncident(CreateIncidentDto createIncident)
        {
            var incident = mapper.Map<Incident>(createIncident);
            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        public async Task<Incident?> GetIncidentById(Guid id)
        {
            return await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }
    }
}
