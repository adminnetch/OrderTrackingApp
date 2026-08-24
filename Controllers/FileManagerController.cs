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

        // Mostra cartelle e file
        [HttpGet]
        [HasPermission("File.FileRead")]
        public IActionResult Index(int projectId, string? folderName)
        {
            var project = _context.CinemaOrders.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return NotFound();

            var rootPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var currentPath = string.IsNullOrEmpty(folderName)
                                  ? rootPath
                                  : Path.Combine(rootPath, folderName);

            if (!Directory.Exists(currentPath))
                return NotFound("Cartella non trovata.");

            ViewBag.Project = project;
            ViewBag.ProjectId = projectId;
            ViewBag.FolderName = folderName ?? "";

            ViewBag.Folders = Directory.GetDirectories(currentPath)
                                       .Select(Path.GetFileName)
                                       .OrderBy(n => n)
                                       .ToList();

            ViewBag.Files = Directory.GetFiles(currentPath)
                                     .Select(Path.GetFileName)
                                     .OrderBy(n => n)
                                     .ToList();

            // Passa l'entità ProjectFile completa, non un anonimo
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
            var filePath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                folderName,
                fileName);

            // 1) File esiste?
            if (!System.IO.File.Exists(filePath))
                return NotFound("File non trovato.");

            // 2) Determina MIME type in base all'estensione
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
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
                // aggiungi qui altri se vuoi...
                _ => "application/octet-stream"
            };

            // 3) Leggi e ritorna
            var bytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(bytes, contentType);
        }

        // Upload multiplo (+ drag&drop)
        [HttpPost]
        [HasPermission("File.FileUpload")]
        public async Task<IActionResult> UploadFile(
            int projectId,
            string? folderName,
            List<IFormFile> files)
        {
            folderName ??= "";
            var basePath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                folderName);

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var user = await _userManager.GetUserAsync(User);

            foreach (var file in files)
            {
                if (file?.Length > 0)
                {
                    var path = Path.Combine(basePath, file.FileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    // salva metadati
                    _context.ProjectFiles.Add(new ProjectFile
                    {
                        ProjectId = projectId,
                        FolderName = folderName,
                        FileName = file.FileName,
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
        public IActionResult DownloadFile(
            int projectId,
            string? folderName,
            string fileName)
        {
            folderName ??= "";
            var filePath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                folderName,
                fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File non trovato.");

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(bytes, "application/octet-stream")
            {
                FileDownloadName = fileName
            };
        }

        // Scarica cartella come ZIP
        [HttpGet]
        [HasPermission("File.Download")]
        public IActionResult DownloadFolderZip(int projectId, string folderName)
        {
            var rootPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var folderPath = Path.Combine(rootPath, folderName);
            if (!Directory.Exists(folderPath))
                return NotFound();

            var tempZip = Path.Combine(Path.GetTempPath(), $"{folderName}.zip");
            if (System.IO.File.Exists(tempZip))
                System.IO.File.Delete(tempZip);

            ZipFile.CreateFromDirectory(folderPath, tempZip);
            var bytes = System.IO.File.ReadAllBytes(tempZip);
            return new FileContentResult(bytes, "application/zip")
            {
                FileDownloadName = $"{folderName}.zip"
            };
        }

        // Scarica più cartelle in ZIP
        [HttpPost]
        [HasPermission("File.Download")]
        public IActionResult DownloadMultipleFolders(
            int projectId,
            string? currentFolder,
            List<string> selectedFolders)
        {
            currentFolder ??= "";
            var rootPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var baseFolder = Path.Combine(rootPath, currentFolder);

            var tempZip = Path.Combine(Path.GetTempPath(), $"cartelle_{Guid.NewGuid()}.zip");
            using var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create);

            foreach (var folder in selectedFolders)
            {
                var fullPath = Path.Combine(baseFolder, folder);
                if (!Directory.Exists(fullPath)) continue;

                foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(rootPath, file);
                    zip.CreateEntryFromFile(file, entryName);
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
        public IActionResult CreateFolder(
            int projectId,
            string? parentFolder,
            string folderName)
        {
            parentFolder ??= "";
            var basePath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                parentFolder);
            var fullPath = Path.Combine(basePath, folderName);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            return RedirectToAction("Index", new { projectId, folderName = parentFolder });
        }

        // Elimina una cartella
        [HttpGet]
        [HasPermission("File.Folder.Delete")]
        public IActionResult DeleteFolder(int projectId, string folderName)
        {
            var rootPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var fullPath = Path.Combine(rootPath, folderName);

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
            var root = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var oldFull = Path.Combine(root, oldPath);
            var parent = Path.GetDirectoryName(oldPath) ?? "";
            var newFull = Path.Combine(root, parent, newName);

            if (!Directory.Exists(oldFull)) return NotFound("Cartella originale non trovata.");
            if (Directory.Exists(newFull)) return Conflict("Cartella con quel nome già esistente.");

            Directory.Move(oldFull, newFull);
            return RedirectToAction("Index", new { projectId, folderName = parent });
        }

        // Rinomina un file
        [HttpGet]
        [HasPermission("File.FileRename")]
        public IActionResult RenameFile(
            int projectId,
            string? folderName,
            string oldFileName,
            string newFileName)
        {
            folderName ??= "";
            var folderPath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                folderName);

            var oldPath = Path.Combine(folderPath, oldFileName);
            var newPath = Path.Combine(folderPath, newFileName);

            if (!System.IO.File.Exists(oldPath)) return NotFound("File non trovato.");
            if (System.IO.File.Exists(newPath)) return Conflict("File con lo stesso nome già esistente.");

            System.IO.File.Move(oldPath, newPath);
            return RedirectToAction("Index", new { projectId, folderName });
        }

        // Elimina un file
        [HttpGet]
        [HasPermission("File.FileDelete")]
        public IActionResult DeleteFile(
            int projectId,
            string? folderName,
            string fileName)
        {
            folderName ??= "";
            var filePath = Path.Combine(
                _storageService.GetRootPath(),
                projectId.ToString(),
                folderName,
                fileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            return RedirectToAction("Index", new { projectId, folderName });
        }

        // Sposta un file
        [HttpPost]
        [HasPermission("File.FileUpload")]
        public IActionResult MoveFile(
            int projectId,
            string? srcFolder,
            string fileName,
            string? destFolder)
        {
            srcFolder ??= "";
            destFolder ??= "";
            var root = Path.Combine(_storageService.GetRootPath(), projectId.ToString());
            var oldPath = Path.Combine(root, srcFolder, fileName);
            var newPath = Path.Combine(root, destFolder, fileName);

            if (!System.IO.File.Exists(oldPath)) return NotFound("File non trovato.");

            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
            System.IO.File.Move(oldPath, newPath);
            return RedirectToAction("Index", new { projectId, folderName = srcFolder });
        }

        [HttpGet]
        [HasPermission("File.FileRead")]
        public IActionResult EditFile(int projectId, string? folderName, string fileName)
        {
            folderName ??= string.Empty;

            // 📂 Path locale (solo per verificare che il file esista)
            var safeFolderFs = (folderName ?? string.Empty)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .Trim(Path.DirectorySeparatorChar);

            var filePath = Path.Combine(_storageService.GetRootPath(), projectId.ToString(), safeFolderFs, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File non trovato.");

            // 📄 Tipo documento
            var ext = Path.GetExtension(fileName).ToLowerInvariant().Trim('.');
            var documentType = ext switch
            {
                "doc" or "docx" => "word",
                "xls" or "xlsx" => "cell",
                "ppt" or "pptx" => "slide",
                _ => "text"
            };

            // 🔑 document.key (≤128; cambia se cambia il file)
            var fi = new FileInfo(filePath);
            var keySource = $"{projectId}/{(folderName ?? string.Empty)}/{fileName}|{fi.LastWriteTimeUtc.Ticks}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var keyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(keySource));
            var documentKey = Convert.ToHexString(keyBytes)[..64]; // stabile e compatto

            // 🔐 JWT base
            var secret = _configuration["JwtSettings:Secret"];
                if (string.IsNullOrEmpty(secret)) throw new Exception("Errore critico: Segreto JWT non configurato in appsettings.json.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var jwtHandler = new JwtSecurityTokenHandler();

            // 🎫 Token per accesso al file (usato nella query) — 15 min
            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object>
        {
            { "projectId", projectId },
            { "folderPath", folderName ?? "" },
            { "fileName", fileName }
        }
            };
            var accessToken = jwtHandler.CreateEncodedJwt(accessTokenDescriptor);

            // ⚠️ NON codificare gli slash delle cartelle
            var folderSegment = string.IsNullOrWhiteSpace(folderName)
                ? ""
                : folderName!.Trim().Trim('/');

            var baseAppUrl = "https://ota.projectcesare.ch"; // se cambi dominio dell’app, cambia qui

            var fileUrl = string.IsNullOrEmpty(folderSegment)
                ? $"{baseAppUrl}/files/{projectId}/{Uri.EscapeDataString(fileName)}?access_token={WebUtility.UrlEncode(accessToken)}"
                : $"{baseAppUrl}/files/{projectId}/{folderSegment}/{Uri.EscapeDataString(fileName)}?access_token={WebUtility.UrlEncode(accessToken)}";

            // 🎫 Token che OnlyOffice metterà in HEADER (onRequestHeaders) — 30 min
            var callbackHeaderTokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object>
        {
            { "projectId", projectId },
            { "folderPath", folderName ?? "" },
            { "fileName", fileName }
        }
            };
            var callbackHeaderToken = jwtHandler.CreateEncodedJwt(callbackHeaderTokenDescriptor);

            var callbackUrl = string.IsNullOrEmpty(folderSegment)
                ? $"{baseAppUrl}/onlyoffice/callback?projectId={projectId}&fileName={Uri.EscapeDataString(fileName)}"
                : $"{baseAppUrl}/onlyoffice/callback?projectId={projectId}&folderName={Uri.EscapeDataString(folderSegment)}&fileName={Uri.EscapeDataString(fileName)}";

            // ⚙️ Config per OnlyOffice

            // === Utente reale per OnlyOffice ===
            // Id stabile per la sessione di co-editing
            string userId =
                User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User?.Identity?.Name
                ?? HttpContext.Session?.Id
                ?? Guid.NewGuid().ToString("N");

            // Recupera utente dal DB
            string displayName = "Utente";
            try
            {
                var login = User?.Identity?.Name;
                var idClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                OrderTrackingApp.Models.User? dbUser = null;

                if (!string.IsNullOrWhiteSpace(idClaim))
                    dbUser = _context.Users.FirstOrDefault(u => u.Id == idClaim);

                if (dbUser == null && !string.IsNullOrWhiteSpace(login))
                    dbUser = _context.Users.FirstOrDefault(u =>
                        u.UserName == login || u.Email == login);

                if (dbUser != null)
                {
                    displayName = !string.IsNullOrWhiteSpace(dbUser.VisualName) ? dbUser.VisualName
                                : !string.IsNullOrWhiteSpace(dbUser.FirstName + " " + dbUser.LastName) ? $"{dbUser.FirstName} {dbUser.LastName}".Trim()
                                : !string.IsNullOrWhiteSpace(dbUser.UserName) ? dbUser.UserName
                                : !string.IsNullOrWhiteSpace(dbUser.Email) ? dbUser.Email.Split('@')[0]
                                : displayName;

                    if (string.IsNullOrWhiteSpace(userId))
                        userId = dbUser.Id;
                }
            }
            catch
            {
                // fallback ai claims se query fallisce
                var claimName = User?.FindFirst("name")?.Value;
                var given = User?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
                var sur = User?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
                var email = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var gnSn = $"{(given ?? "").Trim()} {(sur ?? "").Trim()}".Trim();
                displayName = !string.IsNullOrWhiteSpace(claimName) ? claimName
                            : !string.IsNullOrWhiteSpace(gnSn) ? gnSn
                            : !string.IsNullOrWhiteSpace(email) ? email.Split('@')[0]
                            : (User?.Identity?.Name ?? displayName);
            }

            // …poi dentro la config:
            var config = new
            {
                document = new { fileType = ext, key = documentKey, title = fileName, url = fileUrl },
                documentType = documentType,
                editorConfig = new
                {
                    callbackUrl = callbackUrl,
                    mode = "edit",
                    lang = "it",
                    user = new { id = userId, name = displayName },
                    autosave = true
                },
                events = new
                {
                    onRequestHeaders = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {callbackHeaderToken}" }
        }
                }
            };


            // 🧾 JWT “config token” (se hai JWT attivo sul DocServer)
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

            // 🧪 Log utili
            Console.WriteLine("📄 fileUrl: " + fileUrl);
            Console.WriteLine("🔁 callbackUrl: " + callbackUrl);

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

            // 1) Autorizzazione tramite JWT: query ?access_token=... oppure Header: Authorization: Bearer ...
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
                    if (string.IsNullOrEmpty(secret)) throw new Exception("Errore critico: Segreto JWT non configurato in appsettings.json.");
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

            // 2) Normalizza percorso
            var decoded = Uri.UnescapeDataString(folderPath ?? "");
            var safePath = decoded.Replace('\\', '/').Trim().Trim('/');
            if (safePath.Contains("..")) return Unauthorized("Percorso non valido."); // anti-traversal

            // Route cattura anche il filename nell’ultimo segmento
            var fullPath = Path.Combine(_storageService.GetRootPath(), projectId.ToString(),
                                        safePath.Replace('/', Path.DirectorySeparatorChar));

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
            // Importante: niente Content-Disposition → diamo solo il binario
            return File(bytes, mime);
        }


        // ---------------------- CALLBACK SALVATAGGIO ----------------------
        [AllowAnonymous] // OnlyOffice deve poterlo chiamare senza login
        [HttpPost("/onlyoffice/callback")]
        public async Task<IActionResult> OnlyOfficeCallback(
            [FromQuery] int? projectId,
            [FromQuery] string? folderName,
            [FromQuery] string? fileName)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine("📥 Callback ricevuto");
                Console.WriteLine($"📦 Corpo JSON ricevuto:\n{body}");

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                // ONLYOFFICE manda vari status; 1 = opened
                var status = root.TryGetProperty("status", out var s) ? s.GetInt32() : 0;
                Console.WriteLine($"🔁 Status ricevuto: {status}");
                if (status == 1) return Json(new { error = 0 });

                // 🔐 Verifica header Authorization
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(token)) return Unauthorized("Token assente");

                var secret = _configuration["JwtSettings:Secret"];
                if (string.IsNullOrEmpty(secret)) throw new Exception("Errore critico: Segreto JWT non configurato in appsettings.json.");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = key
                }, out _);
                Console.WriteLine("✅ Token JWT ricevuto valido");

                if ((status == 2 || status == 6) && root.TryGetProperty("url", out var urlProp))
                {
                    var url = urlProp.GetString();
                    Console.WriteLine($"🌐 URL da cui scaricare il file modificato: {url}");

                    if (!string.IsNullOrEmpty(url) && projectId.HasValue && !string.IsNullOrEmpty(fileName))
                    {
                        // Normalizza cartella
                        var folderSegment = Uri.UnescapeDataString(folderName ?? "").Replace('\\', '/').Trim().Trim('/');
                        if (folderSegment.Contains("..")) return Unauthorized("Percorso non valido.");

                        // Scarica file aggiornato
                        using var http = new HttpClient();
                        var fileBytes = await http.GetByteArrayAsync(url);

                        var savePath = string.IsNullOrEmpty(folderSegment)
                            ? Path.Combine(_storageService.GetRootPath(), projectId.Value.ToString(), fileName!)
                            : Path.Combine(_storageService.GetRootPath(), projectId.Value.ToString(),
                                           folderSegment.Replace('/', Path.DirectorySeparatorChar), fileName!);

                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                        await System.IO.File.WriteAllBytesAsync(savePath, fileBytes);
                        Console.WriteLine($"✅ File salvato correttamente in: {savePath}");

                        // Metadati DB (se presenti)
                        var entry = _context.ProjectFiles.FirstOrDefault(f =>
                            f.ProjectId == projectId.Value &&
                            f.FolderName == (folderName ?? "") &&
                            f.FileName == fileName);

                        if (entry != null)
                        {
                            entry.LastModifiedAt = DateTime.UtcNow;
                            entry.LastModifiedBy = "ONLYOFFICE";
                            await _context.SaveChangesAsync();
                            Console.WriteLine("🗂️ Metadati aggiornati nel database.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ Status non gestito ({status}) o url mancante.");
                }

                return Json(new { error = 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OnlyOfficeCallback] ❌ Eccezione: {ex.Message}\n{ex.StackTrace}");
                return Json(new { error = 1 });
            }
        }
    }
}