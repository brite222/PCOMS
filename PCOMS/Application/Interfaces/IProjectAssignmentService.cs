using PCOMS.Application.DTOs;
using PCOMS.Models;

namespace PCOMS.Application.Interfaces
{
    public interface IProjectAssignmentService
    {
        void SaveAssignment(AssignDevelopersDto dto);
        List<ProjectAssignment> GetAssignment(int projectId);
        void RemoveAssignment(int projectId, string developerId);
        List<int> GetProjectIdsForDeveloper(string developerId);


    }
}
