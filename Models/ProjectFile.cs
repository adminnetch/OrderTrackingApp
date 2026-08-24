public class ProjectFile
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FileName { get; set; } = "";
    public string FolderName { get; set; } = "";
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
