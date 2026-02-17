using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IProjectTemplateService
    {
        // Template CRUD
        Task<ProjectTemplateDto?> CreateTemplateAsync(CreateProjectTemplateDto dto, string userId);
        Task<ProjectTemplateDto?> GetTemplateByIdAsync(int id);
        Task<IEnumerable<ProjectTemplateDto>> GetAllTemplatesAsync(bool includeInactive = false);
        Task<IEnumerable<ProjectTemplateDto>> GetTemplatesByCategoryAsync(string category);
        Task<IEnumerable<ProjectTemplateDto>> GetMyTemplatesAsync(string userId);
        Task<IEnumerable<ProjectTemplateDto>> GetPublicTemplatesAsync();
        Task<bool> UpdateTemplateAsync(UpdateProjectTemplateDto dto, string userId);
        Task<bool> DeleteTemplateAsync(int id);
        Task<bool> ToggleTemplateActiveAsync(int id);

        // Create from existing project
        Task<ProjectTemplateDto?> CreateTemplateFromProjectAsync(int projectId, string name, string category, string userId);

        // Create project from template
        Task<int?> CreateProjectFromTemplateAsync(CreateProjectFromTemplateDto dto, string userId);

        // Categories
        Task<IEnumerable<string>> GetCategoriesAsync();

        // Analytics
        Task<Dictionary<string, int>> GetTemplateUsageStatsAsync();
        Task<IEnumerable<ProjectTemplateDto>> GetMostUsedTemplatesAsync(int count = 5);
    }
}