using CareerConnect.Server.Models;
using CareerConnect.Server.Repositories;

namespace CareerConnect.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToUserDto);
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Utilizatorul cu ID {id} nu a fost găsit");

            return MapToUserDto(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new InvalidOperationException("Email-ul este deja înregistrat");

            var user = new User
            {
                Email = createUserDto.Email,
                Parola = BCrypt.Net.BCrypt.HashPassword(createUserDto.Parola),
                Nume = createUserDto.Nume,
                Prenume = createUserDto.Prenume,
                Telefon = createUserDto.Telefon,
                DataNastere = createUserDto.DataNastere,
                RolId = createUserDto.RolId,
                CreatedAt = DateTime.UtcNow
            };

            user = await _userRepository.CreateAsync(user);
            user = await _userRepository.GetByIdAsync(user.Id);

            return MapToUserDto(user!);
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Utilizatorul cu ID {id} nu a fost găsit");

            if (updateUserDto.Email != null && updateUserDto.Email != user.Email)
            {
                if (await _userRepository.EmailExistsAsync(updateUserDto.Email))
                    throw new InvalidOperationException("Email-ul este deja folosit");
                user.Email = updateUserDto.Email;
            }

            if (updateUserDto.Nume != null) user.Nume = updateUserDto.Nume;
            if (updateUserDto.Prenume != null) user.Prenume = updateUserDto.Prenume;
            if (updateUserDto.Telefon != null) user.Telefon = updateUserDto.Telefon;
            if (updateUserDto.DataNastere.HasValue) user.DataNastere = updateUserDto.DataNastere.Value;
            if (updateUserDto.RolId.HasValue) user.RolId = updateUserDto.RolId.Value;

            user = await _userRepository.UpdateAsync(user);
            user = await _userRepository.GetByIdAsync(user.Id);

            return MapToUserDto(user!);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Nume = user.Nume,
                Prenume = user.Prenume,
                Telefon = user.Telefon,
                DataNastere = user.DataNastere,
                RolNume = user.Rol.Nume,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
