using PCOMS.Models;

namespace PCOMS.Application.Helpers
{
    public static class ProjectStatusRules
    {
        public static bool CanAssignDevelopers(ProjectStatus status)
        {
            return status == ProjectStatus.Active;
        }

        public static bool CanEditRate(ProjectStatus status)
        {
            return status == ProjectStatus.Planned ||
                   status == ProjectStatus.Active;
        }

        public static bool CanEditProject(ProjectStatus status)
        {
            return status != ProjectStatus.Archived;
        }
    }
}
