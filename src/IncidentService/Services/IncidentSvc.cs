using IncidentService.Persistance;
using IncidentService.Domain;
using Microsoft.EntityFrameworkCore;
using IncidentService.DTOs;
using AutoMapper;
using IncidentService.Application.Incident;
using System.Net;
using IncidentService.Helpers;
using Microsoft.AspNetCore.Http;

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

        public async Task<Incident> CreateIncident(Create.Command createIncident, IS3Service s3Service, CancellationToken cancellationToken = default)
        {
            try
            {
                var incident = mapper.Map<Incident>(createIncident);
                incident.IncidentId = HelperService.GenerateIncidentCode();

                _context.Incidents.Add(incident);
                await _context.SaveChangesAsync(cancellationToken);

                if (createIncident.MediaFiles != null && createIncident.MediaFiles.Any())
                {
                    var mediaFiles = new List<MediaFile>();

                    foreach (var file in createIncident.MediaFiles)
                    {
                        using var fileStream = file.OpenReadStream();
                        var s3Url = await s3Service.UploadFileAsync(
                            fileStream,
                            file.FileName,
                            file.ContentType,
                            cancellationToken
                        );

                        var mediaFile = new MediaFile
                        {
                            ID = Guid.NewGuid(),
                            IncidentId = incident.Id,
                            URL = s3Url,
                            MediaType = file.ContentType
                        };

                        mediaFiles.Add(mediaFile);
                        _context.MediaFiles.Add(mediaFile);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                var incidentWithMedia = await _context.Incidents
                    .Include(i => i.MediaFiles)
                    .FirstOrDefaultAsync(i => i.Id == incident.Id, cancellationToken);

                return incidentWithMedia ?? incident;
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
