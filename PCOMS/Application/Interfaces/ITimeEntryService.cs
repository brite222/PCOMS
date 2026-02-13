using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface ITimeEntryService
    {
        void Create(string developerId, CreateTimeEntryDto dto);
        void Update(int id, string developerId, CreateTimeEntryDto dto);
        void Delete(int id, string developerId);
        List<TimeEntryDto> GetForDeveloper(string developerId);
        List<TimeEntryDto> GetAll();
        List<TimeEntryDto> GetForProject(int projectId);
        TimeEntryDto? GetById(int id);
        void Approve(int id);
        void Reject(int id);
    }
}