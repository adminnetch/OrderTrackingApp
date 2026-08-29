using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Services;
using OrderTrackingApp.Filters;
using OrderTrackingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Security.Claims;

namespace OrderTrackingApp.Controllers
{
    public class FileManagerController : Controller
    {
        private readonly ProjectStorageService _storageService;
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public FileManagerController(
            ProjectStorageService storageService,
            AppDbContext context,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _storageService = storageService;
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // ==========================================
        // 🔒 HELPER DI SICUREZZA: Anti Path Traversal
        // ==========================================
        private bool IsPathSafe(string rootPath, string targetPath, out string fullTargetPath)
        {
            fullTargetPath = Path.GetFullPath(targetPath);
            var normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            
            return fullTargetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || 
                   fullTargetPath.Equals(Path.GetFullPath(rootPath), StringComparison.OrdinalIgnoreCase);
        }

        // Mostra cartelle e file
        [HttpGet]
        [HasPermission("File.FileRead")]
        public IActionResult Index(int projectId, string? folderName)
        {
            var project = _context.CinemaOrders.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return NotFound();

            var rootPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var currentPath = string.IsNullOrEmpty(folderName) ? rootPath : Path.Combine(rootPath, folderName);

            if (!Directory.Exists(currentPath))
                return NotFound("Cartella non trovata.");

            ViewBag.Project = project;
            ViewBag.ProjectId = projectId;
            ViewBag.FolderName = folderName ?? "";

            ViewBag.Folders = Directory.GetDirectories(currentPath).Select(Path.GetFileName).OrderBy(n => n).ToList();
            ViewBag.Files = Directory.GetFiles(currentPath).Select(Path.GetFileName).OrderBy(n => n).ToList();

            ViewBag.FileMetadata = _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.FolderName == (folderName ?? ""))
                .ToList();

            return View();
        }

        // Visualizza PDF in pagina
        [HttpGet]
        [HasPermission("File.FileRead")]
        public IActionResult ViewFile(int projectId, string? folderName, string fileName)
        {
            folderName ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName, Path.GetFileName(fileName));

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File non trovato.");

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            string contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return new FileContentResult(bytes, contentType);
        }

        // Upload multiplo (+ drag&drop)
        [HttpPost]
        [HasPermission("File.FileUpload")]
        public async Task<IActionResult> UploadFile(int projectId, string? folderName, List<IFormFile> files)
        {
            folderName ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName);

            if (!IsPathSafe(rootPath, targetPath, out var basePath))
                return Forbid("Percorso non valido.");

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var user = await _userManager.GetUserAsync(User);

            foreach (var file in files)
            {
                if (file?.Length > 0)
                {
                    var safeFileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(basePath, safeFileName);

                    if (!IsPathSafe(rootPath, filePath, out var finalPath))
                        continue;

                    using var stream = new FileStream(finalPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.ProjectFiles.Add(new ProjectFile
                    {
                        ProjectId = projectId,
                        FolderName = folderName,
                        FileName = safeFileName,
                        UploadedAt = DateTime.Now,
                        UploadedBy = user?.VisualName ?? "Sconosciuto"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { projectId, folderName });
        }

        // Scarica singolo file
        [HttpGet]
        [HasPermission("File.Download")]
        public IActionResult DownloadFile(int projectId, string? folderName, string fileName)
        {
            folderName ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName, Path.GetFileName(fileName));

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File non trovato.");

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return new FileContentResult(bytes, "application/octet-stream")
            {
                FileDownloadName = Path.GetFileName(fileName)
            };
        }

        // Scarica cartella come ZIP
        [HttpGet]
        [HasPermission("File.Download")]
        public IActionResult DownloadFolderZip(int projectId, string folderName)
        {
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName);

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (!Directory.Exists(fullPath))
                return NotFound();

            var tempZip = Path.Combine(Path.GetTempPath(), $"{Path.GetFileName(folderName)}.zip");
            if (System.IO.File.Exists(tempZip))
                System.IO.File.Delete(tempZip);

            ZipFile.CreateFromDirectory(fullPath, tempZip);
            var bytes = System.IO.File.ReadAllBytes(tempZip);
            return new FileContentResult(bytes, "application/zip")
            {
                FileDownloadName = $"{Path.GetFileName(folderName)}.zip"
            };
        }

        // Scarica più cartelle in ZIP
        [HttpPost]
        [HasPermission("File.Download")]
        public IActionResult DownloadMultipleFolders(int projectId, string? currentFolder, List<string> selectedFolders)
        {
            currentFolder ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var baseFolder = Path.Combine(rootPath, currentFolder);

            if (!IsPathSafe(rootPath, baseFolder, out var safeBaseFolder))
                return Forbid("Percorso non valido.");

            var tempZip = Path.Combine(Path.GetTempPath(), $"cartelle_{Guid.NewGuid()}.zip");
            using var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create);

            foreach (var folder in selectedFolders)
            {
                var safeFolderName = new string(folder.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                var fullPath = Path.Combine(safeBaseFolder, safeFolderName);

                if (!IsPathSafe(rootPath, fullPath, out var finalFullPath))
                    continue;

                if (!Directory.Exists(finalFullPath)) continue;

                foreach (var file in Directory.GetFiles(finalFullPath, "*", SearchOption.AllDirectories))
                {
                    if (IsPathSafe(rootPath, file, out var safeFile))
                    {
                        var entryName = Path.GetRelativePath(rootPath, safeFile);
                        zip.CreateEntryFromFile(safeFile, entryName);
                    }
                }
            }

            var bytes = System.IO.File.ReadAllBytes(tempZip);
            return new FileContentResult(bytes, "application/zip")
            {
                FileDownloadName = "cartelle_selezionate.zip"
            };
        }

        // Crea una sotto‐cartella
        [HttpPost]
        [HasPermission("File.Folder.Create")]
        public IActionResult CreateFolder(int projectId, string? parentFolder, string folderName)
        {
            parentFolder ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, parentFolder, Path.GetFileName(folderName));

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            return RedirectToAction("Index", new { projectId, folderName = parentFolder });
        }

