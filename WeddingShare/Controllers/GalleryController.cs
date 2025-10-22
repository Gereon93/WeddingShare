using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Net;
using WeddingShare.Attributes;
using WeddingShare.Constants;
using WeddingShare.Enums;
using WeddingShare.Extensions;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Notifications;
using WeddingShare.Models;
using WeddingShare.Models.Database;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class GalleryController : BaseController
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ISettingsHelper _settings;
        private readonly IDatabaseHelper _database;
        private readonly IFileHelper _fileHelper;
        private readonly IDeviceDetector _deviceDetector;
        private readonly IImageHelper _imageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly Helpers.IUrlHelper _urlHelper;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        private readonly string ImagesDirectory;
        private readonly string TempDirectory;
        private readonly string UploadsDirectory;
        private readonly string ThumbnailsDirectory;

        public GalleryController(IWebHostEnvironment hostingEnvironment, ISettingsHelper settings, IDatabaseHelper database, IFileHelper fileHelper, IDeviceDetector deviceDetector, IImageHelper imageHelper, INotificationHelper notificationHelper, IEncryptionHelper encryptionHelper, Helpers.IUrlHelper urlHelper, ILogger<GalleryController> logger, IStringLocalizer<Lang.Translations> localizer)
            : base()
        {
            _hostingEnvironment = hostingEnvironment;
            _settings = settings;
            _database = database;
            _fileHelper = fileHelper;
            _deviceDetector = deviceDetector;
            _imageHelper = imageHelper;
            _notificationHelper = notificationHelper;
            _encryptionHelper = encryptionHelper;
            _urlHelper = urlHelper;
            _logger = logger;
            _localizer = localizer;

            ImagesDirectory = Path.Combine(_hostingEnvironment.WebRootPath, Directories.Images);
            TempDirectory = Path.Combine(_hostingEnvironment.WebRootPath, Directories.TempFiles);
            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, Directories.Uploads);
            ThumbnailsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, Directories.Thumbnails);
        }

        [HttpGet]
        public async Task<IActionResult> CaptureIdentity(int? galleryId, string? identifier, string? key = null, string? returnUrl = null)
        {
            try
            {
                var gallery = galleryId.HasValue ? await _database.GetGallery(galleryId.Value) : null;

                if (gallery == null && !string.IsNullOrWhiteSpace(identifier))
                {
                    var gId = await _database.GetGalleryId(identifier);
                    gallery = gId.HasValue ? await _database.GetGallery(gId.Value) : null;
                }

                if (gallery == null)
                {
                    return new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.InvalidGalleryId }, false);
                }

                ViewBag.GalleryIdentifier = gallery.Identifier;
                ViewBag.GalleryId = gallery.Id;
                ViewBag.SecretKey = key;
                ViewBag.ReturnUrl = returnUrl ?? $"/Gallery?identifier={gallery.Identifier}";
                ViewBag.RequireEmail = await _settings.GetOrDefault(Settings.IdentityCheck.RequireEmail, false, gallery.Id);

                // Pre-fill name if already in session
                ViewBag.ExistingName = HttpContext.Session.GetString(SessionKey.ViewerIdentity) ?? string.Empty;
                ViewBag.ExistingEmail = HttpContext.Session.GetString(SessionKey.ViewerEmailAddress) ?? string.Empty;

                return View("~/Views/Gallery/CaptureIdentity.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load identity capture page - {ex?.Message}");
                return new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.InvalidGalleryId }, false);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(string? id, string? identifier, string? key = null)
        {
            int? galleryId = 0;

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                galleryId = await _database.GetGalleryId(identifier);
            }
            else if (!string.IsNullOrWhiteSpace(id))
            {
                galleryId = await _database.GetGalleryIdByName(id);
            }

            GalleryModel? gallery = await _database.GetGallery(galleryId.Value);
            if (gallery == null)
            {
                if (await _settings.GetOrDefault(Settings.Basic.GuestGalleryCreation, false))
                { 
                    if (await _database.GetGalleryCount() < await _settings.GetOrDefault(Settings.Basic.MaxGalleryCount, 1000000))
                    {
                        gallery = await _database.AddGallery(new GalleryModel()
                        {
                            Name = id?.ToLower() ?? GalleryHelper.GenerateGalleryIdentifier(),
                            SecretKey = key,
                            Owner = 0
                        });
                    }
                    else
                    {
                        return new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.GalleryLimitReached }, false);
                    }
                }
                else
                {
                    return new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.GalleryCreationNotAllowed }, false);
                }
            }

            var append = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("identifier", gallery.Identifier)
            };

            if (!string.IsNullOrWhiteSpace(key))
            {
                var enc = _encryptionHelper.IsEncryptionEnabled();
                append.Add(new KeyValuePair<string, string>("key", enc ? _encryptionHelper.Encrypt(key) : key));
                append.Add(new KeyValuePair<string, string>("enc", enc.ToString().ToLower()));
            }

            var redirectUrl = _urlHelper.GenerateFullUrl(HttpContext.Request, "/Gallery", append);

            return new JsonResult(new { success = true, redirectUrl });
        }

        [HttpGet]
        [RequiresSecretKey]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(string? id, string? identifier, string? key = null, ViewMode? mode = null, GalleryGroup? group = null, GalleryOrder? order = null, GalleryFilter? filter = null, string? culture = null, bool partial = false)
        {
            int? galleryId = null;

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                galleryId = await _database.GetGalleryId(identifier);
            }
            else if (!string.IsNullOrWhiteSpace(id))
            {
                galleryId = await _database.GetGalleryIdByName(id);
            }

            if (galleryId != null)
            {
                // Check if user has provided their name (required for first-time visitors)
                var viewerIdentity = HttpContext.Session.GetString(SessionKey.ViewerIdentity);
                var requireNameEntry = await _settings.GetOrDefault(Settings.IdentityCheck.RequireIdentityForUpload, false, galleryId);

                if (requireNameEntry && string.IsNullOrWhiteSpace(viewerIdentity) && !partial)
                {
                    // Redirect to name capture page
                    return RedirectToAction("CaptureIdentity", new { galleryId, identifier, key, returnUrl = Request.Path + Request.QueryString });
                }

                if (!string.IsNullOrWhiteSpace(culture))
                {
                    try
                    {
                        HttpContext.Session.SetString(SessionKey.SelectedLanguage, culture);
                        Response.Cookies.Append(
                            CookieRequestCultureProvider.DefaultCookieName,
                            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                        );
                    }
                    catch { }
                }

                try
                {
                    ViewBag.ViewMode = mode ?? (ViewMode)await _settings.GetOrDefault(Settings.Gallery.DefaultView, (int)ViewMode.Default, galleryId);
                }
                catch
                {
                    ViewBag.ViewMode = ViewMode.Default;
                }

                var deviceType = HttpContext.Session.GetString(SessionKey.DeviceType);
                if (string.IsNullOrWhiteSpace(deviceType))
                {
                    deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                    HttpContext.Session.SetString(SessionKey.DeviceType, deviceType ?? "Desktop");
                }

                ViewBag.IsMobile = !string.Equals("Desktop", deviceType, StringComparison.OrdinalIgnoreCase);

                GalleryModel? gallery = await _database.GetGallery(galleryId.Value);
                if (gallery != null)
                {
                    var galleryPath = Path.Combine(UploadsDirectory, gallery.Identifier);
                    _fileHelper.CreateDirectoryIfNotExists(galleryPath);
                    _fileHelper.CreateDirectoryIfNotExists(Path.Combine(galleryPath, "Pending"));

                    ViewBag.GalleryIdentifier = gallery.Identifier;

                    var secretKey = await _settings.GetOrDefault(Settings.Gallery.SecretKey, string.Empty, gallery.Id);
                    ViewBag.SecretKey = secretKey;

                    var currentPage = 1;
                    try
                    {
                        currentPage = int.Parse((Request.Query.ContainsKey("page") && !string.IsNullOrWhiteSpace(Request.Query["page"])) ? Request.Query["page"].ToString().ToLower() : "1");
                    }
                    catch { }

                    var galleryGroup = group ?? (GalleryGroup)(await _settings.GetOrDefault(Settings.Gallery.DefaultGroup, (int)GalleryGroup.None, gallery?.Id));
                    var galleryOrder = order ?? (GalleryOrder)(await _settings.GetOrDefault(Settings.Gallery.DefaultOrder, (int)GalleryOrder.Descending, gallery?.Id));
                    var galleryFilter = filter ?? (GalleryFilter)(await _settings.GetOrDefault(Settings.Gallery.DefaultFilter, (int)GalleryFilter.All, gallery?.Id));

                    var mediaType = MediaType.All;
                    if (mode == ViewMode.Slideshow)
                    {
                        mediaType = MediaType.Image;
                    }
                    else
                    {
                        switch (galleryFilter)
                        {
                            case GalleryFilter.Images:
                                mediaType = MediaType.Image;
                                break;
                            case GalleryFilter.Videos:
                                mediaType = MediaType.Video;
                                break;
                            default:
                                mediaType = MediaType.All;
                                break;
                        }
                    }

                    var orientation = ImageOrientation.None;
                    switch (galleryFilter)
                    {
                        case GalleryFilter.Landscape:
                            orientation = ImageOrientation.Landscape;
                            break;
                        case GalleryFilter.Portrait:
                            orientation = ImageOrientation.Portrait;
                            break;
                        case GalleryFilter.Square:
                            orientation = ImageOrientation.Square;
                            break;
                        default:
                            orientation = ImageOrientation.None;
                            break;
                    }

                    var itemsPerPage = await _settings.GetOrDefault(Settings.Gallery.ItemsPerPage, 50, gallery?.Id);
                    var allowedFileTypes = (await _settings.GetOrDefault(Settings.Gallery.AllowedFileTypes, ".jpg,.jpeg,.png,.mp4,.mov", gallery?.Id)).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var items = (await _database.GetAllGalleryItems(gallery?.Id, GalleryItemState.Approved, mediaType, orientation, galleryGroup, galleryOrder, itemsPerPage, currentPage))?.Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x.Title).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));

                    var userPermissions = User?.Identity?.GetUserPermissions() ?? AccessPermissions.None;
                    var isGalleryAdmin = User?.Identity != null && User.Identity.IsAuthenticated && userPermissions.HasFlag(AccessPermissions.Gallery_Upload);

                    var uploadActvated = !gallery.Identifier.Equals("All", StringComparison.OrdinalIgnoreCase) && (await _settings.GetOrDefault(Settings.Gallery.Upload, true, gallery?.Id) || isGalleryAdmin);
                    if (uploadActvated)
                    {
                        try
                        {
                            var periods = (await _settings.GetOrDefault(Settings.Gallery.UploadPeriod, "1970-01-01 00:00", gallery?.Id))?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (periods != null)
                            {
                                uploadActvated = false;

                                var now = DateTime.UtcNow;
                                foreach (var period in periods)
                                {
                                    var timeRanges = period?.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                    if (timeRanges != null && timeRanges.Length > 0)
                                    {
                                        var startDate = DateTime.Parse(timeRanges[0]).ToUniversalTime();

                                        if (timeRanges.Length == 2)
                                        {
                                            var endDate = DateTime.Parse(timeRanges[1]).ToUniversalTime();
                                            if (now >= startDate && now < endDate)
                                            {
                                                uploadActvated = true;
                                                break;
                                            }
                                        }
                                        else if (timeRanges.Length == 1 && now >= startDate)
                                        {
                                            uploadActvated = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            uploadActvated = true;
                        }
                    }

                    var itemCounts = await _database.GetGalleryItemCount(gallery?.Id, GalleryItemState.All, mediaType, orientation);
                    var galleryIdentifiers = !gallery.Identifier.Equals("All", StringComparison.OrdinalIgnoreCase) ? new Dictionary<int, string>() { { gallery.Id, gallery.Identifier } } : items?.GroupBy(x => x.Id)?.Select(x => new KeyValuePair<int, string>(x.Key, _database.GetGalleryIdentifier(x.Key).Result))?.ToDictionary();
                    var model = new PhotoGallery()
                    {
                        Gallery = gallery,
                        SecretKey = secretKey,
                        Images = items?.Select(x => {
                            var galleryIdentifier = galleryIdentifiers != null && galleryIdentifiers.ContainsKey(x.GalleryId) ? galleryIdentifiers[x.GalleryId] : gallery.Identifier;
                            return new PhotoGalleryImage()
                            {
                                Id = x.Id,
                                GalleryId = x.GalleryId,
                                Name = Path.GetFileName(x.Title),
                                UploadedBy = x.UploadedBy,
                                UploaderEmailAddress = x.UploaderEmailAddress,
                                UploadDate = x.UploadedDate,
                                ImagePath = $"/{Path.Combine(UploadsDirectory, galleryIdentifier).Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}/{x.Title}",
                                ThumbnailPath = $"/{Path.Combine(ThumbnailsDirectory, galleryIdentifier).Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}/{Path.GetFileNameWithoutExtension(x.Title)}.webp",
                                ThumbnailPathFallback = $"/{ThumbnailsDirectory.Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}/{Path.GetFileNameWithoutExtension(x.Title)}.webp",
                                MediaType = x.MediaType
                            };
                        })?.ToList(),
                        CurrentPage = currentPage,
                        ApprovedCount = (int)itemCounts["Approved"],
                        PendingCount = (int)itemCounts["Pending"],
                        ItemsPerPage = itemsPerPage,
                        UploadActivated = uploadActvated,
                        ViewMode = (ViewMode)ViewBag.ViewMode,
                        GroupBy = galleryGroup,
                        OrderBy = galleryOrder,
                        Pagination = galleryOrder != GalleryOrder.Random,
                        LoadScripts = !partial
                    };

                    return partial ? PartialView("~/Views/Gallery/GalleryWrapper.cshtml", model) : View(model);
                }
            }

            return new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.InvalidGalleryId }, false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // SECURITY FIX: CSRF protection
        public async Task<IActionResult> UploadImage()
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;

            try
            {
                if (!int.TryParse((Request?.Form?.FirstOrDefault(x => string.Equals("Id", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString()?.ToLower() ?? string.Empty, out var galleryId))
                {
                    return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Gallery_Id"].Value } });
                }

                var gallery = await _database.GetGallery(galleryId);
                if (gallery != null)
                {
                    var secretKey = await _settings.GetOrDefault(Settings.Gallery.SecretKey, string.Empty, gallery.Id);
                    string key = (Request?.Form?.FirstOrDefault(x => string.Equals("SecretKey", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(secretKey) && !string.Equals(secretKey, key))
                    {
                        return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Secret_Key_Warning"].Value } });
                    }

                    // SECURITY FIX: Input length validation
                    string uploadedBy = HttpContext.Session.GetString(SessionKey.ViewerIdentity)?.Trim() ?? "Anonymous";
                    if (uploadedBy.Length > 255)
                    {
                        uploadedBy = uploadedBy.Substring(0, 255);
                    }

                    string uploaderEmail = HttpContext.Session.GetString(SessionKey.ViewerEmailAddress)?.Trim() ?? "Anonymous";
                    if (uploaderEmail.Length > 255)
                    {
                        uploaderEmail = uploaderEmail.Substring(0, 255);
                    }

                    // SECURITY FIX: Validate device UUID format
                    string? deviceUuid = HttpContext.Session.GetString(SessionKey.DeviceUuid);
                    if (!string.IsNullOrWhiteSpace(deviceUuid) && !Guid.TryParse(deviceUuid, out _))
                    {
                        return Json(new { success = false, uploaded = 0, errors = new List<string>() { "Invalid session" } });
                    }

                    // Check if user is a photographer FOR THIS SPECIFIC GALLERY (SECURITY FIX)
                    var userLevel = HttpContext.User?.Identity?.GetUserLevel() ?? UserLevel.Basic;
                    var userId = HttpContext.User?.Identity?.GetUserId() ?? -1;
                    var isPhotographer = false;

                    if (userLevel == UserLevel.Photographer && userId > 0)
                    {
                        // SECURITY FIX: Verify photographer is assigned to THIS specific gallery
                        isPhotographer = await _database.IsPhotographerForGallery(gallery.Id, userId);
                    }

                    var files = Request?.Form?.Files;
                    if (files != null && files.Count > 0)
                    {
                        // Photographers don't need review ONLY if authorized for this gallery
                        var requiresReview = isPhotographer ? false : await _settings.GetOrDefault(Settings.Gallery.RequireReview, true, gallery.Id);

                        var uploaded = 0;
                        var errors = new List<string>();
                        foreach (IFormFile file in files)
                        {
                            try
                            {
                                var extension = Path.GetExtension(file.FileName);
                                var maxGallerySize = await _settings.GetOrDefault(Settings.Gallery.MaxSizeMB, 1024L, gallery.Id) * 1000000;
                                var maxFilesSize = await _settings.GetOrDefault(Settings.Gallery.MaxFileSizeMB, 50L, gallery.Id) * 1000000;
                                var galleryPath = Path.Combine(UploadsDirectory, gallery.Identifier);

                                var allowedFileTypes = (await _settings.GetOrDefault(Settings.Gallery.AllowedFileTypes, ".jpg,.jpeg,.png,.mp4,.mov", gallery.Id)).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                if (!allowedFileTypes.Any(x => string.Equals(x.Trim('.'), extension.Trim('.'), StringComparison.OrdinalIgnoreCase)))
                                {
                                    errors.Add($"{_localizer["File_Upload_Failed"].Value}. {_localizer["Invalid_File_Type"].Value}");
                                }
                                else if (file.Length > maxFilesSize)
                                {
                                    errors.Add($"{_localizer["File_Upload_Failed"].Value}. {_localizer["Max_File_Size"].Value} {maxFilesSize} bytes");
                                }
                                else if ((_fileHelper.GetDirectorySize(galleryPath) + file.Length) > maxGallerySize)
                                {
                                    errors.Add($"{_localizer["File_Upload_Failed"].Value}. {_localizer["Gallery_Full"].Value} {maxGallerySize} bytes");
                                }
                                else
                                {
                                    var fileName = _fileHelper.SanitizeFilename($"{(!string.IsNullOrWhiteSpace(uploadedBy) ? $"{uploadedBy.Replace(" ", "_")}-" : string.Empty)}{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                                    galleryPath = requiresReview ? Path.Combine(galleryPath, "Pending") : galleryPath;
                                    
                                    _fileHelper.CreateDirectoryIfNotExists(galleryPath);

                                    var filePath = Path.Combine(galleryPath, fileName);
                                    if (!string.IsNullOrWhiteSpace(filePath))
                                    {
                                        var isDemoMode = await _settings.GetOrDefault(Settings.IsDemoMode, false);
                                        if (!isDemoMode)
                                        {
                                            await _fileHelper.SaveFile(file, filePath, FileMode.Create);
                                        }
                                        else
                                        {
                                            System.IO.File.Copy(Path.Combine(ImagesDirectory, $"DemoImage.png"), filePath, true);
                                        }

                                        var checksum = await _fileHelper.GetChecksum(filePath);
                                        if (await _settings.GetOrDefault(Settings.Gallery.PreventDuplicates, true, gallery.Id) && (string.IsNullOrWhiteSpace(checksum) || await _database.GetGalleryItemByChecksum(gallery.Id, checksum) != null))
                                        {
                                            errors.Add($"{_localizer["File_Upload_Failed"].Value}. {_localizer["Duplicate_Item_Detected"].Value}");
                                            _fileHelper.DeleteFileIfExists(filePath);
                                        }
                                        else
                                        {
                                            var gallerySavePath = Path.Combine(ThumbnailsDirectory, gallery.Identifier);

                                            _fileHelper.CreateDirectoryIfNotExists(ThumbnailsDirectory);
                                            _fileHelper.CreateDirectoryIfNotExists(gallerySavePath);

                                            var savePath = Path.Combine(gallerySavePath, $"{Path.GetFileNameWithoutExtension(filePath)}.webp");
                                            await _imageHelper.GenerateThumbnail(filePath, savePath, await _settings.GetOrDefault(Settings.Basic.ThumbnailSize, 720));
                                            
                                            var item = await _database.AddGalleryItem(new GalleryItemModel()
                                            {
                                                GalleryId = gallery.Id,
                                                Title = fileName,
                                                UploadedBy = uploadedBy,
                                                UploaderEmailAddress = uploaderEmail,
                                                UploadedDate = await _fileHelper.GetCreationDatetime(filePath),
                                                Checksum = checksum,
                                                MediaType = _imageHelper.GetMediaType(filePath),
                                                Orientation = await _imageHelper.GetOrientation(savePath),
                                                State = requiresReview ? GalleryItemState.Pending : GalleryItemState.Approved,
                                                FileSize = file.Length,
                                                DeviceUuid = deviceUuid,
                                                IsOfficial = isPhotographer,  // Photographer uploads are marked as official
                                            });

                                            if (item?.Id > 0)
                                            { 
                                                uploaded++;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"{_localizer["Save_To_Gallery_Failed"].Value} - {ex?.Message}");
                            }
                        }

						Response.StatusCode = (int)HttpStatusCode.OK;

						return Json(new { success = uploaded > 0, uploaded, uploadedBy, requiresReview, errors });
                    }
                    else
                    {
                        return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["No_Files_For_Upload"].Value } });
                    }
                }
                else
                {
                    return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Gallery_Does_Not_Exist"].Value } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Image_Upload_Failed"].Value} - {ex?.Message}");
            }

            return Json(new { success = false, uploaded = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> UploadCompleted()
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;

            try
            {
                if (!int.TryParse((Request?.Form?.FirstOrDefault(x => string.Equals("Id", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString()?.ToLower() ?? string.Empty, out var galleryId))
                {
                    return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Gallery_Id"].Value } });
                }

                var gallery = await _database.GetGallery(galleryId);
                if (gallery != null)
                {
                    var secretKey = await _settings.GetOrDefault(Settings.Gallery.SecretKey, string.Empty, galleryId);
                    string key = (Request?.Form?.FirstOrDefault(x => string.Equals("SecretKey", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(secretKey) && !string.Equals(secretKey, key))
                    {
                        return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Secret_Key_Warning"].Value } });
                    }

                    var uploadedBy = HttpContext.Session.GetString(SessionKey.ViewerIdentity) ?? "Anonymous";
                    var requiresReview = await _settings.GetOrDefault(Settings.Gallery.RequireReview, true, galleryId);

                    int uploaded = int.Parse((Request?.Form?.FirstOrDefault(x => string.Equals("Count", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString() ?? "0");
                    if (uploaded > 0 && requiresReview && await _settings.GetOrDefault(Notifications.Alerts.PendingReview, true))
                    {
                        await _notificationHelper.Send(_localizer["New_Items_Pending_Review"].Value, $"{uploaded} new item(s) have been uploaded to gallery '{gallery.Name}' by '{(!string.IsNullOrWhiteSpace(uploadedBy) ? uploadedBy : "Anonymous")}' and are awaiting your review.", _urlHelper.GenerateBaseUrl(HttpContext?.Request, "/Account"));
                    }

                    Response.StatusCode = (int)HttpStatusCode.OK;

                    return Json(new { success = true, counters = new { total = gallery?.TotalItems ?? 0, approved = gallery?.ApprovedItems ?? 0, pending = gallery?.PendingItems ?? 0 }, uploaded, uploadedBy, requiresReview });
                }
                else
                {
                    return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Gallery_Does_Not_Exist"].Value } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Image_Upload_Failed"].Value} - {ex?.Message}");
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> DownloadGallery(int id, string? secretKey, string? group)
        {
            try
            {
                var gallery = await _database.GetGallery(id);
                if (gallery != null)
                {
                    secretKey = secretKey ?? string.Empty;

                    var gallerySecret = await _settings.GetOrDefault(Settings.Gallery.SecretKey, string.Empty, gallery.Id);
                    if (!secretKey.Equals(gallerySecret))
                    {
                        return Json(new { success = false, message = _localizer["Failed_Download_Gallery_Invalid_Key"].Value });
                    }

                    if (await _settings.GetOrDefault(Settings.Gallery.Download, true, gallery?.Id) || (User?.Identity != null && User.Identity.IsAuthenticated))
                    {
                        var galleryDir = id > 0 ? Path.Combine(UploadsDirectory, gallery.Identifier) : UploadsDirectory;
                        if (_fileHelper.DirectoryExists(galleryDir))
                        {
                            var fileFilter = new List<string>();
                            if (!string.IsNullOrWhiteSpace(group))
                            {
                                var groupParts = group.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                if (groupParts != null && groupParts.Length == 2)
                                {
                                    var items = await _database.GetAllGalleryItems(id, GalleryItemState.Approved);
                                    foreach (GalleryGroup type in Enum.GetValues(typeof(GalleryGroup)))
                                    {
                                        if (((int)type).ToString().Equals(groupParts[0]))
                                        {
                                            try
                                            {
                                                IEnumerable<IGrouping<string, GalleryItemModel>>? filtered = null;
                                                switch (type)
                                                {
                                                    case GalleryGroup.Date:
                                                        filtered = items?.GroupBy(x => x.UploadedDate != null ? x.UploadedDate.Value.ToString("dddd, d MMMM yyyy") : "Unknown");
                                                        break;
                                                    case GalleryGroup.MediaType:
                                                        filtered = items?.GroupBy(x => x.MediaType.ToString());
                                                        break;
                                                    case GalleryGroup.Uploader:
                                                        filtered = items?.GroupBy(x => x.UploadedBy ?? "Anonymous");
                                                        break;
                                                }

                                                if (filtered != null)
                                                {
                                                    foreach (var f in filtered)
                                                    {
                                                        if (f.Key.Equals(groupParts[1]))
                                                        {
                                                            if (f.Any())
                                                            {
                                                                fileFilter.AddRange(f.Select(x => x.Title));
                                                            }

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }

                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    return Json(new { success = false, message = _localizer["Failed_Download_Gallery"].Value });
                                }
                            }

                            var archieveName = $"{gallery?.Identifier ?? "WeddingShare"}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.zip";

                            var listing = new List<ZipListing>();

                            if (User?.Identity == null || !User.Identity.IsAuthenticated)
                            {
                                // Unauthenticated users only get approved photos
                                var files = Directory.GetFiles(galleryDir, "*", SearchOption.TopDirectoryOnly);
                                if (fileFilter != null && fileFilter.Any())
                                {
                                    files = files.Where(x => fileFilter.Exists(y => Path.GetFileName(y).Equals(Path.GetFileName(x), StringComparison.OrdinalIgnoreCase))).ToArray();
                                }

                                if (files != null && files.Any())
                                {
                                    listing.Add(new ZipListing(galleryDir, files));
                                }
                            }
                            else
                            {
                                // SECURITY FIX: Check if user has Review permissions for pending/rejected photos
                                var userPermissions = User.Identity.GetUserPermissions();
                                var hasReviewPermission = userPermissions.HasFlag(AccessPermissions.Review_View);

                                var scanners = new List<ZipListingScanner>();

                                // All authenticated users can download approved photos
                                scanners.Add(new ZipListingScanner("Approved", galleryDir, SearchOption.TopDirectoryOnly));

                                // SECURITY FIX: Only users with Review permissions can download pending/rejected
                                if (hasReviewPermission)
                                {
                                    scanners.Add(new ZipListingScanner("Pending", Path.Combine(galleryDir, "Pending"), SearchOption.AllDirectories));
                                    scanners.Add(new ZipListingScanner("Rejected", Path.Combine(galleryDir, "Rejected"), SearchOption.AllDirectories));
                                }

                                foreach (var scanner in scanners)
                                {
                                    try
                                    {
                                        var files = Directory.GetFiles(scanner.Path, "*", scanner.SearchOption);
                                        if (fileFilter != null && fileFilter.Any())
                                        {
                                            files = files.Where(x => fileFilter.Exists(y => Path.GetFileName(y).Equals(Path.GetFileName(x), StringComparison.OrdinalIgnoreCase))).ToArray();
                                        }

                                        if (files != null && files.Any())
                                        {
                                            listing.Add(new ZipListing(scanner.Path, files, scanner.Name));
                                        }
                                    }
                                    catch { }
                                }
                            }
                                
                            return await ZipFileResponse(archieveName, listing);
                        }
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, message = _localizer["Download_Gallery_Not_Allowed"].Value });
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = false, message = _localizer["Failed_Download_Gallery"].Value });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogError(ex, $"{_localizer["Failed_Download_Gallery"].Value} - {ex?.Message}");
            }

            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyUploads(int galleryId)
        {
            try
            {
                // SECURITY FIX: Validate gallery ID
                if (galleryId <= 0)
                {
                    return Json(new { success = false, message = "Invalid gallery ID" });
                }

                // SECURITY FIX: Validate device UUID format
                var deviceUuid = HttpContext.Session.GetString(SessionKey.DeviceUuid);
                if (string.IsNullOrWhiteSpace(deviceUuid) || !Guid.TryParse(deviceUuid, out _))
                {
                    return Json(new { success = false, message = "Invalid session" });
                }

                var items = await _database.GetGalleryItemsByDeviceUuid(galleryId, deviceUuid);
                var gallery = await _database.GetGallery(galleryId);

                if (gallery == null)
                {
                    return Json(new { success = false, message = "Gallery not found" });
                }

                var result = items.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    uploadedBy = x.UploadedBy,
                    uploadedDate = x.UploadedDate,
                    state = x.State.ToString(),
                    imagePath = $"/{Path.Combine(UploadsDirectory, gallery.Identifier).Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}/{x.Title}",
                    thumbnailPath = $"/{Path.Combine(ThumbnailsDirectory, gallery.Identifier).Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}/{Path.GetFileNameWithoutExtension(x.Title)}.webp"
                });

                return Json(new { success = true, items = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get user uploads - {ex?.Message}");
                return Json(new { success = false, message = "Internal error" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // SECURITY FIX: CSRF protection
        public async Task<IActionResult> DeleteMyUpload(int itemId)
        {
            try
            {
                // SECURITY FIX: Validate item ID
                if (itemId <= 0)
                {
                    return Json(new { success = false, message = "Invalid item ID" });
                }

                // SECURITY FIX: Validate device UUID format
                var deviceUuid = HttpContext.Session.GetString(SessionKey.DeviceUuid);
                if (string.IsNullOrWhiteSpace(deviceUuid) || !Guid.TryParse(deviceUuid, out _))
                {
                    return Json(new { success = false, message = "Invalid session" });
                }

                // Verify ownership before deleting
                var item = await _database.GetGalleryItem(itemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                if (item.DeviceUuid != deviceUuid)
                {
                    return Json(new { success = false, message = "Unauthorized: You can only delete your own uploads" });
                }

                var gallery = await _database.GetGallery(item.GalleryId);
                if (gallery == null)
                {
                    return Json(new { success = false, message = "Gallery not found" });
                }

                var success = await _database.DeleteGalleryItemByDeviceUuid(itemId, deviceUuid);

                if (success)
                {
                    // Delete physical files
                    var uploadPath = Path.Combine(UploadsDirectory, gallery.Identifier, item.Title);
                    var pendingPath = Path.Combine(UploadsDirectory, gallery.Identifier, "Pending", item.Title);
                    var thumbnailPath = Path.Combine(ThumbnailsDirectory, gallery.Identifier, $"{Path.GetFileNameWithoutExtension(item.Title)}.webp");

                    _fileHelper.DeleteFileIfExists(uploadPath);
                    _fileHelper.DeleteFileIfExists(pendingPath);
                    _fileHelper.DeleteFileIfExists(thumbnailPath);
                }

                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete user upload - {ex?.Message}");
                return Json(new { success = false, message = "Internal error" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // SECURITY FIX: CSRF protection
        public async Task<IActionResult> ToggleLike(int itemId)
        {
            try
            {
                // SECURITY FIX: Validate item ID
                if (itemId <= 0)
                {
                    return Json(new { success = false, message = "Invalid item ID" });
                }

                // SECURITY FIX: Validate device UUID format
                var deviceUuid = HttpContext.Session.GetString(SessionKey.DeviceUuid);
                if (string.IsNullOrWhiteSpace(deviceUuid) || !Guid.TryParse(deviceUuid, out _))
                {
                    return Json(new { success = false, message = "Invalid session" });
                }

                var success = await _database.ToggleLike(itemId, deviceUuid);
                var likeCount = await _database.GetLikeCount(itemId);
                var isLiked = await _database.HasUserLiked(itemId, deviceUuid);

                return Json(new { success, likeCount, isLiked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to toggle like - {ex?.Message}");
                return Json(new { success = false, message = "Internal error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLikeInfo(int itemId)
        {
            try
            {
                // SECURITY FIX: Validate item ID
                if (itemId <= 0)
                {
                    return Json(new { success = false, message = "Invalid item ID" });
                }

                // SECURITY FIX: Validate device UUID format
                var deviceUuid = HttpContext.Session.GetString(SessionKey.DeviceUuid);
                var likeCount = await _database.GetLikeCount(itemId);
                var isLiked = !string.IsNullOrWhiteSpace(deviceUuid) && Guid.TryParse(deviceUuid, out _) && await _database.HasUserLiked(itemId, deviceUuid);

                return Json(new { success = true, likeCount, isLiked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get like info - {ex?.Message}");
                return Json(new { success = false, message = "Internal error" });
            }
        }
    }
}