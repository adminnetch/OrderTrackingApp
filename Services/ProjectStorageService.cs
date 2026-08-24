using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace OrderTrackingApp.Services
{
    public class ProjectStorageService
    {
        private readonly string _rootPath;
        private readonly string _templateZipPath;

        public ProjectStorageService(IConfiguration configuration)
        {
            _rootPath = configuration["ProjectStorage:RootPath"]
                        ?? throw new Exception("ProjectStorage:RootPath non definito");

            _templateZipPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template_cartella_prjct.zip");

            if (!File.Exists(_templateZipPath))
                throw new FileNotFoundException("ZIP struttura progetto non trovato", _templateZipPath);
        }

        // ✅ CREA STRUTTURA INIZIALE DA TEMPLATE
        public string CreateProjectFolder(int projectId, string projectTitle)
        {
            var targetPath = Path.Combine(_rootPath, projectId.ToString());

            if (Directory.Exists(targetPath))
                throw new IOException($"La cartella per il progetto {projectId} esiste già.");

            ZipFile.ExtractToDirectory(_templateZipPath, targetPath);

            return targetPath;
        }

        // ✅ CREA SOTTOCARTELLA
        public bool CreateSubfolder(int projectId, string folderName)
        {
            var path = Path.Combine(_rootPath, projectId.ToString(), folderName);
            if (Directory.Exists(path)) return false;

            Directory.CreateDirectory(path);
            return true;
        }

        // ✅ ELIMINA SOTTOCARTELLA
        public bool DeleteSubfolder(int projectId, string folderName, bool recursive = false)
        {
            var path = Path.Combine(_rootPath, projectId.ToString(), folderName);
            if (!Directory.Exists(path)) return false;

            Directory.Delete(path, recursive);
            return true;
        }

        // ✅ RINOMINA SOTTOCARTELLA
        public bool RenameSubfolder(int projectId, string oldName, string newName)
        {
            var basePath = Path.Combine(_rootPath, projectId.ToString());
            var oldPath = Path.Combine(basePath, oldName);
            var newPath = Path.Combine(basePath, newName);

            if (!Directory.Exists(oldPath) || Directory.Exists(newPath)) return false;

            Directory.Move(oldPath, newPath);
            return true;
        }

        // ✅ VERIFICA SE UNA CARTELLA ESISTE
        public bool FolderExists(int projectId, string folderName)
        {
            var path = Path.Combine(_rootPath, projectId.ToString(), folderName);
            return Directory.Exists(path);
        }

        // ✅ RESTITUISCE IL PERCORSO ROOT DEL PROGETTO
        public string GetRootPath()
        {
            return _rootPath;
        }

        // (Facoltativo) CREA ZIP da directory
        public string CreateFolderZip(string folderFullPath, string? outputFileName = null)
        {
            if (!Directory.Exists(folderFullPath))
                throw new DirectoryNotFoundException("Cartella da comprimere non trovata.");

            var zipPath = Path.Combine(Path.GetTempPath(), outputFileName ?? $"{Path.GetFileName(folderFullPath)}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(folderFullPath, zipPath);
            return zipPath;
        }
    }
}
