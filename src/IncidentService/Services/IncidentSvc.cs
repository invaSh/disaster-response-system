using IncidentService.Persistance;
using IncidentService.Domain;
using Microsoft.EntityFrameworkCore;
using IncidentService.DTOs;
using AutoMapper;
using IncidentService.Application.Incident;
using System.Net;
using IncidentService.Helpers;

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

        public async Task<Incident> GetIncidentById(Guid id)
        {
            var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null)
            {
                throw new StatusException(
                    HttpStatusCode.NotFound,
                    "NotFound",
                    "Incident not found",
                    ""
                );
            }
            return incident;
        }

        public async Task<Incident> CreateIncident(Create.Command createIncident)
        {
            try
            {
                var incident = mapper.Map<Incident>(createIncident);
                incident.IncidentId = HelperService.GenerateIncidentCode();

                _context.Incidents.Add(incident);
                await _context.SaveChangesAsync();

                return incident;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<Incident> UpdateIncidentById(Guid id, Application.Incident.Update.Command updateIncident)
        {
            try
            {
                var incident = await GetIncidentById(id);
                mapper.Map(updateIncident, incident);
                await _context.SaveChangesAsync();
                return incident;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<Incident> DeleteIncident(Guid id)
        {
            try
            {
                var incident = await GetIncidentById(id);
                _context.Incidents.Remove(incident);
                await _context.SaveChangesAsync();
                return incident;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }
    }
}
