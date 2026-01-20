using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IClientService
    {
        List<ClientDto> GetAll();
        ClientDto? GetById(int id);
        void Create(CreateClientDto dto);
        void Update(EditClientDto dto);
        void Delete(int id);
    }
}
