using PCOMS.Application.DTOs;
using PCOMS.Models;

namespace PCOMS.Application.Interfaces
{
    public interface IProjectService
    {
        List<ProjectDto> GetByClient(int clientId);
        EditProjectDto? GetById(int id);
        void Create(CreateProjectDto dto);
        void Update(EditProjectDto dto);
        List<Project> GetAll();
        List<Project> GetByIds(List<int> ids);

    }
}
