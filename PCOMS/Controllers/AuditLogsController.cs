using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public IActionResult Index()
        {
            return View(_auditService.GetAll());
        }
    }
}
