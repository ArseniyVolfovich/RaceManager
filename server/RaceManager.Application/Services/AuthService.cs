using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Application.Security;

namespace RaceManager.Application.Services;

public sealed class AuthService(IUserRepository users)
{

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Введите email/логин и пароль.");
        }

        var user = await users.FindByEmailOrLoginAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !PasswordSecurity.Verify(request.Password, user.Password, out var requiresUpgrade))
        {
            throw new InvalidOperationException("Неверный email/логин или пароль.");
        }

        if (requiresUpgrade)
        {
            user.Password = PasswordSecurity.Hash(request.Password);
            await users.UpdateAsync(user, cancellationToken);
        }

        return new AuthResponse("Вы успешно вошли в аккаунт!", user.ToDto());
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Trim().Length < 4)
        {
            throw new InvalidOperationException("Логин должен содержать минимум 4 символа.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            throw new InvalidOperationException("Введите корректный email.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new InvalidOperationException("Пароль должен содержать минимум 6 символов.");
        }

        var login = request.Login.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone?.Trim() ?? string.Empty;

        PhoneNumberValidator.EnsureValid(phone);

        if (await users.LoginExistsAsync(login, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");
        }

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует.");
        }

        if (await users.PhoneExistsAsync(phone, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с таким номером телефона уже существует.");
        }

        var user = new User
        {
            Id = $"user-{Guid.NewGuid():N}"[..13],
            Login = login,
            Email = email,
            Password = PasswordSecurity.Hash(request.Password),
            Role = "Пользователь",
            Profile = new UserProfile
            {
                LastName = request.LastName?.Trim() ?? string.Empty,
                FirstName = request.FirstName?.Trim() ?? string.Empty,
                MiddleName = request.MiddleName?.Trim() ?? string.Empty,
                Phone = phone,
                BirthDate = request.BirthDate?.Trim() ?? string.Empty,
                Car = request.Car?.Trim() ?? string.Empty
            }
        };

        await users.AddAsync(user, cancellationToken);
        return new AuthResponse("Вы успешно создали аккаунт!", user.ToDto());
    }
}
