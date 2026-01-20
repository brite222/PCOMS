using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface ITimeEntryService
    {
        void Create(string developerId, CreateTimeEntryDto dto);
        List<TimeEntryDto> GetForDeveloper(string developerId);
        List<TimeEntryDto> GetAll();
    }
}
