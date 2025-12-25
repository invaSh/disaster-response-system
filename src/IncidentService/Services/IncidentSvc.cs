using IncidentService.Persistance;
using IncidentService.Domain;
using Microsoft.EntityFrameworkCore;
using IncidentService.DTOs.Incidents;
using AutoMapper;
using IncidentService.Application.Incident;

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

        public async Task<Incident> CreateIncident(Create.Command createIncident)
        {
            var incident = mapper.Map<Incident>(createIncident);
            incident.IncidentId = GenerateIncidentCode();
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

        public async Task<Incident> UpdateIncidentById(Guid id, UpdateIncidentDTO updateIncident)
        {
            var incident = await _context.Incidents
                .FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null) throw new KeyNotFoundException("Incident not found");
            mapper.Map(updateIncident, incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        public async Task<Incident> DeleteIncident(Guid id)
        {
            var incident = await _context.Incidents
                .FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null) throw new KeyNotFoundException("Incident not found");
            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();
            return incident;
        }


        private static string GenerateIncidentCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpper();
            return $"INC-{suffix}";
        }

    }
}
