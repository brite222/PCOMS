using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ClientDto> GetAll()
        {
            return _context.Clients
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email
                })
                .ToList();
        }

        public ClientDto? GetById(int id)
        {
            return _context.Clients
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email
                })
                .FirstOrDefault();
        }

        public void Create(CreateClientDto dto)
        {
            var client = new Client
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone
            };

            _context.Clients.Add(client);
            _context.SaveChanges();
        }

        public void Update(EditClientDto dto)
        {
            var client = _context.Clients.FirstOrDefault(c => c.Id == dto.Id);
            if (client == null) return;

            client.Name = dto.Name;
            client.Email = dto.Email;
            client.Phone = dto.Phone;

            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var client = _context.Clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return;

            client.IsDeleted = true;
            _context.SaveChanges();
        }
    }
}