        // Elimina una cartella
        [HttpGet]
        [HasPermission("File.Folder.Delete")]
        public IActionResult DeleteFolder(int projectId, string folderName)
        {
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName);

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, recursive: true);

            var parent = Path.GetDirectoryName(folderName) ?? "";
            return RedirectToAction("Index", new { projectId, folderName = parent });
        }

        // Rinomina una cartella
        [HttpGet]
        [HasPermission("File.Folder.Rename")]
        public IActionResult RenameFolder(int projectId, string oldPath, string newName)
        {
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var oldFull = Path.Combine(rootPath, oldPath);
            var parent = Path.GetDirectoryName(oldPath) ?? "";
            var newFull = Path.Combine(rootPath, parent, Path.GetFileName(newName));

            if (!IsPathSafe(rootPath, oldFull, out var safeOldFull))
                return Forbid("Percorso originale non valido.");
            if (!IsPathSafe(rootPath, newFull, out var safeNewFull))
                return Forbid("Percorso di destinazione non valido.");

            if (!Directory.Exists(safeOldFull)) return NotFound("Cartella originale non trovata.");
            if (Directory.Exists(safeNewFull)) return Conflict("Cartella con quel nome già esistente.");

            Directory.Move(safeOldFull, safeNewFull);
            return RedirectToAction("Index", new { projectId, folderName = parent });
        }

        // Rinomina un file
        [HttpGet]
        [HasPermission("File.FileRename")]
        public IActionResult RenameFile(int projectId, string? folderName, string oldFileName, string newFileName)
        {
            folderName ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var oldPath = Path.Combine(rootPath, folderName, Path.GetFileName(oldFileName));
            var newPath = Path.Combine(rootPath, folderName, Path.GetFileName(newFileName));

            if (!IsPathSafe(rootPath, oldPath, out var safeOldPath))
                return Forbid("Percorso originale non valido.");
            if (!IsPathSafe(rootPath, newPath, out var safeNewPath))
                return Forbid("Percorso di destinazione non valido.");

            if (!System.IO.File.Exists(safeOldPath)) return NotFound("File non trovato.");
            if (System.IO.File.Exists(safeNewPath)) return Conflict("File con lo stesso nome già esistente.");

            System.IO.File.Move(safeOldPath, safeNewPath);
            return RedirectToAction("Index", new { projectId, folderName });
        }

        // Elimina un file
        [HttpGet]
        [HasPermission("File.FileDelete")]
        public IActionResult DeleteFile(int projectId, string? folderName, string fileName)
        {
            folderName ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, folderName, Path.GetFileName(fileName));

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            return RedirectToAction("Index", new { projectId, folderName });
        }

        // Sposta un file
        [HttpPost]
        [HasPermission("File.FileUpload")]
        public IActionResult MoveFile(int projectId, string? srcFolder, string fileName, string? destFolder)
        {
            srcFolder ??= "";
            destFolder ??= "";
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var oldPath = Path.Combine(rootPath, srcFolder, Path.GetFileName(fileName));
            var newPath = Path.Combine(rootPath, destFolder, Path.GetFileName(fileName));

            if (!IsPathSafe(rootPath, oldPath, out var safeOldPath))
                return Forbid("Percorso di origine non valido.");
            if (!IsPathSafe(rootPath, newPath, out var safeNewPath))
                return Forbid("Percorso di destinazione non valido.");

            if (!System.IO.File.Exists(safeOldPath)) return NotFound("File non trovato.");

            Directory.CreateDirectory(Path.GetDirectoryName(safeNewPath)!);
            System.IO.File.Move(safeOldPath, safeNewPath);
            return RedirectToAction("Index", new { projectId, folderName = srcFolder });
        }

        [HttpGet]
        [HasPermission("File.FileRead")]
        public IActionResult EditFile(int projectId, string? folderName, string fileName)
        {
            folderName ??= string.Empty;
            var safeFolderFs = (folderName ?? string.Empty).Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar);
            
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var filePath = Path.Combine(rootPath, safeFolderFs, fileName);
            
            if (!IsPathSafe(rootPath, filePath, out var fullPath))
                return Forbid("Percorso non valido.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File non trovato.");

            var ext = Path.GetExtension(fileName).ToLowerInvariant().Trim('.');
            var documentType = ext switch
            {
                "doc" or "docx" => "word",
                "xls" or "xlsx" => "cell",
                "ppt" or "pptx" => "slide",
                _ => "text"
            };

            var fi = new FileInfo(fullPath);
            var keySource = $"{projectId}/{(folderName ?? string.Empty)}/{fileName}|{fi.LastWriteTimeUtc.Ticks}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var keyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(keySource));
            var documentKey = Convert.ToHexString(keyBytes)[..64];

            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret) || secret.StartsWith("${")) 
                throw new Exception("Errore critico: Segreto JWT non configurato.");
                
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var jwtHandler = new JwtSecurityTokenHandler();

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object> { { "projectId", projectId }, { "folderPath", folderName ?? "" }, { "fileName", fileName } }
            };
            var accessToken = jwtHandler.CreateEncodedJwt(accessTokenDescriptor);

            var folderSegment = string.IsNullOrWhiteSpace(folderName) ? "" : folderName!.Trim().Trim('/');
            var baseAppUrl = "https://ota.projectcesare.ch"; 

            var fileUrl = string.IsNullOrEmpty(folderSegment)
                ? $"{baseAppUrl}/files/{projectId}/{Uri.EscapeDataString(fileName)}?access_token={WebUtility.UrlEncode(accessToken)}"
                : $"{baseAppUrl}/files/{projectId}/{folderSegment}/{Uri.EscapeDataString(fileName)}?access_token={WebUtility.UrlEncode(accessToken)}";

            var callbackHeaderTokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object> { { "projectId", projectId }, { "folderPath", folderName ?? "" }, { "fileName", fileName } }
            };
            var callbackHeaderToken = jwtHandler.CreateEncodedJwt(callbackHeaderTokenDescriptor);

            var callbackUrl = string.IsNullOrEmpty(folderSegment)
                ? $"{baseAppUrl}/onlyoffice/callback?projectId={projectId}&fileName={Uri.EscapeDataString(fileName)}"
                : $"{baseAppUrl}/onlyoffice/callback?projectId={projectId}&folderName={Uri.EscapeDataString(folderSegment)}&fileName={Uri.EscapeDataString(fileName)}";

            string userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User?.Identity?.Name ?? HttpContext.Session?.Id ?? Guid.NewGuid().ToString("N");
            string displayName = "Utente";
            try
            {
                var login = User?.Identity?.Name;
                var idClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                OrderTrackingApp.Models.User? dbUser = null;

                if (!string.IsNullOrWhiteSpace(idClaim)) dbUser = _context.Users.FirstOrDefault(u => u.Id == idClaim);
                if (dbUser == null && !string.IsNullOrWhiteSpace(login)) dbUser = _context.Users.FirstOrDefault(u => u.UserName == login || u.Email == login);

                if (dbUser != null)
                {
                    displayName = !string.IsNullOrWhiteSpace(dbUser.VisualName) ? dbUser.VisualName
                                : !string.IsNullOrWhiteSpace(dbUser.FirstName + " " + dbUser.LastName) ? $"{dbUser.FirstName} {dbUser.LastName}".Trim()
                                : !string.IsNullOrWhiteSpace(dbUser.UserName) ? dbUser.UserName
                                : !string.IsNullOrWhiteSpace(dbUser.Email) ? dbUser.Email.Split('@')[0]
                                : displayName;
                    if (string.IsNullOrWhiteSpace(userId)) userId = dbUser.Id;
                }
            }
            catch { }

            var config = new
            {
                document = new { fileType = ext, key = documentKey, title = fileName, url = fileUrl },
                documentType = documentType,
                editorConfig = new { callbackUrl = callbackUrl, mode = "edit", lang = "it", user = new { id = userId, name = displayName }, autosave = true },
                events = new { onRequestHeaders = new Dictionary<string, string> { { "Authorization", $"Bearer {callbackHeaderToken}" } } }
            };

            var configTokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object>
                {
                    { "document", JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(config.document)) },
                    { "editorConfig", JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(config.editorConfig)) },
                    { "documentType", config.documentType },
                    { "events", JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(config.events)) }
                }
            };
            var configToken = jwtHandler.CreateEncodedJwt(configTokenDescriptor);

            ViewBag.ConfigJson = JsonSerializer.Serialize(config);
            ViewBag.Token = configToken;
            ViewBag.CallbackToken = callbackHeaderToken;

            return View("EditDocument");
        }

        // ---------------------- SERVE FILE (binario) ----------------------
        [HttpGet("/files/{projectId}/{*folderPath}")]
        [HttpHead("/files/{projectId}/{*folderPath}")]
        public IActionResult ServeFile(int projectId, string folderPath)
        {
            Console.WriteLine("📥 ServeFile chiamato");
            Console.WriteLine($"🔧 projectId: {projectId}, folderPath(raw): {folderPath}");

            var token = Request.Query["access_token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = authHeader.Substring("Bearer ".Length).Trim();
            }
            if (string.IsNullOrEmpty(token)) return Unauthorized("Accesso non autorizzato.");

            try
            {
                var secret = _configuration["JwtSettings:Secret"];
                if (string.IsNullOrEmpty(secret) || secret.StartsWith("${")) 
                    throw new Exception("Errore critico: Segreto JWT non configurato.");
                    
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServeFile] ❌ Token non valido: {ex.Message}");
                return Unauthorized("Token non valido.");
            }

            var decoded = Uri.UnescapeDataString(folderPath ?? "");
            var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.ToString()));
            var targetPath = Path.Combine(rootPath, decoded.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));

            if (!IsPathSafe(rootPath, targetPath, out var fullPath))
                return Unauthorized("Percorso non valido.");

            Console.WriteLine($"📄 Path file calcolato: {fullPath}");
            if (!System.IO.File.Exists(fullPath)) return NotFound("File non trovato.");

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var mime = ext switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            Console.WriteLine("📤 File inviato correttamente");
            return File(bytes, mime);
        }

        // ---------------------- CALLBACK SALVATAGGIO ----------------------
        [AllowAnonymous]
        [HttpPost("/onlyoffice/callback")]
        public async Task<IActionResult> OnlyOfficeCallback([FromQuery] int? projectId, [FromQuery] string? folderName, [FromQuery] string? fileName)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine("📥 Callback ricevuto");

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                var status = root.TryGetProperty("status", out var s) ? s.GetInt32() : 0;
                if (status == 1) return Json(new { error = 0 });

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(token)) return Unauthorized("Token assente");

                var secret = _configuration["JwtSettings:Secret"];
                if (string.IsNullOrEmpty(secret) || secret.StartsWith("${")) 
                    throw new Exception("Errore critico: Segreto JWT non configurato.");
                    
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = key
                }, out _);

                if ((status == 2 || status == 6) && root.TryGetProperty("url", out var urlProp))
                {
                    var url = urlProp.GetString();
                    if (!string.IsNullOrEmpty(url) && projectId.HasValue && !string.IsNullOrEmpty(fileName))
                    {
                        var folderSegment = Uri.UnescapeDataString(folderName ?? "").Replace('\\', '/').Trim().Trim('/');
                        var rootPath = Path.GetFullPath(Path.Combine(_storageService.GetRootPath(), projectId.Value.ToString()));
                        var savePath = string.IsNullOrEmpty(folderSegment)
                            ? Path.Combine(rootPath, fileName!)
                            : Path.Combine(rootPath, folderSegment.Replace('/', Path.DirectorySeparatorChar), fileName!);

                        if (!IsPathSafe(rootPath, savePath, out var finalSavePath))
                            return Unauthorized("Percorso di salvataggio non valido.");

                        using var http = new HttpClient();
                        var fileBytes = await http.GetByteArrayAsync(url);

                        Directory.CreateDirectory(Path.GetDirectoryName(finalSavePath)!);
                        await System.IO.File.WriteAllBytesAsync(finalSavePath, fileBytes);
                        Console.WriteLine($"✅ File salvato correttamente in: {finalSavePath}");

                        var entry = _context.ProjectFiles.FirstOrDefault(f => f.ProjectId == projectId.Value && f.FolderName == (folderName ?? "") && f.FileName == fileName);
                        if (entry != null)
                        {
                            entry.LastModifiedAt = DateTime.UtcNow;
                            entry.LastModifiedBy = "ONLYOFFICE";
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                return Json(new { error = 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OnlyOfficeCallback] ❌ Eccezione: {ex.Message}");
                return Json(new { error = 1 });
            }
        }
    }
}