using PCOMS.Models;

namespace PCOMS.Application.Interfaces
{
    public interface IProjectAssignmentService
    {
        Task AssignAsync(int projectId, string developerId, string currentUserId);

        void Remove(int projectId, string developerId);

        List<ProjectAssignment> GetAssignmentsForProject(int projectId);

        List<int> GetProjectIdsForDeveloper(string developerId);
    }
}
