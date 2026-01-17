using Microsoft.AspNetCore.Mvc;
using IncidentService.Services;
using IncidentService.DTOs;
using AutoMapper;

namespace IncidentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaGalleryController : ControllerBase
    {
        private readonly IncidentSvc _incidentService;
        private readonly IMapper _mapper;

        public MediaGalleryController(IncidentSvc incidentService, IMapper mapper)
        {
            _incidentService = incidentService;
            _mapper = mapper;
        }

        [HttpGet]
        [Produces("text/html")]
        public async Task<ContentResult> Index()
        {
            var incidents = await _incidentService.GetAllIncidents();
            var incidentDtos = _mapper.Map<List<IncidentDTO>>(incidents);

            var mediaFiles = incidentDtos
                .Where(i => i.MediaFiles != null)
                .SelectMany(i => i.MediaFiles
                    .Where(mf => mf.MediaType != null && 
                        (mf.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                         mf.MediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)))
                    .Select(mf => new ImageMediaFileInfo
                    {
                        IncidentId = i.IncidentId,
                        IncidentTitle = i.Title,
                        MediaFile = mf
                    }))
                .ToList();

            var html = GenerateGalleryHtml(mediaFiles);
            return Content(html, "text/html");
        }

        private string GenerateGalleryHtml(List<ImageMediaFileInfo> mediaFiles)
        {
            var html = $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Swagger UI - Media Gallery</title>
                    <style>
                        * {{
                            box-sizing: border-box;
                        }}
                        
                        body {{
                            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, ""Fira Sans"", ""Droid Sans"", ""Helvetica Neue"", sans-serif;
                            margin: 0;
                            padding: 0;
                            background-color: #fafafa;
                            color: #3b4151;
                            font-size: 14px;
                            line-height: 1.5;
                        }}

                        .topbar {{
                            background-color: #1b1b1b;
                            padding: 8px 30px;
                            display: flex;
                            align-items: center;
                            justify-content: space-between;
                            box-shadow: 0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24);
                        }}
                        
                        .topbar-left {{
                            display: flex;
                            align-items: center;
                            gap: 20px;
                        }}
                        
                        .topbar h1 {{
                            color: #54b13c;
                            font-size: 20px;
                            margin: 0;
                            font-weight: 600;
                            letter-spacing: -0.5px;
                        }}
                        
                        .topbar-nav {{
                            display: flex;
                            gap: 15px;
                            align-items: center;
                        }}
                        
                        .topbar-nav a {{
                            color: #fff;
                            text-decoration: none;
                            font-size: 14px;
                            padding: 5px 10px;
                            border-radius: 3px;
                            transition: background-color 0.2s;
                        }}
                        
                        .topbar-nav a:hover {{
                            background-color: rgba(255,255,255,0.1);
                        }}
                        
                        .topbar-nav a.active {{
                            background-color: rgba(84,177,60,0.2);
                            color: #54b13c;
                        }}

                        .swagger-ui {{
                            max-width: 1460px;
                            margin: 0 auto;
                            padding: 0 20px 40px;
                        }}

                        .info {{
                            margin: 50px 0;
                        }}
                        
                        .info hgroup.main {{
                            margin: 0 0 20px;
                        }}
                        
                        .info hgroup.main a {{
                            font-size: 36px;
                            font-weight: 700;
                            color: #3b4151;
                            text-decoration: none;
                            display: inline-block;
                            margin: 0;
                        }}
                        
                        .info hgroup.main .version {{
                            background: #9b9b9b;
                            color: #fff;
                            padding: 2px 10px;
                            border-radius: 50px;
                            font-size: 12px;
                            vertical-align: middle;
                            margin-left: 10px;
                            font-weight: 400;
                        }}
                        
                        .info .description {{
                            margin: 0;
                            font-size: 14px;
                            color: #3b4151;
                        }}

                        .gallery {{
                            margin-top: 30px;
                        }}

                        .opblock {{
                            margin: 0 0 15px;
                            border: 1px solid #000;
                            border-radius: 4px;
                            background: #fff;
                            box-shadow: 0 0 3px rgba(0,0,0,0.19);
                        }}
                        
                        .opblock.opblock-get {{
                            border-color: #61affe;
                            background: rgba(97,175,254,.1);
                        }}
                        
                        .opblock.opblock-get:hover {{
                            background: rgba(97,175,254,.2);
                        }}
                        
                        .opblock-summary {{
                            display: flex;
                            align-items: center;
                            padding: 15px 20px;
                            cursor: pointer;
                            transition: all 0.2s;
                        }}
                        
                        .opblock-summary-method {{
                            font-size: 14px;
                            font-weight: 700;
                            min-width: 80px;
                            padding: 6px 15px;
                            text-align: center;
                            border-radius: 3px;
                            background: #61affe;
                            color: #fff;
                            text-shadow: 0 1px 0 rgba(0,0,0,0.1);
                            font-family: Titillium Web, sans-serif;
                        }}
                        
                        .opblock-summary-path {{
                            font-size: 16px;
                            font-weight: 600;
                            color: #3b4151;
                            font-family: monospace;
                            word-break: break-all;
                            flex: 1;
                            margin-left: 15px;
                        }}
                        
                        .opblock-summary-description {{
                            font-size: 13px;
                            color: #3b4151;
                            margin-left: 15px;
                            flex: 1;
                        }}
                        
                        .gallery-item-image {{
                            width: 120px;
                            height: 80px;
                            object-fit: cover;
                            border-radius: 4px;
                            border: 1px solid #61affe;
                            margin-left: 15px;
                            background: #fff;
                            flex-shrink: 0;
                        }}
                        
                        .gallery-item-image video {{
                            width: 100%;
                            height: 100%;
                            object-fit: cover;
                        }}
                        
                        .opblock-body {{
                            padding: 20px;
                            border-top: 1px solid #61affe;
                            background: #fff;
                        }}
                        
                        .opblock-description-wrapper {{
                            margin: 0 0 20px;
                        }}
                        
                        .opblock-description {{
                            font-size: 14px;
                            margin: 0;
                            color: #3b4151;
                        }}
                        
                        .parameter__name {{
                            font-weight: 600;
                            color: #3b4151;
                            font-size: 14px;
                        }}
                        
                        .parameter__type {{
                            color: #3b4151;
                            font-family: monospace;
                            font-size: 12px;
                            margin-left: 5px;
                        }}

                        .empty-state {{
                            padding: 60px 20px;
                            text-align: center;
                            border: 1px solid #ebebeb;
                            background: #fff;
                            border-radius: 4px;
                        }}
                        
                        .empty-state h3 {{
                            font-size: 24px;
                            color: #3b4151;
                            margin: 0 0 10px;
                        }}
                        
                        .empty-state p {{
                            font-size: 14px;
                            color: #3b4151;
                            margin: 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""topbar"">
                        <div class=""topbar-left"">
                            <h1><span>swagger</span></h1>
                            <div class=""topbar-nav"">
                                <a href=""/swagger"">API</a>
                                <a href=""/api/mediagallery"" class=""active"">Media Gallery</a>
                            </div>
                        </div>
                    </div>

                    <div class=""swagger-ui"">
                        <div class=""info"">
                            <hgroup class=""main"">
                                <a href=""/api/mediagallery"">Incident Media Gallery</a>
                                <span class=""version"">1.0.0</span>
                            </hgroup>
                            <div class=""description"">
                                Visual documentation for reported incidents. Browse and view all media files associated with incidents.
                                <br><strong>Total media files: {mediaFiles.Count}</strong>
                            </div>
                        </div>";

                            if (mediaFiles.Count == 0)
                            {
                                html += @"
                        <div class=""empty-state"">
                            <h3>No media files found</h3>
                            <p>Upload images when creating or updating incidents to see them displayed here.</p>
                        </div>";
                            }
                            else
                            {
                                html += @"
                        <div class=""gallery"">";

                                foreach (var item in mediaFiles)
                                {
                                    var incidentId = item.IncidentId;
                                    var incidentTitle = item.IncidentTitle;
                                    var mediaFile = item.MediaFile;
                                    var imageUrl = mediaFile.URL;
                                    var mediaType = mediaFile.MediaType ?? "image/unknown";

                                    var isVideo = mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
                                    var mediaTag = isVideo ? "VIDEO" : "IMAGE";
                                    var mediaElement = isVideo 
                                        ? $@"<video class=""gallery-item-image"" controls><source src=""{imageUrl}"" type=""{System.Net.WebUtility.HtmlEncode(mediaType)}"">Your browser does not support the video tag.</video>"
                                        : $@"<img class=""gallery-item-image"" src=""{imageUrl}"" alt=""{incidentTitle}"" onerror=""this.style.display='none'"">";
                                    var clickText = isVideo ? "Click to view full video in new tab" : "Click to view full image in new tab";

                                    html += $@"
                            <div class=""opblock opblock-get"" onclick=""window.open('{imageUrl}', '_blank')"">
                                <div class=""opblock-summary"">
                                    <span class=""opblock-summary-method"">GET</span>
                                    <span class=""opblock-summary-path"">{System.Net.WebUtility.HtmlEncode(incidentTitle)}</span>
                                    {mediaElement}
                                </div>
                                <div class=""opblock-body"">
                                    <div class=""opblock-description-wrapper"">
                                        <div class=""opblock-description"">
                                            <p><span class=""parameter__name"">Incident ID:</span> <span class=""parameter__type"">{incidentId}</span></p>
                                            <p><span class=""parameter__name"">Media Type:</span> <span class=""parameter__type"">{System.Net.WebUtility.HtmlEncode(mediaType)}</span> <span class=""parameter__type"">({mediaTag})</span></p>
                                            <p style=""margin-top: 10px;""><em>{clickText}</em></p>
                                        </div>
                                    </div>
                                </div>
                            </div>";
                                }

                                html += @"
                        </div>";
                            }

                            html += @"
                    </div>
                </body>
                </html>";

            return html;
        }
    }

    public class ImageMediaFileInfo
    {
        public string IncidentId { get; set; }
        public string IncidentTitle { get; set; }
        public MediaFileDTO MediaFile { get; set; }
    }
}
