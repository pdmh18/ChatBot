using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Auth.Commands;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _pwd;
    private readonly IMapper _mapper;

    public RegisterHandler(IUnitOfWork uow, IJwtService jwt, IPasswordService pwd, IMapper mapper)
    {
        _uow = uow; _jwt = jwt; _pwd = pwd; _mapper = mapper;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var existing = await _uow.Repository<User>().FindAsync(u => u.Email == request.Email);
        if (existing.Any()) throw new Exception("Email already registered");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _pwd.Hash(request.Password)
        };

        await _uow.Repository<User>().AddAsync(user);
        await _uow.SaveChangesAsync();

        return new AuthResponse(_jwt.GenerateToken(user), string.Empty, _mapper.Map<UserDto>(user));
    }
}
