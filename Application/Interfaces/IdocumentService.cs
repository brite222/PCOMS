using PCOMS.Application.Interfaces.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IDocumentService
    {
        // Create
        Task<DocumentDto?> UploadDocumentAsync(CreateDocumentDto dto, string uploadedBy);
        Task<DocumentDto?> UploadNewVersionAsync(int documentId, IFormFile file, string uploadedBy, string? description = null);

        // Read
        Task<DocumentDto?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<DocumentDto>> GetDocumentsByProjectIdAsync(int projectId);
        Task<IEnumerable<DocumentDto>> GetDocumentsByCategoryAsync(int projectId, string category);
        Task<IEnumerable<DocumentDto>> GetClientVisibleDocumentsAsync(int projectId);
        Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(DocumentFilterDto filter);
        Task<IEnumerable<DocumentVersionDto>> GetDocumentVersionHistoryAsync(int documentId);
        Task<byte[]?> GetDocumentFileAsync(int id);
        Task<string?> GetDocumentFilePathAsync(int id);

        // Update
        Task<bool> UpdateDocumentMetadataAsync(UpdateDocumentDto dto);
        Task<bool> ToggleClientVisibilityAsync(int id);

        // Delete
        Task<bool> DeleteDocumentAsync(int id); // Soft delete
        Task<bool> PermanentlyDeleteDocumentAsync(int id); // Hard delete

        // Statistics
        Task<long> GetTotalStorageSizeAsync(int projectId);
        Task<int> GetDocumentCountAsync(int projectId);
        Task<Dictionary<string, int>> GetDocumentCountByCategoryAsync(int projectId);
    }
}