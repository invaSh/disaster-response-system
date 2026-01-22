using IncidentService.Persistance;
using IncidentService.Domain;
using IncidentService.Enums;
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
             .Include(i => i.MediaFiles)
             .AsNoTracking()
             .OrderByDescending(i => i.ReportedAt)
             .ToListAsync();
        }

        public async Task<Incident> GetIncidentById(Guid id)
        {
            var incident = await _context.Incidents
                .Include(i => i.MediaFiles)
                .FirstOrDefaultAsync(i => i.Id == id);
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
                incident.Status = Status.Open; // Explicitly set initial status to Open

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

        public async Task<Incident> UpdateIncidentById(Guid id, Application.Incident.Update.Command updateIncident, IS3Service s3Service, CancellationToken cancellationToken = default)
        {
            try
            {
                var incident = await _context.Incidents
                    .Include(i => i.MediaFiles)
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

                if (incident == null)
                {
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Incident not found",
                        ""
                    );
                }

                mapper.Map(updateIncident, incident);
                await _context.SaveChangesAsync(cancellationToken);

                if (updateIncident.MediaFiles != null && updateIncident.MediaFiles.Any())
                {
                    foreach (var file in updateIncident.MediaFiles)
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

        public void ValidateMediaFiles(List<IFormFile> mediaFiles)
        {
            var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            var allowedVideoTypes = new[] { "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo", "video/webm" };
            var allowedTypes = allowedImageTypes.Concat(allowedVideoTypes).ToArray();

            var maxFileSize = 50 * 1024 * 1024; // 50 MB

            foreach (var file in mediaFiles)
            {
                if (file == null || file.Length == 0)
                {
                    throw new StatusException(
                        HttpStatusCode.BadRequest,
                        "ValidationError",
                        "Media file cannot be empty",
                        new Dictionary<string, string[]> { { "MediaFiles", new[] { "One or more media files are empty" } } }
                    );
                }

                if (file.Length > maxFileSize)
                {
                    throw new StatusException(
                        HttpStatusCode.BadRequest,
                        "ValidationError",
                        "File size exceeds maximum allowed size",
                        new Dictionary<string, string[]> { { "MediaFiles", new[] { $"File {file.FileName} exceeds maximum size of 50MB" } } }
                    );
                }

                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    throw new StatusException(
                        HttpStatusCode.BadRequest,
                        "ValidationError",
                        "Invalid file type",
                        new Dictionary<string, string[]> { { "MediaFiles", new[] { $"File {file.FileName} has an unsupported type. Allowed types: images (JPEG, PNG, GIF, WebP) and videos (MP4, MPEG, MOV, AVI, WebM)" } } }
                    );
                }
            }
        }
    }
}
